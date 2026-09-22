using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logica core de Reaccion Domino (GDD 2) SIN MonoBehaviour (clase pura).
/// Uso:
///   var game = new GameLogic(gridSize: 6, maxLevel: 4, maxTurns: 30);
///   var r = game.PlacePoint(x, y, player); // player 1 o 2
///   if (r.gameOver) ... game.Winner (0 = empate, 1 / 2)
/// owner: 0 = neutral, 1 = P1 rosado, 2 = P2 naranja.
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

    public struct MoveResult
    {
        public bool success;
        public string error;
        public List<Vector2Int> explosions; // en orden, para animar + "CADENA xN"
        public bool gameOver;
        public int winner; // 0 = empate/pendiente (ver gameOver), 1 / 2
    }

    public GameLogic(int gridSize = 6, int maxLevel = 4, int maxTurns = 30)
    {
        this.gridSize = Mathf.Max(3, gridSize);
        this.maxLevel = Mathf.Max(2, maxLevel);
        this.maxTurns = Mathf.Max(4, maxTurns);
        owner = new int[this.gridSize, this.gridSize];
        level = new int[this.gridSize, this.gridSize];
    }

    public void Reset()
    {
        owner = new int[gridSize, gridSize];
        level = new int[gridSize, gridSize];
        currentPlayer = 1;
        totalMoves = 0;
        movesP1 = 0;
        movesP2 = 0;
    }

    public bool IsValidMove(int x, int y, int player)
    {
        if (player != 1 && player != 2) return false;
        if (player != currentPlayer) return false;
        if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return false;
        return owner[x, y] == 0 || owner[x, y] == player;
    }

    public MoveResult PlacePoint(int x, int y, int player)
    {
        var res = new MoveResult { explosions = new List<Vector2Int>() };
        if (!IsValidMove(x, y, player))
        {
            res.success = false;
            res.error = "Jugada ilegal: celda rival, fuera de rango o turno incorrecto.";
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
        res.winner = GetWinner();
        res.gameOver = res.winner != 0 || totalMoves >= maxTurns;
        if (res.gameOver && res.winner == 0)
            res.winner = WinnerByCells(); // desempate a 30 turnos (0 = empate real)

        if (!res.gameOver)
            currentPlayer = currentPlayer == 1 ? 2 : 1;

        return res;
    }

    private void ResolveExplosions(int sx, int sy, int attacker, List<Vector2Int> log)
    {
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(sx, sy));
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

    private IEnumerable<Vector2Int> Neighbours(int x, int y)
    {
        if (x > 0) yield return new Vector2Int(x - 1, y);
        if (x < gridSize - 1) yield return new Vector2Int(x + 1, y);
        if (y > 0) yield return new Vector2Int(x, y - 1);
        if (y < gridSize - 1) yield return new Vector2Int(x, y + 1);
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
