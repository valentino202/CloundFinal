using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Copia SUMERGIDA de un objeto que pasea (el hueso). Se usa cuando el
/// objeto tiene una parte dentro del liquido y otra al aire:
///
///   - El original (Hueso) pasea y se ve NORMAL (sin tinte, solido): esa
///     es la parte al aire.
///   - Esta copia (Hueso_Agua) lo sigue frame a frame (posicion, rotacion
///     y escala IDENTICAS) pero vive dentro de una zona con RectMask2D
///     ("ZonaAgua", el rectangulo del agua): solo se ve la parte que queda
///     bajo la linea del agua, teñida con el color REAL del liquido y
///     translucida — tapando al original en esa zona.
///
/// Armado (a mano):
///   ZonaAgua   (hijo del boton, RectMask2D, rect = la zona del agua)
///     Hueso_Agua  (Image Huesito + ESTE script, Original = el Hueso)
///   ZonaAgua va DESPUES del Hueso en la jerarquia (dibuja encima de el)
///   y ANTES del Crystal.
/// </summary>
public class ParteSumergidaUI : MonoBehaviour
{
    [Header("Seguir al original")]
    [Tooltip("El objeto que pasea (el Hueso). La copia lo copia frame a frame.")]
    public Transform original;

    [Header("Tinte sumergido")]
    [Tooltip("Cuanto recibe del color REAL del liquid (se adapta a cambios).")]
    [Range(0f, 1f)] public float fuerzaSumergido = 0.5f;
    [Tooltip("Transparencia de la parte sumergida.")]
    [Range(0.3f, 1f)] public float opacidad = 0.9f;
    [Tooltip("El tinte 'respira' igual que los demas objetos.")]
    public bool respirarColor = true;
    [Range(0.1f, 3f)] public float velocidadRespiro = 0.8f;

    Image img;
    Image liquid;
    float fase;

    void Awake()
    {
        img = GetComponent<Image>();
        // SAFARI DE SINCRONIA: si esta copia tiene un ObjetoEnLiquidoUI
        // (se agrego por error), se lo saca — la copia NO pasea sola, solo
        // copia al original frame a frame.
        var flotador = GetComponent<ObjetoEnLiquidoUI>();
        if (flotador != null)
        {
            Debug.LogWarning($"[ParteSumergidaUI] {name}: tenia un ObjetoEnLiquidoUI y se lo saque — la copia solo sigue a su original.", this);
            Destroy(flotador);
        }
        // Si no te arrastraron el original, lo BUSCO solo: es el hijo
        // "Hueso" del boton (aunque me cambies de padre, lo encuentro).
        // Sin importar mayusculas/minusculas en el nombre.
        if (original == null)
        {
            var caldero = GetComponentInParent<SwatchCaldero>();
            if (caldero != null)
            {
                foreach (Transform c in caldero.transform)
                {
                    if (c.name.Equals("Hueso", System.StringComparison.OrdinalIgnoreCase))
                    {
                        original = c;
                        Debug.Log($"[ParteSumergidaUI] {name}: original no asignado — enlazado automaticamente a {c.name}.", this);
                        break;
                    }
                }
            }
        }
        if (original == null)
            Debug.LogWarning($"[ParteSumergidaUI] {name}: sin original. Arrastra el Hueso al campo Original (o nombra al hueso 'Hueso').", this);
        // Fase propia: el respiro no va sincronizado con nada.
        fase = Random.value * Mathf.PI * 2f;
    }

    void LateUpdate()
    {
        // Copia EXACTA del original: misma posicion mundial, rotacion y
        // escala — se ve como el mismo hueso, solo cambia el tinte.
        if (original != null)
        {
            transform.position = original.position;
            transform.rotation = original.rotation;
            transform.localScale = original.localScale;
        }

        if (img == null) return;
        Color liq = ColorDelLiquido();
        if (liq.a <= 0f) return;

        // El velo verde del brebaje, con el mismo respiro del resto.
        float fuerza = respirarColor
            ? fuerzaSumergido * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * velocidadRespiro + fase))
            : fuerzaSumergido;
        Color c = Color.Lerp(Color.white, liq, fuerza);
        c.a = opacidad;
        img.color = c;
    }

    /// <summary>Color del Liquid del boton (respaldo: el color del caldero).</summary>
    Color ColorDelLiquido()
    {
        if (liquid == null)
        {
            var caldero = GetComponentInParent<SwatchCaldero>();
            if (caldero != null)
            {
                var t = caldero.transform.Find("Liquid");
                if (t != null) liquid = t.GetComponent<Image>();
            }
        }
        if (liquid != null) return liquid.color;
        var c = GetComponentInParent<SwatchCaldero>();
        return c != null ? BoardFx.Boost(c.colorLiquido) : new Color(0f, 0f, 0f, 0f);
    }
}
