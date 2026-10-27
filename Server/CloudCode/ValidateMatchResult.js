
// Agente 2 (servidor): Cloud Code "ValidateMatchResult"

// Subir con: Deployment package (Editor) o `ugs deploy` CLI.

// Valida resultado, otorga Coins/Gems (Cloud Save, Economy cerrado sep-2026)

// y MMR (Leaderboards).



const { DataApi } = require("@unity-services/cloud-save-1.4");

const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");



const GEM_CURRENCY_ID = "GEMS";       // solo etiqueta, vive en Cloud Save

const COIN_CURRENCY_ID = "COINS";   // solo etiqueta, vive en Cloud Save

const LEADERBOARD_ID = "ranked_mmr";  // crear en dashboard Leaderboards

const WIN_GEMS = 50;

const LOSE_GEMS = 10;

const WIN_COINS = 100;

const LOSE_COINS = 25;



module.exports = async ({ params, context, logger }) => {

  try {

    const { winnerId, loserId, gridSize, totalMoves, winnerCells, loserCells } = params;


    // --- Validacion anti-hack basica ---

    if (!winnerId || !loserId || winnerId === loserId)

      return { valid: false, gemsAwarded: 0, coinsAwarded: 0, newMmr: 0, message: "IDs invalidos" };

    if (!Number.isInteger(gridSize) || gridSize < 3 || gridSize > 12)

      return { valid: false, gemsAwarded: 0, coinsAwarded: 0, newMmr: 0, message: "GRID_SIZE fuera de rango" };

    if (!Number.isInteger(totalMoves) || totalMoves < 1 || totalMoves > gridSize * gridSize * 20)

      return { valid: false, gemsAwarded: 0, coinsAwarded: 0, newMmr: 0, message: "totalMoves sospechoso" };

    if (winnerCells + loserCells > gridSize * gridSize || winnerCells <= loserCells)

      return { valid: false, gemsAwarded: 0, coinsAwarded: 0, newMmr: 0, message: "Conteo de celdas inconsistente" };


    const callerId = context.playerId;

    const iWon = callerId === winnerId;

    const gems = iWon ? WIN_GEMS : LOSE_GEMS;

    const coins = iWon ? WIN_COINS : LOSE_COINS;



    const leaderboards = new LeaderboardsApi(context);

    const cloudSave = new DataApi(context);



    // 1. Monedas en Cloud Save del llamante (server-authoritative, anti-hack)

    let newGems = gems;

    let newCoins = coins;

    try {

      const saved = await cloudSave.getItems(callerId, ["Gems", "Coins", "TotalWins", "TotalMatches"]);

      newGems = (saved?.Gems ?? 0) + gems;

      newCoins = (saved?.Coins ?? 0) + coins;

      const wins = (saved?.TotalWins ?? 0) + (iWon ? 1 : 0);

      const matches = (saved?.TotalMatches ?? 0) + 1;

      await cloudSave.setItemBatch(callerId, [

        { key: "Gems", value: newGems },

        { key: "Coins", value: newCoins },

        { key: "TotalWins", value: wins },

        { key: "TotalMatches", value: matches }

      ]);

    } catch (e) { logger.warn("No se pudo actualizar CloudSave: " + e.message); }


    // 2. MMR: +25 ganador / +5 perdedor (nunca negativo para no castigar novatos)

    const mmrDelta = iWon ? 25 : 5;

    let newMmr = mmrDelta;

    try {

      const scores = await leaderboards.getPlayerScore({ leaderboardId: LEADERBOARD_ID, playerId: callerId });

      newMmr = (scores?.score ?? 0) + mmrDelta;

    } catch (e) { logger.warn("Sin score previo, parte de 0"); }

    await leaderboards.addPlayerScore({ leaderboardId: LEADERBOARD_ID, score: newMmr });



    return { valid: true, gemsAwarded: gems, coinsAwarded: coins, newMmr, message: iWon ? "Victoria validada" : "Derrota registrada" };

  } catch (error) {

    return { valid: false, gemsAwarded: 0, coinsAwarded: 0, newMmr: 0, message: "Error servidor: " + error.message };

  }

};
