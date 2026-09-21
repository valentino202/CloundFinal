using UnityEngine;

/// <summary>
/// Agente 1: Oponente Bot local SIN MonoBehaviour (clase pura).
/// Uso: var bot = new BotAgent(BotAgent.Difficulty.Medio, botPlayerId: 2);
/// gridSize y maxLevel se pasan por parametro (vienen de Remote Config).
/// owner[x,y]: 0 = vacia, 1 = jugador1, 2 = jugador2/bot
/// level[x,y]: nivel actual de la celda.
/// </summary>
public class BotAgent
{
    public enum Difficulty { Facil = 0, Medio = 1, Dificil = 2 }

    public Difficulty difficulty;
    public int botPlayerId;

    private readonly System.Random rng = new System.Random();

    public BotAgent(Difficulty difficulty = Difficulty.Medio, int botPlayerId = 2)
    {
        this.difficulty = difficulty;
        this.botPlayerId = botPlayerId;
    }

    public void ApplySuggestedDifficulty(Difficulty suggested)
    {
        difficulty = suggested;
    }

    /// <summary>Llamar cuando es turno del bot. Devuelve celda elegida.</summary>
    public Vector2Int ChooseMove(int[,] owner, int[,] level, int gridSize, int maxLevel)
    {
        return difficulty switch
        {
            Difficulty.Facil => ChooseRandom(owner, gridSize),
            Difficulty.Medio => ChooseHeuristic(owner, level, gridSize, maxLevel),
            Difficulty.Dificil => ChooseMinimaxLite(owner, level, gridSize, maxLevel),
            _ => ChooseRandom(owner, gridSize),
        };
    }

    private Vector2Int ChooseRandom(int[,] owner, int gridSize)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            int x = rng.Next(0, gridSize);
            int y = rng.Next(0, gridSize);
            if (owner[x, y] == 0 || owner[x, y] == botPlayerId)
                return new Vector2Int(x, y);
        }
        return new Vector2Int(0, 0);
    }

    private Vector2Int ChooseHeuristic(int[,] owner, int[,] level, int gridSize, int maxLevel)
    {
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                if (owner[x, y] == botPlayerId && level[x, y] + 1 >= maxLevel)
                    return new Vector2Int(x, y);

        int rival = botPlayerId == 1 ? 2 : 1;
        Vector2Int best = new Vector2Int(-1, -1);
        int bestScore = int.MinValue;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (owner[x, y] != 0 && owner[x, y] != botPlayerId) continue;
                int score = level[x, y];
                if (IsAdjacentTo(owner, gridSize, x, y, rival)) score += 3;
                if (IsAdjacentTo(owner, gridSize, x, y, botPlayerId)) score += 1;
                if (owner[x, y] == 0) score += 1;
                if (score > bestScore) { bestScore = score; best = new Vector2Int(x, y); }
            }
        }
        if (best.x >= 0) return best;
        return ChooseRandom(owner, gridSize);
    }

    private Vector2Int ChooseMinimaxLite(int[,] owner, int[,] level, int gridSize, int maxLevel)
    {
        Vector2Int best = new Vector2Int(-1, -1);
        int bestScore = int.MinValue;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (owner[x, y] != 0 && owner[x, y] != botPlayerId) continue;
                int score = EvaluateMove(owner, level, gridSize, maxLevel, x, y);
                if (score > bestScore) { bestScore = score; best = new Vector2Int(x, y); }
            }
        }
        return best.x >= 0 ? best : ChooseRandom(owner, gridSize);
    }

    private int EvaluateMove(int[,] owner, int[,] level, int gridSize, int maxLevel, int x, int y)
    {
        int score = level[x, y] * 2;
        if (owner[x, y] == botPlayerId && level[x, y] + 1 >= maxLevel) score += 50;
        score += CountAdjacent(owner, gridSize, x, y, botPlayerId) * 4;
        score += CountAdjacent(owner, gridSize, x, y, 0) * 1;
        int rival = botPlayerId == 1 ? 2 : 1;
        score -= CountAdjacent(owner, gridSize, x, y, rival) * 2;
        score += rng.Next(0, 3);
        return score;
    }

    private bool IsAdjacentTo(int[,] owner, int gridSize, int x, int y, int value)
        => CountAdjacent(owner, gridSize, x, y, value) > 0;

    private int CountAdjacent(int[,] owner, int gridSize, int x, int y, int value)
    {
        int c = 0;
        if (x > 0 && owner[x - 1, y] == value) c++;
        if (x < gridSize - 1 && owner[x + 1, y] == value) c++;
        if (y > 0 && owner[x, y - 1] == value) c++;
        if (y < gridSize - 1 && owner[x, y + 1] == value) c++;
        return c;
    }
}
