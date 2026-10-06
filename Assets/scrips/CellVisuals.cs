using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Capa visual del tablero: world-space sprites con PlayableGraph.
///
/// Estructura por celda:
///   Liquid  SpriteRenderer  sortingOrder 0  <- TINTADO con el color del jugador
///   Glass   SpriteRenderer  sortingOrder 1  <- NUNCA tintado, se queda gris (0tapa)
///
/// El cristal va ENCIMA del liquido (como un vaso visto de frente) y la
/// explosion (4animation) va en sortingOrder 3 sin clipeo, para que desborde
/// hacia las 4 casillas vecinas.
///
/// Setup: agrega este componente al GameObject "Game" de GameScene y arrastra
/// el prefab del cristal y los 4 clips. Nada mas. Sin controller: cada celda
/// tiene un Animator sin controller, manejado 100% por codigo.
/// </summary>
public class CellVisuals : MonoBehaviour
{
    [Header("Pack")]
    public string packId = "clasico";

    [Header("Arte del pack (0tapa + 4 clips)")]
    public GameObject glassPrefab;
    [Header("Cristal (0tapa: NUNCA se tinta, solo transparencia)")]
    [Tooltip("1 = solido, 0 = invisible. 70% transparente = 0.3.")]
    [Range(0f, 1f)] public float glassAlpha = 0.3f;

    [Header("Burbujas (prueba, apagado por ahora)")]
    public bool bubbles = false;
    [Tooltip("Burbujas por segundo en nivel 3. Se escala por nivel.")]
    public float bubbleRate = 6f;
    public float bubbleSize = 0.09f;
    public float bubbleRise = 0.35f;
    public int bubbleOrder = 1;
    public AnimationClip clip1;
    public AnimationClip clip2;
    public AnimationClip clip3;
    public AnimationClip clipBoom;

    [Header("Capas (de atras hacia adelante)")]
    public int liquidOrder = 0;
    public int glassOrder = 1;
    public int numberOrder = 2;
    public int explosionOrder = 3;

    [Header("Numero de nivel (blanco -> rojo)")]
    public Color numberLow = Color.white;
    public Color numberHigh = Color.red;
    [Tooltip("1 = solido, 0 = invisible.")]
    [Range(0f, 1f)] public float numberAlpha = 1f;
    [Tooltip("Tamano directo de la fuente. Mas chico = numero mas chico.")]
    [Range(1, 96)] public int numberFontSize = 28;
    [Tooltip("Lado de la caja del numero en celdas.")]
    [Range(0.1f, 1f)] public float numberSize = 0.25f;

    [Header("Feel")]
    public float shakeDuration = 0.22f;
    public float shakeStrength = 0.16f;

    [Header("Ajuste al tamano de celda (sprites vienen en ~3.7 unidades)")]
    [Range(0.2f, 1.2f)] public float crystalFill = 1.0f;
    [Range(0.2f, 1.0f)] public float liquidFill = 0.9f;
    [Tooltip("Sube el liquido en celdas (el arte viene cargado abajo).")]
    [Range(-0.2f, 0.3f)] public float liquidLift = -0.05f;
    [Tooltip("Lado del boom en celdas. 2.4 cubre el centro y toca las 4 vecinas.")]
    public float boomCells = 2.4f;

    [Header("Velocidad por nivel (1 = normal, <1 mas lento)")]
    public float speed1 = 1f;
    public float speed2 = 1f;
    public float speed3 = 1f;
    public float speedBoom = 1f;

    [Header("Juice: squash & stretch (GDD)")]
    [Tooltip("0 = sin rebote al aterrizar el liquido. Alto = se sale de los bordes.")]
    public float punchAmount = 0.18f;
    public float punchTime = 0.24f;
    [Tooltip("Respiracion sutil mientras la celda esta llena (0 = quieta)")]
    public float idlePulse = 0.025f;
    public float idleSpeed = 1.5f;

