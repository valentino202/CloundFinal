using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Burbujas del caldero, IGUAL que en el juego (CellVisuals), version UI:
///   - Nacen en una franja del FONDO del liquido.
///   - Suben parejas (rango angosto de velocidad) con una deriva lateral
///     minima, sin apelotonarse.
///   - Crecen un poco al subir (menos presion).
///   - REVIENTAN al llegar a la superficie: pop rapido, alpha a cero.
///   - Blancas sobre el verde, translucidas.
/// A mas nivel, mas burbujeo (la tasa escala con el nivel, como el juego).
/// Si el pack trae sprites de burbuja, cada burbuja usa uno al azar y se
/// queda con el toda su vida; si no, se genera el anillo por codigo.
/// La agrega SwatchCaldero solo: no hay que armar nada a mano.
/// </summary>
public class BurbujasCalderoUI : MonoBehaviour
{
    [Header("Burbujas (igual que en el juego)")]
    [Tooltip("Burbujas por segundo al nivel 3 (escala con el nivel).")]
    public float burbujasPorSegundo = 2.5f;
    [Tooltip("Tamano de la burbuja, como fraccion del ANCHO del liquido.")]
    [Range(0.01f, 0.2f)] public float tamanoFraccion = 0.05f;
    [Tooltip("Velocidad de subida, como fraccion del ALTO del liquido por segundo.")]
    [Range(0.05f, 1.5f)] public float subidaFraccion = 0.35f;
    [Tooltip("Deriva lateral suave, fraccion del ancho.")]
    [Range(0f, 0.3f)] public float derivaFraccion = 0.05f;
    [Tooltip("Transparencia de las burbujas (son bien translucidas).")]
    [Range(0.1f, 1f)] public float alpha = 0.4f;
    [Tooltip("Respiran el color del liquido: cuanto reciben del color REAL (se adapta a cambios).")]
    [Range(0f, 1f)] public float mezclaLiquido = 0.55f;
    [Tooltip("El color del liquido 'respira' en cada burbuja: sube y baja suave, cada una con su fase.")]
    public bool respirarColor = true;
    [Range(0.1f, 3f)] public float velocidadRespiro = 0.8f;

    [Header("Variacion por burbuja (profundidad: nadie es igual)")]
    [Tooltip("Unas casi INVISIBLES y otras bien MARCADAS. Hasta 2 = contraste fuerte. 0 = todas iguales.")]
    [Range(0f, 2f)] public float variacionAlpha = 0.6f;
    [Tooltip("Unas reciben MAS color del liquido, otras quedan mas blancas.")]
    [Range(0f, 1f)] public float variacionMezcla = 0.4f;
    [Tooltip("Unas respiran rapido, otras lento.")]
    [Range(0f, 1f)] public float variacionRespiro = 0.5f;

    [Header("Capas (profundidad de planos)")]
    [Tooltip("Porcentaje de burbujas que nacen DELANTE de los objetos (hongos/ojo/hueso). El resto queda detras, como siempre.")]
    [Range(0f, 1f)] public float porcentajeDelante = 0.3f;
    [Tooltip("Cuan fuerte el plano decide la transparencia: 0 = al azar igual, 1 = las de delante SIEMPRE marcadas y las de atras SIEMPRE tenues.")]
    [Range(0f, 1f)] public float sesgoProfundidad = 0.7f;

    [Tooltip("Sprites de burbuja del pack (opcional). Sin ellos: anillos generados por codigo, variantes al azar.")]
    public Sprite[] sprites;

    [Header("Multiplicadores (los maneja SwatchCaldero desde sus opciones)")]
    [Tooltip("Multiplica cuantas nacen por segundo. 1 = normal.")]
    public float multiplicadorTasa = 1f;
    [Tooltip("Multiplica el tamano de las burbujas. 1 = normal.")]
    public float multiplicadorTamano = 1f;
    [Tooltip("Multiplica lo rapido que suben. 1 = normal.")]
    public float multiplicadorVelocidad = 1f;
    [Tooltip("Multiplica la opacidad. 1 = normal, 2 = el doble de notorio.")]
    public float multiplicadorAlpha = 1f;

    RectTransform contenedor;    // el Liquid
    RectTransform vidrio;        // el boton (para crear la capa DELANTE)
    RectTransform frente;        // capa de burbujas DELANTE de los objetos
    int nivelActual;             // 0 = vacio (no burbujea)
    float superficieFraccion = 0.85f;
    float acumulador;

