using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vapor del caldero: humo verde que SUBE desde la superficie del liquido
/// hacia arriba (la zona de aire del vaso). Version UI, todo generado por
/// codigo (puffs esponjosos, sin assets nuevos):
///   - Nace justo en la linea del agua (la MISMA franja calibrada de las
///     burbujas: alturaFranjaNivel1/2/3) y sube en diagonal suave.
///   - Crece y se abre mientras sube, y se desvanece.
///   - RESPIRA el color REAL del liquido: si cambias el color del caldero,
///     el vapor cambia con el (nada de verde hardcodeado).
///   - Se dibuja DETRAS del vidrio (Crystal), delante del resto.
///   - Tope duro: nunca sale del rect del boton.
/// A mas nivel, mas vapor. La agrega SwatchCaldero sola: cero armado manual.
///
/// Lecciones aplicadas (12 y 13): el hijo "Vapor" lleva su RectTransform
/// DESDE EL MINUTO UNO, y TODAS las referencias son de instancia, nunca
/// static, tolerantes a que la escena cambie debajo de nuestros pies.
/// </summary>
public class VaporCalderoUI : MonoBehaviour
{
    [Header("Vapor en LINEA (bloque que sube junto)")]
    [Tooltip("Lineas por segundo al nivel 3 (escala con el nivel). Cada linea nace JUNTA, en fila recta.")]
    [Range(0.1f, 3f)] public float lineasPorSegundo = 0.6f;
    [Tooltip("Cuantos puffs forman cada linea recta.")]
    [Range(2, 8)] public int puffsPorLinea = 4;
    [Tooltip("Ancho de la linea, fraccion del vidrio (0.5 = mitad del ancho, columna concentrada; 1 = de pared a pared).")]
    [Range(0.2f, 1f)] public float anchoLinea = 0.5f;
    [Tooltip("Torpeza de la linea: 0 = perfectamente recta y pareja; mas = chispas de azar en la separacion.")]
    [Range(0f, 1f)] public float irregularidadLinea = 0.1f;
    [Tooltip("Tamano del puff al nacer, como fraccion del ANCHO del vidrio.")]
    [Range(0.05f, 0.5f)] public float tamanoFraccion = 0.22f;
    [Tooltip("Velocidad de subida, fraccion del ALTO del vidrio por segundo.")]
    [Range(0.05f, 1f)] public float subidaFraccion = 0.2f;
    [Tooltip("Cuanto se abre el humo mientras sube (0 = no crece, 1 = al doble).")]
    [Range(0f, 2f)] public float crecimiento = 0.8f;
    [Tooltip("Transparencia del vapor (mas alto = mas notorio).")]
    [Range(0.05f, 1f)] public float alpha = 0.45f;
    [Tooltip("Respira el color del liquido: cuanto recibe del color REAL (se adapta a cambios).")]
    [Range(0f, 1f)] public float mezclaLiquido = 0.55f;
    [Tooltip("El color del liquido 'respira' en cada puff: sube y baja suave.")]
    public bool respirarColor = true;
    [Range(0.1f, 3f)] public float velocidadRespiro = 0.7f;
    [Tooltip("Sprites de vapor propios (opcional). Sin ellos: puffs generados por codigo.")]
    public Sprite[] sprites;

    [Header("Verificacion y capa")]
    [Tooltip("Si se prende, el vapor sube DELANTE del vidrio. APAGADO = detras del vidrio (humo dentro del pote, como debe ser).")]
    public bool sobreVidrio = false;
    [Tooltip("Donde nace DENTRO del liquido, como fraccion de SU profundidad (0 = en la superficie, 0.5 = la MITAD del verde, 1 = el fondo). El humo sube desde el cuerpo del brebaje, como las burbujas.")]
    [Range(0f, 1f)] public float nacerAbajo = 0.5f;
    [Tooltip("TECHO del vapor: cuanto sube por ENCIMA de la superficie del nivel actual. 0.25 = apenas sobresale (nivel 1 chiquito); al nivel 3, por la altura de la superficie, llega cerca del borde.")]
    [Range(0.05f, 1f)] public float topeVapor = 0.25f;
    [Tooltip("MODO DE PRUEBA: puffs GIGANTES y ROJOS, para confirmar que el vapor existe y donde nace. APAGAR cuando se vea.")]
    public bool modoPruebaVapor = false;
    bool sobreVidrioAplicado = !true; // fuerza el primer aplicado