    private class CellVisual
    {
        public Transform root;
        public SpriteRenderer glass;
        public SpriteRenderer liquid;
        public Animator anim;
        public Vector2Int coord;
        public int owner = -1;
        public int level = -1;
        /// <summary>Escala normalizada a 1 celda. Nunca se pierde: el juice multiplica sobre ella.</summary>
        public float baseScale = 1f;
        public TextMeshPro label;
        public ParticleSystem foam;
        public AnimationClipPlayable playable;
        public AnimationPlayableOutput output;
        public bool outputValid;
        public Coroutine juice;
    }

    private readonly Dictionary<Vector2Int, CellVisual> cells = new Dictionary<Vector2Int, CellVisual>();
    private readonly List<CellVisual> expired = new List<CellVisual>();

    private PlayableGraph graph;
    private bool graphReady;
    private FxPack pack;
    private MatchController match;
    private Transform cellsRoot;
    private Camera cam;
    private Vector3 camHome;
    private bool camHomeSet;
    private bool shaking;

    /// <summary>Escala para llevar un sprite del atlas a 1 unidad de celda.</summary>
    private float unitScale = 1f;

    /// <summary>Salidas vivas del grafo. En 0 el grafo se detiene y no avisa.</summary>
    private int liveOutputs = 0;

    /// <summary>
    /// El aviso "being evaluated with no outputs" sale cuando el grafo corre
    /// vacio. Se evita deteniendolo cuando no hay nada conectado.
    /// </summary>
    private void RefreshGraphState()
    {
        if (!graphReady || !graph.IsValid()) return;
        if (liveOutputs > 0)
        {
            if (!graph.IsPlaying()) graph.Play();
        }
        else if (graph.IsPlaying())
        {
            graph.Stop();
        }
    }

    public bool Ready => graphReady && pack != null && pack.HasArt;

    private void Awake()
    {
        FxPackCatalog.AttachArt(packId, glassPrefab, clip1, clip2, clip3, clipBoom, speed1, speed2, speed3, speedBoom);
        pack = FxPackCatalog.Get(packId) ?? FxPackCatalog.Get("clasico");

        if (pack == null || !pack.HasArt)
        {
            Debug.LogError("[CellVisuals] Faltan referencias: arrastra el prefab del cristal y los 4 clips en el Inspector. Sigo con el fallback por codigo.");
            return;
        }

graph = PlayableGraph.Create("CellVisuals");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            // Sin Play() aqui: arranca en Stop para no evaluar un grafo vacio
            // (eso era el aviso "no outputs"). RefreshGraphState lo enciende
            // solo cuando hay una salida conectada.
            graphReady = true;
    }

    private void OnDestroy()
    {
        if (graphReady && graph.IsValid()) graph.Destroy();
        graphReady = false;
    }

    private void OnEnable()
    {
        BoardFx.LevelFx += OnLevelFx;
        BoardFx.ExplosionFx += OnExplosionFx;
    }

    private void OnDisable()
    {
        BoardFx.LevelFx -= OnLevelFx;
        BoardFx.ExplosionFx -= OnExplosionFx;
    }

    /// <summary>GameManager avisa cuando la partida ya existe.</summary>
    public void Bind(MatchController match)
    {
        this.match = match;
        Rebuild();
    }

    // Al mover un slider en el Inspector con el juego corriendo, reaplica el
    // layout y los colores sin reconstruir (no pierde las animaciones).
    private void OnValidate()
    {
        if (!Application.isPlaying || match == null || cells.Count == 0) return;
        ApplyLayout();
        SyncAll();
    }

