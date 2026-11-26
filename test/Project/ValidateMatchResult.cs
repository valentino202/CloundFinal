using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Apis;

namespace HelloWorld;

/// <summary>
/// Resultado devuelto al cliente (espejo del JS ValidateMatchResult).
/// </summary>
public class ValidateResult
{
    public bool valid { get; set; }
    public int gemsAwarded { get; set; }
    public int coinsAwarded { get; set; }
    public int newMmr { get; set; }
    public string message { get; set; } = "";
    public bool leaderboardOk { get; set; }
    public string leaderboardError { get; set; } = "";
    public string echo { get; set; } = "";
    public string trace { get; set; } = "";
}

/// <summary>
/// Conversion a C# del Cloud Code "ValidateMatchResult" (antes en JavaScript).
/// Otorga Coins/Gems (via cliente) y actualiza los leaderboards ranked_normal
/// y ranked_infinito. El profesor pidio C# en vez de JS.
/// </summary>
public class ValidateMatchResultModule
{
    private readonly ILogger<ValidateMatchResultModule> _logger;

    public ValidateMatchResultModule(ILogger<ValidateMatchResultModule> logger)
    {
        _logger = logger;
    }

    private const string LEADERBOARD_NORMAL = "ranked_normal";
    private const string LEADERBOARD_INFINITO = "ranked_infinito";
    private const int WIN_GEMS = 50;
    private const int LOSE_GEMS = 10;
    private const int WIN_COINS = 100;
    private const int LOSE_COINS = 25;
    private const int NEAR_BEST_THRESHOLD = 5;

