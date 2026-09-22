using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controlador de escena GameScene (MonoBehaviour OK: gestiona GameObjects).
/// Prueba: Humano (P1 rosado) vs BotAgent (P2 naranja) con GameLogic puro.
/// Uso: en GameScene crea un GameObject "Game" y agregale este componente. Solo.
/// Todo el tablero se construye por codigo, sin prefabs.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Prueba")]
    public float botDelay = 0.6f;

    private MatchController match;
    private GameLogic game => match.game;

    private Button[,] cells;
    private TMP_Text statusText;
    private TMP_Text turnText;
    private bool busy = false;
    private bool gameOver = false;

    private readonly Color colNeutral = new Color(0.12f, 0.16f, 0.28f);
    private readonly Color colP1 = new Color(1f, 0.15f, 0.6f);   // rosado neon
    private readonly Color colP2 = new Color(1f, 0.45f, 0.05f);  // naranja electrico

    private void Start()
    {
        var diff = BotAgent.Difficulty.Medio;
        if (UGSProfileManager.Instance != null) diff = UGSProfileManager.Instance.SuggestedDifficulty;
        else diff = (BotAgent.Difficulty)Mathf.Clamp(UGSRemoteConfig.botDifficulty, 0, 2);

        match = new MatchController(UGSRemoteConfig.gridSize, UGSRemoteConfig.maxCellLevel, UGSRemoteConfig.maxTurns, diff);

        BuildUI();
        Refresh();
    }

    private void BuildUI()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        var canvasGO = new GameObject("GameCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Titulo turno
        turnText = MakeLabel(canvasGO.transform, "TurnText", 0, 250, 34);
        statusText = MakeLabel(canvasGO.transform, "StatusText", 0, 200, 24);

        // Tablero
        var boardGO = new GameObject("Board");
        boardGO.transform.SetParent(canvasGO.transform, false);
        var rt = boardGO.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -40);
        rt.sizeDelta = new Vector2(420, 420);
        var gridLayout = boardGO.AddComponent<GridLayoutGroup>();
        int cellPx = Mathf.FloorToInt((420 - (game.gridSize - 1) * 4) / game.gridSize);
        gridLayout.cellSize = new Vector2(cellPx, cellPx);
        gridLayout.spacing = new Vector2(4, 4);

        cells = new Button[game.gridSize, game.gridSize];
        for (int y = game.gridSize - 1; y >= 0; y--)
            for (int x = 0; x < game.gridSize; x++)
            {
                int cx = x, cy = y;
                var bGO = new GameObject($"Cell_{x}_{y}");
                bGO.transform.SetParent(boardGO.transform, false);
                bGO.AddComponent<RectTransform>(); // primero: los UI necesitan RectTransform
                bGO.AddComponent<CanvasRenderer>();
                var img = bGO.AddComponent<Image>();
                img.color = colNeutral;
                var btn = bGO.AddComponent<Button>();
                btn.onClick.AddListener(() => OnCellClick(cx, cy));
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(bGO.transform, false);
                var lrt = labelGO.AddComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                labelGO.AddComponent<CanvasRenderer>();
                var txt = labelGO.AddComponent<TextMeshProUGUI>();
                if (txt == null) { Debug.LogError("[Game] TextMeshProUGUI null en celda."); continue; }
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 28;
                txt.color = Color.white;
                cells[x, y] = btn;
            }

        // Botones abajo
        MakeButton(canvasGO.transform, "RestartBtn", "Reiniciar", 0, -300, () =>
        {
            match.Reset();
            busy = false;
            gameOver = false;
            statusText.text = "";
            Refresh();
        });
        MakeButton(canvasGO.transform, "BackBtn", "Menu", 0, -350, () =>
        {
            SceneManager.LoadScene("SampleScene");
        });
    }

    private TMP_Text MakeLabel(Transform parent, string name, float x, float y, int size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(800, 50);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = size;
        t.color = Color.white;
        return t;
    }

    private void MakeButton(Transform parent, string name, string label, float x, float y, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(220, 40);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.25f, 0.45f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        var tGO = new GameObject("Label");
        tGO.transform.SetParent(go.transform, false);
        var trt = tGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        var t = tGO.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = 22;
        t.color = Color.white;
    }

    private void OnCellClick(int x, int y)
    {
        if (busy || gameOver || match == null) return;
        if (game.currentPlayer != match.humanPlayer) return; // no es tu turno
        DoHumanMove(x, y);
    }

    private void DoHumanMove(int x, int y)
    {
        var r = match.PlayHuman(x, y);
        if (!r.success)
        {
            statusText.text = r.error;
            return;
        }
        AfterMove(r);
        if (!r.gameOver && game.currentPlayer != match.humanPlayer)
            StartCoroutine(BotTurn());
    }

    private IEnumerator BotTurn()
    {
        busy = true;
        Refresh(); // bloquea clics durante el turno del bot
        yield return new WaitForSeconds(botDelay);
        GameLogic.MoveResult r;
        try
        {
            r = match.PlayBot();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Game] Bot fallo: {ex.Message}");
            game.currentPlayer = match.humanPlayer; // devuelve el turno
            busy = false;
            Refresh();
            yield break;
        }
        busy = false; // liberar ANTES de refrescar para reactivar botones
        if (!r.success)
        {
            Debug.LogWarning("[Game] Bot sin jugada valida, pasa el turno.");
            game.currentPlayer = match.humanPlayer;
        }
        AfterMove(r);
    }

    private void AfterMove(GameLogic.MoveResult r)
    {
        Refresh();
        if (r.explosions != null && r.explosions.Count > 1)
            statusText.text = $"¡CADENA x{r.explosions.Count}!";
        else if (r.explosions != null && r.explosions.Count == 1)
            statusText.text = "¡REACCIÓN!";
        else if (!r.gameOver)
            statusText.text = "";

        if (r.gameOver)
        {
            gameOver = true;
            if (r.winner == 0) statusText.text = "Empate.";
            else if (r.winner == match.humanPlayer) statusText.text = "¡Ganaste!";
            else statusText.text = "Ganó el Bot.";
            _ = UGSProfileManager.Instance.RecordMatchResultAsync(r.winner == match.humanPlayer);
        }
    }

    private void Refresh()
    {
        for (int x = 0; x < game.gridSize; x++)
            for (int y = 0; y < game.gridSize; y++)
            {
                var btn = cells[x, y];
                var img = btn.GetComponent<Image>();
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (img == null || txt == null) continue;
                int o = game.owner[x, y], l = game.level[x, y];
                img.color = o == 1 ? colP1 : o == 2 ? colP2 : colNeutral;
                txt.text = l == 0 ? "" : l.ToString();
                btn.interactable = !busy && !gameOver && game.currentPlayer == match.humanPlayer && (o == 0 || o == match.humanPlayer);
            }
        turnText.text = game.currentPlayer == match.humanPlayer ? "Tu turno (Rosa)" : "Turno Bot (Naranja)";
    }
}
