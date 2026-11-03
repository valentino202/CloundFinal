// Cloud Code "ValidateMatchResult"
// Subir/actualizar desde Unity: Services > Deployment > seleccionar este archivo > Deploy Selected.
// Otorga Coins/Gems en Cloud Save y actualiza los leaderboards ranked_normal y ranked_infinito.
const { DataApi } = require("@unity-services/cloud-save-1.4");
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");
const LEADERBOARD_NORMAL = "ranked_normal";
const LEADERBOARD_INFINITO = "ranked_infinito";
const WIN_GEMS = 50;
const LOSE_GEMS = 10;
const WIN_COINS = 100;
const LOSE_COINS = 25;
const NEAR_BEST_THRESHOLD = 5;
const invalid = (message) => ({
  valid: false,
  gemsAwarded: 0,
  coinsAwarded: 0,
  newMmr: 0,
  message,
});

module.exports = async ({ params, context, logger }) => {
  try {
    const {
      winnerId,
      loserId,
      gridSize,
      totalMoves,
      winnerCells,
      loserCells,
      matchMode,
      maxTurns,
      duration,
    } = params || {};
    const { projectId, playerId, accessToken } = context;

    // --- Validacion anti-hack basica ---
    if (!winnerId || !loserId || winnerId === loserId)
      return invalid("IDs invalidos");
    if (!Number.isInteger(gridSize) || gridSize < 3 || gridSize > 12)
      return invalid("GRID_SIZE fuera de rango");
    if (!Number.isInteger(totalMoves) || totalMoves < 1 || totalMoves > gridSize * gridSize * 20)
      return invalid("totalMoves sospechoso");
    if (!Number.isInteger(winnerCells) || !Number.isInteger(loserCells))
      return invalid("Celdas invalidas");
    if (winnerCells + loserCells > gridSize * gridSize || winnerCells <= loserCells)
      return invalid("Conteo de celdas inconsistente");

    if (matchMode !== 0 && matchMode !== 1)
      return invalid("matchMode invalido");
    if (matchMode === 0) {
      if (!Number.isInteger(maxTurns) || maxTurns < 30 || maxTurns > 100)
        return invalid("maxTurns invalido");
    }
    if (typeof duration !== "number" || !Number.isFinite(duration) || duration < 0 || duration > 7200)
      return invalid("duration invalida");

    const iWon = playerId === winnerId;
    const iLost = playerId === loserId;
    if (!iWon && !iLost)
      return invalid("El jugador no participo en esta partida");

    const gems = iWon ? WIN_GEMS : LOSE_GEMS;
    const coins = iWon ? WIN_COINS : LOSE_COINS;
    const mmrDelta = iWon ? 25 : 5;

    const cloudSave = new DataApi({ accessToken });
    const leaderboards = new LeaderboardsApi({ accessToken });

    // Claves a leer segun el modo.
    const baseKeys = ["Gems", "Coins", "TotalWins", "TotalMatches"];
    const modeKeys = matchMode === 0
      ? ["NormalWins_30", "NormalWins_40", "NormalWins_50", "NormalWins_60", "NormalWins_70", "NormalWins_80", "NormalWins_90", "NormalWins_100", "NormalWins_Total"]
      : ["InfinitoWins", "InfinitoBestTime", "InfinitoNearBest", "InfinitoDurations"];
    const keysToRead = baseKeys.concat(modeKeys);

    let read = (key, fallback = 0) => fallback;
    let items = [];
    try {
      const current = await cloudSave.getItems(projectId, playerId, keysToRead);
      items = current?.data?.results || [];
      read = (key, fallback = 0) => {
        const item = items.find((entry) => entry.key === key);
        const value = item?.value;
        if (value === undefined || value === null) return fallback;
        if (typeof value === "string") {
          const n = Number(value);
          return Number.isFinite(n) ? n : value;
        }
        const n = Number(value);
        return Number.isFinite(n) ? n : fallback;
      };
    } catch (e) {
      logger.warn("No se pudo leer Cloud Save: " + e.message);
    }

    const newGems = read("Gems", 0) + gems;
    const newCoins = read("Coins", 0) + coins;
    const totalWins = read("TotalWins", 0) + (iWon ? 1 : 0);
    const totalMatches = read("TotalMatches", 0) + 1;

    const batch = [
      { key: "Gems", value: newGems },
      { key: "Coins", value: newCoins },
      { key: "TotalWins", value: totalWins },
      { key: "TotalMatches", value: totalMatches },
    ];

    // Estadisticas de ranking (solo victorias).
    let normalTotal = read("NormalWins_Total", 0);
    let infinitoWins = read("InfinitoWins", 0);
    let infinitoBest = read("InfinitoBestTime", 0);
    let infinitoNear = read("InfinitoNearBest", 0);
    let infinitoDurations = [];
    try {
      const raw = items.find((e) => e.key === "InfinitoDurations")?.value;
      if (typeof raw === "string") infinitoDurations = JSON.parse(raw);
      if (!Array.isArray(infinitoDurations)) infinitoDurations = [];
    } catch (e) {
      infinitoDurations = [];
    }

    if (iWon) {
      if (matchMode === 0) {
        const turnKey = `NormalWins_${maxTurns}`;
        const prevTurn = read(turnKey, 0);
        batch.push({ key: turnKey, value: prevTurn + 1 });
        normalTotal = normalTotal + 1;
        batch.push({ key: "NormalWins_Total", value: normalTotal });
      } else {
        infinitoWins = infinitoWins + 1;
        infinitoDurations.push(duration);
        if (infinitoDurations.length > 50) infinitoDurations = infinitoDurations.slice(-50);
        infinitoBest = infinitoDurations.length ? Math.min.apply(null, infinitoDurations) : duration;
        infinitoNear = infinitoDurations.filter((d) => typeof d === "number" && d <= infinitoBest + NEAR_BEST_THRESHOLD).length;
        batch.push({ key: "InfinitoWins", value: infinitoWins });
        batch.push({ key: "InfinitoBestTime", value: infinitoBest });
        batch.push({ key: "InfinitoNearBest", value: infinitoNear });
        batch.push({ key: "InfinitoDurations", value: JSON.stringify(infinitoDurations) });
      }
    }

    try {
      await cloudSave.setItemBatch(projectId, playerId, { items: batch });
    } catch (e) {
      logger.warn("No se pudo actualizar Cloud Save: " + e.message);
    }

    // Ranking Normal: score = total de victorias normales. Metadata = victorias por modo (serializado).
    if (iWon && matchMode === 0) {
      try {
        const meta = { total: normalTotal };
        for (let t = 30; t <= 100; t += 10) {
          const key = `NormalWins_${t}`;
          let v = read(key, 0);
          if (t === maxTurns) v = v + 1;
          meta[`wins${t}`] = v;
        }
        await leaderboards.addLeaderboardPlayerScore(projectId, LEADERBOARD_NORMAL, playerId, {
          score: normalTotal,
          metadata: JSON.stringify(meta),
        });
      } catch (e) {
        logger.warn("Leaderboard Normal no disponible: " + e.message);
      }
    }

    // Ranking Infinito: score compuesto = wins*1e6 - bestTime.
    // Metadata = wins, bestTime, nearBest (serializado).
    if (iWon && matchMode === 1) {
      try {
        const composite = infinitoWins * 1000000 - Math.round(infinitoBest);
        await leaderboards.addLeaderboardPlayerScore(projectId, LEADERBOARD_INFINITO, playerId, {
          score: composite,
          metadata: JSON.stringify({ wins: infinitoWins, bestTime: infinitoBest, nearBest: infinitoNear }),
        });
      } catch (e) {
        logger.warn("Leaderboard Infinito no disponible: " + e.message);
      }
    }

    return {
      valid: true,
      gemsAwarded: gems,
      coinsAwarded: coins,
      newMmr: 0,
      message: iWon ? "Victoria validada" : "Derrota registrada",
    };
  } catch (error) {
    logger.error("ValidateMatchResult fallo: " + error.message);
    return invalid("Error servidor: " + error.message);
  }
};