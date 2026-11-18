using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Objeto que PASEA libremente dentro del liquido del boton del caldero (UI).
/// Agarralo a cualquier Image hija del boton (hongos, ojo, etc.).
///
/// MOVIMIENTO: sin ancla y sin volver a un punto fijo. El objeto elige un
/// destino aleatorio dentro del liquido, camina hacia el con ritmo propio,
/// y al llegar (o al vencerse su tiempo) elige otro. Semejante a algo a la
/// deriva que se pasea por el brebaje. Cada objeto tiene tiempos, rapidez
/// y fases propias: nunca van sincronizados ni repiten la ruta.
///
/// CONTENCION: nunca sale del liquido. Los destinos y el paso se recortan
/// contra el rectangulo del hijo "Liquid" (la franja de abajo).
///
/// COLOR: tinte sumergido leido del color REAL del liquid (adaptable a
/// cambios), con "respiro" (la fuerza del velo sube y baja suave) y
/// transparencia (opacidad) para vender la profundidad.
///
/// ROTACION: giro continuo (como algo rodando en la corriente) o vaiven
/// de lado a lado, a eleccion.
/// </summary>
public class ObjetoEnLiquidoUI : MonoBehaviour
{
    [Header("Paseo libre (deambula por todo el liquido)")]
    [Tooltip("Rapidez con que recorre el liquido, en pixeles por segundo.")]
    [Range(2f, 60f)] public float velocidadPaseo = 26f;
    [Tooltip("Segundos entre destino y destino (minimo y maximo, al azar).")]
    public float minEntreDestinos = 0.5f;
    public float maxEntreDestinos = 1.4f;
    [Tooltip("Como una CUCHARA en el vaso: los destinos salen pegados a los bordes/esquinas, de lado a lado (objetos grandes).")]
    public bool pasearPorBordes = false;
    [Tooltip("Cuanto sube y baja mientras pasea, en pixeles.")]
    [Range(0f, 30f)] public float bobVertical = 4f;
    [Tooltip("Rapidez del sube/baja.")]
    [Range(0.1f, 4f)] public float velocidadBob = 2.2f;

    [Header("Contencion (nunca sale del liquido)")]
    [Tooltip("Recorta el paseo contra el rectangulo del hijo 'Liquid'.")]
    public bool limitarAlLiquido = true;
    [Tooltip("Alto del liquido visible, como fraccion del rectangulo del Liquid (desde abajo). El nivel 1 es una franja fina. Mas de 1 = puede asomar por encima del agua (hueso).")]
    [Range(0.05f, 1.5f)] public float franjaAltura = 0.35f;
    [Tooltip("USAR SU PROPIA Franja Altura aunque el nivel cambie (para el hueso: franja ~1.25, parte afuera del agua).")]
    public bool usarFranjaPropia = false;
    [Tooltip("Copia sumergida (ej: Hueso_Agua dentro de ZonaAgua): se enciende con este objeto y se apaga cuando el se apaga.")]
    public GameObject copiaSumergida;

    [Header("Rotacion")]
    [Tooltip("Si esta activo, el objeto da vueltas continuas (reemplaza el vaiven).")]
    public bool girar = true;
    [Tooltip("Grados por segundo del giro (con signo: negativo gira al reves).")]
    [Range(-90f, 90f)] public float velocidadGiro = 34f;
    [Tooltip("Si Girar esta apagado: cuanto oscila la inclinacion (grados). Objetos grandes: se apoyan y se mecen, no giran.")]
    [Range(0f, 45f)] public float oscilacionInclinacion = 8f;
    [Tooltip("Si Girar esta apagado: rapidez del vaiven (lento = pesado).")]
    [Range(0.1f, 2f)] public float velocidadVaiven = 0.5f;

    [Header("Respiro del tinte")]
    [Tooltip("El tinte del liquido 'respira': la fuerza del velo sube y baja suave, como si el brebaje banara al objeto.")]
    public bool respirarColor = true;
    [Range(0.1f, 3f)] public float velocidadRespiro = 0.8f;