    [Header("Multiplicadores (los maneja SwatchCaldero desde sus opciones)")]
    [Tooltip("Multiplica cuantos puffs nacen por segundo. 1 = normal.")]
    public float multiplicadorTasa = 1f;
    [Tooltip("Multiplica el tamano de los puffs. 1 = normal.")]
    public float multiplicadorTamano = 1f;
    [Tooltip("Multiplica lo rapido que suben. 1 = normal.")]
    public float multiplicadorVelocidad = 1f;
    [Tooltip("Multiplica la opacidad. 1 = normal, 2 = el doble de notorio.")]
    public float multiplicadorAlpha = 1f;

    RectTransform vidrio;        // el boton (el vidrio, donde vive el vapor)
    RectTransform espacio;       // SU hijo "Vapor", estirado a todo el vidrio
    Image liquidImg;             // color real del liquido (INSTANCIA, nunca static)
    int nivelActual;             // 0 = sin vapor
    float superficieFraccion = 0.85f;
    float acumulador;

    class Puff
    {
        public RectTransform rt;
        public Image img;
        public float x0, y, fase, faseSway, giro, nacido, vida;
        public float factorT;        // dado de tamano (el resto se lee EN VIVO)
        public float factorFx, factorFy; // SU forma (ancho/alto propios)
        public bool muriendo;
        public float muerteT;
    }
    readonly List<Puff> vivos = new List<Puff>();
    readonly List<Puff> libres = new List<Puff>();

    // Puffs blandos generados por codigo (3 durezas de humo distintas).
    static readonly List<Sprite> puffs = new List<Sprite>(); // SOLO texturas: no toca la escena

    /// <summary>La llama SwatchCaldero al arrancar: donde vive el vapor y de donde sale.</summary>
    public void Configurar(RectTransform vidrioBoton, Image liquido)
    {
        vidrio = vidrioBoton;
        liquidImg = liquido;
        if (vidrio == null) return;
        AsegurarContenedor();
    }

    /// <summary>La llama SwatchCaldero al completar cada nivel del llenado.</summary>
    public void SetNivel(int nivel, float superficieDeFranja)
    {
        nivelActual = nivel;
        superficieFraccion = superficieDeFranja;
        // Diagnostico: si esta linea NO aparece en consola, el vapor nunca
        // recibe el nivel (el problema esta antes, en el llenado).
        Debug.Log($"[VaporCalderoUI] nivel {nivel} — vapor encendido (franja {superficieDeFranja:P0})", this);
    }

    /// <summary>Limpia todo el vapor (al reabrir el panel arranca limpio).</summary>
    public void Reiniciar()
    {
        nivelActual = 0;
        acumulador = 0f;
        for (int i = 0; i < vivos.Count; i++) { vivos[i].img.gameObject.SetActive(false); libres.Add(vivos[i]); }
        vivos.Clear();
    }

    /// <summary>
    /// El contenedor "Vapor": hijo del boton que cubre TODO su rect. Va
    /// justo DESPUES del Liquid y ANTES del Crystal (el humo queda delante
    /// del liquido y detras del vidrio). Con su RectTransform SIEMPRE.
    /// </summary>
    void AsegurarContenedor()
    {
        var t = vidrio.Find("Vapor");
        if (t == null)
        {
            var go = new GameObject("Vapor");
            go.transform.SetParent(vidrio, false);
            t = go.transform;
        }
        espacio = t as RectTransform;
        if (espacio == null) espacio = t.gameObject.AddComponent<RectTransform>();
        espacio.anchorMin = new Vector2(0.5f, 0.5f);
        espacio.anchorMax = new Vector2(0.5f, 0.5f);
        espacio.pivot = new Vector2(0.5f, 0.5f);
        espacio.sizeDelta = vidrio.rect.size;
        espacio.anchoredPosition = Vector2.zero;
        LimpiezaDeRestos();
        AplicarOrdenCapas();
    }

    /// <summary>
    /// Si la escena se guardo DURANTE el Play, quedaron puffs "cocidos"
    /// (guardados apagados, con sprites muertos) que el pool no conoce:
    /// nunca se prenden. Se borran al arrancar y el pool crea puffs nuevos.
    /// </summary>
    void LimpiezaDeRestos()
    {
        for (int i = espacio.childCount - 1; i >= 0; i--)
            if (espacio.GetChild(i).name == "VaporPuff")
                Destroy(espacio.GetChild(i).gameObject);
    }

