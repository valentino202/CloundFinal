using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pack de animaciones SIN MonoBehaviour (solo datos).
/// Un pack = 4 llaves: anim de nivel 1, 2, 3 y explosion (4).
/// Las llaves son strings que TU defines (nombre de clip, prefab,
/// sprite, addressable...). Ej: pack "acuario" -> anim1..animBoom.
/// El jugador solo elige el pack; el tablero rutea cada llave a su nivel.
///
/// Ademas cada pack puede llevar las referencias reales de arte (glassPrefab
/// + los 4 AnimationClip). CellVisuals inyecta esas refs al arrancar, asi que
/// el catalogo de abajo solo define el esqueleto: id, nombre y llaves.
/// </summary>
[System.Serializable]
public class FxPack
{
    public string id;
    public string displayName;
    public string anim1;
    public string anim2;
    public string anim3;
    public string animBoom;

    // --- Arte real del pack (lo asigna CellVisuals desde el Inspector) ---
    /// <summary>Cristal/vaso que envuelve al liquido. NUNCA se tinta: va en gris.</summary>
    public GameObject glassPrefab;
    public AnimationClip clip1;
    public AnimationClip clip2;
    public AnimationClip clip3;
    public AnimationClip clipBoom;

    // Velocidad de reproduccion por nivel. Menos de 1 = mas lento (mas legible).
    public float speed1 = 1f;
    public float speed2 = 1f;
    public float speed3 = 1f;
    public float speedBoom = 1f;

    public FxPack(string id, string displayName, string anim1, string anim2, string anim3, string animBoom)
    {
        this.id = id; this.displayName = displayName;
        this.anim1 = anim1; this.anim2 = anim2; this.anim3 = anim3; this.animBoom = animBoom;
    }

    /// <summary>Burbujas propias del pack (los especiales las traen, el clasico no).</summary>
    public Sprite[] bubbleSprites;

    // --- Efectos del pack (paso 3b): viven en el CASILLERO del jugador ---
    /// <summary>Sonido del boom del pack (A, B o los dos alternando).</summary>
    public AudioClip sonidoBoom;
    public AudioClip sonidoBoomB;
    [Tooltip("0 = solo A, 1 = solo B, 2 = alternar A/B (comparar en partida).")]
    public int modoSonidoBoom = 2;
    [Range(0f, 1f)] public float boomVolume = 0.8f;

    [Tooltip("El pack TRAE burbujas: false = clasico (sin burbujas); true = pack especial (ej: caldero). No hay switch global: lo decide el casillero del jugador.")]
    public bool burbujasOn = false;
    [Tooltip("Burbujas por segundo en nivel 3 (escala con el nivel).")]
    public float bubbleRate = 2.5f;
    [Tooltip("Tamano de la burbuja, en celdas.")]
    public float bubbleSize = 0.07f;
    [Tooltip("Velocidad de subida, en celdas por segundo.")]
    public float bubbleRise = 0.5f;
    [Tooltip("Deriva lateral, en celdas.")]
    public float bubbleDrift = 0.01f;
    [Range(0f, 1f)] public float bubbleAlpha = 0.55f;
    public int bubbleOrder = 1;

    // --- Objetos del pack (paso 8): receta tomada del menu (boton 100px) ---
    [Tooltip("Hongo pequeno del nivel 1 (14/100 del boton = 0.14 de celda).")]
    public Sprite hongo1Sprite;
    [Tooltip("Segundo hongo del nivel 1 (7/100 del boton).")]
    public Sprite hongo2Sprite;
    [Range(0.02f, 1f)] public float hongo1Tamano = 0.14f;  // menu: 14px / 100
    [Range(0.02f, 1f)] public float hongo2Tamano = 0.07f;  // menu: 7px / 100
    [Tooltip("Franja del liquido donde pasean los hongos (0.239 del menu).")]
    [Range(0.05f, 1f)] public float hongosFranja = 0.239f;
    [Tooltip("Paseo en fracciones de celda por segundo (10px/100 = 0.1).")]
    public float hongosVelocidadPaseo = 0.1f;
    [Tooltip("Grados/segundo de giro (22 del menu).")]
    public float hongosGiro = 22f;
    [Tooltip("Amplitud del respiro del tamano del hongo 1 (0.467 del menu).")]
    [Range(0f, 0.8f)] public float hongo1Respiro = 0.467f;
    [Tooltip("Velocidad del respiro del TINTE sumergido (0.8 del menu).")]
    [Range(0.1f, 3f)] public float hongosRespiroColor = 0.8f;
    [Tooltip("Arcoiris de los hongos del menu (arcoiris: 1).")]
    public bool hongosArcoiris = true;
    [Tooltip("Velocidad del ciclo de color (0.5 del menu).")]
    [Range(0.05f, 2f)] public float hongosVelocidadArcoiris = 0.5f;
    [Tooltip("Cuanto reciben del color del liquido (1.0 del menu = totalmente sumergidos).")]
    [Range(0f, 1f)] public float hongosFuerzaSumergido = 1f;
    [Tooltip("Opacidad del objeto (0.82 del menu).")]
    [Range(0.3f, 1f)] public float hongosOpacidad = 0.82f;