    [CloudCodeFunction("ValidateMatchResult")]
    public async Task<ValidateResult> ValidateMatchResult(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        string winnerId,
        string loserId,
        int gridSize,
        int totalMoves,
        int winnerCells,
        int loserCells,
        int matchMode,
        int maxTurns,
        float duration,
        string playerName)
    {
        var trace = new List<string>();
        var rawEcho = JsonSerializer.Serialize(new
        {
            winnerId, loserId, gridSize, totalMoves, winnerCells, loserCells,
            matchMode, maxTurns, duration
        });

        try
        {
            trace.Add("params=" + rawEcho);
            var projectId = context.ProjectId;
            var playerId = context.PlayerId;
            var accessToken = context.AccessToken;

            // --- Validacion anti-hack basica ---
            if (string.IsNullOrEmpty(winnerId) || string.IsNullOrEmpty(loserId) || winnerId == loserId)
                return Invalid("IDs invalidos", rawEcho, trace);
            if (gridSize < 3 || gridSize > 12)
                return Invalid("GRID_SIZE fuera de rango", rawEcho, trace);
            if (totalMoves < 1 || totalMoves > gridSize * gridSize * 20)
                return Invalid("totalMoves sospechoso", rawEcho, trace);
            if (winnerCells + loserCells > gridSize * gridSize || winnerCells <= loserCells)
                return Invalid("Conteo de celdas inconsistente", rawEcho, trace);
            if (matchMode != 0 && matchMode != 1)
                return Invalid("matchMode invalido", rawEcho, trace);
            if (matchMode == 0 && (maxTurns < 30 || maxTurns > 100))
                return Invalid("maxTurns invalido", rawEcho, trace);
            if (duration < 0 || duration > 7200)
                return Invalid("duration invalida", rawEcho, trace);

            var iWon = playerId == winnerId;
            var iLost = playerId == loserId;
            if (!iWon && !iLost)
                return Invalid("El jugador no participo en esta partida", rawEcho, trace);

            var gems = iWon ? WIN_GEMS : LOSE_GEMS;
            var coins = iWon ? WIN_COINS : LOSE_COINS;

            // --- Lectura de Cloud Save ---
            var keysToRead = new List<string> { "Gems", "Coins", "TotalWins", "TotalMatches" };
            if (matchMode == 0)
            {
                for (int t = 30; t <= 100; t += 10)
                    keysToRead.Add($"NormalWins_{t}");
                keysToRead.Add("NormalWins_Total");
            }
            else
            {
                keysToRead.Add("InfinitoWins");
                keysToRead.Add("InfinitoBestTime");
                keysToRead.Add("InfinitoNearBest");
                keysToRead.Add("InfinitoDurations");
            }

            var readValues = new Dictionary<string, object>();
            try
            {
                var items = await gameApiClient.CloudSaveData.GetItemsAsync(
                    context, accessToken, projectId, playerId, keysToRead, null, CancellationToken.None);
                foreach (var item in items.Data.Results)
                    readValues[item.Key] = item.Value;
            }
            catch (Exception e)
            {
                trace.Add("ERR readCloudSave:" + e.Message);
                _logger.LogError("No se pudo leer Cloud Save: " + e.Message);
            }

            long ReadLong(string key, long fallback = 0)
            {
                if (!readValues.TryGetValue(key, out var v) || v == null) return fallback;
                if (v is long l) return l;
                if (v is int i) return i;
                if (v is double dd) return (long)dd;
                if (long.TryParse(v.ToString(), out var n)) return n;
                return fallback;
            }

            var newGems = ReadLong("Gems", 0) + gems;
            var newCoins = ReadLong("Coins", 0) + coins;
            var totalWins = ReadLong("TotalWins", 0) + (iWon ? 1 : 0);
            var totalMatches = ReadLong("TotalMatches", 0) + 1;
            trace.Add($"leidos={readValues.Count} wins={totalWins} matches={totalMatches}");

            // Gems/Coins/TotalWins los maneja el perfil del CLIENTE (GrantCurrencyAsync).
            // El servidor solo escribe claves de ranking para evitar conflictos (400).
            trace.Add($"recompensa={gems}g/{coins}c");

            // --- Cloud Save snapshot previo ---
            var csNormalWins = new Dictionary<int, long>();
            for (int t = 30; t <= 100; t += 10)
                csNormalWins[t] = ReadLong($"NormalWins_{t}", 0);
            var csInfinitoWins = ReadLong("InfinitoWins", 0);
            var csInfinitoDurations = new List<double>();
            if (readValues.TryGetValue("InfinitoDurations", out var durRaw) && durRaw is string durStr)
            {
                try { csInfinitoDurations = JsonSerializer.Deserialize<List<double>>(durStr) ?? new List<double>(); }
                catch { csInfinitoDurations = new List<double>(); }
            }

            // --- Lectura de metadata previa del leaderboard ---
            Dictionary<string, object> ReadLBMeta(string leaderboardId)
            {
                try
                {
                    var res = gameApiClient.Leaderboards.GetLeaderboardPlayerScoreAsync(
                        context, accessToken, Guid.Parse(projectId), leaderboardId, playerId, true, CancellationToken.None).Result;
                    return ToDict(res.Data.Metadata);
                }
                catch (Exception e)
                {
                    trace.Add($"lbLect={leaderboardId}=fail {(e.Message.Contains("404") ? "404 (sin entrada)" : e.Message)}");
                }
                return null;
            }

            bool leaderboardOk = false;
            string leaderboardError = null;

            async Task WriteScore(string leaderboardId, double score, Dictionary<string, object> meta)
            {
                try
                {
                    await gameApiClient.Leaderboards.AddLeaderboardPlayerScoreAsync(
                        context, accessToken, Guid.Parse(projectId), leaderboardId, playerId,
                        new Unity.Services.Leaderboards.Model.AddLeaderboardScore(score, meta, null), CancellationToken.None);
                    leaderboardOk = true;
                    trace.Add($"lb={leaderboardId} ok");
                }
                catch (Exception e)
                {
                    var detail = e.Message;
                    trace.Add($"lb={leaderboardId} 400 {detail}");
                    leaderboardError = "metadata rechazada, score sin desglose";
                    _logger.LogError("No se pudo escribir metadata en " + leaderboardId);
                }
            }

            async Task SaveCloudSaveKey(string key, object value)
            {
                try
                {
                    await gameApiClient.CloudSaveData.SetItemAsync(
                        context, accessToken, projectId, playerId,
                        new Unity.Services.CloudSave.Model.SetItemBody(key, value, null),
                        CancellationToken.None);
                    trace.Add($"cs={key} ok");
                }
                catch (Exception e)
                {
                    trace.Add($"cs={key} fail {e.Message}");
                }
            }

            // --- Ranking Normal ---
            if (iWon && matchMode == 0)
            {
                try
                {
                    var prevMeta = ReadLBMeta(LEADERBOARD_NORMAL);
                    var meta = new Dictionary<string, object>();
                    long total = 0;
                    for (int t = 30; t <= 100; t += 10)
                    {
                        long lbPrev = ReadMetaLong(prevMeta, $"wins{t}", 0);
                        long val = Math.Max(lbPrev, csNormalWins[t]);
                        if (t == maxTurns) val = val + 1;
                        meta[$"wins{t}"] = val;
                        total += val;
                    }
                    meta["total"] = total;
                    if (!string.IsNullOrEmpty(playerName)) meta["playerName"] = playerName;
                    await WriteScore(LEADERBOARD_NORMAL, total, meta);

                    await SaveCloudSaveKey($"NormalWins_{maxTurns}", meta[$"wins{maxTurns}"]);
                    await SaveCloudSaveKey("NormalWins_Total", total);
                }
                catch (Exception e)
                {
                    trace.Add("ERR Normal:" + e.Message);
                    _logger.LogError("Leaderboard Normal no disponible: " + e.Message);
                }
            }

            // --- Ranking Infinito ---
            if (iWon && matchMode == 1)
            {
                try
                {
                    var prevMeta = ReadLBMeta(LEADERBOARD_INFINITO);
                    long lbPrevWins = ReadMetaLong(prevMeta, "wins", 0);
                    long lbPrevBest = ReadMetaLong(prevMeta, "bestTime", 0);
                    long lbPrevNear = ReadMetaLong(prevMeta, "nearBest", 0);
                    var wins = Math.Max(csInfinitoWins, lbPrevWins) + 1;

                    var durations = new List<double>(csInfinitoDurations);
                    if (prevMeta != null && prevMeta.TryGetValue("durations", out var pd))
                    {
                        List<double> prevDurs = null;
                        if (pd is List<double> ld) prevDurs = ld;
                        else if (pd is JsonElement je && je.ValueKind == JsonValueKind.Array)
                        {
                            prevDurs = new List<double>();
                            foreach (var el in je.EnumerateArray())
                                if (el.TryGetDouble(out var dd)) prevDurs.Add(dd);
                        }
                        if (prevDurs != null && prevDurs.Count > durations.Count)
                            durations = prevDurs;
                    }
                    durations.Add(duration);
                    if (durations.Count > 50) durations = durations.GetRange(durations.Count - 50, 50);
                    var best = durations.Count > 0 ? durations.Min() : duration;
                    var near = durations.Count(d => d <= best + NEAR_BEST_THRESHOLD);

                    var bestSinDur = lbPrevBest > 0 ? Math.Min(lbPrevBest, (long)duration) : (long)duration;
                    var nearSinDur = lbPrevNear + (lbPrevBest > 0 && duration <= lbPrevBest + NEAR_BEST_THRESHOLD ? 1 : 0);

                    var meta = new Dictionary<string, object>
                    {
                        ["wins"] = wins,
                        ["bestTime"] = bestSinDur,
                        ["nearBest"] = nearSinDur,
                        ["durations"] = durations,
                        ["playerName"] = playerName ?? ""
                    };

                    await WriteScore(LEADERBOARD_INFINITO, wins * 1000000 - Math.Round((double)bestSinDur), meta);

                    await SaveCloudSaveKey("InfinitoWins", wins);
                    await SaveCloudSaveKey("InfinitoBestTime", bestSinDur);
                    await SaveCloudSaveKey("InfinitoNearBest", nearSinDur);
                    await SaveCloudSaveKey("InfinitoDurations", JsonSerializer.Serialize(durations));
                }
                catch (Exception e)
                {
                    trace.Add("ERR Infinito:" + e.Message);
                    _logger.LogError("Leaderboard Infinito no disponible: " + e.Message);
                }
            }

            return new ValidateResult
            {
                valid = true,
                gemsAwarded = gems,
                coinsAwarded = coins,
                newMmr = 0,
                message = iWon ? "Victoria validada" : "Derrota registrada",
                leaderboardOk = leaderboardOk,
                leaderboardError = leaderboardError,
                echo = rawEcho,
                trace = string.Join(" | ", trace) + $" | lb={leaderboardOk}" + (leaderboardError != null ? " lbErr=" + leaderboardError : "")
            };
        }
        catch (Exception error)
        {
            trace.Add("ERR fatal:" + error.Message);
            _logger.LogError("ValidateMatchResult fallo: " + error.Message);
            return new ValidateResult
            {
                valid = false,
                gemsAwarded = 0,
                coinsAwarded = 0,
                newMmr = 0,
                message = "Error servidor: " + error.Message,
                leaderboardOk = false,
                leaderboardError = "Error servidor: " + error.Message,
                echo = rawEcho,
                trace = string.Join(" | ", trace)
            };
        }
    }

    private static ValidateResult Invalid(string message, string rawEcho, List<string> trace)
    {
        trace.Add("ERR " + message);
        return new ValidateResult
        {
            valid = false,
            gemsAwarded = 0,
            coinsAwarded = 0,
            newMmr = 0,
            message = message,
            leaderboardOk = false,
            leaderboardError = message,
            echo = rawEcho,
            trace = string.Join(" | ", trace)
        };
    }

    private static Dictionary<string, object> ToDict(object meta)
    {
        if (meta == null) return null;
        if (meta is Dictionary<string, object> d) return d;
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(meta)); }
        catch { return null; }
    }

    private static long ReadMetaLong(Dictionary<string, object> meta, string key, long fallback)
    {
        if (meta == null || !meta.TryGetValue(key, out var v) || v == null) return fallback;
        if (v is long l) return l;
        if (v is int i) return i;
        if (v is double dd) return (long)dd;
        if (long.TryParse(v.ToString(), out var n)) return n;
        return fallback;
    }
}