    /// <summary>
    /// Orden de capas del vapor: DELANTE del vidrio (sobreVidrio, se ve
    /// seguro) o DETRAS (mas sutil, lo tapa el vidrio). Se reaplica si
    /// cambia la opcion, sin tocar la jerarquia de mas.
    /// </summary>
    void AplicarOrdenCapas()
    {
        if (espacio == null || sobreVidrioAplicado == sobreVidrio) return;
        sobreVidrioAplicado = sobreVidrio;
        var crystal = vidrio.Find("Crystal");
        if (sobreVidrio)
        {
            espacio.SetSiblingIndex(vidrio.childCount - 1);          // vapor ultimo
            if (crystal != null) crystal.SetSiblingIndex(vidrio.childCount - 2); // vidrio antes
        }
        else
        {
            if (liquidImg != null)
                espacio.SetSiblingIndex(liquidImg.transform.GetSiblingIndex() + 1);
            if (crystal != null) crystal.SetSiblingIndex(vidrio.childCount - 1);
        }
    }

    /// <summary>La llama SwatchCaldero cuando cambia la opcion (en vivo).</summary>
    public void SetSobreVidrio(bool valor)
    {
        sobreVidrio = valor;
        AplicarOrdenCapas();
    }

    void Update()
    {
        // Todo puede faltar si la escena cambio: si falta, nos sentamos y
        // esperamos sin tirar excepcion NUNCA.
        if (vidrio == null || espacio == null || nivelActual <= 0) return;
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        // Lineas que nacen JUNTAS y suben en bloque (escala con el nivel y
        // con el multiplicador de densidad de las opciones).
        acumulador += lineasPorSegundo * multiplicadorTasa * (nivelActual / 3f) * dt;
        while (acumulador >= 1f) { acumulador -= 1f; NacerLinea(); }

        // Color REAL del liquido, leido una vez por frame (nunca explota:
        // ColorDelLiquido se re-busca solo si el viejo se destruyo).
        Color liq = ColorDelLiquido();
        float w = vidrio.rect.width;
        float h = vidrio.rect.height;
        float superficie = SuperficieLocal();
        // TECHO por NIVEL: el vapor sube solo un poco por encima de la
        // superficie del nivel actual. Nivel 1 (franja 0.25) = apenas
        // sobresale del verde chico; nivel 3 llega cerca del borde. Y el
        // borde del vidrio manda igual: jamas lo toca.
        float tope = Mathf.Min(superficie + h * topeVapor, h * 0.5f);

        for (int i = vivos.Count - 1; i >= 0; i--)
        {
            var p = vivos[i];
            float edad = t - p.nacido;

            // Tope duro del vidrio: si toca arriba, arranca su despedida.
            if (!p.muriendo && p.y >= tope) { p.muriendo = true; p.muerteT = 0f; }

            // Envelope de alpha: fade in suave + fade out en el ultimo tramo.
            float a = Mathf.Clamp01(edad / 0.5f);
            float queda = p.vida - edad;
            a *= Mathf.Clamp01(queda / (p.vida * 0.35f));
            if (p.muriendo)
            {
                p.muerteT += dt;
                a *= Mathf.Clamp01(1f - p.muerteT / 0.6f);
            }

            // Respiro del color: cada puff con su fase (y la opacidad de
            // las opciones multiplicando).
            float resp = respirarColor
                ? 0.7f + 0.3f * Mathf.Sin(t * velocidadRespiro + p.fase)
                : 1f;
            Color c;
            if (modoPruebaVapor)
            {
                // MODO DE PRUEBA: rojo intenso y grande — si el vapor existe,
                // ESTO se ve. Sirve para confirmar que nacen y donde nacen.
                c = Color.red;
                c.a = 0.9f * Mathf.Clamp01(a * 2f);
            }
            else
            {
                c = Color.Lerp(Color.white, liq, mezclaLiquido);
                c.a = alpha * multiplicadorAlpha * resp * a;
            }
            p.img.color = c;

            // Subida en diagonal suave (deriva con su fase) + crecimiento,
            // con el multiplicador de velocidad de las opciones. PERO cuando
            // ya se esta despidiendo se queda QUIETO: no sigue subiendo mas
            // alla del vidrio mientras se desvanece.
            if (!p.muriendo) p.y += h * subidaFraccion * multiplicadorVelocidad * dt;
            // Vaiven de la LINEA ENTERA (misma fase): el bloque se mueve
            // junto, y tan sutil como lo ancho de la linea (columna
            // concentrada = vaiven corto, no se va a los bordes).
            float x = p.x0 + Mathf.Sin(t * 0.9f + p.faseSway) * w * 0.04f * (0.4f + anchoLinea);
            float crece = 1f + crecimiento * Mathf.Clamp01(edad / p.vida);

            // Tamano leido EN VIVO: factores del puff + opciones actuales.
            float baseTamano = w * tamanoFraccion * multiplicadorTamano * p.factorT;
            float sx = baseTamano * p.factorFx;
            float sy = baseTamano * p.factorFy;

            // TOPE DURO del boton: el centro JAMAS pasa del techo del vapor
            // (y de los bordes, menos la mitad del puff por el giro).
            float mitX = sx * 0.45f, mitY = sy * 0.45f;
            x = Mathf.Clamp(x, -w * 0.5f + mitX, w * 0.5f - mitX);
            p.y = Mathf.Min(p.y, tope - mitY);

            p.rt.anchoredPosition = new Vector2(x, p.y);
            p.rt.sizeDelta = new Vector2(sx * crece, sy * crece);
            p.rt.localEulerAngles = new Vector3(0f, 0f, p.giro + Mathf.Sin(t * 0.5f + p.fase) * 8f);

            // SOLO se libera cuando su vida termina, o cuando ya se estaba
            // despidiendo (tope del vidrio) y se apago del todo. ANTES: un
            // puff recien nacido tenia alpha 0 (el fade in empieza en cero)
            // y moria en el MISMO frame — por eso el vapor JAMAS se veia.
            if (edad >= p.vida || (p.muriendo && a <= 0.001f)) Liberar(i);
        }
    }