    // --- Objetos del nivel 2 (paso 9): ojo vivo + hongo grande ---
    [Tooltip("Hongo grande del nivel 2 (17/100 del boton => 0.24 de celda).")]
    public Sprite hongoGrandeSprite;
    [Range(0.02f, 1f)] public float hongoGrandeTamano = 0.17f; // menu: 17px / 100
    [Tooltip("Giro del hongo grande (-30 del menu).")]
    public float hongoGrandeGiro = -30f;
    [Tooltip("Respiro del tamano del hongo grande (0.482 del menu).")]
    [Range(0f, 0.8f)] public float hongoGrandeRespiro = 0.482f;

    [Tooltip("Esclerotica del ojo (menu: 23/100 = 0.33 de celda; el usuario lo prefirio mas chico).")]
    public Sprite ojoSprite;
    [Range(0.02f, 1f)] public float ojoTamano = 0.23f; // menu: 23px / 100
    public Sprite irisCentral;
    public Sprite irisLateral;
    public Sprite irisVertical;
    [Tooltip("Tamano de la iris como fraccion del ojo (0.68 = el original del menu, correcto).")]
    [Range(0.2f, 0.9f)] public float irisFraccion = 0.68f;
    [Tooltip("Rojo intenso de la iris del primer ojo (menu).")]
    public Color irisColor = new Color(1f, 0.13f, 0.13f, 1f);
    [Tooltip("Cuanto se corre la iris (0.22 del menu).")]
    [Range(0f, 0.4f)] public float irisDesplazamiento = 0.22f;
    [Tooltip("Los barridos laterales recorren mas (1.4 del menu).")]
    [Range(1f, 3f)] public float irisExtraLateral = 1.4f;
    [Range(0.05f, 0.5f)] public float irisSuavidad = 0.15f;
    public float irisMinCambio = 0.7f;
    public float irisMaxCambio = 2.2f;

    // --- Objetos del nivel 3 (paso 10): hueso + segundo ojo azul ---
    [Tooltip("Hueso del menu (65px; el usuario lo prefirio mas chico en la celda).")]
    public Sprite huesoSprite;
    [Range(0.2f, 1f)] public float huesoTamano = 0.65f; // menu: 65px / 100
    [Tooltip("Paseo del hueso: 14px/s => 0.14 celda/s, pegado a los bordes.")]
    public float huesoVelocidadPaseo = 0.14f;
    [Tooltip("El hueso NO gira: se apoya y se meca (44.7 grados del menu).")]
    public float huesoOscilacion = 44.7f;
    public float huesoVelVaiven = 0.366f;
    [Range(0.3f, 1f)] public float huesoBob = 0.04f;
    [Tooltip("Copia sumergida del hueso: fuerza del velo verde (1.0 del menu).")]
    [Range(0f, 1f)] public float huesoCopiaFuerza = 1f;
    [Range(0.3f, 1f)] public float huesoCopiaOpacidad = 1f;
    public float huesoCopiaVelRespiro = 0.8f;

    [Tooltip("Segundo ojo del nivel 3 (11px => mas chico que el primero).")]
    [Range(0.05f, 1f)] public float ojo2Tamano = 0.11f; // menu: 11px / 100
    [Tooltip("Azul de la iris del SEGUNDO ojo (menu).")]
    public Color irisColor2 = new Color(0f, 0.192f, 1f, 1f);

    [Tooltip("Unica perilla para escalar TODOS los objetos juntos (1 = medidas convertidas del menu).")]
    [Range(0.4f, 2f)] public float objetosEscalaGlobal = 1f;
    [Tooltip("Factor extra SOLO en nivel 1 (1 = mismo tamano del menu; bajalo si la franja fina se ve cargada).")]
    [Range(0.4f, 1f)] public float factorTamanoNivel1 = 1f;

