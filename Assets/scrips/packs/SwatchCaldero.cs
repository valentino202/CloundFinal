using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Boton del pack "Caldero de Bruja" para los paneles (tienda / personalizacion).
/// Se ve IGUAL que un ColorSwatch (mismas animaciones de toque: hover 1.14,
/// press 0.86, smooth 14), pero el liquido SE LLENA con los frames reales
/// del juego: 1a..1e -> 2a..2e -> 3a..3e. Una sola pasada al abrirse el
/// panel y queda lleno, congelado en el ultimo frame (como las casillas).
///
/// PASO 1 del pack: solo tapa (0tapa) + liquido verde. Sin objetos ni
/// burbujas: eso viene en los pasos siguientes.
///
/// Uso: lo crea el builder (Herramientas > Packs). Los frames y la tapa se
/// cargan por nombre del archivo 'liquido animacion', no a mano.
/// </summary>
public class SwatchCaldero : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Arte (lo asigna el builder)")]
    [Tooltip("Frames del nivel 1 (1a..1e).")]
    public Sprite[] framesNivel1;
    [Tooltip("Frames del nivel 2 (2a..2e).")]
    public Sprite[] framesNivel2;
    [Tooltip("Frames del nivel 3, el lleno (3a..3e).")]
    public Sprite[] framesNivel3;
    [Tooltip("La tapa del cristal (0tapa). NUNCA se tinta.")]
    public Sprite spriteTapa;

    [Header("Color del liquido")]
    [Tooltip("Verde de bruja. Se aplica con el boost de brillo del juego.")]
    public Color colorLiquido = new Color(0.15f, 0.65f, 0.25f, 1f);

    [Header("Como en el juego")]
    [Tooltip("Fraccion del boton que ocupa el liquido.")]
    [Range(0.3f, 1f)] public float liquidFill = 0.92f;
    [Tooltip("Fraccion del boton que ocupa el cristal.")]
    [Range(0.3f, 1f)] public float crystalFill = 1f;
    [Tooltip("Alpha del vidrio (el PNG ya trae su transparencia horneada).")]
    [Range(0f, 1f)] public float glassAlpha = 1f;
    [Tooltip("Frames por segundo del llenado.")]
    [Range(4f, 30f)] public float fps = 14f;
    [Tooltip("Pausa entre niveles del llenado, en segundos.")]
    [Range(0f, 1.5f)] public float pausaNivel = 0.45f;
    [Tooltip("Brillo del liquido en el MENU. Igual que 'Color Boost' de CellVisuals en el juego. 1.0 = colores exactos (rosa/naranja no se distorsionan).")]
    [Range(0.5f, 3f)] public float colorBoost = 1f;

    [Header("Animaciones de toque (igual que ColorSwatch)")]
    [Range(1f, 1.5f)] public float hoverScale = 1.14f;
    [Range(0.5f, 1f)] public float pressScale = 0.86f;
    [Range(1f, 30f)] public float smooth = 14f;
    [Tooltip("Escala cuando este pack esta equipado. Mas alto = resalta mas (pack premium).")]
    [Range(1f, 1.6f)] public float escalaSeleccion = 1.25f;

    [Header("Seleccion del pack (clic = equipar / quitar)")]
    [Tooltip("Id del pack que equipa este boton en el perfil del jugador local.")]
    public string packIdSeleccion = "caldero";
    [Tooltip("Clic: si no esta equipado lo desbloquea, equipa y guarda (nube + local); si ya esta, vuelve al clasico.")]
    public bool seleccionarAlClic = true;
    [Tooltip("Slot de la TIENDA: si el caldero esta ahi, solo se MUESTRA (no se puede seleccionar). Arrastralo desde TiendaPacks.")]
    public Transform slotEnTienda;
    bool seleccionEnMarcha;

    /// <summary>True si este boton esta actualmente en la tienda (vista previa).</summary>
    bool EnTienda => slotEnTienda != null && transform.parent == slotEnTienda;

    [Header("Objetos del caldero (los armas vos a mano)")]
    [Tooltip("Nivel 1: hongos, de a dos. Hijos DESACTIVADOS del boton.")]
    public GameObject hongo1;
    public GameObject hongo2;
    [Tooltip("Nivel 2: el ojo (OjoFlotanteUI en el padre, Iris hija). DESACTIVADO.")]
    public GameObject ojo;
    [Tooltip("Nivel 3: el hueso, el mas grande. DESACTIVADO.")]
    public GameObject hueso;
    [Header("Objetos extra (mas dinamico)")]
    [Tooltip("Nivel 2: clon del HONGO 1 pero MAS GRANDE (el liquido medio da mas espacio). DESACTIVADO.")]
    public GameObject hongo3;
    [Tooltip("Nivel 3: un SEGUNDO ojo (clon del primero). DESACTIVADO.")]
    public GameObject ojo2;

    [Header("Espacio de flotado por nivel (mas liquido = mas libertad)")]
    [Tooltip("Superficie del nivel 1 para las BURBUJAS (los hongos usan su franja propia).")]
    [Range(0.05f, 1f)] public float alturaFranjaNivel1 = 0.25f;
    [Tooltip("Franja donde flotan los objetos al completar el nivel 2. Ajustala al VERDE PINTADO del frame 2e, no mas.")]
    [Range(0.05f, 1f)] public float alturaFranjaNivel2 = 0.5f;
    [Tooltip("Franja al completar el nivel 3. Ajustala al VERDE PINTADO del frame 3e (dejale el aire de arriba al hueso).")]
    [Range(0.05f, 1f)] public float alturaFranjaNivel3 = 0.85f;
    [Tooltip("MODO DE PRUEBA del paso 3: el llenado se detiene en 1e (franja verde) para calibrar los hongos. Al pasar al ojo/nivel 2 se apaga.")]
    public bool detenerEnNivel1 = true;
    [Tooltip("MODO DE PRUEBA del paso 3: el llenado se detiene en 2e (nivel medio) para calibrar el ojo. Al pasar al hueso/nivel 3 se apaga.")]
    public bool detenerEnNivel2 = true;
    [Tooltip("MODO DE PRUEBA del paso 3: el llenado se detiene en 3e (lleno) para calibrar el hueso. Al aprobarlo se apaga y queda el llenado completo.")]
    public bool detenerEnNivel3 = true;

    [Header("OPCIONES DE EFECTOS (SE GUARDAN con la escena — editar aca, no en Play)")]
    [Tooltip("Burbujas ON/OFF. Al apagar, las que esten volando se limpian.")]
    public bool burbujasOn = true;
    [Tooltip("Vapor ON/OFF. Al apagar, el humo que este flotando se limpia.")]
    public bool vaporOn = true;

    [Header("Burbujas — fino")]
    [Tooltip("Burbujas por segundo al nivel 3 (escala con el nivel).")]
    public float burbujaPorSegundo = 2.5f;
    [Tooltip("Tamano de la burbuja, fraccion del ANCHO del liquido.")]
    [Range(0.01f, 0.2f)] public float burbujaTamano = 0.05f;
    [Tooltip("Velocidad de subida, fraccion del ALTO por segundo.")]
    [Range(0.05f, 1.5f)] public float burbujaSubida = 0.35f;
    [Tooltip("Deriva lateral suave, fraccion del ancho.")]
    [Range(0f, 0.3f)] public float burbujaDeriva = 0.05f;
    [Tooltip("Transparencia de las burbujas.")]
    [Range(0.05f, 1f)] public float burbujaAlpha = 0.4f;
    [Tooltip("Cuanto reciben del color REAL del liquido (0 = blancas).")]
    [Range(0f, 1f)] public float burbujaMezclaColor = 0.55f;
    [Tooltip("Velocidad del respiro del color.")]
    [Range(0.1f, 3f)] public float burbujaRespiro = 0.8f;
    [Tooltip("Variacion de transparencia: unas casi invisibles, otras bien marcadas. 2 = contraste fuerte.")]
    [Range(0f, 2f)] public float burbujaVariacionAlpha = 0.6f;
    [Tooltip("Variacion de color: unas mas verdes, otras mas blancas.")]
    [Range(0f, 1f)] public float burbujaVariacionMezcla = 0.4f;
    [Tooltip("Variacion del ritmo del respiro.")]
    [Range(0f, 1f)] public float burbujaVariacionRespiro = 0.5f;
    [Tooltip("Porcentaje de burbujas que pasan DELANTE de los objetos (0 = todas detras).")]
    [Range(0f, 1f)] public float burbujaPorcentajeDelante = 0.3f;
    [Tooltip("Cuan fuerte el plano decide la transparencia: delante = marcadas, atras = tenues.")]
    [Range(0f, 1f)] public float burbujaSesgoProfundidad = 0.7f;

    [Header("Vapor — fino")]
    [Tooltip("Lineas de humo por segundo al nivel 3 (cada linea = bloque recto que sube junto).")]
    [Range(0.1f, 3f)] public float vaporLineasPorSegundo = 0.6f;
    [Tooltip("Cuantos puffs forman cada linea recta.")]
    [Range(2, 8)] public int vaporPuffsPorLinea = 4;
    [Tooltip("Ancho de la linea de humo, fraccion del vidrio (0.5 = columna concentrada al centro).")]
    [Range(0.2f, 1f)] public float vaporAnchoLinea = 0.5f;
    [Tooltip("Tamano del puff, fraccion del ANCHO del vidrio.")]
    [Range(0.05f, 0.5f)] public float vaporTamano = 0.22f;
    [Tooltip("Velocidad de subida, fraccion del ALTO por segundo.")]
    [Range(0.05f, 1f)] public float vaporSubida = 0.2f;
    [Tooltip("Cuanto se abre el humo mientras sube.")]
    [Range(0f, 2f)] public float vaporCrecimiento = 0.8f;
    [Tooltip("Transparencia del vapor.")]
    [Range(0.05f, 1f)] public float vaporAlpha = 0.45f;
    [Tooltip("Cuanto recibe del color REAL del liquido (0 = blanco).")]
    [Range(0f, 1f)] public float vaporMezclaColor = 0.55f;
    [Tooltip("Velocidad del respiro del color.")]
    [Range(0.1f, 3f)] public float vaporRespiro = 0.7f;
    [Tooltip("Vapor DELANTE del vidrio. Dejarlo APAGADO: el humo queda detras del vidrio (dentro del pote).")]
    public bool vaporSobreVidrio = false;
    [Tooltip("Donde nace el vapor DENTRO del liquido: fraccion de SU profundidad (0.5 = la mitad del verde, sube desde el cuerpo del brebaje).")]
    [Range(0f, 1f)] public float vaporNaceAbajo = 0.5f;
    [Tooltip("TECHO del vapor: cuanto sube por encima de la superficie del nivel (0.25 = apenas sobresale en niveles bajos).")]
    [Range(0.05f, 1f)] public float vaporTecho = 0.25f;
    [Tooltip("MODO DE PRUEBA: puffs gigantes ROJOS para verificar que el vapor existe. APAGAR despues.")]
    public bool vaporModoPrueba = false;

    [Header("Multiplicadores generales (1 = como esta)")]
    [Tooltip("DENSIDAD: cuantas nacen por segundo (burbujas y vapor).")]
    [Range(0f, 4f)] public float densidadEfectos = 1f;
    [Tooltip("TAMANO: cuan grandes son burbujas y puffs.")]
    [Range(0.2f, 3f)] public float tamanoEfectos = 1f;
    [Tooltip("VELOCIDAD: lo rapido que suben.")]
    [Range(0.2f, 3f)] public float velocidadEfectos = 1f;
    [Tooltip("OPACIDAD: cuan notorios se ven. 2 = el doble.")]
    [Range(0f, 3f)] public float opacidadEfectos = 1f;

    public Image liquidImage;
    public Image crystalImage;

    BurbujasCalderoUI burbujasUI;
    VaporCalderoUI vaporUI;

    private Vector3 baseScale;
    private Vector3 targetScale;
    private Coroutine llenado;

    private void Awake()
    {
        // El MENU usa el MISMO brillo que el juego: BoardFx.ColorBoost es
        // global, asi todos los botones de color y este caldero se tinen
        // con el mismo valor (2.3 = el que se ve vivo en el juego).
        BoardFx.ColorBoost = colorBoost;

        // Paso 4: el pack se registra como DATOS (sin arte) para que
        // EquipPackAsync lo acepte y pueda guardarse en el perfil.
        // La receta visual completa llega en el paso 6b; el juego sigue
        // usando el clasico hasta el paso 7 (HasArt = false).
        if (FxPackCatalog.Get(packIdSeleccion) == null)
            FxPackCatalog.RegisterPack(new FxPack(packIdSeleccion, "Caldero de Bruja",
                "caldero_1", "caldero_2", "caldero_3", "caldero_boom"));

        baseScale = transform.localScale;
        targetScale = baseScale;
        ConstruirHijos();
        // El Image del padre solo sirve de raycast target: queda invisible
        // para que se vea solo el arte (liquido + cristal), igual que
        // ColorSwatch hace con sus botones.
        var img = GetComponent<Image>();
        if (img != null) img.color = new Color(0f, 0f, 0f, 0f);

        // Burbujas: mismo comportamiento que en el juego, version UI. Se
        // agrega SOLA: no hay que armar nada a mano.
        burbujasUI = GetComponent<BurbujasCalderoUI>();
        if (burbujasUI == null) burbujasUI = gameObject.AddComponent<BurbujasCalderoUI>();
        if (liquidImage != null) burbujasUI.Configurar(liquidImage.rectTransform);

        // Vapor: humo verde que sube desde la superficie. Tambien se agrega
        // SOLO, y respira el color REAL del liquido (lecciones 12/13 aplicadas).
        vaporUI = GetComponent<VaporCalderoUI>();
        if (vaporUI == null) vaporUI = gameObject.AddComponent<VaporCalderoUI>();
        vaporUI.Configurar(GetComponent<RectTransform>(), liquidImage);
    }

    private void OnEnable()
    {
        BoardFx.ColorBoost = colorBoost; // por si el valor cambio en Play
        UGSProfileManager.OnProfileLoaded += AlCargarPerfil;
        // Cada vez que se abre el panel, el caldero se llena de nuevo:
        // una pasada y queda lleno (nada de loop).
        if (Application.isPlaying) Llenar();
    }

    private void OnDisable()
    {
        UGSProfileManager.OnProfileLoaded -= AlCargarPerfil;
        if (llenado != null) { StopCoroutine(llenado); llenado = null; }
    }

    /// <summary>Cuando llega el perfil de la nube se refresca el estado
    /// seleccionado (antes no se mostraba nada para no mostrar un color
    /// provisional).</summary>
    void AlCargarPerfil()
    {
        ultimoEquipado = EquipadoAhora;
        targetScale = baseScale * FactorSeleccion;
        Debug.Log($"[Selector] Perfil cargado — caja: " +
                  $"'{UGSProfileManager.Instance?.Profile.EquippedPackId}'.", this);
    }

    private void Start()
    {
        // Al abrir el panel se ve en consola que caja tiene guardada este
        // jugador (util para confirmar que la eleccion persiste).
        string caja = UGSProfileManager.Instance != null
            ? UGSProfileManager.Instance.Profile.EquippedPackId : "(sin perfil)";
        Debug.Log($"[Selector] Caja actual del jugador: '{caja}' — " +
                  (EquipadoAhora ? "caldero seleccionado." : "pack clasico activo."), this);
    }

    /// <summary>True mientras SU pack este equipado en el perfil local.</summary>
    bool EquipadoAhora => Application.isPlaying && UGSProfileManager.PerfilListo &&
        UGSProfileManager.Instance != null &&
        UGSProfileManager.Instance.Profile.EquippedPackId == packIdSeleccion;

    /// <summary>True si el jugador ya compro este pack (esta en su perfil).</summary>
    bool Comprado => Application.isPlaying && UGSProfileManager.PerfilListo &&
        UGSProfileManager.Instance != null &&
        UGSProfileManager.Instance.Profile.OwnedPackIds.Contains(packIdSeleccion);

    /// <summary>Escala del estado seleccionado (1 = no seleccionado).</summary>
    float FactorSeleccion => EquipadoAhora ? escalaSeleccion : 1f;

    bool ultimoEquipado;

    private void Update()
    {
        // Si el estado de seleccion cambio (clic aca o en otro boton),
        // se recalcula la escala objetivo. Es un GRUPO: solo el pack
        // equipado se agranda; al cambiar, este vuelve a su tamano.
        bool equipado = EquipadoAhora;
        if (equipado != ultimoEquipado)
        {
            ultimoEquipado = equipado;
            targetScale = baseScale * FactorSeleccion;
        }
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * smooth);

        // OCULTO hasta comprar: alpha 0 = no se ve en CustomizationPanel
        // (solo se ven los 3 colores), pero el GameObject SIGUE ACTIVO para
        // que este Update detecte la compra y lo haga aparecer solo.
        // NO usar SetActive(false): mataria el Update y nunca volveria.
        // Esperamos a PerfilListo para no ocultarlo por un dato no cargado.
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        bool visible = !UGSProfileManager.PerfilListo || Comprado;
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = Comprado;  // no recibe clics si no lo compro
        cg.interactable = Comprado;

        AplicarOpcionesDeEfectos();
    }

    /// <summary>
    /// Las OPCIONES DE EFECTOS se aplican EN VIVO cada frame: apagar/encender
    /// burbujas o vapor (al apagar se limpian) y mover los tres
    /// multiplicadores (densidad, tamano, velocidad). Cambialo en Play y se
    /// ve al toque; fuera de Play queda guardado en la escena.
    /// </summary>
    void AplicarOpcionesDeEfectos()
    {
        if (burbujasUI != null)
        {
            if (burbujasUI.enabled != burbujasOn)
            {
                burbujasUI.enabled = burbujasOn;
                if (!burbujasOn)
                {
                    burbujasUI.Reiniciar(); // sin cuadrados congelados
                    Debug.LogWarning("[SwatchCaldero] Burbujas APAGADAS por la opcion 'Burbujas On' — prenderla para verlas.", this);
                }
            }
            // Valores finos (viven aca y SE GUARDAN con la escena).
            burbujasUI.burbujasPorSegundo = burbujaPorSegundo;
            burbujasUI.tamanoFraccion = burbujaTamano;
            burbujasUI.subidaFraccion = burbujaSubida;
            burbujasUI.derivaFraccion = burbujaDeriva;
            burbujasUI.alpha = burbujaAlpha;
            burbujasUI.mezclaLiquido = burbujaMezclaColor;
            burbujasUI.velocidadRespiro = burbujaRespiro;
            burbujasUI.variacionAlpha = burbujaVariacionAlpha;
            burbujasUI.variacionMezcla = burbujaVariacionMezcla;
            burbujasUI.variacionRespiro = burbujaVariacionRespiro;
            burbujasUI.porcentajeDelante = burbujaPorcentajeDelante;
            burbujasUI.sesgoProfundidad = burbujaSesgoProfundidad;
            // Multiplicadores generales encima.
            burbujasUI.multiplicadorTasa = densidadEfectos;
            burbujasUI.multiplicadorTamano = tamanoEfectos;
            burbujasUI.multiplicadorVelocidad = velocidadEfectos;
            burbujasUI.multiplicadorAlpha = opacidadEfectos;
        }
        if (vaporUI != null)
        {
            if (vaporUI.enabled != vaporOn)
            {
                vaporUI.enabled = vaporOn;
                if (!vaporOn)
                {
                    vaporUI.Reiniciar(); // sin humo congelado
                    Debug.LogWarning("[SwatchCaldero] Vapor APAGADO por la opcion 'Vapor On' — prenderla para ver el humo.", this);
                }
            }
            // Valores finos (viven aca y SE GUARDAN con la escena).
            vaporUI.lineasPorSegundo = vaporLineasPorSegundo;
            vaporUI.puffsPorLinea = vaporPuffsPorLinea;
            vaporUI.anchoLinea = vaporAnchoLinea;
            vaporUI.tamanoFraccion = vaporTamano;
            vaporUI.subidaFraccion = vaporSubida;
            vaporUI.crecimiento = vaporCrecimiento;
            vaporUI.alpha = vaporAlpha;
            vaporUI.mezclaLiquido = vaporMezclaColor;
            vaporUI.velocidadRespiro = vaporRespiro;
            vaporUI.SetSobreVidrio(vaporSobreVidrio);
            vaporUI.nacerAbajo = vaporNaceAbajo;
            vaporUI.topeVapor = vaporTecho;
            vaporUI.modoPruebaVapor = vaporModoPrueba;
            // Multiplicadores generales encima.
            vaporUI.multiplicadorTasa = densidadEfectos;
            vaporUI.multiplicadorTamano = tamanoEfectos;
            vaporUI.multiplicadorVelocidad = velocidadEfectos;
            vaporUI.multiplicadorAlpha = opacidadEfectos;
        }
    }

    // El hover NUNCA encoge un boton seleccionado: se queda en la escala
    // de seleccion (o mas) para que siga resaltando.
    public void OnPointerEnter(PointerEventData _)
        => targetScale = baseScale * Mathf.Max(hoverScale, FactorSeleccion);
    public void OnPointerExit(PointerEventData _)
        => targetScale = baseScale * FactorSeleccion;
    public void OnPointerDown(PointerEventData _) => targetScale = baseScale * pressScale;
    public void OnPointerUp(PointerEventData _)
    {
        targetScale = baseScale * FactorSeleccion;
        // SOLO se puede seleccionar/equipar en Personalizacion. En la
        // tienda es solo vista previa: ni clic, ni equipar, ni guardar.
        if (seleccionarAlClic && !EnTienda) SeleccionarPack();
    }

    /// <summary>
    /// CLIC = EQUIPAR / QUITAR (toggle) en el perfil del jugador LOCAL.
    /// Si no esta equipado: lo desbloquea (por ahora gratis; la compra
    /// con monedas/gemas viene en el paso 5), lo equipa y guarda en
    /// nube + local. Si ya esta equipado: vuelve al "clasico".
    /// La nube guarda SOLO la eleccion (EquippedPackId); la receta visual
    /// del pack se arma en el paso 6b.
    /// </summary>
    public async void SeleccionarPack()
    {
        if (EnTienda) return; // la tienda es SOLO vista, nunca selecciona
        if (seleccionEnMarcha) return;
        seleccionEnMarcha = true;
        try
        {
            var perfil = UGSProfileManager.Instance;
            if (perfil == null) return;
            if (perfil.Profile.EquippedPackId == packIdSeleccion)
            {
                await perfil.EquipPackAsync("clasico"); // quitar el especial
                Debug.Log($"[Selector] ✅ Pack '{packIdSeleccion}' QUITADO. " +
                          "Caja guardada: 'clasico' → LISTO PARA JUGAR CLASICO (sin burbujas, sonido base).", this);
            }
            else
            {
                // Ya NO se desbloquea automaticamente: hay que COMPRARLO en
                // la tienda. Si no lo tiene, solo avisamos (no se equipa).
                if (!perfil.OwnsPack(packIdSeleccion))
                {
                    Debug.LogWarning($"[Selector] Pack '{packIdSeleccion}' NO comprado. " +
                                     "Compralo en la TIENDA para poder equiparlo.", this);
                    return;
                }
                await perfil.EquipPackAsync(packIdSeleccion);
                Debug.Log($"[Selector] ✅ Pack '{packIdSeleccion}' EQUIPADO y GUARDADO (nube + local). " +
                          "LISTO PARA JUGAR CON CALDERO.", this);
            }
        }
        finally { seleccionEnMarcha = false; }
    }

    /// <summary>Reproduce el llenado completo (una pasada) y queda lleno.</summary>
    public void Llenar()
    {
        if (liquidImage == null) return;
        if (burbujasUI != null) burbujasUI.Reiniciar(); // sin burbujas viejas
        if (vaporUI != null) vaporUI.Reiniciar();       // sin vapor viejo
        if (llenado != null) StopCoroutine(llenado);
        llenado = StartCoroutine(Llenado());
    }

    /// <summary>Estado lleno al instante (lo usa el builder en el editor).</summary>
    public void PonerLleno()
    {
        if (liquidImage == null) return;
        liquidImage.sprite = BoardFx.AclararLiquido(Ultimo(framesNivel3));
    }

    /// <summary>
    /// Lo llama el builder en el EDITOR: deja las capas armadas y el frame
    /// lleno puesto, para que el boton se vea completo sin darle Play.
    /// </summary>
    public void ConstruirEnEditor()
    {
        ConstruirHijos();
        PonerLleno();
    }

    private void ConstruirHijos()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // --- Liquido: debajo, tintado con el boost del juego ---
        if (liquidImage == null)
        {
            liquidImage = BuscarHijo("Liquid");
            if (liquidImage == null)
            {
                var go = new GameObject("Liquid");
                go.transform.SetParent(rt, false);
                var lrt = go.AddComponent<RectTransform>();
                Center(lrt, liquidFill);
                liquidImage = go.AddComponent<Image>();
            }
        }
        liquidImage.sprite = BoardFx.AclararLiquido(Ultimo(framesNivel3));
        liquidImage.color = BoardFx.Boost(colorLiquido);
        liquidImage.raycastTarget = false;

        // --- Cristal: encima, con su alpha propio (nunca se tinta) ---
        if (crystalImage == null)
        {
            crystalImage = BuscarHijo("Crystal");
            if (crystalImage == null)
            {
                var go = new GameObject("Crystal");
                go.transform.SetParent(rt, false);
                var crt = go.AddComponent<RectTransform>();
                Center(crt, crystalFill);
                crystalImage = go.AddComponent<Image>();
            }
        }
        crystalImage.sprite = spriteTapa;
        crystalImage.color = new Color(1f, 1f, 1f, glassAlpha);
        crystalImage.raycastTarget = false;

        // Orden de capas garantizado por codigo: Liquid ATRAS de todo
        // (los objetos flotan DELANTE de el) y Crystal SIEMPRE ultimo
        // (el vidrio encima de todo). Asi da igual donde esten los hijos
        // que se agreguen a mano.
        if (liquidImage != null) liquidImage.transform.SetSiblingIndex(0);
        if (crystalImage != null) crystalImage.transform.SetSiblingIndex(transform.childCount - 1);
    }

    private Image BuscarHijo(string nombre)
    {
        var t = transform.Find(nombre);
        return t != null ? t.GetComponent<Image>() : null;
    }

    static Sprite Ultimo(Sprite[] frames)
        => frames != null && frames.Length > 0 ? frames[frames.Length - 1] : null;

    /// <summary>
    /// El llenado: nivel 1 (1a..1e), pausa, nivel 2, pausa, nivel 3.
    /// Una sola pasada y queda congelado en el ultimo frame, como las
    /// casillas del juego. Sin loop.
    /// PASO 3 (mini-meta actual): al terminar el nivel 1 aparecen los
    /// hongos. Ojo y hueso vienen despues.
    /// </summary>
    IEnumerator Llenado()
    {
        var espera = new WaitForSeconds(1f / Mathf.Max(1f, fps));
        var pausa = new WaitForSeconds(pausaNivel);
        ReiniciarObjetos();
        liquidImage.sprite = null; // arranca con el vaso vacio

        var niveles = new[] { framesNivel1, framesNivel2, framesNivel3 };
        int indice = 0;
        foreach (var nivel in niveles)
        {
            indice++;
            if (nivel == null || nivel.Length == 0) continue;
            for (int i = 0; i < nivel.Length; i++)
            {
                liquidImage.sprite = BoardFx.AclararLiquido(nivel[i]);
                yield return espera;
            }
            // Al completar cada nivel aparece su objeto (se acumulan) y el
            // flotado gana espacio: mas liquido = mas libertad de movimiento.
            ActivarObjetosDelNivel(indice);
            AjustarEspacio(indice);
            // Las burbujas suben hasta la superficie del nivel alcanzado y
            // el vapor sale desde esa MISMA linea del agua.
            if (burbujasUI != null || vaporUI != null)
            {
                float alt = indice == 1 ? alturaFranjaNivel1
                          : indice == 2 ? alturaFranjaNivel2 : alturaFranjaNivel3;
                if (burbujasUI != null) burbujasUI.SetNivel(indice, alt);
                if (vaporUI != null) vaporUI.SetNivel(indice, alt);
            }
            // Modo de prueba del paso 3: congela en 1e (franja fina) para
            // calibrar los hongos, en 2e para el ojo, o en 3e para el hueso.
            // APAGAR TODOS para el llenado completo.
            if (indice == 1 && detenerEnNivel1) { llenado = null; yield break; }
            if (indice == 2 && detenerEnNivel2) { llenado = null; yield break; }
            if (indice == 3 && detenerEnNivel3) { llenado = null; yield break; }
            if (pausaNivel > 0f) yield return pausa;
        }
        PonerLleno();
        llenado = null;
    }

    /// <summary>Apaga todos los objetos (el vaso arranca sin nada).</summary>
    void ReiniciarObjetos()
    {
        if (hongo1 != null) hongo1.SetActive(false);
        if (hongo2 != null) hongo2.SetActive(false);
        if (ojo != null) ojo.SetActive(false);
        if (hueso != null) hueso.SetActive(false);
        if (hongo3 != null) hongo3.SetActive(false);
        if (ojo2 != null) ojo2.SetActive(false);
    }

    /// <summary>Enciende el objeto de cada nivel (se acumulan).</summary>
    void ActivarObjetosDelNivel(int nivel)
    {
        switch (nivel)
        {
            case 1:
                if (hongo1 != null) hongo1.SetActive(true);
                if (hongo2 != null) hongo2.SetActive(true);
                break;
            case 2:
                if (ojo != null) ojo.SetActive(true);
                if (hongo3 != null) hongo3.SetActive(true); // hongo GRANDE entra en el medio
                break;
            case 3:
                if (hueso != null) hueso.SetActive(true);
                if (ojo2 != null) ojo2.SetActive(true);     // un SEGUNDO ojo acompana al hueso
                break;
        }
    }

    /// <summary>
    /// Mas liquido = mas libertad: al completar cada nivel, la franja donde
    /// flotan los objetos crece (nivel 1 = franja chica abajo, nivel 2 y 3
    /// mas espacio). El codigo MANDA: pisa valores viejos del Inspector.
    /// </summary>
    void AjustarEspacio(int nivel)
    {
        // Nivel 1 tambien fija franja: los hongos van SIEMPRE abajo, en la
        // franja verde de 1e (0.25), aunque el Inspector tenga valores viejos.
        float altura = nivel == 1 ? alturaFranjaNivel1
                     : nivel == 2 ? alturaFranjaNivel2 : alturaFranjaNivel3;
        if (nivel == 1)
        {
            FijarFranja(hongo1, altura);
            FijarFranja(hongo2, altura);
            return;
        }
        FijarFranja(hongo1, altura);
        FijarFranja(hongo2, altura);
        FijarFranja(ojo, altura);
        FijarFranja(hueso, altura);
        FijarFranja(hongo3, altura);
        FijarFranja(ojo2, altura);
    }

    void FijarFranja(GameObject go, float altura)
    {
        if (go == null) return;
        foreach (var f in go.GetComponentsInChildren<ObjetoEnLiquidoUI>(true))
            if (!f.usarFranjaPropia) f.franjaAltura = altura;
    }

    private static void Center(RectTransform child, float frac)
    {
        child.anchorMin = new Vector2(0.5f, 0.5f);
        child.anchorMax = new Vector2(0.5f, 0.5f);
        child.pivot = new Vector2(0.5f, 0.5f);
        var p = child.parent as RectTransform;
        float w = p != null ? p.rect.width : 100f;
        float h = p != null ? p.rect.height : 100f;
        child.sizeDelta = new Vector2(w * frac, h * frac);
    }
}
