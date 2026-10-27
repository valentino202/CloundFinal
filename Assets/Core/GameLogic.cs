using System;
using System.Collections.Generic;

namespace ReactionDomino.Core
{
    /// <summary>
    /// Logica core de Reaccion Domino (GDD 2): clase pura, SIN MonoBehaviour y SIN UnityEngine.
    ///
    /// Esto es lo importante de la capa Core: como no depende de Unity, el MISMO archivo lo compila
    /// el juego (cliente) y el modulo de Cloud Code (servidor). Las reglas no se escriben dos veces,
    /// asi que cliente y servidor nunca pueden estar en desacuerdo sobre una jugada.
    ///
    /// Uso:
    ///   var game = new GameLogic(gridSize: 6, maxLevel: 4, maxTurns: 30);
    ///   var r = game.PlacePoint(x, y, player); // player 1 o 2
    ///   if (r.gameOver) ... r.winner (0 = empate, 1 / 2)
    /// owner: 0 = neutral, 1 = P1 rosado, 2 = P2 naranja.
    ///
    /// DETERMINISTA: no hay azar. La misma lista de jugadas da siempre el mismo estado. Por eso el
    /// servidor guarda solo la lista de jugadas y la reproduce (ver <see cref="MatchReplay"/>).
    /// </summary>
    public class GameLogic
    {
        public readonly int gridSize;
        public readonly int maxLevel;
        public readonly int maxTurns;

        public int[,] owner;
        public int[,] level;

        public int currentPlayer = 1;
        public int totalMoves = 0;
        public int movesP1 = 0;
        public int movesP2 = 0;

        /// <summary>True cuando la partida ya termino. Despues de eso no se aceptan mas jugadas.</summary>
        public bool IsOver { get; private set; }

        /// <summary>Ganador final cuando <see cref="IsOver"/> es true: 1, 2 o 0 si fue empate.</summary>
        public int FinalWinner { get; private set; }

        public struct MoveResult
        {
            public bool success;
            /// <summary>Codigo del resultado (para que el cliente sepa que mostrar). Ok si se aplico.</summary>
            public MoveOutcome outcome;
            public string error;
            public List<GridPoint> explosions; // en orden, para animar + "CADENA xN"
            public bool gameOver;
            public int winner; // 0 = empate/pendiente (ver gameOver), 1 / 2
        }

        public GameLogic(int gridSize = 6, int maxLevel = 4, int maxTurns = 30)
        {
            this.gridSize = Math.Max(3, gridSize);
            this.maxLevel = Math.Max(2, maxLevel);
            // maxTurns <= 0 = infinito: solo se gana eliminando al rival.
            this.maxTurns = maxTurns;
            owner = new int[this.gridSize, this.gridSize];
            level = new int[this.gridSize, this.gridSize];
        }

        /// <summary>Sin limite de turnos: se juega hasta eliminar al enemigo.</summary>
        public bool IsInfinite => maxTurns <= 0;

        public void Reset()
        {
            owner = new int[gridSize, gridSize];
            level = new int[gridSize, gridSize];
            currentPlayer = 1;
            totalMoves = 0;
            movesP1 = 0;
            movesP2 = 0;
            IsOver = false;
            FinalWinner = 0;
        }

        /// <summary>
        /// Dice si una jugada seria legal y, si no, por que. No cambia nada.
        /// Es lo primero que consulta el servidor antes de aplicar una jugada.
        /// </summary>
        public MoveOutcome Validate(int x, int y, int player)
        {
            if (IsOver) return MoveOutcome.MatchFinished;
            if (player != 1 && player != 2) return MoveOutcome.InvalidPlayer;
            if (player != currentPlayer) return MoveOutcome.NotYourTurn;
            if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return MoveOutcome.OutOfRange;
            if (owner[x, y] != 0 && owner[x, y] != player) return MoveOutcome.CellOwnedByRival;
            return MoveOutcome.Ok;
        }

