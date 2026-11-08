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
const invalid = (message, echo) => ({
  valid: false,
  gemsAwarded: 0,
  coinsAwarded: 0,
  newMmr: 0,
  message,
  leaderboardOk: false,
  leaderboardError: message,
  echo: echo ?? null,
});

module.exports = async ({ params, context, logger }) => {
  // Diagnostico: eco crudo de params para ver que recibe el servidor.
  const rawEcho = JSON.stringify(params ?? null);
  const trace = [];
  try {
    trace.push("params=" + rawEcho);
    const { projectId, playerId, accessToken } = context;

    const cloudSave = new DataApi({ accessToken });
    const leaderboards = new LeaderboardsApi({ accessToken });

    // Si params llega vacio (bug/version del SDK cliente), intenta leer el
    // resultado desde Cloud Save (canal garantizado que escribe el cliente).
    let p = params || {};
    let fromCloud = false;
    if (!p || Object.keys(p).length === 0) {
      try {
        const pending = await cloudSave.getItems(projectId, playerId, [
          "_mr_winnerId", "_mr_loserId", "_mr_gridSize", "_mr_totalMoves",
          "_mr_winnerCells", "_mr_loserCells", "_mr_matchMode", "_mr_maxTurns", "_mr_duration",
        ]);
        const it = (k) => {
          const f = (pending?.data?.results || []).find((e) => e.key === k);
          return f ? f.value : undefined;
        };
        p = {
          winnerId: it("_mr_winnerId"),
          loserId: it("_mr_loserId"),
          gridSize: Number(it("_mr_gridSize")),
          totalMoves: Number(it("_mr_totalMoves")),
          winnerCells: Number(it("_mr_winnerCells")),
          loserCells: Number(it("_mr_loserCells")),
          matchMode: Number(it("_mr_matchMode")),
          maxTurns: Number(it("_mr_maxTurns")),
          duration: Number(it("_mr_duration")),
        };
        fromCloud = typeof p.winnerId === "string" && p.winnerId.length > 0;
        trace.push("cloud=" + fromCloud + (fromCloud ? " w=" + p.winnerId : ""));
      } catch (ce) {
        trace.push("ERR canalCloud:" + ce.message);
        logger.error("Canal Cloud Save fallo: " + ce.message);
      }
    }

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
    } = p;

    // --- Validacion anti-hack basica ---
    // El mensaje incluye lo recibido para diagnosticar desde la consola del cliente.
    const invalidWithTrace = (message) => {
      trace.push("ERR " + message);
      return invalid(message, rawEcho);
    };
    if (!winnerId || !loserId || winnerId === loserId)
      return invalidWithTrace("IDs invalidos [w=" + (typeof winnerId === "string" ? winnerId : JSON.stringify(winnerId ?? null)) + ", l=" + (typeof loserId === "string" ? loserId : JSON.stringify(loserId ?? null)) + ", keys=" + Object.keys(params || {}).join("|") + ", fromCloud=" + fromCloud + "]");
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
      trace.push("ERR readCloudSave:" + e.message);
      logger.error("No se pudo leer Cloud Save: " + e.message);
    }

    const newGems = read("Gems", 0) + gems;
    const newCoins = read("Coins", 0) + coins;
    const totalWins = read("TotalWins", 0) + (iWon ? 1 : 0);
    const totalMatches = read("TotalMatches", 0) + 1;
    trace.push("leidos=" + items.length + " wins=" + totalWins + " matches=" + totalMatches);

    // Gems/Coins/TotalWins los maneja el perfil del CLIENTE (GrantCurrencyAsync).
    // El servidor solo escribe claves de ranking para evitar conflictos de escritura (400).
    // Nota: gems/coins van al trace pero el servidor no los escribe.
    trace.push("recompensa=" + gems + "g/" + coins + "c");

    // Fuente de verdad DUAL: metadata del leaderboard (persistente, funciona con
    // metaObj) + snapshot de Cloud Save leido ANTES de esta partida. Con ambos se
    // toma el valor mayor, asi no se pierde ni duplica historial.
    const csNormalWins = {};
    for (let t = 30; t <= 100; t += 10) csNormalWins[t] = read(`NormalWins_${t}`, 0);
    const csInfinitoWins = read("InfinitoWins", 0);
    let csInfinitoDurations = [];
    try {
      const raw = items.find((e) => e.key === "InfinitoDurations")?.value;
      if (typeof raw === "string") csInfinitoDurations = JSON.parse(raw);
      if (!Array.isArray(csInfinitoDurations)) csInfinitoDurations = [];
    } catch (e) {
      csInfinitoDurations = [];
    }

    // Lectura (GET) de la metadata previa del leaderboard. Escalera de nombres
    // porque varia entre versiones del wrapper.
    const readLBMeta = async (leaderboardId) => {
      const getters = [
        ["getLeaderboardPlayerScore", () => leaderboards.getLeaderboardPlayerScore(projectId, leaderboardId, playerId)],
        ["getLeaderboardScore", () => leaderboards.getLeaderboardScore(projectId, leaderboardId, playerId)],
        ["getPlayerScore", () => leaderboards.getPlayerScore(projectId, leaderboardId, playerId)],
      ];
      for (const [label, fn] of getters) {
        try {
          const res = await fn();
          const sc = res?.data ?? res;
          const meta = sc?.metadata;
          trace.push("lbLect=" + label + "=ok");
          return meta && typeof meta === "object" ? meta : null;
        } catch (e) {
          const msg = "" + (e.message || "");
          trace.push("lbLect=" + label + "=fail " + (msg.includes("404") ? "404 (sin entrada)" : msg));
        }
      }
      return null;
    };

    // Escritura de Cloud Save probando las formas candidatas del body. El error
    // "cannot unmarshal string ... server.request" indica que el body HTTP llega
    // como string: la forma t3 manda el body ({key, value}) como 3er argumento.
    const csSave = async (key, value) => {
      const forms = [
        ["t3", () => cloudSave.setItem(projectId, playerId, { key: key, value: value })],
        ["t4w", () => cloudSave.setItem(projectId, playerId, key, { value: value })],
        ["t4d", () => cloudSave.setItem(projectId, playerId, key, value)],
        ["lote", () => cloudSave.setItemBatch(projectId, playerId, [{ key: key, value: value }])],
      ];
      for (const [label, fn] of forms) {
        try {
          await fn();
          trace.push("cs=" + key + " " + label + "=ok");
          return true;
        } catch (e) {
          const detail = e && e.response && e.response.data ? JSON.stringify(e.response.data) : "";
          trace.push("cs=" + key + " " + label + "=fail " + e.message + (detail ? " det=" + detail : ""));
        }
      }
      logger.error("No se pudo guardar en Cloud Save: " + key);
      return false;
    };

    // Backup Cloud Save con los valores finales (se escribe al terminar todo).
    const csBackup = [];

    // Escritura robusta del leaderboard: escalera de metadata candidata y caida
    // final sin metadata. Cada intento queda en el trace con el detalle del 400.
    let leaderboardOk = false;
    let leaderboardError = null;
    const writeScore = async (leaderboardId, score, metaCandidates) => {
      const attempts = (metaCandidates || []).map((c) => [c.label, { score: score, metadata: c.meta }]);
      attempts.push(["sinMeta", { score: score }]);
      for (const [label, body] of attempts) {
        try {
          await leaderboards.addLeaderboardPlayerScore(projectId, leaderboardId, playerId, body);
          leaderboardOk = true;
          trace.push("lb=" + leaderboardId + " " + label + "=ok");
          return;
        } catch (e) {
          const detail = e && e.response && e.response.data ? JSON.stringify(e.response.data) : "";
          trace.push("lb=" + leaderboardId + " " + label + "=400 " + e.message + (detail ? " det=" + detail : ""));
        }
      }
      leaderboardError = "metadata rechazada, score sin desglose";
      logger.error("No se pudo escribir metadata en " + leaderboardId);
    };

    // Ranking Normal: score = total de victorias normales. Metadata = victorias por modo.
    // Fuente de verdad: max(metadata previa del leaderboard, Cloud Save) + esta victoria.
    if (iWon && matchMode === 0) {
      try {
        const prevMeta = await readLBMeta(LEADERBOARD_NORMAL);
        const meta = {};
        for (let t = 30; t <= 100; t += 10) {
          const lbPrev = Number(prevMeta?.[`wins${t}`] ?? 0) || 0;
          let v = Math.max(lbPrev, csNormalWins[t]);
          if (t === maxTurns) v = v + 1;
          meta[`wins${t}`] = v;
        }
        let total = 0;
        for (let t = 30; t <= 100; t += 10) total += meta[`wins${t}`];
        meta.total = total;
        await writeScore(LEADERBOARD_NORMAL, total, [{ label: "metaObj", meta: meta }]);
        csBackup.push({ key: `NormalWins_${maxTurns}`, value: meta[`wins${maxTurns}`] });
        csBackup.push({ key: "NormalWins_Total", value: total });
      } catch (e) {
        trace.push("ERR Normal:" + e.message);
        logger.error("Leaderboard Normal no disponible: " + e.message);
      }
    }

    // Ranking Infinito: score compuesto = wins*1e6 - bestTime.
    // Metadata = wins, bestTime, nearBest (durations si el servicio la acepta).
    if (iWon && matchMode === 1) {
      try {
        const prevMeta = await readLBMeta(LEADERBOARD_INFINITO);
        const lbPrevWins = Number(prevMeta?.wins ?? 0) || 0;
        const lbPrevBest = Number(prevMeta?.bestTime ?? 0) || 0;
        const lbPrevNear = Number(prevMeta?.nearBest ?? 0) || 0;
        const wins = Math.max(csInfinitoWins, lbPrevWins) + 1;

        // Duraciones: fusion del snapshot Cloud Save y la metadata previa (gana la mas larga).
        let durations = csInfinitoDurations.slice();
        if (Array.isArray(prevMeta?.durations) && prevMeta.durations.length > durations.length) {
          durations = prevMeta.durations.map(Number).filter((d) => Number.isFinite(d));
        }
        durations.push(duration);
        if (durations.length > 50) durations = durations.slice(-50);
        const best = Math.min.apply(null, durations);
        const near = durations.filter((d) => Number.isFinite(d) && d <= best + NEAR_BEST_THRESHOLD).length;

        // Alternativa si el servicio rechaza el array en metadata: recontar desde lo previo.
        const bestSinDur = lbPrevBest > 0 ? Math.min(lbPrevBest, duration) : duration;
        const nearSinDur = lbPrevNear + (lbPrevBest > 0 && duration <= lbPrevBest + NEAR_BEST_THRESHOLD ? 1 : 0);
        const candidates = [
          { label: "metaObj+dur", meta: { wins: wins, bestTime: best, nearBest: near, durations: durations } },
          { label: "metaObj", meta: { wins: wins, bestTime: bestSinDur, nearBest: nearSinDur } },
        ];

        await writeScore(LEADERBOARD_INFINITO, wins * 1000000 - Math.round(bestSinDur), candidates);
        csBackup.push({ key: "InfinitoWins", value: wins });
        csBackup.push({ key: "InfinitoBestTime", value: bestSinDur });
        csBackup.push({ key: "InfinitoNearBest", value: nearSinDur });
        csBackup.push({ key: "InfinitoDurations", value: JSON.stringify(durations) });
      } catch (e) {
        trace.push("ERR Infinito:" + e.message);
        logger.error("Leaderboard Infinito no disponible: " + e.message);
      }
    }

    // Backup Cloud Save al final, ya con los valores definitivos.
    if (csBackup.length > 0) {
      let guardOk = 0;
      for (const item of csBackup) {
        if (await csSave(item.key, item.value)) guardOk++;
      }
      trace.push("guardadoCS=" + guardOk + "/" + csBackup.length);
    } else {
      trace.push("guardadoCS=saltado(sin claves de ranking)");
    }

    return {
      valid: true,
      gemsAwarded: gems,
      coinsAwarded: coins,
      newMmr: 0,
      message: iWon ? "Victoria validada" : "Derrota registrada",
      leaderboardOk: leaderboardOk,
      leaderboardError: leaderboardError,
      echo: rawEcho,
      trace: trace.join(" | ") + " | lb=" + leaderboardOk + (leaderboardError ? " lbErr=" + leaderboardError : ""),
    };
  } catch (error) {
    trace.push("ERR fatal:" + error.message);
    logger.error("ValidateMatchResult fallo: " + error.message);
    const r = invalid("Error servidor: " + error.message, rawEcho);
    r.trace = trace.join(" | ");
    return r;
  }
};