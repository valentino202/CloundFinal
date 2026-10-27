using System.Text.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace ReactionDomino.CloudCode
{
    /// <summary>Perfil online de un jugador: lo que decide el servidor.</summary>
    public class OnlineProfile
    {
        public long Coins { get; set; }
        public long Gems { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int Matches { get; set; }

        /// <summary>Ultimas partidas ya acreditadas. Evita pagar dos veces la misma partida aunque se reintente.</summary>
        public List<string> SettledMatchIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// Lee y escribe el <see cref="OnlineProfile"/> en Cloud Save, en los datos del jugador.
    ///
    /// ACCESS CLASS: PROTECTED. Esta es la decision mas importante de este archivo. El jugador puede
    /// LEER su saldo (para mostrarlo) pero NO escribirlo. Si estas claves estuvieran en Default (lo
    /// que pasa con todo lo que el cliente guarda con <c>Data.Player.SaveAsync</c>), cualquiera
    /// podria escribirlas con su propio token por la API REST de Cloud Save sin pasar por el juego,
    /// y todos los controles del servidor serian decoracion.
    ///
    /// La pregunta que hay que hacerse para cada dato nuevo no es "¿el cliente necesita leerlo?"
    /// sino "¿que pasa si el cliente lo escribe?".
    ///
    /// Se usa un item con otro nombre ("onlineProfile") y NO se reutilizan "Coins"/"Gems"/"TotalWins"
    /// de <c>UGSProfileManager</c>, que siguen en Default: una misma clave en otra clase de acceso es
    /// otro dato distinto, y mezclarlas confunde. Migrar el cliente a leer este item es la Tarea 5.
    ///
    /// Las dos llamadas usan <c>context.ServiceToken</c> (permiso elevado). Es lo que permite que UNA
    /// sola llamada de PlayMove acredite a los DOS jugadores, no solo al que llama.
    ///
    /// Limitacion conocida: en los datos de jugador no hay transacciones, asi que dos partidas del
    /// mismo jugador que terminen en el mismo instante podrian pisarse (leer-modificar-escribir).
    /// </summary>
    public static class PlayerRewardStore
    {
        /// <summary>Access class Protected.</summary>
        private const string ProfileItemKey = "onlineProfile";
        private const int MaxRememberedMatches = 50;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public static async Task<OnlineProfile> GetAsync(IExecutionContext context, IGameApiClient gameApiClient, string playerId)
        {
            var result = await gameApiClient.CloudSaveData.GetProtectedItemsAsync(
                context, context.ServiceToken, context.ProjectId, playerId, new List<string> { ProfileItemKey });
            var item = result.Data.Results.FirstOrDefault(i => i.Key == ProfileItemKey);
            if (item == null) return new OnlineProfile();

            string json = item.Value is JsonElement element ? element.GetRawText() : item.Value?.ToString() ?? string.Empty;
            return string.IsNullOrEmpty(json) ? new OnlineProfile() : JsonSerializer.Deserialize<OnlineProfile>(json, JsonOptions) ?? new OnlineProfile();
        }

        public static Task SetAsync(IExecutionContext context, IGameApiClient gameApiClient, string playerId, OnlineProfile profile)
        {
            return gameApiClient.CloudSaveData.SetProtectedItemAsync(
                context, context.ServiceToken, context.ProjectId, playerId, new SetItemBody(ProfileItemKey, profile));
        }

        /// <summary>
        /// Acredita el premio de una partida a un jugador SI esa partida no se acredito antes.
        /// Devuelve false (y no toca nada) si <paramref name="matchId"/> ya estaba acreditada.
        /// </summary>
        public static async Task<bool> CreditOnceAsync(
            IExecutionContext context, IGameApiClient gameApiClient, string playerId, string matchId,
            int coins, int gems, int outcome /* 1 gana, 0 empata, -1 pierde */)
        {
            var profile = await GetAsync(context, gameApiClient, playerId);
            if (profile.SettledMatchIds.Contains(matchId)) return false;

            profile.Coins += coins;
            profile.Gems += gems;
            profile.Matches++;
            if (outcome > 0) profile.Wins++;
            else if (outcome == 0) profile.Draws++;
            else profile.Losses++;

            profile.SettledMatchIds.Add(matchId);
            if (profile.SettledMatchIds.Count > MaxRememberedMatches)
                profile.SettledMatchIds.RemoveRange(0, profile.SettledMatchIds.Count - MaxRememberedMatches);

            await SetAsync(context, gameApiClient, playerId, profile);
            return true;
        }
    }
}