        public bool IsValidMove(int x, int y, int player)
            => Validate(x, y, player) == MoveOutcome.Ok;

        public MoveResult PlacePoint(int x, int y, int player)
        {
            var res = new MoveResult { explosions = new List<GridPoint>() };
            var check = Validate(x, y, player);
            if (check != MoveOutcome.Ok)
            {
                res.success = false;
                res.outcome = check;
                res.error = ErrorText(check);
                return res;
            }

            // Colocar 1 punto
            owner[x, y] = player;
            level[x, y]++;

            if (level[x, y] >= maxLevel)
                ResolveExplosions(x, y, player, res.explosions);

            totalMoves++;
            if (player == 1) movesP1++; else movesP2++;

            res.success = true;
            res.outcome = MoveOutcome.Ok;
            res.winner = GetWinner();
            res.gameOver = res.winner != 0 || (!IsInfinite && totalMoves >= maxTurns);
            if (res.gameOver && res.winner == 0)
                res.winner = WinnerByCells(); // desempate a maxTurns (0 = empate real)

            if (res.gameOver)
            {
                IsOver = true;
                FinalWinner = res.winner;
            }
            else
            {
                currentPlayer = currentPlayer == 1 ? 2 : 1;
            }

            return res;
        }

        private static string ErrorText(MoveOutcome outcome)
        {
            switch (outcome)
            {
                case MoveOutcome.MatchFinished: return "La partida ya termino.";
                case MoveOutcome.InvalidPlayer: return "Jugador invalido.";
                case MoveOutcome.NotYourTurn: return "No es tu turno.";
                case MoveOutcome.OutOfRange: return "Celda fuera del tablero.";
                case MoveOutcome.CellOwnedByRival: return "Esa celda es del rival.";
                default: return "Jugada ilegal.";
            }
        }

        private void ResolveExplosions(int sx, int sy, int attacker, List<GridPoint> log)
        {
            var queue = new Queue<GridPoint>();
            queue.Enqueue(new GridPoint(sx, sy));
            int guard = gridSize * gridSize * 4; // anti bucle infinito

            while (queue.Count > 0 && guard-- > 0)
            {
                var c = queue.Dequeue();
                if (level[c.x, c.y] < maxLevel) continue;

                // Explota: pasa a Nivel 0 vacia (neutral)
                log.Add(c);
                owner[c.x, c.y] = 0;
                level[c.x, c.y] = 0;

                // Lanza 1 punto a las 4 adyacentes: conquista + suma +1
                foreach (var n in Neighbours(c.x, c.y))
                {
                    owner[n.x, n.y] = attacker;
                    level[n.x, n.y]++;
                    if (level[n.x, n.y] >= maxLevel)
                        queue.Enqueue(n);
                }
            }
        }

        private IEnumerable<GridPoint> Neighbours(int x, int y)
        {
            if (x > 0) yield return new GridPoint(x - 1, y);
            if (x < gridSize - 1) yield return new GridPoint(x + 1, y);
            if (y > 0) yield return new GridPoint(x, y - 1);
            if (y < gridSize - 1) yield return new GridPoint(x, y + 1);
        }

        public int CountCells(int player)
        {
            int c = 0;
            for (int x = 0; x < gridSize; x++)
                for (int y = 0; y < gridSize; y++)
                    if (owner[x, y] == player) c++;
            return c;
        }

        // Eliminacion solo tras 1 ronda completa (ambos movieron al menos 1 vez).
        public int GetWinner()
        {
            if (movesP1 == 0 || movesP2 == 0) return 0;
            int c1 = CountCells(1), c2 = CountCells(2);
            if (c2 == 0 && c1 > 0) return 1;
            if (c1 == 0 && c2 > 0) return 2;
            return 0;
        }

        // Desempate a maxTurns: mas casillas gana, igual = empate.
        public int WinnerByCells()
        {
            int c1 = CountCells(1), c2 = CountCells(2);
            if (c1 > c2) return 1;
            if (c2 > c1) return 2;
            return 0;
        }
    }
}
