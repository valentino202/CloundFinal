using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tienda de packs (Paso 5) — por ahora solo el CALDERO DE BRUJA.
///
/// NO hace una copia del pack: el caldero ya existe como datos en
/// FxPackCatalog (id "caldero"). La tienda solo:
///   1) muestra su precio y los balances de monedas/gemas,
///   2) al comprar: descuenta monedas/gemas, DESBLOQUEA el pack en el
///      perfil y guarda en la nube (Cloud Save),
///   3) actualiza los textos y el estado del boton.
///
/// El mismo pack desbloqueado aparece luego en el menu de personalizacion
/// (SwatchCaldero), que SOLO permite equiparlo si ya fue comprado.
///
/// Uso: pon este script en un GameObject del ShopPanel y arrastra:
///   - botonComprar, textoBoton, textoPrecio, textoEstado
///   - textos de monedas/gemas del Tienda_Caldero (solo lectura del perfil;
///     la economia general y sus textos los maneja UIManager como siempre)
/// </summary>
public class TiendaPacks : MonoBehaviour
{
    [Header("Pack en venta")]
    [Tooltip("Id del pack a vender (el caldero).")]
    public string packId = "caldero";
    [Tooltip("Nombre que se muestra en la tienda.")]
    public string nombrePack = "Caldero de Bruja";

    [Header("Precio")]
    [Tooltip("Monedas necesarias para comprarlo.")]
    public long precioMonedas = 200;
    [Tooltip("Gemas necesarias para comprarlo (0 = no requiere gemas).")]
    public long precioGemas = 0;

    [Header("UI del ShopPanel (arrastra desde el Inspector)")]
    public Button botonComprar;
    public TMP_Text textoBoton;
    public TMP_Text textoEstado;
    [Tooltip("Tu saldo: textos de monedas/gemas del panel.")]
    public TMP_Text textoMonedas;
    public TMP_Text textoGemas;
    [Tooltip("Etiquetas del PRECIO fijo (ej: oro_cald / diamante_cald). Se escriben una vez con el precio.")]
    public TMP_Text textoPrecioMonedas;
    public TMP_Text textoPrecioGemas;

    [Header("Vista previa del caldero (el MISMO boton animado)")]
    [Tooltip("El GameObject Selector_Caldero del menu (con SwatchCaldero, burbujas, vapor, objetos). Se mueve aqui en la tienda y vuelve a Personalizacion.")]
    public GameObject selectorCaldero;
    [Tooltip("Donde se muestra en la TIENDA (un RectTransform hijo del ShopPanel).")]
    public RectTransform slotEnTienda;
    [Tooltip("Donde se muestra en PERSONALIZACION (el lugar original dentro del CustomizationPanel).")]
    public RectTransform slotEnPersonalizacion;

    bool comprando;

    // Referencia estatica: UIManager necesita devolver el caldero DESPUES
    // de apagar el ShopPanel, y FindFirstObjectByType no encuentra objetos
    // inactivos. Con esto funciona aunque la tienda este apagada.
    static TiendaPacks instancia;
    Transform padreOriginal; // respaldo: donde estaba el caldero al arrancar

