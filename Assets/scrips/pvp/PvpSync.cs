using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ESQUELETO — Paso 5: sincronizacion de la partida por red.
/// Solo se envia la JUGADA (x, y, playerId), no el tablero entero. Cada
/// cliente aplica GameLogic.PlacePoint en su copia y obtiene el mismo
/// resultado (la logica es determinista, validado en Paso 3).
///
/// ESTADO: esqueleto (no implementado). Se ensambla con el profesor.
/// Paquete necesario (no instalado aun): com.unity.netcode.gameobjects
/// </summary>
public class PvpSync
{
    /// <summary>Se dispara cuando llega una jugada del rival.</summary>
    public event Action<int, int, int> OnJugadaRecibida; // x, y, playerId

    /// <summary>Envia una jugada al otro cliente. (ESQUELETO)</summary>
    public void EnviarJugada(int x, int y, int playerId)
    {
        // TODO (con profesor): usar Netcode RPC, ej.:
        //   if (IsHost) JugadaRpc(x, y, playerId);   // ServerRpc
        // o Relay + transporte.
        Debug.Log($"[PvP-Sync-ESQUELETO] EnviarJugada({x},{y}, jugador {playerId}) — no implementado.");
    }

    /// <summary>Aplica una jugada remota en la copia local del tablero.
    /// El HOST valida que sea legal antes de difundirla. (ESQUELETO)</summary>
    public bool AplicarJugadaRemota(GameLogic game, int x, int y, int playerId)
    {
        // TODO (con profesor): si soy host, validar con game.IsValidMove;
        // si es legal, difundir por red y aplicar aqui.
        var res = game.PlacePoint(x, y, playerId);
        if (!res.success)
        {
            Debug.LogWarning($"[PvP-Sync-ESQUELETO] Jugada remota ilegal: {res.error}");
            return false;
        }
        OnJugadaRecibida?.Invoke(x, y, playerId);
        return true;
    }

    /// <summary>Recibe una jugada de la red y la aplica. (ESQUELETO)</summary>
    public void RecibirJugadaDeRed(int x, int y, int playerId, GameLogic game)
    {
        // TODO (con profesor): llamar desde el RPC handler.
        AplicarJugadaRemota(game, x, y, playerId);
    }
}