    class Burbuja
    {
        public RectTransform rt;
        public Image img;
        public float x0, y, tamano, fase, nacido;
        public float sx, sy;         // forma: cada burbuja es un pelin distinta
        public float alphaPropia;    // SU transparencia (dado al nacer)
        public float mezclaPropia;   // SU mezcla de color (dado al nacer)
        public float respiroVel;     // SU ritmo de respiro (dado al nacer)
        public bool pop;
        public float popT;
    }
    readonly List<Burbuja> vivas = new List<Burbuja>();
    readonly List<Burbuja> libres = new List<Burbuja>();

    // VARIOS anillos con formas distintas (los elige al azar cada burbuja).
    static readonly List<Sprite> anillos = new List<Sprite>();
    Image liquidImg;             // color real del liquido (por INSTANCIA: si es
                                 // static, al cambiar de escena queda apuntando
                                 // a un Liquid destruido y tumba el Update)

    /// <summary>La llama SwatchCaldero al arrancar: donde viven las burbujas.</summary>
    public void Configurar(RectTransform contenedorLiquido)
    {
        contenedor = contenedorLiquido;
        vidrio = contenedor != null ? contenedor.parent as RectTransform : null;
        AsegurarCapas();
        LimpiezaDeRestos();
    }

    /// <summary>
    /// Restos "cocidos": si la escena se guardo durante el Play, quedaron
    /// burbujas guardadas (apagadas, con sprites muertos) que el pool no
    /// conoce. Se borran al arrancar; el pool crea burbujas nuevas.
    /// </summary>
    void LimpiezaDeRestos()
    {
        if (contenedor != null)
            for (int i = contenedor.childCount - 1; i >= 0; i--)
                if (contenedor.GetChild(i).name == "Burbuja")
                    Destroy(contenedor.GetChild(i).gameObject);
        if (frente != null)
            for (int i = frente.childCount - 1; i >= 0; i--)
                if (frente.GetChild(i).name == "Burbuja")
                    Destroy(frente.GetChild(i).gameObject);
    }

    /// <summary>
    /// La capa DELANTE: un contenedor "BurbujasFrente" hijo del boton, justo
    /// ANTES del Crystal. Las burbujas de esa capa pasan por ENCIMA de los
    /// objetos (hongos/ojo/hueso) pero siguen detras del vidrio.
    /// </summary>
    void AsegurarCapas()
    {
        if (vidrio == null) return;
        var t = vidrio.Find("BurbujasFrente");
        if (t == null)
        {
            var go = new GameObject("BurbujasFrente");
            go.transform.SetParent(vidrio, false);
            t = go.transform;
        }
        frente = t as RectTransform;
        if (frente == null) frente = t.gameObject.AddComponent<RectTransform>();
        frente.anchorMin = new Vector2(0.5f, 0.5f);
        frente.anchorMax = new Vector2(0.5f, 0.5f);
        frente.pivot = new Vector2(0.5f, 0.5f);
        frente.sizeDelta = vidrio.rect.size;
        frente.anchoredPosition = Vector2.zero;
        var crystal = vidrio.Find("Crystal");
        if (crystal != null)
        {
            frente.SetSiblingIndex(crystal.GetSiblingIndex());
            crystal.SetSiblingIndex(vidrio.childCount - 1); // el vidrio siempre ultimo
        }
    }

    /// <summary>La llama SwatchCaldero al completar cada nivel del llenado.</summary>
    public void SetNivel(int nivel, float alturaFranja)
    {
        nivelActual = nivel;
        superficieFraccion = alturaFranja;
    }

    /// <summary>Limpia todas (al reabrir el panel arranca sin burbujas).</summary>
    public void Reiniciar()
    {
        nivelActual = 0;
        acumulador = 0f;
        for (int i = 0; i < vivas.Count; i++) { vivas[i].img.gameObject.SetActive(false); libres.Add(vivas[i]); }
        vivas.Clear();
    }