    /// <summary>
    /// La linea del agua (la franja calibrada) convertida al espacio local
    /// del vapor: asi nace EXACTO en la superficie pintada del liquido.
    /// </summary>
    float SuperficieLocal()
    {
        if (liquidImg == null || espacio == null) return vidrio.rect.height * 0.1f;
        var lrt = liquidImg.rectTransform;
        float local = -lrt.rect.height * 0.5f + lrt.rect.height * superficieFraccion;
        Vector3 mundo = lrt.TransformPoint(new Vector3(0f, local, 0f));
        return espacio.InverseTransformPoint(mundo).y;
    }

    /// <summary>
    /// El FONDO del liquido verde en espacio local del vapor: el nacimiento
    /// profundo interpola entre superficie y fondo (nacerAbajo).
    /// </summary>
    float FondoLocal()
    {
        if (liquidImg == null || espacio == null) return SuperficieLocal();
        var lrt = liquidImg.rectTransform;
        Vector3 mundo = lrt.TransformPoint(new Vector3(0f, -lrt.rect.height * 0.5f, 0f));
        return espacio.InverseTransformPoint(mundo).y;
    }

    /// <summary>
    /// Una LINEA recta de puffs: nacen juntos, a la MISMA altura, repartidos
    /// parejos en el ancho, con el mismo vaivén y la misma vida — suben como
    /// un bloque y mueren juntos.
    /// </summary>
    void NacerLinea()
    {
        float w = vidrio.rect.width;
        // Nace en el CUERPO del liquido (como las burbujas): la linea nace
        // a cierta profundidad del verde de ese nivel, con variacion —
        // unas lineas mas hondas que otras, pero SIEMPRE bajo la superficie.
        float superficie = SuperficieLocal();
        float y = Mathf.Lerp(superficie, FondoLocal(), Mathf.Clamp01(nacerAbajo * Random.Range(0.4f, 1f)));
        float medio = w * anchoLinea * 0.5f;
        float faseSway = Random.value * Mathf.PI * 2f; // el vaiven de la linea entera
        float vidaLinea = Random.Range(2.2f, 4f);      // el bloque muere junto
        int n = Mathf.Max(1, puffsPorLinea);
        for (int i = 0; i < n; i++)
        {
            float t01 = n == 1 ? 0.5f : (float)i / (n - 1);
            float x = Mathf.Lerp(-medio, medio, t01);
            if (irregularidadLinea > 0f)
                x += Random.Range(-1f, 1f) * w * 0.04f * irregularidadLinea;
            NacerEn(x, y, faseSway, vidaLinea);
        }
    }

