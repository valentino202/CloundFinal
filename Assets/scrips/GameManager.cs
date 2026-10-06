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

    private CellVisuals visuals;

    private void Start()
    {
        var diff = BotAgent.Difficulty.Medio;
        if (UGSProfileManager.Instance != null) diff = UGSProfileManager.Instance.SuggestedDifficulty;
        else diff = (BotAgent.Difficulty)Mathf.Clamp(UGSRemoteConfig.botDifficulty, 0, 2);

        match = new MatchController(UGSRemoteConfig.gridSize, UGSRemoteConfig.maxCellLevel, UGSRemoteConfig.maxTurns, diff);

        visuals = GetComponent<CellVisuals>();
        if (visuals == null)
        {
            // Fallback: si falta el componente se crea en runtime, pero entonces
            // sus camposSerialized quedan vacios y no habra arte. El aviso de
            // CellVisuals lo explica; aqui solo se avisa de forma explicita.
            Debug.LogWarning("[Game] CellVisuals no esta en la escena. Agregalo al GameObject 'Game' (Component > Add Component > Cell Visuals) y arrastra el cristal + los 4 clips.");
            visuals = gameObject.AddComponent<CellVisuals>();
        }
        visuals.Bind(match);

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

        // El tablero ocupa +-3.6 unidades (+-389px). El texto va arriba libre
        // y SIN raycast: antes tapaba la fila superior y robaba los clics.
        turnText = MakeLabel(canvasGO.transform, "TurnText", 0, 440, 34);
        statusText = MakeLabel(canvasGO.transform, "StatusText", 0, 400, 24);

        // Tablero: los sprites world-space los dibuja CellVisuals. Aqui solo
        // van botones invisibles que capturan el click, alineados a la misma
        // rejilla para que el clic caiga en la celda correcta.
        var boardGO = new GameObject("Board");
        boardGO.transform.SetParent(canvasGO.transform, false);
        var rt = boardGO.AddComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        var gridLayout = boardGO.AddComponent<GridLayoutGroup>();
        float pxPerUnit = PixelsPerWorldUnit();
        float cellPx = BoardFx.CellSize * pxPerUnit;
        float gapPx = BoardFx.CellGap * pxPerUnit;
        gridLayout.cellSize = new Vector2(cellPx, cellPx);
        gridLayout.spacing = new Vector2(gapPx, gapPx);
        float boardPx = cellPx * game.gridSize + gapPx * (game.gridSize - 1);
        rt.sizeDelta = new Vector2(boardPx, boardPx);

        bool spriteArt = SpriteArt;

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
                // Con arte activo el sprite manda; el Image solo queda como raycast target.
                img.color = spriteArt ? new Color(0f, 0f, 0f, 0f) : BoardFx.ColNeutral;
                var btn = bGO.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => OnCellClick(cx, cy));
                cells[x, y] = btn;

                // El numero de nivel solo hace falta en el fallback por codigo;
                // con arte activo lo comunica la animacion del liquido.
                if (!spriteArt)
                {
                    var labelGO = new GameObject("Label");
                    labelGO.transform.SetParent(bGO.transform, false);
                    var lrt = labelGO.AddComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                    labelGO.AddComponent<CanvasRenderer>();
                    var txt = labelGO.AddComponent<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.alignment = TextAlignmentOptions.Center;
                        txt.fontSize = 28;
                        txt.color = Color.white;
                    }
                    else { Debug.LogError("[Game] TextMeshProUGUI null en celda."); }
                }
            }

        // Botones abajo, libres del tablero (+-389px)
        MakeButton(canvasGO.transform, "RestartBtn", "Reiniciar", 0, -440, () =>
        {
            StopAllCoroutines();
            match.Reset();
            busy = false;
            gameOver = false;
            statusText.text = "";
            Refresh();
        });
        MakeButton(canvasGO.transform, "BackBtn", "Menu", 0, -485, () =>
        {
            SceneManager.LoadScene("SampleScene");
        });
    }

    // Convierte unidades de mundo a pixeles de pantalla usando la camara
    // ortografica, para que los botones coincidan con los sprites.
    private static float PixelsPerWorldUnit()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic || Screen.height <= 0) return 100f;
        return Screen.height / (2f * cam.orthographicSize);
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
        // Decorativo: no debe interceptar clics del tablero.
        t.raycastTarget = false;
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
        // El clic lo recibe la imagen del boton; la etiqueta no intercepta.
        t.raycastTarget = false;
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
        BoardFx.LastAttacker = match.humanPlayer;
        PunchCell(x, y); // rebote al colocar el punto (fallback)
        FireLevelFx(x, y); // tu animacion de nivel 1/2/3 del pack equipado
        AfterMove(r);
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
            BoardFx.LastAttacker = match.botPlayerId;
            PunchCell(match.lastMove.x, match.lastMove.y);
            FireLevelFx(match.lastMove.x, match.lastMove.y);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Game] Bot fallo: {ex.Message}");
            game.currentPlayer = match.humanPlayer; // devuelve el turno
            busy = false;
            Refresh();
            yield break;
        }
        if (!r.success)
        {
            Debug.LogWarning("[Game] Bot sin jugada valida, pasa el turno.");
            game.currentPlayer = match.humanPlayer;
            busy = false;
            Refresh();
            yield break;
        }
        AfterMove(r); // maneja busy hasta terminar el fx
    }

    private void AfterMove(GameLogic.MoveResult r)
    {
        if (r.explosions != null && r.explosions.Count > 0)
            StartCoroutine(ExplosionSequence(r));
        else
            FinishMove(r);
    }

    // Dispara tu animacion de nivel del pack equipado (1/2/3, 4 = boom).
    private void FireLevelFx(int x, int y)
    {
        try
        {
            if (BoardFx.LevelFx == null) return;
            int lvl = game.level[x, y];
            BoardFx.LevelFx.Invoke(cells[x, y].transform, lvl, BoardFx.Current.KeyForLevel(lvl));
        }
        catch (System.Exception ex) { Debug.LogWarning($"[Game] LevelFx fallo: {ex.Message}"); }
    }

    // Destello celda por celda + lados, con el color del articulo equipado.
    private IEnumerator ExplosionSequence(GameLogic.MoveResult r)
    {
        busy = true;
        Refresh();
        int i = 0;
        foreach (var c in r.explosions)
        {
            i++;
            // Cada explosion de la cadena dispara su propio boom, con el ritmo
            // natural de la secuencia. Asi una gota que cae en un 3 y lo lleva
            // a 4 tambien hace su animacion de explosion.
            try { BoardFx.ExplosionFx?.Invoke(new System.Collections.Generic.List<Vector2Int> { c }, BoardFx.Current.animBoom); }
            catch (System.Exception ex) { Debug.LogWarning($"[Game] ExplosionFx fallo: {ex.Message}"); }

            if (!BoardFx.SuppressFallback)
            {
                FlashCell(c.x, c.y, BoardFx.ColBoom);
                FlashNeighbours(c.x, c.y, BoardFx.ColBoom);
            }
            statusText.text = i > 1 ? $"¡CADENA x{i}!" : "¡REACCIÓN!";
            yield return new WaitForSeconds(0.18f);
        }
        busy = false;
        FinishMove(r);
    }

    private void FinishMove(GameLogic.MoveResult r)
    {
        busy = false;
        Refresh();
        if (r.gameOver)
        {
            gameOver = true;
            if (r.winner == 0) statusText.text = "Empate.";
            else if (r.winner == match.humanPlayer) statusText.text = "Ã‚Â¡Ganaste!";
            else statusText.text = "GanÃƒÂ³ el Bot.";
            _ = UGSProfileManager.Instance.RecordMatchResultAsync(r.winner == match.humanPlayer);
            return;
        }
        statusText.text = "";
        if (game.currentPlayer != match.humanPlayer)
            StartCoroutine(BotTurn());
    }

    private bool SpriteArt => visuals != null && visuals.Ready;

    // Con arte activo el destello y el rebote los dan las animaciones, y los
    // botones son transparentes: pintar aqui solo los ensuciaria.
    private void FlashCell(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= game.gridSize || y >= game.gridSize) return;
        if (SpriteArt) return;
        var img = cells[x, y].GetComponent<Image>();
        if (img != null) img.color = c;
        PunchCell(x, y);
    }

    private void FlashNeighbours(int x, int y, Color c)
    {
        FlashCell(x - 1, y, c); FlashCell(x + 1, y, c);
        FlashCell(x, y - 1, c); FlashCell(x, y + 1, c);
    }

    private void PunchCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= game.gridSize || y >= game.gridSize) return;
        if (SpriteArt) return;
        StartCoroutine(Punch(cells[x, y].transform));
    }

    private IEnumerator Punch(Transform t)
    {
        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / 0.18f;
            float s = 1f + Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 0.35f;
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private void Refresh()
    {
        // Con arte activo, Refresh solo sincroniza sprites; los botones solo
        // cambian su estado de interaccion.
        bool spriteArt = SpriteArt;

        for (int x = 0; x < game.gridSize; x++)
            for (int y = 0; y < game.gridSize; y++)
            {
                var btn = cells[x, y];
                int o = game.owner[x, y];
                btn.interactable = !busy && !gameOver && game.currentPlayer == match.humanPlayer && (o == 0 || o == match.humanPlayer);

                if (spriteArt) continue;

                // Fallback por codigo: cuadrado de color + numero de nivel.
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = BoardFx.LiquidColorFor(o);
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt == null) continue;
                int l = game.level[x, y];
                txt.text = l > 0 ? l.ToString() : "";
            }

        if (spriteArt) visuals.SyncAll();

        turnText.text = game.currentPlayer == match.humanPlayer ? "Tu turno (Rosa)" : "Turno Bot (Naranja)";
    }
}