    void Update()
    {
        if (contenedor == null || nivelActual <= 0) return;
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        // Tasa que escala con el nivel, igual que en el juego (y con el
        // multiplicador de densidad de las opciones).
        acumulador += burbujasPorSegundo * multiplicadorTasa * (nivelActual / 3f) * dt;
        while (acumulador >= 1f) { acumulador -= 1f; Nacer(); }

        float h = contenedor.rect.height;
        float w = contenedor.rect.width;
        float fondo = -h * 0.5f;
        float superficie = fondo + h * superficieFraccion;
        float subida = h * subidaFraccion * multiplicadorVelocidad;

        // Color del liquido, leido UNA vez por frame (respira el color REAL
        // del caldero: si cambia el color, cambian todas las burbujas).
        Color liq = ColorDelLiquido();

        for (int i = vivas.Count - 1; i >= 0; i--)
        {
            var b = vivas[i];
            float edad = t - b.nacido;

            // Seguro anti-cuadrados: si la burbuja perdio su sprite (casilla
            // vacia en el arreglo), se le asigna uno de nuevo al instante.
            if (b.img.sprite == null) b.img.sprite = ElegirSprite();

            // Respiro: la fuerza del color sube y baja con SU ritmo.
            float resp = respirarColor
                ? 0.75f + 0.25f * Mathf.Sin(t * b.respiroVel + b.fase)
                : 1f;
            // El color base: blanco lavado con SU mezcla del liquido.
            Color baseC = Color.Lerp(Color.white, liq, b.mezclaPropia);

            if (b.pop)
            {
                // Pop rapido: crece un pelin y desaparece.
                b.popT += dt;
                float k = Mathf.Clamp01(b.popT / 0.15f);
                float esc = 1.15f + 0.45f * k;
                b.rt.sizeDelta = new Vector2(b.sx * esc, b.sy * esc);
                baseC.a = b.alphaPropia * multiplicadorAlpha * (1f - k);
                b.img.color = baseC;
                if (k >= 1f) Liberar(i);
                continue;
            }

            // Subida + deriva lateral suave (cada una con su fase).
            b.y += subida * dt;
            if (b.y >= superficie) { b.pop = true; b.popT = 0f; }

            float x = b.x0 + Mathf.Sin(t * 1.5f + b.fase) * w * derivaFraccion;

            // Crecen un poco al subir (0.85 -> 1.15), como en el juego,
            // conservando SU forma (cada una es un pelin distinta).
            float prog = Mathf.Clamp01((b.y - fondo) / Mathf.Max(1f, superficie - fondo));
            float crece = Mathf.Lerp(0.85f, 1.15f, prog);

            // Fade in al nacer (primeros 0.12s), como la curva del juego.
            baseC.a = b.alphaPropia * multiplicadorAlpha * resp * Mathf.Clamp01(edad / 0.12f);

            b.rt.anchoredPosition = new Vector2(x, b.y);
            b.rt.sizeDelta = new Vector2(b.sx * crece, b.sy * crece);
            b.img.color = baseC;
        }
    }

    void Nacer()
    {
        var b = libres.Count > 0 ? SacarLibre() : Crear();
        float w = contenedor.rect.width;
        float h = contenedor.rect.height;
        float margen = b.tamano * 0.5f + w * 0.04f;
        b.x0 = Random.Range(-w * 0.5f + margen, w * 0.5f - margen);
        b.y = -h * 0.5f + Random.Range(0.02f, 0.08f) * h;
        b.tamano = w * tamanoFraccion * multiplicadorTamano * Random.Range(0.6f, 1.5f);  // tamanos MUY variados
        // Forma: cada burbuja es un pelin distinta (ni redonda perfecta).
        b.sx = b.tamano * Random.Range(0.85f, 1.2f);
        b.sy = b.tamano * Random.Range(0.85f, 1.2f);

        // CAPAS: el plano se decide PRIMERO, porque define SU transparencia.
        // Delante de los objetos = tendencia a MARCADA; atras = tendencia a
        // TENUE (el sesgoProfundidad mide cuan fuerte se inclina el dado).
        bool delante = frente != null && Random.value < porcentajeDelante;
        if (delante != (b.rt.parent == frente))
            b.rt.SetParent(delante ? frente : contenedor, false);
        b.rt.SetAsLastSibling();

        // PROFUNDIDAD: SU alpha (sesgado por plano), SU mezcla de color y
        // SU ritmo de respiro — nadie plana, y el vaso tiene capas reales.
        float dado = Random.value;
        if (delante) dado = Mathf.Lerp(dado, 1f, sesgoProfundidad);
        else dado = Mathf.Lerp(dado, 0f, sesgoProfundidad);
        b.alphaPropia = Mathf.Clamp01(alpha * Mathf.Lerp(1f - variacionAlpha, 1f + variacionAlpha, dado));
        b.mezclaPropia = Mathf.Clamp01(mezclaLiquido * Random.Range(1f - variacionMezcla, 1f + variacionMezcla));
        b.respiroVel = velocidadRespiro * Random.Range(1f - variacionRespiro, 1f + variacionRespiro);
        b.fase = Random.value * Mathf.PI * 2f;
        b.nacido = Time.unscaledTime;
        b.pop = false;

        // Sprite: uno del pack al azar (y se queda con el), o una variante
        // de anillo generada por codigo (formas distintas entre si).
        b.img.sprite = ElegirSprite();
        b.img.color = new Color(1f, 1f, 1f, 0f); // nace invisible, hace fade in
        b.rt.sizeDelta = new Vector2(b.sx, b.sy);
        b.rt.anchoredPosition = new Vector2(b.x0, b.y);
        b.rt.localScale = Vector3.one;
    }

