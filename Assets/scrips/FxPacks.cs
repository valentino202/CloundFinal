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
    [Tooltip("Burbujas por segundo en nivel 3 (menu: 15).")]
    public float bubbleRate = 15f;
    [Tooltip("Tamano de la burbuja, en celdas (menu: 0.0538; reducido a pedido).")]
    public float bubbleSize = 0.035f;
    [Tooltip("Velocidad de subida, en celdas por segundo (menu: 0.276).")]
    public float bubbleRise = 0.276f;
    [Tooltip("Deriva lateral, en celdas (menu: 0.005).")]
    public float bubbleDrift = 0.005f;
    [Range(0f, 1f)] public float bubbleAlpha = 0.231f;
    public int bubbleOrder = 1;
    [Tooltip("Franjas del verde por nivel del MENU (0.25 / 0.5 / 0.85).")]
    public float franjaNivel1 = 0.25f;
    public float franjaNivel2 = 0.5f;
    public float franjaNivel3 = 0.85f;

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
    [Tooltip("Opacidad de los objetos (0.85 = 85%). Bajar para que se vean mas dentro del liquido.")]
    [Range(0.3f, 1f)] public float hongosOpacidad = 0.85f;

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

    // --- Efectos del menu portados (burbujas de 2 capas + vapor en lineas) ---
    // Multiplicadores generales del Inspector del menu (todos en 1).
    [Tooltip("Densidad: cuantas nacen por segundo.")]
    [Range(0f, 4f)] public float densidadEfectos = 1f;
    [Tooltip("Tamano de burbujas y puffs.")]
    [Range(0.2f, 3f)] public float tamanoEfectos = 1f;
    [Tooltip("Velocidad de subida.")]
    [Range(0.2f, 3f)] public float velocidadEfectos = 1f;
    [Tooltip("Opacidad de burbujas y vapor.")]
    [Range(0f, 3f)] public float opacidadEfectos = 1f;

    [Header("Numeros del pack (color por nivel 1->3)")]
    [Tooltip("Color del numero en nivel 1. Vacio = usa el blanco del Inspector.")]
    public Color numeroColorBajo = new Color(1f, 0.97f, 0.35f, 1f);   // amarillo claro
    [Tooltip("Color del numero en nivel alto (3 = a punto de explotar): amarillo brillante tipo indicador, que destaque aunque haya muchos objetos.")]
    public Color numeroColorAlto = new Color(1f, 0.92f, 0.05f, 1f);   // amarillo explosion
    [Tooltip("Si el pack define su propia paleta de numeros (caldero = si; clasico = no).")]
    public bool numeroPropio = false;
    [Tooltip("Contorno del numero para que destaque sobre objetos/verde. Negativo = negro, 0 = sin contorno.")]
    public float numeroContorno = 0.25f;
    [Tooltip("Color del contorno (negro por defecto = maximo contraste).")]
    public Color numeroContornoColor = new Color(0f, 0f, 0f, 0.85f);

    [Header("Ciclo de descanso de efectos (evita saturar la vista)")]
    [Tooltip("Segundos que los efectos estan ACTIVOS (rafaga).")]
    [Range(0.5f, 10f)] public float efectosRafaga = 2f;
    [Tooltip("Segundos de DESCANSO sin burbujas ni vapor; luego vuelven.")]
    [Range(0f, 10f)] public float efectosDescanso = 2f;
    [Header("Burbujas: variacion y planos (menu)")]
    [Range(0f, 2f)] public float burbujaVariacionAlpha = 2f;      // menu: 2
    [Range(0f, 1f)] public float burbujaVariacionMezcla = 0.299f; // menu: 0.299
    [Range(0f, 1f)] public float burbujaVariacionRespiro = 0.72f; // menu: 0.72
    [Range(0f, 1f)] public float burbujaMezclaLiquido = 1f;       // menu: 1
    [Tooltip("Velo extra del color del liquido para que las burbujas se vean SUMERGIDAS, no flotando.")]
    [Range(0f, 1f)] public float burbujaVeloSumergido = 0.35f;
    public float burbujaVelocidadRespiro = 0.61f;                 // menu: 0.61
    [Tooltip("Porcentaje de burbujas DELANTE de los objetos. 0.5 = mitad delante, mitad atras (mezcla de capas).")]
    [Range(0f, 1f)] public float burbujaPorcentajeDelante = 0.5f;
    [Tooltip("Las de delante marcadas, las de atras tenues (menu: 0.206).")]
    [Range(0f, 1f)] public float burbujaSesgoProfundidad = 0.206f;
    [Tooltip("CANTIDAD extra de burbujas: compensa que las celdas nivel 1 recorren menos que el menu siempre-lleno. 1 = valores del menu.")]
    [Range(1f, 6f)] public float burbujaCantidad = 4f;
    [Tooltip("Contraste entre la capa DELANTE (marcada) y la de ATRAS (tenue). 0 = como el menu; subir para notar mas las dos capas.")]
    [Range(0f, 1f)] public float burbujaContrasteCapas = 0.45f;

    [Header("Vapor en lineas (menu)")]
    [Tooltip("El pack TRAE vapor. false = clasico (sin vapor), true = caldero.")]
    public bool vaporOn = false;
    [Tooltip("Lineas por segundo al nivel 3 (menu: 1.94).")]
    [Range(0.1f, 3f)] public float vaporLineas = 1.94f;        // menu: 1.94
    [Range(2, 8)] public int vaporPuffsPorLinea = 6;           // menu: 6
    [Range(0.2f, 1f)] public float vaporAnchoLinea = 0.606f;   // menu: 0.606
    [Range(0f, 1f)] public float vaporIrregularidad = 0.1f;
    [Range(0.05f, 0.5f)] public float vaporTamano = 0.145f;    // menu: 0.145
    [Range(0.05f, 1f)] public float vaporSubida = 0.2f;        // menu: 0.2
    [Range(0f, 2f)] public float vaporCrecimiento = 1.022f;    // menu: 1.022
    [Range(0.05f, 1f)] public float vaporAlpha = 0.213f;       // menu: 0.213
    [Range(0f, 1f)] public float vaporMezclaLiquido = 0.729f;  // menu: 0.729
    public float vaporVelocidadRespiro = 0.7f;                 // menu: 0.7
    [Tooltip("0 = nace en la superficie, 0.5 = mitad del verde (menu).")]
    [Range(0f, 1f)] public float vaporNacerAbajo = 0.5f;
    [Tooltip("Cuanto sube por encima de la superficie del nivel (0.25 del menu).")]
    [Range(0.05f, 1f)] public float vaporTecho = 0.08f;        // menu: 0.08

    [Tooltip("Segundo ojo del nivel 3 (11px => mas chico que el primero).")]
    [Range(0.05f, 1f)] public float ojo2Tamano = 0.11f; // menu: 11px / 100
    [Tooltip("Azul de la iris del SEGUNDO ojo (menu).")]
    public Color irisColor2 = new Color(0f, 0.192f, 1f, 1f);

    // --- BOOM con voladores (paso 12, solo en el juego) ---
    [Tooltip("Cuantos objetos salen volando al explotar.")]
    [Range(1, 12)] public int boomObjetos = 6;
    [Tooltip("Rapidez de los voladores, en celdas por segundo.")]
    [Range(0.5f, 8f)] public float boomVelocidad = 3.5f;
    [Tooltip("Cuanto viven los voladores antes de desaparecer, en segundos.")]
    [Range(0.3f, 2f)] public float boomVida = 0.8f;

    [Tooltip("Unica perilla para escalar TODOS los objetos juntos (1 = medidas convertidas del menu).")]
    [Range(0.4f, 2f)] public float objetosEscalaGlobal = 1f;
    [Tooltip("Factor extra SOLO en nivel 1 (1 = mismo tamano del menu; bajalo si la franja fina se ve cargada).")]
    [Range(0.4f, 1f)] public float factorTamanoNivel1 = 1f;

    /// <summary>El pack trae objetos para las casillas (hongos del nivel 1).</summary>
    public bool TieneObjetos => hongo1Sprite != null || hongo2Sprite != null;

    // --- Color del liquido del pack (paso 1 del plan de packs individualistas) ---
    [Tooltip("Brillo extra SOLO para el color propio del pack (1 = usa el color tal cual; subir para mas vivo).")]
    [Range(1f, 2.5f)] public float colorBoostPropio = 1.8f;
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
        caldero.burbujasOn = true;
        caldero.vaporOn = true;
        caldero.numeroPropio = true; // numeros amarillo->naranja con contraste sobre el verde

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