    /// <summary>El pack trae objetos para las casillas (hongos del nivel 1).</summary>
    public bool TieneObjetos => hongo1Sprite != null || hongo2Sprite != null;

    // --- Color del liquido del pack (paso 1 del plan de packs individualistas) ---
    /// <summary>
    /// false = el liquido se tiñe con el color del DUEÑO de la celda
    /// (rosa/naranja, como siempre). true = el pack trae su propio color
    /// (ej: verde del caldero). El "clasico" lo deja en false, asi que
    /// NO cambia nada de su aspecto.
    /// </summary>
    public bool usaColorPropio = false;
    [Tooltip("Color del liquido cuando 'Usa Color Propio' esta prendido.")]
    public Color colorLiquidoPropio = new Color(0.15f, 0.65f, 0.25f, 1f); // verde bruja

    /// <summary>Clip real para un nivel (1-3). 4 = boom. Null si el pack no lo trae.</summary>
    public AnimationClip ClipForLevel(int level) => level switch
    {
        1 => clip1,
        2 => clip2,
        3 => clip3,
        _ => clipBoom,
    };

    /// <summary>
    /// Las animaciones son de UNA PASADA: al terminar quedan congeladas en el
    /// ultimo frame (WrapMode.ClampForever), que es justo el estado "lleno"
    /// de la casilla. Nunca repiten.
    /// </summary>
    public static bool LoopsForLevel(int level) => false;

    /// <summary>Velocidad de reproduccion para un nivel.</summary>
    public float SpeedForLevel(int level) => level switch
    {
        1 => speed1,
        2 => speed2,
        3 => speed3,
        _ => speedBoom,
    };

    public bool HasArt =>
        glassPrefab != null && clip1 != null && clip2 != null && clip3 != null && clipBoom != null;

    public string KeyForLevel(int level) => level switch
    {
        1 => anim1,
        2 => anim2,
        3 => anim3,
        _ => animBoom, // 4 = explosion
    };
}

/// <summary>
/// Catalogo de packs. El "clasico" trae llaves vacias = look por codigo.
/// Agrega los tuyos AQUI una sola vez (ej. acuario) o con RegisterPack
/// desde tu propio loader. El jugador los cambia sin tocar codigo.
/// </summary>
public static class FxPackCatalog
{
    // Conversion lineal menu -> juego: boton 100px = 1 celda (1.2 unidades).
    public const float AnchoBotonMenuPx = 100f;

    /// <summary>
    /// Pasa un tamano medido en PIXELES del boton del menu a FRACCION de
    /// celda del juego (dimensiones lineales, sin factores arbitrarios).
    /// Ej: 23px / 100 = 0.23 de celda = 0.276 unidades de mundo.
    /// </summary>
    public static float FraccionDeCeldaDesdePx(float px)
        => px / AnchoBotonMenuPx;

    private static readonly List<FxPack> packs = new List<FxPack>
    {
        new FxPack("clasico", "Clasico", "clasico_1", "clasico_2", "clasico_3", "clasico_boom"),
        // Paso 6: el caldero entra como DATOS (aun sin arte). El juego
        // lo ve aunque abras GameScene directo, porque el catalogo se
        // arma aqui, no depende de que el boton del menu este en escena.
        new FxPack("caldero", "Caldero de Bruja", "caldero_1", "caldero_2", "caldero_3", "caldero_boom"),
        // Ejemplo para cuando hagas tus animaciones:
        // new FxPack("acuario", "Acuario", "acuario_1", "acuario_2", "acuario_3", "acuario_boom"),
    };

