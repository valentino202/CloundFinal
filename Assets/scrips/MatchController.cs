using UnityEngine;

/// <summary>
/// Orquesta la partida SIN MonoBehaviour (clase pura).
/// Junta GameLogic + BotAgent + dificultad. La vista (GameManager)
/// solo pinta y reenvia clics. Asi la regla es testeable sin escena.
/// </summary>
public class MatchController
{
    public readonly GameLogic game;
    public readonly BotAgent bot;
    public readonly int humanPlayer = 1;
    public int botPlayerId => bot.botPlayerId;

    public MatchController(int gridSize, int maxLevel, int maxTurns, BotAgent.Difficulty difficulty)
    {
        game = new GameLogic(gridSize, maxLevel, maxTurns);
        bot = new BotAgent(difficulty, botPlayerId: 2);
    }

    public GameLogic.MoveResult PlayHuman(int x, int y)
    {
        lastMove = new Vector2Int(x, y);
        return game.PlacePoint(x, y, humanPlayer);
    }

    public GameLogic.MoveResult PlayBot()
    {
        var m = bot.ChooseMove(game.owner, game.level, game.gridSize, game.maxLevel);
        lastMove = m;
        return game.PlacePoint(m.x, m.y, bot.botPlayerId);
    }

    public Vector2Int lastMove { get; private set; }

    public void Reset() => game.Reset();
}
