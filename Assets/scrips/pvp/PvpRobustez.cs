using System;
using UnityEngine;

/// <summary>
/// ESQUELETO — Paso 9: robustez (reconexion, abandono, timeout, anti-trampas).
///
/// ESTADO: esqueleto (no implementado). Se ensambla con el profesor.
/// </summary>
public class PvpRobustez
{
    /// <summary>Se dispara si se pierde la conexion.</summary>
    public event Action OnDesconexion;
    /// <summary>Se dispara si el rival abandona.</summary>
    public event Action OnRivalAbandono;

    /// <summary>Intenta reconectar tras una caida de red. (ESQUELETO)</summary>
    public async System.Threading.Tasks.Task Reconectar()
    {
        // TODO (con profesor): reintentar conexion al Relay/Lobby, re-sincronizar estado.
        Debug.Log("[PvP-Robustez-ESQUELETO] Reconectar — no implementado.");
        await System.Threading.Tasks.Task.CompletedTask;
        OnDesconexion?.Invoke();
    }

    /// <summary>El jugador abandona la partida (rendicion). (ESQUELETO)</summary>
    public void Abandonar()
    {
        // TODO (con profesor): notificar al rival, reportar resultado (loser = este jugador).
        Debug.Log("[PvP-Robustez-ESQUELETO] Abandonar — no implementado.");
        OnRivalAbandono?.Invoke();
    }

    /// <summary>Timeout de matchmaking: si no aparece nadie, avisar. (ESQUELETO)</summary>
    public void TimeoutMatchmaking(float segundos)
    {
        // TODO (con profesor): coroutine que espera 'segundos' y si no hay sala, cancela.
        Debug.Log($"[PvP-Robustez-ESQUELETO] TimeoutMatchmaking({segundos}s) — no implementado.");
    }

    /// <summary>Anti-trampas: el host valida que la jugada sea legal. (ESQUELETO)</summary>
    public bool ValidarJugadaLegal(GameLogic game, int x, int y, int player)
    {
        // Reutiliza la validacion existente del motor.
        return game.IsValidMove(x, y, player);
    }
}