    [Header("Respiro del tamano (pulsa chico <-> grande)")]
    [Tooltip("El objeto se hace mas grande y mas chico, suave (ej: el hongo grande del nivel 2 'palpita').")]
    public bool respirarTamano = false;
    [Tooltip("Cuanto crece y encoge alrededor de su escala (0.15 = pulsa entre -15% y +15%).")]
    [Range(0f, 0.6f)] public float amplitudTamano = 0.15f;
    [Tooltip("Rapidez del pulso (lento = magia, rapido = palpitada).")]
    [Range(0.1f, 3f)] public float velocidadTamano = 0.9f;

    [Header("Color")]
    [Tooltip("Blanco = el arte tal cual (ya trae sus colores base).")]
    public Color tinte = Color.white;
    [Tooltip("SUMERGIDO: mezcla el objeto con el color REAL del liquid, para que se vea DENTRO y se adapte a cambios de color.")]
    public bool sumergido = true;
    [Tooltip("Cuanto recibe del color del liquido (0 = nada, 1 = todo teñido).")]
    [Range(0f, 1f)] public float fuerzaSumergido = 0.35f;
    [Tooltip("Teñir tambien las Images de los hijos. Hongos: SI. Ojo: NO, para que la iris roja quede viva.")]
    public bool tenirHijos = true;
    [Tooltip("Transparencia del objeto: 1 = solido, menos = se ve a traves del liquido.")]
    [Range(0.3f, 1f)] public float opacidad = 0.82f;
    [Tooltip("OPCIONAL: el color cicla por el arcoiris (efecto magico).")]
    public bool arcoiris = false;
    [Range(0.05f, 2f)] public float velocidadArcoiris = 0.25f;

    RectTransform rt;
    Image[] propias;    // Images del propio objeto
    Image[] todas;      // propias + las de sus hijos
    float baseRotZ;
    Vector3 baseEscala; // su escala original (para el respiro del tamano)
    float faseY, faseR;

    // El rectangulo del Liquid: lo busca solo entre los hermanos.
    RectTransform contenedor;
    Image contenedorImg;
    // Respaldo: el color del caldero, si el Liquid no se encuentra.
    SwatchCaldero caldero;

