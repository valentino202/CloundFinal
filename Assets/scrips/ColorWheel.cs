using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Rueda de colores táctil SIN assets: la textura se genera por código (círculo
/// HSV). Se usa así:
///   - Arrastra una Image cuadrada al campo Wheel Image.
///   - Suelta el GameObject de la rueda en el campo On Color Changed de UIManager
///     -> OnWheelColorChanged.
///   - El centro es gris/blanco (saturación 0), el borde son colores puros.
///   - El angulo = matiz, la distancia al centro = saturación.
/// </summary>
public class ColorWheel : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Rueda")]
    public Image wheelImage;

    [Header("Brillo (opcional)")]
    public Slider brightnessSlider;

    [Header("Salida")]
    public System.Action<Color> OnColorChanged;

    [Range(0.05f, 1f)] public float minSaturation = 0.15f;

    private float value = 1f;
    private static Texture2D cached;

    private void Awake()
    {
        if (brightnessSlider != null)
        {
            value = Mathf.Clamp01(brightnessSlider.value);
            brightnessSlider.onValueChanged.AddListener(OnBrightness);
        }
        // La rueda SIEMPRE se dibuja con el circulo HSV generado por codigo.
        // (Antes solo se generaba si el sprite era null; con el UISprite
        // puesto a mano quedaba un rectangulo celeste, no una rueda.)
        if (wheelImage != null)
            wheelImage.sprite = Sprite.Create(WheelTexture(256), new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
    }

    private void OnDestroy()
    {
        if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveListener(OnBrightness);
    }

    private void OnBrightness(float v) => value = Mathf.Clamp01(v);

    public void OnPointerDown(PointerEventData e) => Pick(e);
    public void OnDrag(PointerEventData e) => Pick(e);

    private void Pick(PointerEventData e)
    {
        if (wheelImage == null) return;
        var rt = wheelImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local))
            return;

        // Normalizado 0..1 con el centro en (0.5, 0.5)
        Vector2 n = new Vector2(local.x / rt.rect.width + 0.5f, local.y / rt.rect.height + 0.5f);
        Vector2 d = n - new Vector2(0.5f, 0.5f);

        float angle = Mathf.Atan2(d.y, d.x);                 // -PI..PI
        float hue = (angle + Mathf.PI) / (2f * Mathf.PI);     // 0..1
        float sat = Mathf.Clamp01(d.magnitude * 2f);
        if (sat < minSaturation) sat = minSaturation;         // nunca totalmente gris

        var c = Color.HSVToRGB(hue, sat, value);
        OnColorChanged?.Invoke(c);
    }

    /// <summary>Círculo HSV generado por código: matiz por ángulo, saturación por radio.</summary>
    public static Texture2D WheelTexture(int size)
    {
        if (cached != null && cached.width == size) return cached;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)(size - 1);
                float ny = y / (float)(size - 1);
                Vector2 d = new Vector2(nx - 0.5f, ny - 0.5f) * 2f;
                float r = d.magnitude;
                if (r > 1f) { tex.SetPixel(x, y, new Color(0, 0, 0, 0)); continue; }
                float hue = (Mathf.Atan2(d.y, d.x) + Mathf.PI) / (2f * Mathf.PI);
                tex.SetPixel(x, y, Color.HSVToRGB(hue, Mathf.Clamp01(r), 1f));
            }
        tex.Apply();
        cached = tex;
        return tex;
    }
}