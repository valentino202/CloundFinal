using System.Text.Json;
using ReactionDomino.Core;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace ReactionDomino.CloudCode
{
    /// <summary>
    /// Guarda y carga el <see cref="MatchRecord"/> y reconstruye la partida reproduciendo sus jugadas.
    ///
    /// ACCESS CLASS: Custom Data. Un "custom item" de Cloud Save no pertenece a ningun jugador:
    /// solo se lee y se escribe desde el servidor con el token de servicio (<c>context.ServiceToken</c>),
    /// asi que ningun cliente puede tocar el registro de una partida. La clave es el matchId.
    ///
    /// Los workers de Cloud Code no guardan nada en memoria entre llamadas, por eso en cada llamada
    /// se carga el registro y se reproduce (<see cref="Replay"/>).
    /// </summary>
    public static class MatchStateStore
    {
        private const string StateItemKey = "state";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        /// <summary>Devuelve el registro o null si no existe ninguna partida con ese id.</summary>
        public static async Task<MatchRecord> TryLoadAsync(IExecutionContext context, IGameApiClient gameApiClient, string matchId)
        {
            var result = await gameApiClient.CloudSaveData.GetCustomItemsAsync(
                context, context.ServiceToken, context.ProjectId, matchId);
            var item = result.Data.Results.FirstOrDefault(i => i.Key == StateItemKey);
            if (item == null) return null;

            string json = item.Value is JsonElement element ? element.GetRawText() : item.Value?.ToString() ?? string.Empty;
            var record = JsonSerializer.Deserialize<MatchRecord>(json, JsonOptions);
            if (record == null) throw new Exception($"El registro de la partida '{matchId}' no se pudo leer.");
            return record;
        }

        public static async Task<MatchRecord> LoadAsync(IExecutionContext context, IGameApiClient gameApiClient, string matchId)
        {
            return await TryLoadAsync(context, gameApiClient, matchId)
                ?? throw new Exception($"La partida '{matchId}' no existe.");
        }

        public static Task SaveAsync(IExecutionContext context, IGameApiClient gameApiClient, MatchRecord record)
        {
            return gameApiClient.CloudSaveData.SetCustomItemAsync(
                context, context.ServiceToken, context.ProjectId, record.MatchId, new SetItemBody(StateItemKey, record));
        }

        /// <summary>
        /// Reconstruye la partida reproduciendo todas las jugadas con el mismo motor del cliente.
        /// Si una jugada guardada es invalida al reproducirla, el registro esta corrupto: error duro.
        /// </summary>
        public static GameLogic Replay(MatchRecord record)
        {
            return MatchReplay.Replay(record.GridSize, record.MaxCellLevel, record.MaxTurns, ToPoints(record.Moves, record.Moves.Count));
        }

        /// <summary>Reproduce todas las jugadas menos la ultima y aplica la ultima para obtener sus explosiones.</summary>
        private static List<GridPoint> LastMoveExplosions(MatchRecord record)
        {
            var before = MatchReplay.Replay(record.GridSize, record.MaxCellLevel, record.MaxTurns, ToPoints(record.Moves, record.Moves.Count - 1));
            var last = record.Moves[record.Moves.Count - 1];
            return before.PlacePoint(last.X, last.Y, before.currentPlayer).explosions;
        }

        private static List<GridPoint> ToPoints(List<MoveRecord> moves, int count)
        {
            var points = new List<GridPoint>(count);
            for (int i = 0; i < count; i++) points.Add(new GridPoint(moves[i].X, moves[i].Y));
            return points;
        }

        /// <summary>
        /// Construye la VISTA que recibe el cliente a partir del registro interno y la partida
        /// reproducida. <paramref name="callerPlayerId"/> sirve para decir "tu asiento" y "tu premio".
        /// </summary>
        public static MatchStateDto ToDto(MatchRecord record, GameLogic game, string callerPlayerId)
        {
            int n = record.GridSize;
            var owner = new int[n * n];
            var level = new int[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    owner[y * n + x] = game.owner[x, y];
                    level[y * n + x] = game.level[x, y];
                }

            string phase = record.Players.Count < 2 ? "waiting" : (game.IsOver ? "finished" : "playing");
            var me = record.Players.FirstOrDefault(p => p.PlayerId == callerPlayerId);

            var dto = new MatchStateDto
            {
                MatchId = record.MatchId,
                ProtocolVersion = GameProtocol.Version,
                GridSize = n,
                MaxCellLevel = record.MaxCellLevel,
                MaxTurns = record.MaxTurns,
                Phase = phase,
                Owner = owner,
                Level = level,
                CurrentSeat = phase == "playing" ? game.currentPlayer : 0,
                MySeat = me?.Seat ?? 0,
                TotalMoves = game.totalMoves,
                WinnerSeat = phase == "finished" ? game.FinalWinner : 0,
                Players = record.Players.Select(p => new MatchPlayerDto { PlayerId = p.PlayerId, Seat = p.Seat }).ToArray(),
            };

            if (record.Moves.Count > 0)
            {
                dto.LastMove = record.Moves[record.Moves.Count - 1];
                dto.LastMoveExplosions = LastMoveExplosions(record).Select(p => new[] { p.x, p.y }).ToArray();
            }

            if (callerPlayerId != null && record.Rewards.TryGetValue(callerPlayerId, out var grant))
            {
                dto.AwardedCoins = grant.Coins;
                dto.AwardedGems = grant.Gems;
            }

            return dto;
        }
    }
}