    void Liberar(int indice)
    {
        var b = vivas[indice];
        b.img.gameObject.SetActive(false);
        vivas.RemoveAt(indice);
        libres.Add(b);
    }

    Burbuja SacarLibre()
    {
        var b = libres[libres.Count - 1];
        libres.RemoveAt(libres.Count - 1);
        b.img.gameObject.SetActive(true);
        vivas.Add(b);
        return b;
    }

    Burbuja Crear()
    {
        var go = new GameObject("Burbuja");
        go.transform.SetParent(contenedor, false);
        var b = new Burbuja
        {
            rt = go.AddComponent<RectTransform>(),
            img = go.AddComponent<Image>()
        };
        b.img.raycastTarget = false;
        vivas.Add(b);
        return b;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Si la escena se guardo DURANTE el Play, las burbujas creadas por
    /// codigo quedaron cocidas en la escena (y sus sprites mueren al salir
    /// del Play = cuadrados blancos congelados). En el editor se borran:
    /// las burbujas SOLO existen durante el Play.
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying) return;
        var caldero = GetComponentInParent<SwatchCaldero>();
        if (caldero == null) return;
        var hijos = caldero.GetComponentsInChildren<Transform>(true);
        foreach (var h in hijos)
            if (h != null && h.name == "Burbuja")
                DestroyImmediate(h.gameObject);
    }
#endif

    /// <summary>
    /// Anillos generados por codigo (base: la MISMA textura del juego,
    /// CellVisuals.BubbleTexture) pero en VARIAS formas: anillo mas adentro/
    /// afuera, mas grueso/finos, con mas/menos relleno. Cada burbuja elige
    /// uno al azar y se queda con el toda su vida.
    /// </summary>
    static List<Sprite> Anillos()
    {
        // A prueba de sprites muertos: si la lista sobrevivio con anillos
        // viejos cuyas texturas se destruyeron, se reconstruye completa.
        if (anillos.Count > 0)
        {
            for (int i = 0; i < anillos.Count; i++)
                if (anillos[i] == null) { anillos.Clear(); break; }
        }
        if (anillos.Count > 0) return anillos;
        int s = 64;
        // Cuatro variantes: anillo posicion, grosor y relleno distintos.
        float[] posAnillo = { 0.62f, 0.70f, 0.76f, 0.84f };
        float[] grosor = { 7f, 5f, 6f, 4f };
        float[] relleno = { 0.30f, 0.22f, 0.16f, 0.26f };
        for (int v = 0; v < 4; v++)
        {
            var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - s * 0.5f) / (s * 0.5f);
                    float dy = (y - s * 0.5f) / (s * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(d - posAnillo[v]) * grosor[v]);
                    float fill = Mathf.Clamp01(1f - d) * relleno[v];
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ring + fill)));
                }
            tex.Apply();
            anillos.Add(Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f));
        }
        return anillos;
    }

    /// <summary>
    /// Elige el sprite: uno del pack (si el arreglo lo tiene en esa casilla);
    /// si la casilla esta VACIA o no hay pack, cae a un anillo generado.
    /// Asi JAMAS queda sin sprite: sin sprite = cuadrado blanco de Unity.
    /// </summary>
    Sprite ElegirSprite()
    {
        if (sprites != null && sprites.Length > 0)
        {
            int i = Random.Range(0, sprites.Length);
            if (sprites[i] != null) return sprites[i];
        }
        var an = Anillos();
        return an[Random.Range(0, an.Count)];
    }

    /// <summary>
    /// Color REAL del Liquid, para el respiro. A prueba de escenas: si el
    /// Liquid se destruyo (cambio de escena/panel), Unity lo ve como null y
    /// se vuelve a buscar. NUNCA tira excepcion: sin color, blanco y listo.
    /// </summary>
    Color ColorDelLiquido()
    {
        if (liquidImg == null && contenedor != null)
            liquidImg = contenedor.GetComponent<Image>();
        if (liquidImg != null) return liquidImg.color;
        return Color.white;
    }
}
