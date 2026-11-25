using UnityEngine;

/// <summary>
/// PASO 3 del PvP: prueba de lógica para 2 jugadores en la misma máquina
/// (sin red, sin cuentas, sin escena especial).
///
/// Crea un GameLogic y juega una partida COMPLETA alternando
/// jugador 1 → jugador 2 → jugador 1..., eligiendo la jugada con el BotAgent
/// para AMBOS (como si fueran dos humanos pensando). Imprime en consola
/// cada jugada y el resultado final.
///
/// OBJETIVO: confirmar que GameLogic.PlacePoint se comporta igual sin importar
/// quién mueve (la lógica es indiferente al jugador).
///
/// USO: adjuntar a cualquier GameObject en cualquier escena, dar Play y mirar
/// la consola. No toca GameManager, MatchController ni la escena del juego.
/// </summary>
public class Paso3PruebaDosJugadores : MonoBehaviour
{
    [Header("Configuracion de la prueba")]
    [Tooltip("Tamaño del tablero (igual que el juego real: 6).")]
    public int gridSize = 6;
    [Tooltip("Nivel maximo de celda (igual que el juego real: 4).")]
    public int maxLevel = 4;
    [Tooltip("Limite de turnos (30 = normal). Pon 0 para infinito.")]
    public int maxTurns = 30;
    [Tooltip("Dificultad del BotAgent para ambos jugadores.")]
    public BotAgent.Difficulty dificultad = BotAgent.Difficulty.Medio;
    [Tooltip("Si esta activo, usa BotAgent para ambos (mas realista). Si no, elige celda legal al azar.")]
    public bool usarBotAgent = true;
    [Tooltip("Tope de seguridad para no colgarse en modo infinito.")]
    public int topeMovimientos = 200;

    void Start()
    {
        Debug.Log($"[Paso3] Iniciando prueba: tablero {gridSize}x{gridSize}, nivel {maxLevel}, " +
                  $"turnos {(maxTurns <= 0 ? "infinito" : maxTurns.ToString())}, " +
                  $"modo {(usarBotAgent ? "BotAgent para ambos" : "azar")}.");

        var game = new GameLogic(gridSize, maxLevel, maxTurns);

        // Dos "decisores": uno para jugador 1 y otro para jugador 2.
        // El BotAgent usa botPlayerId internamente, asi que le pasamos 1 y 2.
        BotAgent bot1 = usarBotAgent ? new BotAgent(dificultad, botPlayerId: 1) : null;
        BotAgent bot2 = usarBotAgent ? new BotAgent(dificultad, botPlayerId: 2) : null;

        int movimientos = 0;
        while (movimientos < topeMovimientos)
        {
            int jugador = game.currentPlayer; // 1 o 2
            Vector2Int celda;

            if (usarBotAgent)
            {
                var bot = (jugador == 1) ? bot1 : bot2;
                celda = bot.ChooseMove(game.owner, game.level, game.gridSize, game.maxLevel);
            }
            else
            {
                celda = ElegirCeldaLegalAlAzar(game, jugador);
                if (celda.x < 0) // no hay jugada legal
                {
                    Debug.LogWarning($"[Paso3] Jugador {jugador} sin jugadas legales en turno {movimientos + 1}. Fin.");
                    break;
                }
            }

            var res = game.PlacePoint(celda.x, celda.y, jugador);
            movimientos++;

            if (!res.success)
            {
                Debug.LogError($"[Paso3] Jugada ILEGAL jugador {jugador} en ({celda.x},{celda.y}): {res.error}");
                break;
            }

            string explosiones = (res.explosions != null && res.explosions.Count > 0)
                ? $"explosiones={res.explosions.Count}" : "sin explosion";
            Debug.Log($"[Paso3] Turno {movimientos}: jugador {jugador} -> ({celda.x},{celda.y}) | {explosiones}" +
                      (res.gameOver ? " | GAME OVER" : ""));

            if (res.gameOver)
            {
                int c1 = game.CountCells(1);
                int c2 = game.CountCells(2);
                string ganadorTxt = res.winner == 0 ? "EMPATE" : $"jugador {res.winner}";
                Debug.Log($"[Paso3] FIN DE PARTIDA: ganador = {ganadorTxt} | " +
                          $"casillas P1={c1} P2={c2} | turnos totales={game.totalMoves} " +
                          $"(P1={game.movesP1}, P2={game.movesP2})");
                return;
            }
        }

        if (movimientos >= topeMovimientos)
            Debug.LogWarning($"[Paso3] Se alcanzo el tope de {topeMovimientos} movimientos sin fin de partida " +
                             $"(probable bucle en modo infinito). Turnos jugados={game.totalMoves}.");
    }

    /// <summary>Elige una celda legal al azar para el jugador dado.
    /// Devuelve (-1,-1) si no hay ninguna.</summary>
    static Vector2Int ElegirCeldaLegalAlAzar(GameLogic game, int player)
    {
        // Recolectar celdas legales
        var legales = new System.Collections.Generic.List<Vector2Int>();
        for (int x = 0; x < game.gridSize; x++)
            for (int y = 0; y < game.gridSize; y++)
                if (game.IsValidMove(x, y, player))
                    legales.Add(new Vector2Int(x, y));

        if (legales.Count == 0) return new Vector2Int(-1, -1);
        return legales[Random.Range(0, legales.Count)];
    }
}