    // Receta del pack caldero (paso 6): color de bruja y burbujas propias.
    // Sin clips ni prefab todavia (HasArt = false): el tablero sigue
    // dibujando el cristal/clips del clasico hasta el paso 7.
    static FxPackCatalog()
    {
        var caldero = Get("caldero");
        if (caldero == null) return;
        caldero.usaColorPropio = true;
        caldero.colorLiquidoPropio = new Color(0.15f, 0.65f, 0.25f, 1f); // verde

        // Tamaños EXACTOS del menu convertidos por dimension lineal
        // (px del boton de 100 / 100 = fraccion de celda).
        caldero.hongo1Tamano      = FraccionDeCeldaDesdePx(14f);
        caldero.hongo2Tamano      = FraccionDeCeldaDesdePx(7f);
        caldero.hongoGrandeTamano = FraccionDeCeldaDesdePx(17f);
        caldero.ojoTamano         = FraccionDeCeldaDesdePx(23f);
        caldero.ojo2Tamano        = FraccionDeCeldaDesdePx(11f);
        caldero.huesoTamano       = FraccionDeCeldaDesdePx(65f);
        caldero.burbujasOn = true;
        caldero.bubbleRate = 5f;    // mas densas que el clasico (el usuario las vio poquitas)
        caldero.bubbleSize = 0.09f;
        caldero.bubbleRise = 0.5f;
        caldero.bubbleDrift = 0.01f;
        caldero.bubbleAlpha = 0.55f;

        // Sonido NUEVO del usuario: explosion_caldero.mp3 (en Assets/musica
        // y sonidos). Se busca por nombre para no arrastrar nada a mano.
        // El clasico conserva su sonido A/B del Inspector.
#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("explosion_caldero t:AudioClip");
        foreach (var g in guids)
        {
            string ruta = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            if (clip == null || clip.name != "explosion_caldero") continue;
            caldero.sonidoBoom = clip;      // ranura A del pack
            caldero.sonidoBoomB = null;     // no alternar: solo el nuevo
            caldero.modoSonidoBoom = 0;     // 0 = solo A
            // El mp3 viene masterizado mas alto que el del clasico: se
            // baja el volumen para que se perciban IGUAL de fuertes.
            caldero.boomVolume = 0.5f;      // clasico = 0.8
            Debug.Log($"[FxPacks] Sonido de explosion del caldero cargado: {ruta} (volumen {caldero.boomVolume})");
            break;
        }
#endif
        // NOTA para build: si el mp3 no esta en Resources, cuando hagas
        // build habria que arrastrarlo al Inspector del pack o moverlo a
        // Assets/Resources y usar Resources.Load. En el editor ya funciona.

        // Objetos del paso 8: se cargan los sprites por NOMBRE del proyecto
        // (igual que el sonido). Las proporciones vienen del menu calibrado.
#if UNITY_EDITOR
        caldero.hongo1Sprite = SpritePorNombre("hongo_color1");
        caldero.hongo2Sprite = SpritePorNombre("Hongo_color2");
        caldero.hongoGrandeSprite = SpritePorNombre("hongo_color1"); // clon mas grande
        caldero.ojoSprite = SpritePorNombre("base_ojo_color");
        caldero.irisCentral = SpritePorNombre("ojo_iris_central");
        caldero.irisLateral = SpritePorNombre("ojo_iris_lateral");
        caldero.irisVertical = SpritePorNombre("ojo_iris_vertical");
        caldero.huesoSprite = SpritePorNombre("Huesito");
#endif
    }

#if UNITY_EDITOR
    static Sprite SpritePorNombre(string nombre)
    {
        foreach (var g in UnityEditor.AssetDatabase.FindAssets(nombre + " t:Sprite"))
        {
            string ruta = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (sp != null && sp.name == nombre) return sp;
        }
        Debug.LogWarning($"[FxPacks] No se encontro el sprite '{nombre}'.");
        return null;
    }
#endif

    public static void RegisterPack(FxPack pack)
    {
        if (pack == null || string.IsNullOrEmpty(pack.id)) return;
        packs.RemoveAll(p => p.id == pack.id);
        packs.Add(pack);
    }

    public static FxPack Get(string id) => packs.Find(p => p.id == id);
    public static List<FxPack> All() => new List<FxPack>(packs);

    /// <summary>
    /// Adjunta el arte real a un pack existente sin pisar id/nombre/llaves.
    /// Lo llama CellVisuals en Awake con lo que hay en su Inspector.
    /// </summary>
    public static void AttachArt(string packId, GameObject glass, AnimationClip c1, AnimationClip c2, AnimationClip c3, AnimationClip cBoom, float s1, float s2, float s3, float sBoom, Sprite[] bubbles = null, AudioClip sonido = null)
    {
        var pack = Get(packId);
        if (pack == null) return;
        if (sonido != null) pack.sonidoBoom = sonido;
        if (glass != null) pack.glassPrefab = glass;
        if (c1 != null) pack.clip1 = c1;
        if (c2 != null) pack.clip2 = c2;
        if (c3 != null) pack.clip3 = c3;
        if (cBoom != null) pack.clipBoom = cBoom;
        pack.speed1 = s1;
        pack.speed2 = s2;
        pack.speed3 = s3;
        pack.speedBoom = sBoom;
        if (bubbles != null && bubbles.Length > 0) pack.bubbleSprites = bubbles;
    }
}