    // Paseo
    Vector2 destinoPaseo;
    float proximoDestino;
    bool destinoListo;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        // TINEMOS TODAS las Images: la del propio objeto y las de sus hijos.
        // Asi funciona sin importar si el sprite esta en el objeto o en un hijo.
        propias = GetComponents<Image>();
        todas = GetComponentsInChildren<Image>(true);
        caldero = GetComponentInParent<SwatchCaldero>();
        if (rt != null) baseRotZ = rt.localEulerAngles.z;
        if (rt != null) baseEscala = rt.localScale; // recuerda SU tamano calibrado
        // Fases aleatorias: dos objetos nunca van al mismo ritmo ni color.
        faseY = Random.value * Mathf.PI * 2f;
        faseR = Random.value * Mathf.PI * 2f;
        // Arranca paseando desde donde lo pusiste (y de ahi se muda solo).
        if (rt != null) destinoPaseo = rt.anchoredPosition;
        proximoDestino = Random.Range(0.4f, 1.2f);
    }

    void OnDisable()
    {
        // Al apagarse el objeto, su copia sumergida tambien (el vaso
        // arranca limpio la proxima vez).
        if (copiaSumergida != null) copiaSumergida.SetActive(false);
    }

    void Update()
    {
        if (rt == null) return;
        // La copia sumergida vive mientras el original este vivo.
        if (copiaSumergida != null && !copiaSumergida.activeSelf)
            copiaSumergida.SetActive(true);
        BuscarContenedor();
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        // Nuevo destino al azar dentro del liquido (con su propio ritmo).
        if (!destinoListo && contenedor != null) { ElegirDestino(); destinoListo = true; }
        if (t >= proximoDestino)
        {
            proximoDestino = t + Random.Range(minEntreDestinos, maxEntreDestinos);
            ElegirDestino();
        }

        // Camina hacia el destino con ritmo constante, mientras el bob lo
        // mece arriba y abajo. Direccion nueva cada vez = paseo organico.
        Vector2 objetivo = destinoPaseo + new Vector2(0f, Mathf.Sin(t * velocidadBob + faseY) * bobVertical);
        Vector2 pos = Vector2.MoveTowards(rt.anchoredPosition, objetivo, velocidadPaseo * dt);
        if (limitarAlLiquido && contenedor != null) pos = Contener(pos);
        rt.anchoredPosition = pos;

        // Rotacion: vueltas continuas alrededor de tu rotacion base, o el
        // vaiven de lado a lado (objetos grandes: se apoyan y se mecen,
        // como una cuchara en el vaso) si Girar esta apagado.
        float rotZ = girar
            ? baseRotZ + t * velocidadGiro
            : baseRotZ + Mathf.Sin(t * velocidadVaiven + faseR) * oscilacionInclinacion;
        rt.localRotation = Quaternion.Euler(0f, 0f, rotZ);

        // Respiro del tamano: pulsa alrededor de SU escala calibrada (nunca
        // la pisa — el pulso es relativo). Fase propia: ningun objeto
        // palpita al mismo ritmo que otro.
        if (respirarTamano && rt != null)
            rt.localScale = baseEscala * (1f + amplitudTamano * Mathf.Sin(t * velocidadTamano + faseR));

        // Color: arcoiris opcional; si no, el tinte sumergido con el color
        // REAL del liquid (se relee cada frame: si cambia el color del
        // caldero, el objeto se adapta solo). Al final se aplica la
        // TRANSPARENCIA: ver el objeto medio translucido es lo que mas
        // vende que esta DENTRO del liquido, no pegado encima.
        if (todas != null && todas.Length > 0)
        {
            Color final = arcoiris
                ? ColorSumergido(Color.HSVToRGB(
                    Mathf.Repeat(t * velocidadArcoiris + faseY, 1f),
                    0.8f, 0.9f + 0.1f * Mathf.Sin(t * 2.2f + faseR)))
                : ColorSumergido(tinte);
            final.a *= opacidad;
            // Teñir Hijos apagado: solo el propio objeto recibe el velo
            // (los hijos quedan con su arte, ej: la iris roja del ojo).
            var objetivos = tenirHijos || propias == null || propias.Length == 0
                ? todas : propias;
            foreach (var g in objetivos) g.color = final;
        }
    }

    /// <summary>Encuentra el Liquid entre los hermanos (lo crea el caldero).</summary>
    void BuscarContenedor()
    {
        if (contenedor != null) return;
        var padre = transform.parent;
        if (padre == null) return;
        var t = padre.Find("Liquid");
        if (t == null) return;
        contenedor = t.GetComponent<RectTransform>();
        contenedorImg = contenedor != null ? contenedor.GetComponent<Image>() : null;
    }

    /// <summary>
    /// Calcula los limites del paseo: la franja del liquido (que puede
    /// pasar de 1 para que el hueso asome) recortada por un TOPE DURO: el
    /// vidrio. NINGUN objeto sale del boton Selector_Caldero, ni siquiera
    /// el hueso con su franja propia.
    /// </summary>
    void Limites(out float minX, out float maxX, out float minY, out float maxY)
    {
        Vector2 cPos = contenedor.anchoredPosition;
        Vector2 cSize = contenedor.rect.size;
        Vector2 mitad = rt.rect.size * 0.5f;

        float fondo = cPos.y - cSize.y * 0.5f;
        float topeFranja = fondo + cSize.y * franjaAltura;

        minX = cPos.x - cSize.x * 0.5f + mitad.x;
        maxX = cPos.x + cSize.x * 0.5f - mitad.x;
        minY = fondo + mitad.y;
        maxY = topeFranja - mitad.y;

        // TOPE DURO: el vidrio manda. Si la franja propia del hueso se pasa
        // del boton, el boton gana.
        var vidrio = rt.parent as RectTransform;
        if (vidrio != null)
        {
            Vector2 vSize = vidrio.rect.size;
            minX = Mathf.Max(minX, -vSize.x * 0.5f + mitad.x);
            maxX = Mathf.Min(maxX, vSize.x * 0.5f - mitad.x);
            minY = Mathf.Max(minY, -vSize.y * 0.5f + mitad.y);
            maxY = Mathf.Min(maxY, vSize.y * 0.5f - mitad.y);
        }

        // Si el objeto es mas grande que la zona, se pasea centrado en ella.
        if (maxY < minY) minY = maxY = (fondo + topeFranja) * 0.5f;
        if (maxX < minX) minX = maxX = cPos.x;
    }

    /// <summary>
    /// Elige un punto al azar DENTRO de la franja del liquido: ahi va a
    /// pasear hasta que le toque mudarse de nuevo.
    /// </summary>
    void ElegirDestino()
    {
        if (contenedor == null || rt == null) { destinoListo = true; return; }

        Limites(out float minX, out float maxX, out float minY, out float maxY);

        // Modo CUCHARA: los destinos salen pegados a los bordes/esquinas,
        // de lado a lado — como un hueso grande apoyado en la pared del vaso.
        float x;
        if (pasearPorBordes)
        {
            float margen = (maxX - minX) * 0.25f;
            x = Random.value < 0.5f
                ? Random.Range(minX, minX + margen)
                : Random.Range(maxX - margen, maxX);
        }
        else x = Random.Range(minX, maxX);

        destinoPaseo = new Vector2(x, Random.Range(minY, maxY));
    }

    /// <summary>
    /// Recorte de seguridad: aunque el destino sea valido, el paso del
    /// objeto nunca lo deja salir del liquido ni del vidrio.
    /// </summary>
    Vector2 Contener(Vector2 p)
    {
        Limites(out float minX, out float maxX, out float minY, out float maxY);
        return new Vector2(Mathf.Clamp(p.x, minX, maxX), Mathf.Clamp(p.y, minY, maxY));
    }

    /// <summary>
    /// Mezcla el color del objeto con el color REAL del liquido. Fuente del
    /// color: el Image del Liquid; si no se encuentra, el Color Liquido del
    /// SwatchCaldero (con su boost del juego). Si el color del caldero
    /// cambia, el objeto cambia con el: nada de verde fijo en codigo.
    /// </summary>
    Color ColorSumergido(Color c)
    {
        if (!sumergido) return c;
        Color liq = ColorDelLiquido();
        if (liq.a <= 0f) return c; // sin referencia: el arte tal cual
        // Respiro: la fuerza del velo sube y baja suave (75%..100%),
        // como si el brebaje bañara al objeto con su propia respiracion.
        float fuerza = respirarColor
            ? fuerzaSumergido * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * velocidadRespiro + faseY))
            : fuerzaSumergido;
        return Color.Lerp(c, liq * c, fuerza);
    }

    Color ColorDelLiquido()
    {
        if (contenedorImg != null) return contenedorImg.color;
        if (caldero != null) return BoardFx.Boost(caldero.colorLiquido);
        return new Color(0f, 0f, 0f, 0f);
    }

    /// <summary>Diagnostico: clic derecho en el componente > Diagnosticar Tinte.</summary>
    [ContextMenu("Diagnosticar Tinte")]
    void DiagnosticarTinte()
    {
        BuscarContenedor();
        var liq = ColorDelLiquido();
        int nImg = todas != null ? todas.Length : 0;
        Debug.Log($"[ObjetoEnLiquidoUI] {name}: sumergido={sumergido}, arcoiris={arcoiris}, " +
                  $"fuerza={fuerzaSumergido}, opacidad={opacidad}, tenirHijos={tenirHijos}, " +
                  $"Images totales={nImg}, " +
                  $"Liquid encontrado={contenedor != null}, color liquido={liq} (a={liq.a:F2})", this);
    }
}
