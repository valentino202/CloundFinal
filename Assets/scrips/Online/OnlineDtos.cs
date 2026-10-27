using System;

namespace ReactionDomino.Online
{
    // Contratos con el modulo de Cloud Code (CloudCode~/ReactionDominoModule/Dtos.cs).
    // Si cambias algo ahi (campos, nombres, significado de un outcome), cambialo aqui TAMBIEN y sube
    // la version del protocolo en los dos lados (GameProtocol.Version y OnlineMatchService.ProtocolVersion).
    //
    // El servidor escribe los nombres en PascalCase (MatchId) y la deserializacion de Unity no distingue
    // mayusculas de minusculas, por eso aqui los campos van en camelCase (matchId).

    [Serializable]
    public class GameConfigDto
    {
        public int protocolVersion;
        public int gridSize;
        public int maxCellLevel;
        public int maxTurns;
    }

    [Serializable]
    public class MatchPlayerDto
    {
        public string playerId;
        public int seat;
    }

    [Serializable]
    public class MoveDto
    {
        public int x;
        public int y;
    }

    /// <summary>La VISTA de la partida que da el servidor. El cliente solo la pinta; nunca la modifica.</summary>
    [Serializable]
    public class MatchStateDto
    {
        public string matchId;
        public int protocolVersion;
        public int gridSize;
        public int maxCellLevel;
        public int maxTurns;

        /// <summary>"waiting" (falta el rival), "playing" o "finished".</summary>
        public string phase;

        /// <summary>Dueno de cada celda (0 neutral, 1, 2). Indice = y * gridSize + x.</summary>
        public int[] owner;
        /// <summary>Nivel de cada celda. Mismo indice que owner.</summary>
        public int[] level;

        /// <summary>Asiento al que le toca (1 o 2). 0 si la partida no esta en juego.</summary>
        public int currentSeat;
        /// <summary>Tu asiento (1 = rosado, 2 = naranja).</summary>
        public int mySeat;
        public int totalMoves;
        /// <summary>Solo si phase = "finished": 1, 2 o 0 si fue empate.</summary>
        public int winnerSeat;

        public MoveDto lastMove;
        /// <summary>Explosiones (en orden) de la ultima jugada, como pares [x, y]. Sirven para animar.</summary>
        public int[][] lastMoveExplosions;

        public MatchPlayerDto[] players;

        /// <summary>Premio que recibiste al terminar la partida (lo decide el servidor).</summary>
        public int awardedCoins;
        public int awardedGems;

        public bool IsMyTurn => phase == "playing" && currentSeat == mySeat && mySeat != 0;
        public int OwnerAt(int x, int y) => owner[y * gridSize + x];
        public int LevelAt(int x, int y) => level[y * gridSize + x];
    }

    /// <summary>
    /// Respuesta de PlayMove. Un rechazo NO es una excepcion: outcome distinto de "ok"
    /// ("not_your_turn", "out_of_range", "cell_owned_by_rival", "match_finished"...) y el estado no cambia.
    /// </summary>
    [Serializable]
    public class PlayMoveResponseDto
    {
        public string outcome;
        public MatchStateDto state;

        public bool Accepted => outcome == "ok";
    }
}
