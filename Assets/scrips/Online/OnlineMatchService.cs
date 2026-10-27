using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.Subscriptions;

namespace ReactionDomino.Online
{
    /// <summary>
    /// Cliente del modo online. Es la parte del juego que habla con el modulo de Cloud Code
    /// (CloudCode~/ReactionDominoModule). Clase pura (no MonoBehaviour), igual que UGSCloudCodeManager.
    ///
    /// La idea central: el cliente ENVIA INTENCIONES ("pongo un punto en (x, y)") y PINTA lo que el
    /// servidor responde. Nunca decide quien gano ni cuanto premio recibe.
    ///
    /// Como se entera de la jugada del rival (esto es "el sistema de mensajes" del curso):
    ///   1. PUSH: el servidor envia al rival un mensaje {type, matchId} cuando guarda una jugada.
    ///   2. El mensaje NO lleva el estado: solo es una senal. Al recibirla, tu vuelves a pedir el
    ///      estado con GetMatchStateAsync. Asi nadie puede forjar un mensaje para ganar algo.
    ///   3. SONDEO de respaldo (cada ~10 s) por si un push se pierde.
    /// Las dos senales terminan en el MISMO evento <see cref="MatchUpdated"/>.
    ///
    /// Uso tipico:
    ///   var service = new OnlineMatchService();          // crealo en el hilo principal
    ///   service.MatchUpdated += async id => { var s = await service.GetMatchStateAsync(id); Pintar(s); };
    ///   await service.StartListeningAsync();             // push
    ///   var state = await service.CreateMatchAsync("PRUEBA1");
    ///   service.StartPolling(state.matchId);             // respaldo
    ///   var r = await service.PlayMoveAsync(state.matchId, 2, 2);
    /// </summary>
    public class OnlineMatchService
    {
        /// <summary>Gemelo de GameProtocol.Version del servidor. Se sube en AMBOS lados a la vez.</summary>
        public const int ProtocolVersion = 1;

        /// <summary>Nombre del modulo en Cloud Code (el del .csproj / .ccmr).</summary>
        public const string ModuleName = "ReactionDominoModule";

        /// <summary>Tipo del mensaje push que envia el servidor.</summary>
        public const string MatchUpdatedMessageType = "match_updated";

        public const float DefaultPollSeconds = 10f;

        /// <summary>
        /// Se dispara con el id de la partida cuando llega un push o toca el sondeo. Siempre en el hilo
        /// principal de Unity (se capturo el SynchronizationContext al crear el servicio), asi que es
        /// seguro tocar GameObjects. Lo que hay que hacer al recibirlo: pedir el estado de nuevo.
        /// </summary>
        public event Action<string> MatchUpdated;

        private readonly SynchronizationContext unityContext;
        private ISubscriptionEvents subscription;
        private CancellationTokenSource pollCts;

        /// <summary>Crealo en el hilo principal (Awake, Start...), no desde otro hilo.</summary>
        public OnlineMatchService()
        {
            unityContext = SynchronizationContext.Current;
        }

        // ------------------------------------------------------------------------------------
        // Llamadas al servidor
        // ------------------------------------------------------------------------------------

        /// <summary>Pide la configuracion al servidor y comprueba que habla la misma version del protocolo.</summary>
        public async Task<GameConfigDto> GetGameConfigAsync()
        {
            var config = await Call<GameConfigDto>("GetGameConfig", new Dictionary<string, object>());
            if (config.protocolVersion != ProtocolVersion)
                throw new InvalidOperationException(
                    $"Version de protocolo distinta: servidor={config.protocolVersion}, cliente={ProtocolVersion}. Actualiza el juego o el modulo.");
            return config;
        }

        /// <summary>
        /// Crea la partida o se une a ella con un codigo. Es idempotente: si ya eres jugador, devuelve la
        /// misma partida. El asiento (1 o 2) lo decide el servidor, no tu.
        /// </summary>
        public Task<MatchStateDto> CreateMatchAsync(string matchCode)
            => Call<MatchStateDto>("CreateMatch", new Dictionary<string, object> { { "matchCode", matchCode } });

