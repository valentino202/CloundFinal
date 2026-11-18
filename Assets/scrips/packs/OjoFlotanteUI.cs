using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// El ojo del caldero: esta VIVO. La iris (roja, arte coloreado) se corre
/// hacia donde mira con 5 miradas: central (base normal), lateral espejada
/// (izquierda/derecha) y vertical espejada (arriba/abajo). Si solo asignas
/// la central, las otras miradas la reutilizan.
/// Cambia de mirada a intervalos aleatorios, movimiento suavizado y un poco
/// de azar en el punto de llegada: nunca se ve mecanico.
/// El ojo en si flota como los hongos: ObjetoEnLiquidoUI en el padre.
///
/// Armado (a mano en la escena):
///   Ojo  (Image base_ojo_color, blanco)  <- ObjetoEnLiquidoUI + ESTE script
///     Iris  (Image iris roja, ~68% del ojo)  <- arrastrala al campo Iris
/// La iris se mueve por anchoredPosition dentro del ojo; al ser hija,
/// Unity la dibuja encima de la esclerotica sola.
/// </summary>
public class OjoFlotanteUI : MonoBehaviour
{
    [Header("Iris (arrastra la Image hija)")]
    [Tooltip("La Image de la iris (hija de este ojo). Si la dejas vacia la busca sola entre los hijos.")]
    public Image iris;
    [Tooltip("Pone la iris ROJO INTENSO por codigo: nunca se la come el verde del liquido. Si el arte ya viene rojo, podes apagarlo.")]
    public bool irisRojaPorCodigo = true;
    [Tooltip("El rojo intenso de la iris.")]
    public Color tinteIris = new Color(1f, 0.13f, 0.13f, 1f);
    [Tooltip("Iris mirando al frente (la base).")]
    public Sprite irisCentral;
    [Tooltip("Iris mirando a un costado (se espeja para el otro lado). Opcional.")]
    public Sprite irisLateral;
    [Tooltip("Iris mirando arriba/abajo (se espeja para abajo). Opcional.")]
    public Sprite irisVertical;

    [Header("Micro-movimiento")]
    [Tooltip("Cuanto se corre la iris hacia donde mira, como fraccion del ancho del ojo.")]
    [Range(0f, 0.4f)] public float desplazamiento = 0.22f;
    [Tooltip("Los barridos a los costados recorren MAS (como un ojo de verdad).")]
    [Range(1f, 3f)] public float extraLateral = 1.4f;
    [Tooltip("Suavidad del movimiento (segundos).")]
    [Range(0.05f, 0.5f)] public float suavidad = 0.15f;

    [Header("Ritmo de la mirada")]
    [Tooltip("Minimo y maximo de segundos entre cambios de mirada.")]
    public float minEntreCambios = 0.5f;
    public float maxEntreCambios = 1.5f;

    RectTransform irisRt;
    Vector2 irisBase;      // posicion de reposo de la iris
    Vector2 actual;        // offset actual (para el lerp)
    Vector2 destino;       // offset al que va
    float proximoCambio;
    float amplitud;

    struct Mirada
    {
        public Sprite sprite;
        public bool espejoX, espejoY;  // el mismo sprite mira a los dos lados
        public Vector2 dir;            // hacia donde "mira"
        public bool lateral;           // los laterales recorren MAS
        public bool valida;
    }
    Mirada[] miradas;

    void Awake()
    {
        // Si no se arrastro la iris, busca la primera Image hija.
        if (iris == null)
        {
            foreach (Transform hijo in transform)
            {
                var i = hijo.GetComponent<Image>();
                if (i != null) { iris = i; break; }
            }
        }
        if (iris == null) return;
        irisRt = iris.rectTransform;
        irisBase = irisRt.anchoredPosition;
        // La iris SIEMPRE rojo intenso: su color se fija aca y nadie mas la
        // toca (el flotador con Teñir Hijos apagado no la molesta).
        if (irisRojaPorCodigo) iris.color = tinteIris;

        var rt = GetComponent<RectTransform>();
        // Amplitud real: fraccion del ancho del ojo (respeta tu escala).
        amplitud = rt != null && rt.rect.width > 1f ? rt.rect.width * desplazamiento : 14f;

        // 5 miradas: central + espejos. Sin lateral/vertical, la central
        // cubre todo (la pupila vertical es simetrica).
        miradas = new Mirada[5];
        miradas[0] = new Mirada { sprite = irisCentral, valida = irisCentral != null };
        miradas[1] = new Mirada { sprite = irisLateral != null ? irisLateral : irisCentral,
                                  dir = Vector2.right, lateral = true, valida = irisCentral != null };
        miradas[2] = new Mirada { sprite = irisLateral != null ? irisLateral : irisCentral,
                                  espejoX = irisLateral != null, dir = Vector2.left, lateral = true,
                                  valida = irisCentral != null };
        miradas[3] = new Mirada { sprite = irisVertical != null ? irisVertical : irisCentral,
                                  dir = Vector2.up, valida = irisCentral != null };
        miradas[4] = new Mirada { sprite = irisVertical != null ? irisVertical : irisCentral,
                                  espejoY = irisVertical != null, dir = Vector2.down,
                                  valida = irisCentral != null };

        proximoCambio = Random.Range(0.3f, 0.9f);
    }

    void Update()
    {
        if (irisRt == null) return;
        float t = Time.unscaledTime;

        // Cambio de mirada: aleatorio, con pausa distinta cada vez.
        if (t >= proximoCambio)
        {
            proximoCambio = t + Random.Range(minEntreCambios, maxEntreCambios);
            MirarAleatorio();
        }

        // Movimiento suavizado hacia el destino (la "palpitacion" del vaiven).
        actual = Vector2.Lerp(actual, destino, Time.unscaledDeltaTime / Mathf.Max(0.02f, suavidad));
        irisRt.anchoredPosition = irisBase + actual;
    }

    /// <summary>Elige una mirada al azar (solo entre las que tienen sprite).</summary>
    void MirarAleatorio()
    {
        if (miradas == null) return;
        int validas = 0;
        for (int i = 0; i < miradas.Length; i++) if (miradas[i].valida) validas++;
        if (validas == 0) return;

        int elegida = Random.Range(0, validas);
        for (int i = 0; i < miradas.Length; i++)
        {
            if (!miradas[i].valida) continue;
            if (elegida-- == 0) { Aplicar(miradas[i]); return; }
        }
    }

    void Aplicar(Mirada m)
    {
        iris.sprite = m.sprite;
        // Espejo por escala negativa: el mismo sprite mira a ambos lados.
        irisRt.localScale = new Vector3(m.espejoX ? -1f : 1f, m.espejoY ? -1f : 1f, 1f);

        // Los laterales recorren MAS distancia, como un ojo de verdad que
        // barre a los costados. Mas un poco de azar: no siempre llega exacto
        // al tope (nunca mecanico).
        float amplitudReal = amplitud * (m.lateral ? extraLateral : 1f);
        destino = m.dir * amplitudReal + Random.insideUnitCircle * amplitudReal * 0.25f;
    }

    /// <summary>Diagnostico: clic derecho en el componente > Diagnosticar Ojo.</summary>
    [ContextMenu("Diagnosticar Ojo")]
    void DiagnosticarOjo()
    {
        int validas = 0;
        if (miradas != null) for (int i = 0; i < miradas.Length; i++) if (miradas[i].valida) validas++;
        Debug.Log($"[OjoFlotanteUI] {name}: iris={(iris != null ? iris.name : "NO encontrada")}, " +
                  $"miradas validas={validas}/5, amplitud={amplitud:F1}px", this);
    }
}
