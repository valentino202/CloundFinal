using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Services.CloudSave;
using ReactionDomino.Online;

/// <summary>
/// Arnes de PRUEBAS del modo online. NO es el juego: es una herramienta para ejecutar y mirar, desde el
/// editor, cada mensaje entre el cliente y el servidor (ver docs/PRUEBAS.md, P6-P12). Cuando exista la
/// pantalla de partida online (Tarea 5), este componente se puede borrar.
///
/// Como usarlo:
///   1. Abre SampleScene, crea un GameObject vacio y agregale este componente.
///   2. Dale Play e inicia sesion con la interfaz normal (invitado o usuario/contrasena).
///   3. En el Inspector, clic derecho sobre el nombre del componente: aparecen las acciones numeradas.
///   4. Mira la Console: cada accion imprime lo que respondio el servidor.
/// Para probar con DOS jugadores usa una segunda instancia (otro editor o un build) con OTRA cuenta.
/// </summary>
public class OnlineDebugHarness : MonoBehaviour
{
    [Header("Partida")]
    [Tooltip("Codigo compartido entre los dos jugadores (4 a 12 letras o numeros).")]
    public string matchCode = "PRUEBA1";
    [Tooltip("Se rellena solo tras CreateMatch (es 'rd-' + el codigo).")]
    public string matchId = "";

    [Header("Jugada")]
    public int x = 2;
    public int y = 2;

    private OnlineMatchService service;

    private void Awake()
    {
        // Se crea en el hilo principal: asi los avisos (push/sondeo) llegan al hilo principal.
        service = new OnlineMatchService();
        service.MatchUpdated += OnMatchUpdated;
    }

    private void OnDestroy()
    {
        if (service == null) return;
        service.MatchUpdated -= OnMatchUpdated;
        _ = service.StopListeningAsync();
    }

    // Lo que hay que hacer al recibir un aviso: pedir el estado de nuevo. El aviso no trae datos.
    private async void OnMatchUpdated(string id)
    {
        Debug.Log($"[Online] AVISO recibido para la partida '{id}' -> pido el estado al servidor.");
        if (!string.IsNullOrEmpty(id)) await Show(service.GetMatchStateAsync(id));
    }

    [ContextMenu("1 - Suscribirme al push")]
    private async void Subscribe()
    {
        await Safe("Suscribirse", async () =>
        {
            await service.StartListeningAsync();
            Debug.Log("[Online] Suscrito a los mensajes push de Cloud Code.");
        });
    }

    [ContextMenu("2 - GetGameConfig")]
    private async void GetConfig()
    {
        await Safe("GetGameConfig", async () =>
        {
            var c = await service.GetGameConfigAsync();
            Debug.Log($"[Online] Config: protocolo={c.protocolVersion} tablero={c.gridSize} nivelMax={c.maxCellLevel} turnosMax={c.maxTurns}");
        });
    }

    [ContextMenu("3 - CreateMatch (crear o unirme con el codigo)")]
    private async void CreateMatch()
    {
        await Safe("CreateMatch", async () =>
        {
            var s = await service.CreateMatchAsync(matchCode);
            matchId = s.matchId;
            service.StartPolling(matchId); // sondeo de respaldo cada ~10 s
            Print(s);
        });
    }

    [ContextMenu("4 - PlayMove (x, y)")]
    private async void PlayMove()
    {
        await Safe("PlayMove", async () =>
        {
            var r = await service.PlayMoveAsync(matchId, x, y);
            Debug.Log($"[Online] PlayMove({x},{y}) -> outcome='{r.outcome}'" + (r.Accepted ? "" : "  (rechazada, el estado no cambio)"));
            Print(r.state);
        });
    }

    [ContextMenu("5 - GetMatchState")]
    private async void GetState()
    {
        await Safe("GetMatchState", async () => Print(await service.GetMatchStateAsync(matchId)));
    }

    [ContextMenu("6 - Prueba de trampa: escribir onlineProfile desde el cliente (P12)")]
    private async void TryCheat()
    {
        await Safe("Trampa", async () =>
        {
            // Esto guarda en la clase DEFAULT de Cloud Save. El servidor guarda 'onlineProfile' en PROTECTED,
            // que es OTRO dato distinto. Mira el Dashboard: el item Protected no debe cambiar.
            await CloudSaveService.Instance.Data.Player.SaveAsync(
                new Dictionary<string, object> { { "onlineProfile", "trampa" } });
            Debug.Log("[Online] Escribi 'onlineProfile' en DEFAULT desde el cliente. Revisa en el Dashboard que el item PROTECTED no cambio.");
        });
    }

    // ---- ayudas ----

    private async System.Threading.Tasks.Task Show(System.Threading.Tasks.Task<MatchStateDto> task)
        => await Safe("GetMatchState", async () => Print(await task));

    private async System.Threading.Tasks.Task Safe(string name, System.Func<System.Threading.Tasks.Task> action)
    {
        try { await action(); }
        catch (System.Exception ex) { Debug.LogError($"[Online] {name} fallo: {ex.Message}"); }
    }

    private static void Print(MatchStateDto s)
    {
        if (s == null) { Debug.Log("[Online] (sin estado)"); return; }

        var sb = new StringBuilder();
        sb.AppendLine($"[Online] Partida {s.matchId}: fase={s.phase}  tuAsiento={s.mySeat}  turnoDe={s.currentSeat}  jugadas={s.totalMoves}  ganador={s.winnerSeat}");
        if (s.owner != null && s.owner.Length == s.gridSize * s.gridSize)
        {
            for (int row = 0; row < s.gridSize; row++)
            {
                for (int col = 0; col < s.gridSize; col++)
                {
                    int o = s.OwnerAt(col, row);
                    sb.Append(o == 0 ? " . " : $" {(o == 1 ? 'R' : 'N')}{s.LevelAt(col, row)}");
                }
                sb.AppendLine();
            }
            sb.AppendLine("(R = rosado/asiento 1, N = naranja/asiento 2; el numero es el nivel)");
        }
        if (s.lastMoveExplosions != null && s.lastMoveExplosions.Length > 0)
            sb.AppendLine($"Explosiones de la ultima jugada: {s.lastMoveExplosions.Length}");
        if (s.phase == "finished")
            sb.AppendLine($"Premio recibido: {s.awardedCoins} monedas, {s.awardedGems} gemas");
        Debug.Log(sb.ToString());
    }
}