        /// <summary>
        /// Envia la intencion de jugar en (x, y). Ojo: NO envias quien eres ni de que jugador es la jugada:
        /// el servidor lo saca de tu sesion. Revisa <see cref="PlayMoveResponseDto.outcome"/>.
        /// </summary>
        public Task<PlayMoveResponseDto> PlayMoveAsync(string matchId, int x, int y)
            => Call<PlayMoveResponseDto>("PlayMove", new Dictionary<string, object>
            {
                { "matchId", matchId }, { "x", x }, { "y", y },
            });

        public Task<MatchStateDto> GetMatchStateAsync(string matchId)
            => Call<MatchStateDto>("GetMatchState", new Dictionary<string, object> { { "matchId", matchId } });

        private static async Task<T> Call<T>(string function, Dictionary<string, object> args)
        {
            if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
                throw new InvalidOperationException("Hace falta iniciar sesion antes de usar el modo online.");

            return await CloudCodeService.Instance.CallModuleEndpointAsync<T>(ModuleName, function, args);
        }

        // ------------------------------------------------------------------------------------
        // Avisos: push + sondeo de respaldo
        // ------------------------------------------------------------------------------------

        /// <summary>Se suscribe a los mensajes push de Cloud Code (Wire). Llamalo una vez, tras iniciar sesion.</summary>
        public async Task StartListeningAsync()
        {
            if (subscription != null) return;

            var callbacks = new SubscriptionEventCallbacks();
            callbacks.MessageReceived += OnPushMessage;
            callbacks.ConnectionStateChanged += state => Debug.Log($"[Online] Push: conexion {state}");
            callbacks.Kicked += () => Debug.LogWarning("[Online] Push: el servidor cerro la suscripcion.");
            callbacks.Error += e => Debug.LogWarning($"[Online] Push: error {e}");

            subscription = await CloudCodeService.Instance.SubscribeToPlayerMessagesAsync(callbacks);
        }

        public async Task StopListeningAsync()
        {
            StopPolling();
            if (subscription == null) return;
            try { await subscription.UnsubscribeAsync(); }
            finally { subscription = null; }
        }

        private void OnPushMessage(IMessageReceivedEvent e)
        {
            if (e == null || e.MessageType != MatchUpdatedMessageType) return;

            // El cuerpo es {"type":"match_updated","matchId":"..."}. Solo se usa el matchId como senal.
            string matchId = null;
            try { matchId = JsonUtility.FromJson<PushPayload>(e.Message)?.matchId; }
            catch (Exception ex) { Debug.LogWarning($"[Online] Push ilegible: {ex.Message}"); }

            Raise(matchId);
        }

        [Serializable]
        private class PushPayload
        {
            public string type;
            public string matchId;
        }

        /// <summary>
        /// Sondeo de respaldo: cada <paramref name="seconds"/> segundos dispara <see cref="MatchUpdated"/>
        /// aunque no haya llegado ningun push. Es solo una red de seguridad, no el mecanismo principal.
        /// </summary>
        public void StartPolling(string matchId, float seconds = DefaultPollSeconds)
        {
            StopPolling();
            pollCts = new CancellationTokenSource();
            _ = PollLoop(matchId, seconds, pollCts.Token);
        }

        public void StopPolling()
        {
            pollCts?.Cancel();
            pollCts?.Dispose();
            pollCts = null;
        }

        private async Task PollLoop(string matchId, float seconds, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, seconds)), token);
                    Raise(matchId);
                }
            }
            catch (TaskCanceledException) { /* se detuvo el sondeo: normal */ }
        }

        private void Raise(string matchId)
        {
            if (MatchUpdated == null) return;
            if (unityContext != null) unityContext.Post(_ => MatchUpdated?.Invoke(matchId), null);
            else MatchUpdated.Invoke(matchId);
        }
    }
}
