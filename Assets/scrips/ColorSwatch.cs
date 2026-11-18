using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Boton de color de Personalizacion: se ve IGUAL que una casilla del juego
/// (cristal encima, liquido debajo, mismo tint y mismo alpha del vidrio).
/// Trae feedback visual al pasar el raton y al hacer clic, y el liquido
/// SE LLENA animado al abrirse el panel (los frames del juego).
///
/// Uso por boton:
///   1. GameObject -> UI -> Button, tamano cuadrado (ej 120x120).
///   2. Add Component -> ColorSwatch.
///   3. Arrastra Crystal Sprite (0tapa) y los Frames Nivel 1/2/3
///      (1a..1e, 2a..2e, 3a..3e de 'liquido animacion').
///   4. Pon el Color del boton (rosa, naranja, ...).
///   5. Conecta el OnClic a UIManager.OnClickPlayerColorMode con su indice.
///
/// El cristal se dibuja con el alpha del PNG (el vidrio ya trae la transparencia
/// horneada) y el liquido con el boost de brillo del juego.
/// </summary>
public class ColorSwatch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Arte (arrastra una vez)")]
    [Tooltip("0tapa (el cristal, nunca se tinta).")]
    public Sprite crystalSprite;

    [Header("Animaciones del liquido (frames del juego)")]
    [Tooltip("Frames del nivel 1 (1a..1e de 'liquido animacion').")]
    public Sprite[] framesNivel1;
    [Tooltip("Frames del nivel 2 (2a..2e).")]
    public Sprite[] framesNivel2;
    [Tooltip("Frames del nivel 3, el lleno (3a..3e).")]
    public Sprite[] framesNivel3;
    [Tooltip("Frames por segundo del llenado.")]
    [Range(4f, 30f)] public float fpsLlenado = 14f;
    [Tooltip("Pausa entre niveles del llenado, en segundos.")]
    [Range(0f, 1.5f)] public float pausaNivel = 0.45f;

    [Header("Color del boton")]
    [Tooltip("-1 = usa el color manual de abajo. 0 = Rosa del juego, 1 = Naranja del juego. Sincronizado = MISMO color e intensidad que las casillas, sin copiar valores a mano.")]
    public int colorDelJuego = -1;
    public Color color = Color.white;

    /// <summary>El color que este boton representa: del juego si esta sincronizado, o el manual.</summary>
    public Color ColorResuelto()
        => colorDelJuego >= 0 ? PlayerColorOptions.Get(colorDelJuego, color) : color;

    [Header("Igual que en el juego")]
    [Tooltip("Fraccion del boton que ocupa el liquido.")]
    [Range(0.3f, 1f)] public float liquidFill = 0.9f;
    [Tooltip("Fraccion del boton que ocupa el cristal.")]
    [Range(0.3f, 1f)] public float crystalFill = 1f;
    [Tooltip("Alpha del vidrio. 1 = respeta el PNG (trae su transparencia).")]
    [Range(0f, 1f)] public float glassAlpha = 1f;

    [Header("Interaccion")]
    [Tooltip("Escala al pasar el raton.")]
    [Range(1f, 1.5f)] public float hoverScale = 1.14f;
    [Tooltip("Escala al hacer clic.")]
    [Range(0.5f, 1f)] public float pressScale = 0.86f;
    [Tooltip("Suavidad del escalado.")]
    [Range(1f, 30f)] public float smooth = 14f;
    [Tooltip("Al elegir este color se equipa el pack basico 'clasico' (deja el pack especial). Asi los colores y los packs forman UN solo grupo de seleccion.")]
    public bool equiparClasicoAlClic = true;
    [Tooltip("Escala cuando este color esta seleccionado.")]
    [Range(1f, 1.6f)] public float escalaSeleccion = 1.18f;

    public Image liquidImage;
    public Image crystalImage;

    public Color Color => ColorResuelto();

    private Vector3 baseScale;
    private Vector3 targetScale;
    private bool selected;
    private Coroutine llenado;

    /// <summary>Cambia el color y retinta el liquido.</summary>
    public void SetColor(Color c)
    {
        color = c;
        if (liquidImage != null) liquidImage.color = BoardFx.Boost(c);
    }

    /// <summary>Lo llama UIManager para marcar el boton activo.</summary>
    public void SetSelected(bool selected)
    {
        this.selected = selected;
        targetScale = baseScale * (selected ? escalaSeleccion : 1f);
        var frame = transform.Find("SelectedFrame");
        if (frame != null) frame.gameObject.SetActive(selected);
    }

    private void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
        Build();
        // Oculta la imagen del boton padre: solo debe verse el arte (liquido + cristal).
        var parentImg = GetComponent<Image>();
        if (parentImg != null) parentImg.color = new Color(0f, 0f, 0f, 0f);
    }

    private void OnEnable()
    {
        // Al abrirse el panel el liquido se llena animado (una pasada,
        // como las casillas y como los demas botones de packs). Solo si le
        // asignaron frames: sin frames queda estatico como siempre.
        if (Application.isPlaying && HayFrames()) Llenar();
    }

    private void OnDisable()
    {
        if (llenado != null) { StopCoroutine(llenado); llenado = null; }
    }

    private void Update()
    {
        // Seleccion como GRUPO (paso 4): este color solo esta seleccionado
        // si su color coincide con el perfil Y el pack equipado es el
        // "clasico". Si hay un pack especial equipado, NINGUN color se
        // agranda — el grupo lo decide el perfil, no el ultimo clic.
        if (Application.isPlaying && UGSProfileManager.Instance != null)
        {
            var perfil = UGSProfileManager.Instance.Profile;
            Color actual = PlayerColorOptions.Get(perfil.PlayerColorMode, perfil.PlayerCustomColor);
            var mio = ColorResuelto();
            bool sel = perfil.EquippedPackId == "clasico" &&
                       Mathf.Abs(mio.r - actual.r) < 0.02f &&
                       Mathf.Abs(mio.g - actual.g) < 0.02f &&
                       Mathf.Abs(mio.b - actual.b) < 0.02f;
            if (sel != selected) SetSelected(sel);
        }
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * smooth);
    }

    /// <summary>Reproduce el llenado completo (una pasada) y queda lleno.</summary>
    public void Llenar()
    {
        if (liquidImage == null) return;
        if (llenado != null) StopCoroutine(llenado);
        llenado = StartCoroutine(Llenado());
    }

    /// <summary>El llenado: nivel 1, pausa, nivel 2, pausa, nivel 3. Congelado al final, sin loop.</summary>
    IEnumerator Llenado()
    {
        var espera = new WaitForSeconds(1f / Mathf.Max(1f, fpsLlenado));
        var pausa = new WaitForSeconds(pausaNivel);
        liquidImage.sprite = null; // arranca con el vaso vacio

        var niveles = new[] { framesNivel1, framesNivel2, framesNivel3 };
        foreach (var nivel in niveles)
        {
            if (nivel == null || nivel.Length == 0) continue;
            for (int i = 0; i < nivel.Length; i++)
            {
                liquidImage.sprite = nivel[i];
                yield return espera;
            }
            if (pausaNivel > 0f) yield return pausa;
        }
        // Congela en el ultimo frame del nivel 3, como las casillas del juego.
        liquidImage.sprite = Ultimo(framesNivel3);
        llenado = null;
    }

    bool HayFrames()
        => (framesNivel1?.Length ?? 0) + (framesNivel2?.Length ?? 0) + (framesNivel3?.Length ?? 0) > 0;

    static Sprite Ultimo(Sprite[] frames)
        => frames != null && frames.Length > 0 ? frames[frames.Length - 1] : null;

    // El hover no encoge un boton seleccionado.
    public void OnPointerEnter(PointerEventData _)
        => targetScale = baseScale * Mathf.Max(hoverScale, selected ? escalaSeleccion : 1f);
    public void OnPointerExit(PointerEventData _)
        => targetScale = baseScale * (selected ? escalaSeleccion : 1f);
    public void OnPointerDown(PointerEventData _) => targetScale = baseScale * pressScale;
    public void OnPointerUp(PointerEventData _)
    {
        targetScale = baseScale * (selected ? escalaSeleccion : 1f);
        if (equiparClasicoAlClic) EquiparClasico();
    }

    /// <summary>
    /// Elegir un color implica volver al pack basico: asi el selector de
    /// packs y el de colores son UN solo grupo (uno se agranda, el otro
    /// vuelve a su escala). Guarda el cambio en el perfil (nube + local).
    /// </summary>
    async void EquiparClasico()
    {
        var perfil = UGSProfileManager.Instance;
        if (perfil == null || perfil.Profile.EquippedPackId == "clasico") return;
        await perfil.EquipPackAsync("clasico");
        Debug.Log($"[Selector] ✅ Elegiste color '{ColorResuelto()}'. " +
                  "Caja guardada: 'clasico' → LISTO PARA JUGAR CLASICO " +
                  "(pack especial quitado, sin burbujas, sonido base).", this);
    }

    private void Build()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // Liquido: abajo, teñido y con el boost del juego. El sprite lleno
        // sale del ULTIMO frame del nivel 3 (las animaciones son el arte).
        if (liquidImage == null && HayFrames())
        {
            var go = new GameObject("Liquid");
            go.transform.SetParent(rt, false);
            var lrt = go.AddComponent<RectTransform>();
            Center(lrt, liquidFill);
            liquidImage = go.AddComponent<Image>();
            liquidImage.sprite = Ultimo(framesNivel3);
            liquidImage.color = BoardFx.Boost(ColorResuelto());
            liquidImage.raycastTarget = false;
        }

        // Cristal: encima, con su propio alpha (nunca se tinta).
        if (crystalImage == null && crystalSprite != null)
        {
            var go = new GameObject("Crystal");
            go.transform.SetParent(rt, false);
            var crt = go.AddComponent<RectTransform>();
            Center(crt, crystalFill);
            crystalImage = go.AddComponent<Image>();
            crystalImage.sprite = crystalSprite;
            crystalImage.color = new Color(1f, 1f, 1f, glassAlpha);
            crystalImage.raycastTarget = false;
        }
    }

    private static void Center(RectTransform child, float frac)
    {
        child.anchorMin = new Vector2(0.5f, 0.5f);
        child.anchorMax = new Vector2(0.5f, 0.5f);
        child.pivot = new Vector2(0.5f, 0.5f);
        float w = child.parent != null ? ((RectTransform)child.parent).rect.width : 100f;
        float h = child.parent != null ? ((RectTransform)child.parent).rect.height : 100f;
        child.sizeDelta = new Vector2(w * frac, h * frac);
    }
}