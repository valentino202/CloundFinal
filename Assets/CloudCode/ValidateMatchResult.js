// Agente 2 (servidor): Cloud Code "ValidateMatchResult"
// Subir con: Deployment package (Editor) o `ugs deploy` CLI.
// Valida resultado, otorga Gems (Economy) y MMR (Leaderboards).

const { DataApi } = require("@unity-services/cloud-save-1.4");
const { EconomyApi } = require("@unity-services/economy-2.4");
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

const GEM_CURRENCY_ID = "GEMS";       // crear en dashboard Economy
const LEADERBOARD_ID = "ranked_mmr";  // crear en dashboard Leaderboards
const WIN_GEMS = 50;
const LOSE_GEMS = 10;

module.exports = async ({ params, context, logger }) => {
  try {
    const { winnerId, loserId, gridSize, totalMoves, winnerCells, loserCells } = params;

    // --- Validacion anti-hack basica ---
    if (!winnerId || !loserId || winnerId === loserId)
      return { valid: false, gemsAwarded: 0, newMmr: 0, message: "IDs invalidos" };
    if (!Number.isInteger(gridSize) || gridSize < 3 || gridSize > 12)
      return { valid: false, gemsAwarded: 0, newMmr: 0, message: "GRID_SIZE fuera de rango" };
    if (!Number.isInteger(totalMoves) || totalMoves < 1 || totalMoves > gridSize * gridSize * 20)
      return { valid: false, gemsAwarded: 0, newMmr: 0, message: "totalMoves sospechoso" };
    if (winnerCells + loserCells > gridSize * gridSize || winnerCells <= loserCells)
      return { valid: false, gemsAwarded: 0, newMmr: 0, message: "Conteo de celdas inconsistente" };

    const callerId = context.playerId;
    const iWon = callerId === winnerId;
    const gems = iWon ? WIN_GEMS : LOSE_GEMS;

    const economy = new EconomyApi(context);
    const leaderboards = new LeaderboardsApi(context);
    const cloudSave = new DataApi(context);

    // 1. Recompensa Economy al que llama (acceso con contexto de jugador = seguro)
    await economy.incrementBalance({ currencyId: GEM_CURRENCY_ID, amount: gems });

    // 2. MMR: +25 ganador / +5 perdedor (nunca negativo para no castigar novatos)
    const mmrDelta = iWon ? 25 : 5;
    let newMmr = mmrDelta;
    try {
      const scores = await leaderboards.getPlayerScore({ leaderboardId: LEADERBOARD_ID, playerId: callerId });
      newMmr = (scores?.score ?? 0) + mmrDelta;
    } catch (e) { logger.warn("Sin score previo, parte de 0"); }
    await leaderboards.addPlayerScore({ leaderboardId: LEADERBOARD_ID, score: newMmr });

    // 3. Estadisticas en Cloud Save del llamante
    try {
      const saved = await cloudSave.getItems(callerId, ["TotalWins", "TotalMatches"]);
      const wins = (saved?.TotalWins ?? 0) + (iWon ? 1 : 0);
      const matches = (saved?.TotalMatches ?? 0) + 1;
      await cloudSave.setItemBatch(callerId, [
        { key: "TotalWins", value: wins },
        { key: "TotalMatches", value: matches }
      ]);
    } catch (e) { logger.warn("No se pudo actualizar CloudSave: " + e.message); }

    return { valid: true, gemsAwarded: gems, newMmr, message: iWon ? "Victoria validada" : "Derrota registrada" };
  } catch (error) {
    return { valid: false, gemsAwarded: 0, newMmr: 0, message: "Error servidor: " + error.message };
  }
};
