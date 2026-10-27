using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ReactionDomino.Core;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace ReactionDomino.CloudCode
{
    /// <summary>
    /// Las funciones que el cliente puede llamar. TODAS las reglas pasan por el mismo Core que usa
    /// el juego (<see cref="GameLogic"/>): el cliente nunca decide quien gano ni cuanto premio le toca.
    ///
    /// El cliente envia INTENCIONES ("pongo un punto en (x, y)"). El servidor:
    ///   1. identifica al jugador por <c>context.PlayerId</c> (el token), nunca por un dato del cliente;
    ///   2. valida con las reglas; 3. guarda; 4. avisa al rival por push; 5. devuelve la vista.
    ///
    /// Que NO esta hecho todavia (son tareas del alumno, ver docs/TAREAS.md): plazo por turno y
    /// abandono (Tarea 6), leer la configuracion de Remote Config, el cliente de Unity (Tarea 5) y
    /// MMR/leaderboard.
    /// </summary>
    public class GameFunctions
    {
        private const string MatchUpdatedMessageType = "match_updated";
        private static readonly Regex MatchCodePattern = new Regex("^[A-Z0-9]{4,12}$", RegexOptions.Compiled);

        private readonly ILogger<GameFunctions> _logger;

        public GameFunctions(ILogger<GameFunctions> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Entrega la configuracion del juego y la version del protocolo. El cliente NO lee Remote
        /// Config por su cuenta: asi cliente y servidor usan siempre las mismas reglas.
        /// </summary>
        [CloudCodeFunction("GetGameConfig")]
        public Task<GameConfigResponse> GetGameConfig(IExecutionContext context)
        {
            return Task.FromResult(new GameConfigResponse
            {
                ProtocolVersion = GameProtocol.Version,
                GridSize = GameDefaults.GridSize,
                MaxCellLevel = GameDefaults.MaxCellLevel,
                MaxTurns = GameDefaults.MaxTurns,
            });
        }

        /// <summary>
        /// Crea una partida o se une a ella, segun el codigo. Es IDEMPOTENTE:
        ///   - el primero que llama con un codigo crea la partida y es el asiento 1 (rosado, mueve primero);
        ///   - el segundo se une como asiento 2 (y el creador recibe un push);
        ///   - si el mismo jugador llama otra vez (un reintento), recibe la misma partida sin cambios;
        ///   - un tercero recibe un error: la partida esta llena.
        /// El cliente nunca elige su asiento. Luego se puede reemplazar el codigo por el ticket de Matchmaker.
        ///
        /// Limitacion conocida: si dos jugadores llaman a la vez con un codigo NUEVO podrian crear dos
        /// registros (no hay transacciones en Cloud Save). Es poco probable con un codigo compartido a mano.
        /// </summary>
        [CloudCodeFunction("CreateMatch")]
        public async Task<MatchStateDto> CreateMatch(
            IExecutionContext context, IGameApiClient gameApiClient, IPushClient pushClient, string matchCode)
        {
            try
            {
                string code = (matchCode ?? string.Empty).Trim().ToUpperInvariant();
                if (!MatchCodePattern.IsMatch(code))
                    throw new Exception("El codigo de partida debe tener entre 4 y 12 letras o numeros.");

                string matchId = "rd-" + code;
                var record = await MatchStateStore.TryLoadAsync(context, gameApiClient, matchId);
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (record == null)
                {
                    record = new MatchRecord
                    {
                        MatchId = matchId,
                        ProtocolVersion = GameProtocol.Version,
                        GridSize = GameDefaults.GridSize,
                        MaxCellLevel = GameDefaults.MaxCellLevel,
                        MaxTurns = GameDefaults.MaxTurns,
                        CreatedAtUnixSeconds = now,
                        LastActivityAtUnixSeconds = now,
                    };
                    record.Players.Add(new MatchPlayerRecord { PlayerId = context.PlayerId, Seat = 1 });
                    await MatchStateStore.SaveAsync(context, gameApiClient, record);
                }
                else if (!record.Players.Any(p => p.PlayerId == context.PlayerId))
                {
                    if (record.Players.Count >= 2)
                        throw new Exception("La partida ya esta completa.");

                    record.Players.Add(new MatchPlayerRecord { PlayerId = context.PlayerId, Seat = 2 });
                    record.LastActivityAtUnixSeconds = now;
                    await MatchStateStore.SaveAsync(context, gameApiClient, record);

                    // Avisa al creador de que ya hay rival; el vuelve a pedir el estado.
                    await NotifyRivalAsync(context, pushClient, record.MatchId, record.Players[0].PlayerId);
                }
                // else: mismo jugador reintentando -> se devuelve la misma partida.

                return MatchStateStore.ToDto(record, MatchStateStore.Replay(record), context.PlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateMatch fallo para el codigo {MatchCode}.", matchCode);
                throw new Exception($"No se pudo crear la partida: {ex.Message}");
            }
        }

        /// <summary>
        /// Valida y aplica una jugada. Orden de las comprobaciones:
        ///   existe la partida -> hay dos jugadores -> quien llama es jugador -> la partida no termino
        ///   -> es su turno -> la jugada es legal (todo esto con el motor) -> se guarda
        ///   -> si termino, se liquidan los premios UNA vez -> push al rival -> vista.
        ///
        /// Un rechazo de las reglas (fuera de turno, celda del rival, fuera del tablero) NO lanza
        /// excepcion: devuelve Outcome distinto de "ok" y el estado queda igual.
        /// </summary>
        [CloudCodeFunction("PlayMove")]
        public async Task<PlayMoveResponse> PlayMove(
            IExecutionContext context, IGameApiClient gameApiClient, IPushClient pushClient, string matchId, int x, int y)
        {
            try
            {
                var record = await MatchStateStore.LoadAsync(context, gameApiClient, matchId);
                if (record.Players.Count < 2)
                    throw new Exception("La partida esta esperando al rival.");

                var caller = record.Players.FirstOrDefault(p => p.PlayerId == context.PlayerId)
                    ?? throw new Exception("No eres jugador de esta partida.");

                // El asiento sale del registro (del token), nunca de un dato que mande el cliente.
                var game = MatchStateStore.Replay(record);
                var result = game.PlacePoint(x, y, caller.Seat);
                if (!result.success)
                {
                    // Estado sin cambios: `game` ya estaba reproducido y PlacePoint no modifica nada al rechazar.
                    return new PlayMoveResponse
                    {
                        Outcome = OutcomeName(result.outcome),
                        State = MatchStateStore.ToDto(record, game, context.PlayerId),
                    };
                }

                record.Moves.Add(new MoveRecord { X = x, Y = y });
                record.LastActivityAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await MatchStateStore.SaveAsync(context, gameApiClient, record);

                if (game.IsOver)
                    await SettleRewardsAsync(context, gameApiClient, record, game);

                var rival = record.Players.First(p => p.PlayerId != context.PlayerId);
                await NotifyRivalAsync(context, pushClient, record.MatchId, rival.PlayerId);

                return new PlayMoveResponse { Outcome = "ok", State = MatchStateStore.ToDto(record, game, context.PlayerId) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlayMove fallo para matchId={MatchId}, jugada=({X},{Y}).", matchId, x, y);
                throw new Exception($"No se pudo jugar: {ex.Message}");
            }
        }

        /// <summary>
        /// Devuelve la vista actual de la partida. Sirve para reconectar y para refrescar cuando llega
        /// un push. Si la partida ya termino pero sus premios quedaron sin liquidar (por ejemplo, una
        /// caida a mitad de PlayMove), los liquida aqui: la marca "ya liquidado" evita pagar dos veces.
        /// </summary>
        [CloudCodeFunction("GetMatchState")]
        public async Task<MatchStateDto> GetMatchState(IExecutionContext context, IGameApiClient gameApiClient, string matchId)
        {
            try
            {
                var record = await MatchStateStore.LoadAsync(context, gameApiClient, matchId);
                if (!record.Players.Any(p => p.PlayerId == context.PlayerId))
                    throw new Exception("No eres jugador de esta partida.");

                var game = MatchStateStore.Replay(record);
                if (game.IsOver && !record.RewardsSettled && record.Players.Count == 2)
                    await SettleRewardsAsync(context, gameApiClient, record, game);

                return MatchStateStore.ToDto(record, game, context.PlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMatchState fallo para matchId={MatchId}.", matchId);
                throw new Exception($"No se pudo leer la partida: {ex.Message}");
            }
        }

        /// <summary>
        /// Calcula y acredita el premio a los DOS jugadores, una sola vez, en datos PROTECTED.
        /// Dos barreras contra el doble pago: PlayMove rechaza jugadas en una partida terminada, y cada
        /// perfil recuerda las partidas que ya acredito (<see cref="PlayerRewardStore.CreditOnceAsync"/>).
        /// </summary>
        private async Task SettleRewardsAsync(IExecutionContext context, IGameApiClient gameApiClient, MatchRecord record, GameLogic game)
        {
            if (record.RewardsSettled) return;

            int winner = game.FinalWinner; // 1, 2 o 0 (empate)
            foreach (var player in record.Players)
            {
                var reward = RewardRules.For(player.Seat, winner);
                int outcome = winner == 0 ? 0 : (winner == player.Seat ? 1 : -1);
                await PlayerRewardStore.CreditOnceAsync(
                    context, gameApiClient, player.PlayerId, record.MatchId, reward.coins, reward.gems, outcome);
                record.Rewards[player.PlayerId] = new RewardGrantRecord { Coins = reward.coins, Gems = reward.gems };
            }

            record.RewardsSettled = true;
            await MatchStateStore.SaveAsync(context, gameApiClient, record);
        }

        /// <summary>
        /// Push (Wire) al rival: "algo cambio, vuelve a pedir el estado". El payload es liviano
        /// {type, matchId} y NUNCA lleva el estado ni la jugada: el rival no confia en el mensaje, solo
        /// lo usa como senal. Si el push falla NO falla la jugada (ya quedo guardada): el sondeo de
        /// respaldo del cliente lo cubre.
        /// </summary>
        private async Task NotifyRivalAsync(IExecutionContext context, IPushClient pushClient, string matchId, string rivalPlayerId)
        {
            try
            {
                string payload = JsonSerializer.Serialize(new { type = MatchUpdatedMessageType, matchId });
                await pushClient.SendPlayerMessageAsync(context, payload, MatchUpdatedMessageType, rivalPlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push al rival {RivalPlayerId} fallo para la partida {MatchId}; lo cubre el sondeo del cliente.", rivalPlayerId, matchId);
            }
        }

        private static string OutcomeName(MoveOutcome outcome)
        {
            switch (outcome)
            {
                case MoveOutcome.Ok: return "ok";
                case MoveOutcome.MatchFinished: return "match_finished";
                case MoveOutcome.InvalidPlayer: return "invalid_player";
                case MoveOutcome.NotYourTurn: return "not_your_turn";
                case MoveOutcome.OutOfRange: return "out_of_range";
                case MoveOutcome.CellOwnedByRival: return "cell_owned_by_rival";
                default: return outcome.ToString().ToLowerInvariant();
            }
        }
    }
}