    /// <summary>Recoloca y reescala las celdas existentes segun el Inspector.</summary>
    public void ApplyLayout()
    {
        if (match == null) return;
        int n = match.game.gridSize;
        foreach (var cv in cells.Values)
        {
            var pos = BoardFx.CellWorldPosition(cv.coord.x, cv.coord.y, n);
            if (cv.liquid != null)
            {
                cv.liquid.transform.localPosition = pos + Vector3.up * (liquidLift * BoardFx.CellSize);
                cv.baseScale = unitScale * liquidFill;
                cv.liquid.transform.localScale = Vector3.one * cv.baseScale;
            }
            if (cv.glass != null)
            {
                var root = cv.root != null ? cv.root : cv.glass.transform;
                root.localPosition = pos;
                // Si el sprite es la raiz, escala la raiz; si es hijo, el hijo.
                var t = cv.glass.transform == root ? root : cv.glass.transform;
                if (cv.glass.transform != root) cv.glass.transform.localPosition = Vector3.zero;
                t.localScale = Vector3.one * (unitScale * crystalFill);
                // Transparencia en vivo desde el slider (nunca se tinta).
                cv.glass.color = new Color(1f, 1f, 1f, glassAlpha);
            }
            if (cv.label != null)
            {
                cv.label.transform.localPosition = pos;
                var lrt = cv.label.GetComponent<RectTransform>();
                if (lrt != null)
                {
                    lrt.pivot = new Vector2(0.5f, 0.5f);
                    float box = BoardFx.CellSize * numberSize;
                    lrt.sizeDelta = new Vector2(box, box);
                }
                cv.label.fontSize = numberFontSize;
                cv.label.renderer.sortingOrder = numberOrder;
            }
        }
    }

    /// <summary>Crea/recicla las celdas visuales segun el tablero actual.</summary>
    public void Rebuild()
    {
        if (match == null) return;
        int n = match.game.gridSize;

        // Descarta celdas que quedaron fuera del tablero nuevo.
        expired.Clear();
        foreach (var kv in cells)
            if (kv.Key.x >= n || kv.Key.y >= n) expired.Add(kv.Value);
        foreach (var cv in expired)
        {
            cells.Remove(cv.coord);
            DisposeVisual(cv);
        }

        if (!Ready) return;

        unitScale = UnitScaleFor(pack.glassPrefab);

        if (cellsRoot == null)
        {
            // Raiz en el ORIGEN DEL MUNDO, sin colgar de "Game": ese GameObject
            // esta muy lejos de la camara en GameScene y las celdas quedarian
            // fuera de cuadro.
            var go = new GameObject("Cells");
            cellsRoot = go.transform;
            cellsRoot.position = Vector3.zero;
            cellsRoot.rotation = Quaternion.identity;
        }

        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                if (!cells.ContainsKey(new Vector2Int(x, y)))
                    CreateVisual(x, y, n);

        var first = cells[new Vector2Int(0, 0)];
        Debug.Log($"[CellVisuals] grid={n} pitch={BoardFx.Pitch:F3} unitScale={unitScale:F4} celdas={cells.Count}\n" +
                  $"  liquido: escala={first.baseScale:F4} pos={first.liquid.transform.position}\n" +
                  $"  cristal: escala={first.glass.transform.localScale.x:F4} pos={first.glass.transform.position} " +
                  $"(raiz={first.glass.transform == first.root})");

        SyncAll();
    }

