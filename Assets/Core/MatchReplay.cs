using System;
using System.Collections.Generic;

namespace ReactionDomino.Core
{
    /// <summary>
    /// Reconstruye una partida a partir de su lista de jugadas.
    ///
    /// Por que existe: el servidor NO guarda una foto del tablero, guarda solo la lista de jugadas
    /// (solo se le agregan jugadas al final). En cada llamada la reproduce con un GameLogic nuevo y
    /// obtiene tablero, turno y resultado. Eso solo funciona porque las reglas son deterministas, y
    /// garantiza que el servidor nunca se desincroniza de sus propias reglas.
    ///
    /// El jugador que mueve NO viene en la lista: se deduce del orden (<c>game.currentPlayer</c>).
    /// </summary>
    public static class MatchReplay
    {
        /// <summary>
        /// Reproduce todas las jugadas. Devuelve false si alguna es invalida al reproducirla, que
        /// significa que los datos guardados estan corruptos (<paramref name="failedIndex"/> dice cual).
        /// </summary>
        public static bool TryReplay(
            int gridSize, int maxLevel, int maxTurns, IReadOnlyList<GridPoint> moves,
            out GameLogic game, out int failedIndex)
        {
            game = new GameLogic(gridSize, maxLevel, maxTurns);
            failedIndex = -1;
            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                var r = game.PlacePoint(m.x, m.y, game.currentPlayer);
                if (!r.success)
                {
                    failedIndex = i;
                    return false;
                }
            }
            return true;
        }

        /// <summary>Igual que <see cref="TryReplay"/> pero lanza una excepcion si el registro esta corrupto.</summary>
        public static GameLogic Replay(int gridSize, int maxLevel, int maxTurns, IReadOnlyList<GridPoint> moves)
        {
            if (!TryReplay(gridSize, maxLevel, maxTurns, moves, out var game, out int failedIndex))
                throw new InvalidOperationException(
                    "Registro de partida corrupto: la jugada " + failedIndex + " " + moves[failedIndex] + " es invalida al reproducirla.");
            return game;
        }
    }
}