    /// <summary>Un puff de la linea, en la posicion que le toca.</summary>
    void NacerEn(float x0, float y0, float faseSway, float vidaLinea)
    {
        var p = libres.Count > 0 ? SacarLibre() : Crear();
        float w = vidrio.rect.width;
        // Solo los DADOS son de nacimiento: el tamano base se lee EN VIVO,
        // asi cambiar Vapor Tamano en Play redibuja a todos al instante.
        p.factorT = Random.Range(0.7f, 1.4f);
        if (modoPruebaVapor) p.factorT *= 2.5f; // gigantes en modo prueba
        p.factorFx = Random.Range(0.85f, 1.25f);   // cada puff una forma distinta
        p.factorFy = Random.Range(0.85f, 1.25f);
        p.x0 = x0;
        p.y = y0;                               // MISMA altura: linea recta
        p.fase = Random.value * Mathf.PI * 2f;  // respiro propio
        p.faseSway = faseSway;                  // vaiven COMPARTIDO con la linea
        p.giro = Random.Range(-12f, 12f);
        p.nacido = Time.unscaledTime;
        p.vida = vidaLinea;                     // misma vida: muere junto
        p.muriendo = false;

        p.img.sprite = ElegirSprite();
        p.img.color = new Color(1f, 1f, 1f, 0f); // nace invisible, hace fade in
        float baseTamano0 = w * tamanoFraccion * multiplicadorTamano * p.factorT;
        p.rt.sizeDelta = new Vector2(baseTamano0 * p.factorFx, baseTamano0 * p.factorFy);
        p.rt.anchoredPosition = new Vector2(p.x0, p.y);
        p.rt.localScale = Vector3.one;
    }

    void Liberar(int indice)
    {
        var p = vivos[indice];
        p.img.gameObject.SetActive(false);
        vivos.RemoveAt(indice);
        libres.Add(p);
    }

    Puff SacarLibre()
    {
        var p = libres[libres.Count - 1];
        libres.RemoveAt(libres.Count - 1);
        p.img.gameObject.SetActive(true);
        vivos.Add(p);
        return p;
    }

    Puff Crear()
    {
        var go = new GameObject("VaporPuff");
        go.transform.SetParent(espacio, false);
        var p = new Puff
        {
            rt = go.AddComponent<RectTransform>(),
            img = go.AddComponent<Image>()
        };
        p.img.raycastTarget = false;
        vivos.Add(p);
        return p;
    }

    /// <summary>
    /// Sprite del puff: uno del pack (si la casilla existe); si no, un puff
    /// esponjoso generado por codigo (3 durezas de humo). JAMAS queda sin
    /// sprite: sin sprite = cuadrado blanco de Unity.
    /// </summary>
    Sprite ElegirSprite()
    {
        if (sprites != null && sprites.Length > 0)
        {
            int i = Random.Range(0, sprites.Length);
            if (sprites[i] != null) return sprites[i];
        }
        var ps = Puffs();
        return ps[Random.Range(0, ps.Count)];
    }

#if UNITY_EDITOR
    /// <summary>
    /// Si la escena se guardo DURANTE el Play, los puffs creados por codigo
    /// quedaron cocidos en la escena (sprites muertos al salir del Play).
    /// En el editor se borran: el vapor SOLO existe durante el Play.
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying) return;
        var caldero = GetComponentInParent<SwatchCaldero>();
        if (caldero == null) return;
        var hijos = caldero.GetComponentsInChildren<Transform>(true);
        foreach (var h in hijos)
            if (h != null && h.name == "VaporPuff")
                DestroyImmediate(h.gameObject);
    }
#endif

    /// <summary>Puffs blandos generados: nube difusa, tres durezas.</summary>
    static List<Sprite> Puffs()
    {
        // A prueba de sprites muertos: si la lista sobrevivio con puffs
        // viejos cuyas texturas se destruyeron, se reconstruye completa.
        if (puffs.Count > 0)
        {
            for (int i = 0; i < puffs.Count; i++)
                if (puffs[i] == null) { puffs.Clear(); break; }
        }
        if (puffs.Count > 0) return puffs;
        int s = 96;
        float[] dureza = { 2.6f, 4.2f, 6.5f };
        foreach (float k in dureza)
        {
            var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - s * 0.5f) / (s * 0.5f);
                    float dy = (y - s * 0.5f) / (s * 0.5f);
                    float d = dx * dx + dy * dy;
                    float a = Mathf.Exp(-d * k); // nube: mas densa al centro
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            puffs.Add(Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f));
        }
        return puffs;
    }

    /// <summary>
    /// Color REAL del Liquid, para el respiro. A prueba de escenas: si el
    /// Liquid se destruyo, Unity lo ve como null y se vuelve a buscar
    /// (INSTANCIA, nunca static — leccion 13). Sin color: blanco y listo.
    /// </summary>
    Color ColorDelLiquido()
    {
        if (liquidImg == null && vidrio != null)
        {
            var t = vidrio.Find("Liquid"); // SIEMPRE el Liquid, nada mas
            if (t != null) liquidImg = t.GetComponent<Image>();
        }
        if (liquidImg != null) return liquidImg.color;
        return Color.white;
    }
}