    private void CreateVisual(int x, int y, int gridSize)
    {
        var coord = new Vector2Int(x, y);
        var pos = BoardFx.CellWorldPosition(x, y, gridSize);
        var cv = new CellVisual { coord = coord };

        // Escala base: las hojas vienen en 100 PPU (~3.7 unidades) y la celda
        // mide 1. Se calcula una vez y se reaplica a cristal y liquido.
        float baseScale = UnitScaleFor(pack.glassPrefab);

        // --- Cristal: el SpriteRenderer va delante del liquido ---
        if (pack.glassPrefab != null)
        {
            var inst = Instantiate(pack.glassPrefab, cellsRoot);
            inst.name = $"0tapa_{x}_{y}";
            inst.transform.localPosition = pos;

            var sr = inst.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // Cristal en blanco con la transparencia del Inspector: NUNCA se tinta,
// se queda gris. 70% transparente = alpha 0.3.
                sr.color = new Color(1f, 1f, 1f, glassAlpha);
                sr.sortingOrder = glassOrder;

                // 0tapa tiene UN solo GameObject, asi que el SpriteRenderer ES
                // la raiz: si le pusiÃ©ramos localPosition=0 las 36 celdas se
                // apilarÃ­an en el centro. Solo se centra si es un hijo.
                if (sr.transform == inst.transform)
                    inst.transform.localScale = Vector3.one * (baseScale * crystalFill);
                else
                {
                    sr.transform.localPosition = Vector3.zero;
                    sr.transform.localScale = Vector3.one * (baseScale * crystalFill);
                }

                cv.glass = sr;
                cv.root = inst.transform;
            }
        }

        // --- Liquido: tintado por el color del jugador ---
        var liqGO = new GameObject($"Liquid_{x}_{y}");
        liqGO.transform.SetParent(cellsRoot, false);
        // El arte del liquido viene cargado abajo del frame: se levanta para
        // que quede contenido dentro del cristal en vez de salirse por debajo.
        liqGO.transform.localPosition = pos + Vector3.up * (liquidLift * BoardFx.CellSize);
        cv.liquid = liqGO.AddComponent<SpriteRenderer>();
        cv.liquid.sortingOrder = liquidOrder;
        cv.liquid.color = Color.clear;
        // El sprite del liquido lo asigna la animacion, pero viene de la misma
        // hoja: le aplicamos el mismo ajuste para que llene la celda.
        cv.baseScale = baseScale * liquidFill;
        cv.liquid.transform.localScale = Vector3.one * cv.baseScale;
        if (cv.root == null) cv.root = liqGO.transform;

        // --- Animator sin controller: lo maneja el PlayableGraph ---
        cv.anim = liqGO.AddComponent<Animator>();
        cv.anim.applyRootMotion = false;

        // --- Burbujas de prueba: suben dentro del liquido ---
        if (bubbles)
        {
            var bubGO = new GameObject($"Bubbles_{x}_{y}");
            bubGO.transform.SetParent(cellsRoot, false);
            bubGO.transform.localPosition = pos;
            cv.foam = bubGO.AddComponent<ParticleSystem>();
            SetupBubbles(cv.foam);
        }

        // --- Numero de nivel: centrado sobre el cristal, blanco -> rojo ---
        var labelGO = new GameObject($"Label_{x}_{y}");
        labelGO.transform.SetParent(cellsRoot, false);
        labelGO.transform.localPosition = pos;
        cv.label = labelGO.AddComponent<TextMeshPro>();
        cv.label.alignment = TextAlignmentOptions.Center;
        cv.label.fontSize = numberFontSize;
        cv.label.enableAutoSizing = false;
        cv.label.color = Color.white;
        cv.label.text = "";
        var lrt = labelGO.GetComponent<RectTransform>();
        // Caja chica y pivote al centro: el auto-size encaja el digito dentro
        // y queda centrado en la celda.
        lrt.pivot = new Vector2(0.5f, 0.5f);
        float box = BoardFx.CellSize * numberSize;
        lrt.sizeDelta = new Vector2(box, box);
        cv.label.renderer.sortingOrder = numberOrder;

