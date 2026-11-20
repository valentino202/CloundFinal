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
///   - textos de monedas/gemas (o deja vacios y usa UIManager.RefreshCurrencyTexts)
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

    bool comprando;

    void Start()
    {
        if (botonComprar != null) botonComprar.onClick.AddListener(Comprar);
        UGSEconomy.BalancesUpdated += AlCambiarBalances;
        ActualizarUI();
    }

    void OnDestroy()
    {
        UGSEconomy.BalancesUpdated -= AlCambiarBalances;
    }

    void AlCambiarBalances() { ActualizarUI(); }

    void OnEnable() { ActualizarUI(); }

    /// <summary>Llamar desde el boton Comprar (o se conecta solo en Start).</summary>
    public async void Comprar()
    {
        if (comprando) return;
        var perfil = UGSProfileManager.Instance;
        if (perfil == null) { SetEstado("Sin sesion / perfil", new Color(1f, 0.4f, 0.4f)); return; }
        if (!UGSProfileManager.PerfilListo) { SetEstado("Cargando perfil...", Color.yellow); return; }
        if (perfil.OwnsPack(packId)) { SetEstado("Ya lo tenes ✅", new Color(0.4f, 1f, 0.5f)); ActualizarUI(); return; }

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
            SetEstado("¡Comprado! Ya podes equiparlo ✅", new Color(0.4f, 1f, 0.5f));
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

        // Estado PERSISTENTE: una vez comprado siempre dice COMPRADO.
        if (propio)
            SetEstado("COMPRADO ✅ — equipable en Personalizacion", new Color(0.4f, 1f, 0.5f));
        else if (textoEstado != null && !comprando &&
                 !textoEstado.text.Contains("insuficientes"))
            SetEstado("Disponible en la tienda", Color.white);

        // Saldo del jugador: UGSEconomy lee del MISMO perfil de Cloud Save
        // donde se guardan las monedas/gemas que se ganan jugando.
        // Refrescamos para que este al dia y actualizamos los textos de
        // Tienda_Caldero y losGENERALES del panel (CoinsText/GemsText) juntos.
        _ = RefrescarSaldos();
    }

    async System.Threading.Tasks.Task RefrescarSaldos()
    {
        await UGSEconomy.RefreshAsync();
        long coins = UGSEconomy.Coins;
        long gems = UGSEconomy.Gems;
        // Textos del Tienda_Caldero.
        if (textoMonedas != null) textoMonedas.text = coins.ToString();
        if (textoGemas != null) textoGemas.text = gems.ToString();
        // Textos GENERALES del ShopPanel (para que todos digan lo mismo).
        var ui = FindFirstObjectByType<UIManager>();
        if (ui != null)
        {
            if (ui.coinsTexts != null)
                foreach (var t in ui.coinsTexts) if (t != null) t.text = coins.ToString();
            if (ui.gemsTexts != null)
                foreach (var t in ui.gemsTexts) if (t != null) t.text = gems.ToString();
        }
    }

    void SetEstado(string mensaje, Color color)
    {
        if (textoEstado == null) return;
        textoEstado.text = mensaje;
        textoEstado.color = color;
    }
}