    void Awake()
    {
        instancia = this;
        if (selectorCaldero != null) padreOriginal = selectorCaldero.transform.parent;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    /// <summary>Llamar desde UIManager al abrir Personalizacion (aunque la
    /// tienda este apagada).</summary>
    public static void DevolverCaldero()
    {
        if (instancia != null) instancia.DevolverCalderoAPersonalizacion();
    }

    void Start()
    {
        if (botonComprar != null) botonComprar.onClick.AddListener(Comprar);
        ActualizarUI();
    }

    void OnEnable() { ActualizarUI(); }

    /// <summary>Llamar desde el boton Comprar (o se conecta solo en Start).</summary>
    public async void Comprar()
    {
        if (comprando) return;
        var perfil = UGSProfileManager.Instance;
        if (perfil == null) { SetEstado("Sin sesion / perfil", new Color(1f, 0.4f, 0.4f)); return; }
        if (!UGSProfileManager.PerfilListo) { SetEstado("Cargando perfil...", Color.yellow); return; }
        if (perfil.OwnsPack(packId)) { SetEstado("Ya lo tenes", new Color(0.4f, 1f, 0.5f)); ActualizarUI(); return; }

        comprando = true;
        SetEstado("Comprando...", Color.yellow);
        try
        {
            // 1) Descuenta monedas/gemas y guarda en la nube (con debounce).
            bool cobrado = await perfil.SpendCurrencyAsync(precioMonedas, precioGemas);
            if (!cobrado)
            {
                SetEstado("Monedas/gemas insuficientes", new Color(1f, 0.4f, 0.4f));
                ActualizarUI();
                return;
            }
            // 2) Desbloquea el pack en el perfil (nube + local).
            bool desbloqueado = await perfil.UnlockPackAsync(packId);
            if (!desbloqueado)
            {
                // Seguro: si algo fallo, devolvemos el cobro para no perder monedas.
                await perfil.GrantCurrencyAsync(precioMonedas, precioGemas);
                SetEstado("No se pudo desbloquear — reembolsado", new Color(1f, 0.6f, 0.3f));
                ActualizarUI();
                return;
            }
            SetEstado("Comprado! Ya podes equiparlo", new Color(0.4f, 1f, 0.5f));
            Debug.Log($"[Tienda] Pack '{packId}' comprado y guardado en la nube. " +
                      $"-{precioMonedas} monedas, -{precioGemas} gemas.");
        }
        finally
        {
            comprando = false;
            ActualizarUI();
        }
    }

    /// <summary>Refresca precios, balances y el estado del boton.</summary>
    public void ActualizarUI()
    {
        // Precio FIJO en las etiquetas (oro_cald / diamante_cald): cuanto
        // pide cada moneda. No es el saldo, es el costo del pack.
        if (textoPrecioMonedas != null) textoPrecioMonedas.text = precioMonedas.ToString();
        if (textoPrecioGemas != null) textoPrecioGemas.text = precioGemas.ToString();

        var perfil = UGSProfileManager.Instance;
        bool propio = perfil != null && UGSProfileManager.PerfilListo && perfil.OwnsPack(packId);

        if (botonComprar != null) botonComprar.interactable = !propio && !comprando;
        if (textoBoton != null) textoBoton.text = propio ? "COMPRADO" : "COMPRAR";

        // El MISMO caldero animado se coloca en el slot de la tienda para
        // que se vea con todas sus animaciones (burbujas, vapor, objetos).
        ColocarCalderoEnTienda();

        // Estado PERSISTENTE: una vez comprado siempre dice COMPRADO.
        if (propio)
            SetEstado("COMPRADO - equipable en Personalizacion", new Color(0.4f, 1f, 0.5f));
        else if (textoEstado != null && !comprando &&
                 !textoEstado.text.Contains("insuficientes"))
            SetEstado("Disponible en la tienda", Color.white);

        // Los textos del Tienda_Caldero (CoinsText_cald/GemasText_cald)
        // muestran el PRECIO del pack (1000/500), NO el saldo: el saldo
        // general ya lo muestra UIManager arriba (CoinsText/GemsText) y
        // duplicates el numero se veia dos veces.
        if (textoMonedas != null) textoMonedas.text = precioMonedas.ToString();
        if (textoGemas != null) textoGemas.text = precioGemas.ToString();
    }

    /// <summary>
    /// Mueve el MISMO GameObject del caldero al slot de la tienda. Como es
    /// el mismo objeto (no una copia), conserva SwatchCaldero con el
    /// llenado, burbujas, vapor, hongos, ojo y hueso animados.
    /// </summary>
    void ColocarCalderoEnTienda()
    {
        if (selectorCaldero == null || slotEnTienda == null) return;
        var t = selectorCaldero.transform;
        if (t.parent != slotEnTienda)
        {
            t.SetParent(slotEnTienda, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
        // Le decimos al boton que esta en la tienda: asi ignora clics de
        // seleccion/equipo (solo se muestra animado).
        var sw = selectorCaldero.GetComponent<SwatchCaldero>();
        if (sw != null) sw.slotEnTienda = slotEnTienda;
        selectorCaldero.SetActive(true);
    }

    /// <summary>Devuelve el caldero a su lugar en Personalizacion.</summary>
    public void DevolverCalderoAPersonalizacion()
    {
        if (selectorCaldero == null) return;
        // Usar el slot asignado, o el padre original como respaldo (por si
        // el usuario no arrastro el slot en el Inspector).
        Transform destino = slotEnPersonalizacion != null
            ? slotEnPersonalizacion : padreOriginal;
        if (destino == null)
        {
            Debug.LogWarning("[Tienda] No se puede devolver el caldero: asigna 'Slot En Personalizacion' o deja el caldero en su lugar original.", this);
            return;
        }
        var t = selectorCaldero.transform;
        t.SetParent(destino, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    void SetEstado(string mensaje, Color color)
    {
        if (textoEstado == null) return;
        textoEstado.text = mensaje;
        textoEstado.color = color;
    }
}
