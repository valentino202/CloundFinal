using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Boton de color de Personalizacion: se ve IGUAL que una casilla del juego
/// (cristal encima, liquido lleno debajo, mismo tint y mismo alpha del vidrio).
/// Trae feedback visual al pasar el raton y al hacer clic.
///
/// Uso por boton:
///   1. GameObject -> UI -> Button, tamano cuadrado (ej 120x120).
///   2. Add Component -> ColorSwatch.
///   3. Arrastra Crystal Sprite (0tapa) y Liquid Sprite (liquido animacion_15).
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
    [Tooltip("liquido animacion_15 (el frame lleno del nivel 3).")]
    public Sprite liquidSprite;

    [Header("Color del boton")]
    public Color color = Color.white;

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

    public Image liquidImage;
    public Image crystalImage;

    public Color Color => color;

    private Vector3 baseScale;
    private Vector3 targetScale;
    private bool selected;

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
        targetScale = baseScale * (selected ? 1.18f : 1f);
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

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * smooth);
    }

    public void OnPointerEnter(PointerEventData _) => targetScale = baseScale * hoverScale;
    public void OnPointerExit(PointerEventData _) => targetScale = baseScale * (selected ? 1.18f : 1f);
    public void OnPointerDown(PointerEventData _) => targetScale = baseScale * pressScale;
    public void OnPointerUp(PointerEventData _) => targetScale = baseScale * (selected ? 1.18f : 1f);

    private void Build()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // Liquido: abajo, teñido y con el boost del juego.
        if (liquidImage == null && liquidSprite != null)
        {
            var go = new GameObject("Liquid");
            go.transform.SetParent(rt, false);
            var lrt = go.AddComponent<RectTransform>();
            Center(lrt, liquidFill);
            liquidImage = go.AddComponent<Image>();
            liquidImage.sprite = liquidSprite;
            liquidImage.color = BoardFx.Boost(color);
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