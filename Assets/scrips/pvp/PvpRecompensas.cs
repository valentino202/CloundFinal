using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ESQUELETO — Paso 8: recompensas y ranking en PvP.
/// El ganador reporta al Cloud Code ValidateMatchResult (ya existe) con
/// winnerId/loserId REALES (en vez de "BOT"). Monedas/gemas y ranking se
/// guardan por cuenta.
///
/// ESTADO: esqueleto. A diferencia de los otros, ESTE puede llamar al
/// endpoint real porque UGSCloudCodeManager ya esta cableado.
/// </summary>
public class PvpRecompensas
{
    /// <summary>Reporta el resultado al servidor y aplica recompensas.
    /// (ESQUELETO con llamada real al endpoint existente.)</summary>
    public async Task<bool> ReportarResultado(
        string winnerId, string loserId, bool draw,
        int gridSize, int totalMoves, int winnerCells, int loserCells,
        int matchMode, int maxTurns, float duration)
    {
        // Reutilizamos el endpoint que ya existe (ValidateMatchResult.js).
        // En PvP solo cambia que winnerId/loserId son IDs reales, no "BOT".
        var res = await UGSCloudCodeManager.Instance.SubmitMatchResultAsync(
            new UGSCloudCodeManager.MatchResult
            {
                winnerId = winnerId,
                loserId = loserId,
                gridSize = gridSize,
                totalMoves = totalMoves,
                winnerCells = winnerCells,
                loserCells = loserCells,
                matchMode = matchMode,
                maxTurns = maxTurns,
                duration = duration
            });

        if (res != null && res.valid)
        {
            Debug.Log($"[PvP-Recompensas] OK: gems={res.gemsAwarded} coins={res.coinsAwarded} " +
                      $"leaderboardOk={res.leaderboardOk}");
            return true;
        }
        Debug.LogWarning($"[PvP-Recompensas] Servidor rechazo: {res?.message}");
        return false;
    }
}