        cells[coord] = cv;
    }

    /// <summary>Color del numero segun el nivel: blanco (1) -> rojo (max).</summary>
    private Color NumberColorFor(int level)
    {
        int maxVisible = match != null ? Mathf.Max(2, match.game.maxLevel - 1) : 3;
        float t = (float)(level - 1) / (maxVisible - 1);
        return Color.Lerp(numberLow, numberHigh, Mathf.Clamp01(t));
    }

    // Textura de burbuja generada por codigo (anillo suave): sin assets nuevos.
    private static Texture2D bubbleTex;
    private static Texture2D BubbleTexture()
    {
        if (bubbleTex != null) return bubbleTex;
        int s = 64;
        bubbleTex = new Texture2D(s, s, TextureFormat.ARGB32, false);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x - s * 0.5f) / (s * 0.5f);
                float dy = (y - s * 0.5f) / (s * 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.72f) * 5f);
                float fill = Mathf.Clamp01(1f - d) * 0.22f;
                bubbleTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ring + fill)));
            }
        bubbleTex.Apply();
        return bubbleTex;
    }

    private void SetupBubbles(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.duration = 3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(bubbleRise * 0.7f, bubbleRise * 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(bubbleSize * 0.6f, bubbleSize * 1.4f);
        main.startColor = new Color(1f, 1f, 1f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = false;

        var em = ps.emission;
        em.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(BoardFx.CellSize * 0.6f, 0.05f, 0.1f);
        shape.position = new Vector3(0f, -BoardFx.CellSize * 0.3f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.sortingOrder = bubbleOrder;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = BubbleTexture();
        rend.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetBubbleRate(CellVisual cv, int level)
    {
        if (cv.foam == null) return;
        var em = cv.foam.emission;
        // Sin liquido no hay burbujas; mas nivel = mas burbujeo.
        em.rateOverTime = level <= 0 ? 0f : bubbleRate * level / 3f;
        if (level > 0 && !cv.foam.isPlaying) cv.foam.Play();
    }

    /// <summary>
    /// Escala para que un sprite de 1 unidad de celda ocupe exactamente
    /// 1 unidad de mundo. Se deriva del tamano real del sprite del prefab.
    /// </summary>
    private static float UnitScaleFor(GameObject prefab)
    {
        if (prefab == null) return 1f;
        var sr = prefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return 1f;
        var s = sr.sprite.bounds.size;
        float maxDim = Mathf.Max(s.x, s.y);
        if (maxDim <= 0.01f) return 1f;
        return BoardFx.CellSize / maxDim;
    }

    private void DisposeVisual(CellVisual cv)
    {
        if (cv.juice != null) { StopCoroutine(cv.juice); cv.juice = null; }
        KillOutput(cv);
        if (cv.playable.IsValid()) cv.playable.Destroy();
        if (cv.label != null) Destroy(cv.label.gameObject);
        if (cv.root != null) Destroy(cv.root.gameObject);
    }

    // AnimationPlayableOutput es un struct que implementa IPlayableOutput (no
    // IPlayable), asi que se destruye pasandolo directo a DestroyOutput. Su
    // GetHandle() devuelve PlayableOutputHandle, que NO admite IsValid/Destroy.
    private void KillOutput(CellVisual cv)
    {
        if (!cv.outputValid) return;
        if (graphReady && graph.IsValid()) graph.DestroyOutput(cv.output);
        cv.output = default;
        cv.outputValid = false;
        liveOutputs = Mathf.Max(0, liveOutputs - 1);
        RefreshGraphState();
    }

    // =================================================================
    // Sincronizacion con el estado del juego
    // =================================================================
    public void SyncAll()
    {
        if (match == null) return;
        var g = match.game;
        foreach (var cv in cells.Values)
            ApplyState(cv, g.owner[cv.coord.x, cv.coord.y], g.level[cv.coord.x, cv.coord.y]);
    }

    private void ApplyState(CellVisual cv, int owner, int level)
    {
        bool changed = cv.owner != owner || cv.level != level;
        cv.owner = owner;
        cv.level = level;

        // Tint SOLO del liquido. El cristal nunca se toca.
        if (cv.liquid != null)
            cv.liquid.color = level <= 0 ? Color.clear : BoardFx.LiquidColorFor(owner);

        // Numero centrado: vacio en 0, blanco->rojo segun el nivel.
        if (cv.label != null)
        {
            cv.label.text = level <= 0 ? "" : level.ToString();
            if (level > 0)
            {
                var nc = NumberColorFor(level);
                nc.a = numberAlpha;
                cv.label.color = nc;
            }
        }

        // Burbujas solo con liquido.
        SetBubbleRate(cv, level);

        if (!Ready) return;

        if (level <= 0)
        {
            Stop(cv);
            return;
        }

        if (changed)
        {
            PlayLevel(cv, level);
            StartJuice(cv);
        }
    }

    // Los clips son de una pasada y quedan congelados en el ultimo frame, asi
    // que el squash & stretch cubre ese rato muerto: la celda sigue teniendo
    // movimiento y el squash de conservacion de volumen da la sensacion de
    // "gota cayendo" que pide el GDD.
    private void StartJuice(CellVisual cv)
    {
        if (cv.juice != null) StopCoroutine(cv.juice);
        cv.juice = StartCoroutine(Juice(cv));
    }

    private IEnumerator Juice(CellVisual cv)
    {
        if (cv.liquid == null) yield break;
        var tr = cv.liquid.transform;
        float b = cv.baseScale;

        // Rebote elÃ¡stico al aterrizar: squash en Y + stretch en X (conserva volumen).
        if (punchAmount > 0f && punchTime > 0f)
        {
            float t = 0f;
            while (t < punchTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / punchTime);
                float s = 1f + Mathf.Sin(k * Mathf.PI) * punchAmount * (1f - k * 0.35f);
                tr.localScale = new Vector3(s * b, (2f - s) * b, b);
                yield return null;
            }
        }
        tr.localScale = Vector3.one * b;

        // Respiracion sutil mientras la celda siga llena.
        while (idlePulse > 0f && cv.level > 0 && !IdleBreak(cv))
        {
            float p = (Mathf.Sin(Time.time * idleSpeed * Mathf.PI) * 0.5f + 0.5f) * idlePulse;
            tr.localScale = Vector3.one * (b * (1f + p));
            yield return null;
        }
        tr.localScale = Vector3.one * b;
    }

    private bool IdleBreak(CellVisual cv)
        => cv == null || cv.liquid == null || !cv.root.gameObject.activeInHierarchy;

private void PlayLevel(CellVisual cv, int level)
    {
        var clip = pack.ClipForLevel(level);
        if (clip == null) { Stop(cv); return; }

        // Una sola pasada: al acabar queda en el ultimo frame (estado lleno).
        StartClip(cv, clip, pack.SpeedForLevel(level));
    }

    // AnimationClipPlayable no expone loop en la API publica (SetLoopTime y
    // SetOverrideLoopTime son internal), asi que el wrap se fija en el clip.
    private static void SetWrap(AnimationClip clip, bool loop)
    {
        if (clip == null) return;
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
    }

    private void StartClip(CellVisual cv, AnimationClip clip, float speed)
    {
        if (!graphReady) return;

        KillOutput(cv);
        if (cv.playable.IsValid()) cv.playable.Destroy();

        SetWrap(clip, false);

        cv.playable = AnimationClipPlayable.Create(graph, clip);
        cv.playable.SetApplyFootIK(false);
        cv.playable.SetSpeed(Mathf.Max(0.05f, speed));

        cv.output = AnimationPlayableOutput.Create(graph, $"cell_{cv.coord.x}_{cv.coord.y}", cv.anim);
        cv.output.SetSourcePlayable(cv.playable);
        cv.outputValid = true;
        liveOutputs++;
        RefreshGraphState();
        cv.playable.SetTime(0);
    }

    private void Stop(CellVisual cv)
    {
        KillOutput(cv);
        if (cv.playable.IsValid()) cv.playable.Destroy();
        StopJuice(cv);
    }

    private void StopJuice(CellVisual cv)
    {
        if (cv.juice != null) { StopCoroutine(cv.juice); cv.juice = null; }
        // Restaura la escala normalizada, NUNCA Vector3.one: los sprites del
        // atlas miden ~3.7 unidades y a escala 1 se desbordan de la celda.
        if (cv.liquid != null && cv.liquid.transform != null)
            cv.liquid.transform.localScale = Vector3.one * cv.baseScale;
    }

    // =================================================================
    // Hooks de BoardFx
    // =================================================================
    // SyncAll relee todo el tablero, asi que no hace falta mapear el
    // Transform de la celda a coordenadas de grid.
    private void OnLevelFx(Transform cellTransform, int level, string key) => SyncAll();

private void OnExplosionFx(List<Vector2Int> chain, string boomKey)
    {
        if (match == null) return;

        // GameManager dispara un evento por cada eslabon de la cadena, asi que
        // cada explosion (incluidas las provocadas por gotas que caen en un 3)
        // hace su propio boom.
        foreach (var c in chain) PlayBoom(c);

        SyncAll();
        if (shakeDuration > 0f && !shaking) StartCoroutine(Shake());
    }

    private void PlayBoom(Vector2Int coord)
    {
        if (!Ready) return;
        int n = match.game.gridSize;
        var pos = BoardFx.CellWorldPosition(coord.x, coord.y, n);

        var go = new GameObject($"Boom_{coord.x}_{coord.y}");
        // Sin colgar de "Game" (esta fuera del encuadre de la camara).
        go.transform.SetParent(cellsRoot != null ? cellsRoot : transform, false);
        go.transform.localPosition = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = explosionOrder;
        // El boom se tinta del color de QUIEN exploto (rosado jugador,
        // naranja bot), no de un amarillo generico.
        sr.color = BoardFx.LiquidColorFor(BoardFx.LastAttacker);

        // El boom es mas grande que el tablero: se dimensiona en celdas para
        // que desborde hacia las 4 vecinas.
        if (unitScale > 0.01f)
            go.transform.localScale = Vector3.one * (unitScale * boomCells);

        var anim = go.AddComponent<Animator>();
        anim.applyRootMotion = false;

        var playable = AnimationClipPlayable.Create(graph, pack.clipBoom);
        playable.SetApplyFootIK(false);
        playable.SetSpeed(Mathf.Max(0.05f, pack.SpeedForLevel(4)));
        SetWrap(pack.clipBoom, false); // boom es de una pasada

        var output = AnimationPlayableOutput.Create(graph, "boom", anim);
        output.SetSourcePlayable(playable);
        playable.SetTime(0);
        liveOutputs++;
        RefreshGraphState();

        float life = pack.clipBoom.length / Mathf.Max(0.05f, pack.SpeedForLevel(4)) + 0.15f;
        StartCoroutine(ReleaseBoom(go, output, playable, life));
    }

    private IEnumerator ReleaseBoom(GameObject go, AnimationPlayableOutput output, AnimationClipPlayable playable, float life)
    {
        yield return new WaitForSeconds(life);
        if (graphReady && graph.IsValid()) graph.DestroyOutput(output);
        liveOutputs = Mathf.Max(0, liveOutputs - 1);
        RefreshGraphState();
        if (playable.IsValid()) playable.Destroy();
        if (go != null) Destroy(go);
    }

    private IEnumerator Shake()
    {
        if (shakeDuration <= 0f || shakeStrength <= 0f) yield break;
        if (cam == null) cam = Camera.main;
        if (cam == null) yield break;
        if (!camHomeSet) { camHome = cam.transform.position; camHomeSet = true; }

        shaking = true;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float k = 1f - (t / shakeDuration);
            Vector2 off = Random.insideUnitCircle * (shakeStrength * k);
            cam.transform.position = camHome + new Vector3(off.x, off.y, 0f);
            yield return null;
        }
        cam.transform.position = camHome;
        shaking = false;
    }
}