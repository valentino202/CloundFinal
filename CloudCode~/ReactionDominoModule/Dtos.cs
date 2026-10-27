namespace ReactionDomino.CloudCode
{
    // ------------------------------------------------------------------------------------
    // REGISTRO INTERNO (lo que se guarda en Cloud Save). NUNCA se envia tal cual al cliente.
    // ------------------------------------------------------------------------------------

    /// <summary>Una jugada guardada. Solo (x, y): quien la hizo se deduce del orden.</summary>
    public class MoveRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class MatchPlayerRecord
    {
        public string PlayerId { get; set; }
        /// <summary>1 = rosado (el que crea la partida, mueve primero), 2 = naranja.</summary>
        public int Seat { get; set; }
    }

    /// <summary>
    /// Registro completo de una partida: configuracion, jugadores y la LISTA DE JUGADAS (solo se
    /// agregan al final). No guarda el tablero: se reconstruye reproduciendo las jugadas.
    /// </summary>
    public class MatchRecord
    {
        public string MatchId { get; set; }
        public int ProtocolVersion { get; set; }
        public int GridSize { get; set; }
        public int MaxCellLevel { get; set; }
        public int MaxTurns { get; set; }
        public List<MatchPlayerRecord> Players { get; set; } = new List<MatchPlayerRecord>();
        public List<MoveRecord> Moves { get; set; } = new List<MoveRecord>();
        public long CreatedAtUnixSeconds { get; set; }
        public long LastActivityAtUnixSeconds { get; set; }

        /// <summary>True cuando los premios de esta partida ya se acreditaron (se paga UNA sola vez).</summary>
        public bool RewardsSettled { get; set; }
        /// <summary>Premio acreditado a cada jugador (clave = playerId), para mostrarlo en la vista.</summary>
        public Dictionary<string, RewardGrantRecord> Rewards { get; set; } = new Dictionary<string, RewardGrantRecord>();
    }

    public class RewardGrantRecord
    {
        public int Coins { get; set; }
        public int Gems { get; set; }
    }

    // ------------------------------------------------------------------------------------
    // VISTA (lo que SI recibe el cliente). Distinta del registro interno a proposito.
    // En Reaction Domino no hay informacion oculta, pero mantener la separacion te obliga a
    // decidir, campo por campo, que sale del servidor.
    // ------------------------------------------------------------------------------------

    public class GameConfigResponse
    {
        public int ProtocolVersion { get; set; }
        public int GridSize { get; set; }
        public int MaxCellLevel { get; set; }
        public int MaxTurns { get; set; }
    }

    public class MatchPlayerDto
    {
        public string PlayerId { get; set; }
        public int Seat { get; set; }
    }

    public class MatchStateDto
    {
        public string MatchId { get; set; }
        public int ProtocolVersion { get; set; }
        public int GridSize { get; set; }
        public int MaxCellLevel { get; set; }
        public int MaxTurns { get; set; }

        /// <summary>"waiting" (falta el rival), "playing" o "finished".</summary>
        public string Phase { get; set; }

        /// <summary>Dueno de cada celda (0 neutral, 1, 2). Indice = y * GridSize + x.</summary>
        public int[] Owner { get; set; }
        /// <summary>Nivel de cada celda. Mismo indice que Owner.</summary>
        public int[] Level { get; set; }

        /// <summary>Asiento al que le toca jugar (1 o 2). 0 si la partida no esta en juego.</summary>
        public int CurrentSeat { get; set; }
        /// <summary>Asiento de quien pregunta (1 o 2).</summary>
        public int MySeat { get; set; }
        /// <summary>Numero de jugadas hechas.</summary>
        public int TotalMoves { get; set; }
        /// <summary>Solo si Phase = "finished": 1, 2 o 0 si fue empate.</summary>
        public int WinnerSeat { get; set; }

        public MoveRecord LastMove { get; set; }
        /// <summary>Explosiones (en orden) de la ultima jugada, para que ambos clientes animen lo mismo.</summary>
        public int[][] LastMoveExplosions { get; set; } = Array.Empty<int[]>();

        public MatchPlayerDto[] Players { get; set; } = Array.Empty<MatchPlayerDto>();

        /// <summary>Premio que recibio quien pregunta al terminar la partida (0 si aun no terminó).</summary>
        public int AwardedCoins { get; set; }
        public int AwardedGems { get; set; }
    }

    /// <summary>
    /// Respuesta de <c>PlayMove</c>. Un rechazo (fuera de turno, celda del rival...) NO es una
    /// excepcion: viene como <see cref="Outcome"/> distinto de "ok" y el estado queda sin cambios.
    /// </summary>
    public class PlayMoveResponse
    {
        /// <summary>"ok" o el nombre de un MoveOutcome en minusculas: "not_your_turn", "out_of_range", ...</summary>
        public string Outcome { get; set; }
        public MatchStateDto State { get; set; }
    }
}
