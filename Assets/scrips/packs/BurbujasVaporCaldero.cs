using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos del caldero EN EL JUEGO: burbujas de dos capas + vapor en lineas.
///
/// COPIA EXACTA de la logica de BurbujasCalderoUI / VaporCalderoUI del menu,
/// pero en WORLD-SPACE. La diferencia clave (que causo bugs antes): en el
/// menu todo es UI y los tamaños son PIXELES del RectTransform del Liquido
/// (100px). Aca no hay RectTransform, asi que el "rect" del liquido se
/// obtiene de los BOUNDS REALES de su SpriteRenderer (ancho/alto en unidades
/// de mundo). Todo lo demas — tasas, fade in, pop, respiracion, dos planos,
/// origen del vapor — es identico al menu.
///
/// Limpieza: al quedar sin nivel (celda vacia / otro dueno) se LIBERA todo
/// con StopEmittingAndClear: nunca queda humo ni burbujas congeladas.
/// </summary>
public class BurbujasVaporCaldero : MonoBehaviour
{
    class Burbuja
    {
        public SpriteRenderer sr;
        public float x0, y, sx, sy;
        public float fase, respVel, nacido;
        public float alphaPropia, mezclaPropia;
        public bool pop;
        public float popT;
    }

    class Puff
    {
        public SpriteRenderer sr;
        public float x0, y, fase, faseSway, giro, nacido, vida;
        public float factorT, fx, fy;
        public bool muriendo;
        public float muerteT;
    }

    // Z: el liquido esta en 0. Camara en z=-10 (mas negativo = mas cerca).
    // Atras (-0.005) detras de los objetos; delante (-0.018) por encima
    // del ojo pero detras del vidrio (que tiene mayor sortingOrder).
    // ATRAS: por encima del liquido (mismo order 0, Z -0.007) pero DETRAS
    // de los objetos (Z -0.011..-0.017). Antes estaba en order -1 = detras
    // del sprite del liquido, que es opaco y las ocultaba por completo.
    const float ZAtras = -0.007f;
    const float ZVapor = -0.006f;
    // Delante de los OBJETOS (Z -0.011..-0.017) para que las burbujas
    // crucen por encima de hongos/ojo. El aspecto "dentro del liquido" NO
    // se logra bajando el Z (asi las tapan los objetos), sino con el VELO
    // sumergido (burbujaVeloSumergido) que las tiñe del color del brebaje.
    const float ZDelante = -0.019f;
    // Multiplicador de tamano SOLO de la capa DELANTE (las veia grandes).
    const float TamanoCapaDelante = 0.7f;

    readonly List<Burbuja> burbujas = new List<Burbuja>();
    readonly List<Burbuja> burbujasLibres = new List<Burbuja>();
    readonly List<Puff> puffs = new List<Puff>();
    readonly List<Puff> puffsLibres = new List<Puff>();

    static readonly List<Sprite> anillos = new List<Sprite>();
    static readonly List<Sprite> humo = new List<Sprite>();

    /// <summary>El sprite del anillo mide 0.64u (64px / PPU 100). Para que
    /// 'sx' signifique el tamano real pedido por el menu hay que dividir
    /// localScale por el bounds del sprite (igual que los objetos). Sin
    /// esto las burbujas salen 1.6x mas chicas y casi no se ven.</summary>
    static float EscalaAnillo(Sprite sp)
    {
        if (sp == null) return 1f;
        float nativo = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
        return nativo > 0.01f ? 1f / nativo : 1f;
    }

    Transform capaAtras, capaDelante, capaVapor;
    FxPack pack;
    SpriteRenderer liquid;
    int nivel;
    float sup1, sup2, sup3;
    float acumB, acumV;

    /// <summary>Llama CellVisuals al crear la celda.</summary>
    public void Configurar(FxPack pack, SpriteRenderer liquid, Vector3 centroLocal,
                           float s1, float s2, float s3)
    {
        this.pack = pack;
        this.liquid = liquid;
        transform.localPosition = centroLocal;
        sup1 = s1; sup2 = s2; sup3 = s3;

        capaAtras = CrearCapa("BurbujasAtras", ZAtras);
        capaVapor = CrearCapa("Vapor", ZVapor);
        capaDelante = CrearCapa("BurbujasFrente", ZDelante);
    }

    Transform CrearCapa(string nombre, float z)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        return go.transform;
    }

    public void SetNivel(int nivelActual)
    {
        if (nivelActual == nivel) return;
        nivel = nivelActual;
        if (nivel <= 0) Limpiar();
    }

    void Limpiar()
    {
        acumB = acumV = 0f;
        for (int i = burbujas.Count - 1; i >= 0; i--)
        {
            burbujas[i].sr.gameObject.SetActive(false);
            burbujasLibres.Add(burbujas[i]);
        }
        burbujas.Clear();
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            puffs[i].sr.gameObject.SetActive(false);
            puffsLibres.Add(puffs[i]);
        }
        puffs.Clear();
    }

    /// <summary>"Rect" del liquido en unidades de mundo: ancho/alto reales
    /// del sprite ya escalado. Es el equivalente a contenedor.rect del menu.
    /// El fondo del liquido es -alto*0.5 porque el SpriteRenderer tiene el
    /// pivote en el centro ( igual que el RectTransform del menu ).</summary>
    void MedidasLiquido(out float w, out float h)
    {
        if (liquid != null && liquid.sprite != null)
        {
            w = liquid.sprite.bounds.size.x * Mathf.Abs(liquid.transform.localScale.x);
            h = liquid.sprite.bounds.size.y * Mathf.Abs(liquid.transform.localScale.y);
        }
        else
        {
            w = BoardFx.CellSize;
            h = BoardFx.CellSize;
        }
        if (w < 0.01f) w = BoardFx.CellSize;
        if (h < 0.01f) h = BoardFx.CellSize;
    }

    float superficieFraccion => nivel <= 1 ? sup1 : nivel == 2 ? sup2 : sup3;

    void Update()
    {
        if (nivel <= 0 || pack == null) { if (nivel <= 0) Limpiar(); return; }

        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        Color liq = liquid != null ? liquid.color : Color.white;

        MedidasLiquido(out float w, out float h);
        float fondo = -h * 0.5f;
        float superficie = fondo + h * superficieFraccion;

        // === BURBUJAS: tasa fija * densidad * cantidad, sin escalado por nivel ===
        acumB += pack.bubbleRate * pack.densidadEfectos * pack.burbujaCantidad * dt;
        while (acumB >= 1f) { acumB -= 1f; NacerBurbuja(w, h, fondo); }

        float subida = h * pack.bubbleRise * pack.velocidadEfectos;
        for (int i = burbujas.Count - 1; i >= 0; i--)
        {
            var b = burbujas[i];
            float edad = t - b.nacido;

            if (b.sr.sprite == null) b.sr.sprite = Elegir(anillos, 4);

            float resp = 0.75f + 0.25f * Mathf.Sin(t * b.respVel + b.fase);
            // Velo sumergido: mezcla extra del color real para que la
            // burbuja se vea DENTRO del brebaje, no flotando en el frente.
            float mezclaSum = Mathf.Clamp01(b.mezclaPropia + pack.burbujaVeloSumergido);
            var baseC = Color.Lerp(Color.white, liq, mezclaSum);

            if (b.pop)
            {
                b.popT += dt;
                float k = Mathf.Clamp01(b.popT / 0.15f);
                float esc = 1.15f + 0.45f * k;
                float eaPop = EscalaAnillo(b.sr.sprite);
                b.sr.transform.localScale = new Vector3(b.sx * esc * eaPop, b.sy * esc * eaPop, 1f);
                baseC.a = b.alphaPropia * pack.opacidadEfectos * (1f - k);
                b.sr.color = baseC;
                if (k >= 1f) LiberarBurbuja(i);
                continue;
            }

            b.y += subida * dt;
            if (b.y >= superficie) { b.pop = true; b.popT = 0f; }

            float x = b.x0 + Mathf.Sin(t * 1.5f + b.fase) * w * pack.bubbleDrift;
            float prog = Mathf.Clamp01((b.y - fondo) / Mathf.Max(0.01f, superficie - fondo));
            float crece = Mathf.Lerp(0.85f, 1.15f, prog);

            baseC.a = b.alphaPropia * pack.opacidadEfectos * resp * Mathf.Clamp01(edad / 0.12f);
            b.sr.color = baseC;
            b.sr.transform.localPosition = new Vector3(x, b.y, 0f);
            float ea = EscalaAnillo(b.sr.sprite);
            b.sr.transform.localScale = new Vector3(b.sx * crece * ea, b.sy * crece * ea, 1f);
        }

        // === VAPOR: lineas que nacen juntas, limitadas junto al agua ===
        if (pack.vaporOn && pack.vaporLineas > 0f)
        {
            acumV += pack.vaporLineas * pack.densidadEfectos * dt;
            while (acumV >= 1f) { acumV -= 1f; NacerLinea(w, h, fondo, superficie); }
        }

        float tope = Mathf.Min(superficie + BoardFx.CellSize * pack.vaporTecho, h * 0.5f);
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            var p = puffs[i];
            float edad = t - p.nacido;
            if (!p.muriendo && p.y >= tope) { p.muriendo = true; p.muerteT = 0f; }

            float a = Mathf.Clamp01(edad / 0.5f);
            float queda = p.vida - edad;
            a *= Mathf.Clamp01(queda / (p.vida * 0.35f));
            if (p.muriendo)
            {
                p.muerteT += dt;
                a *= Mathf.Clamp01(1f - p.muerteT / 0.6f);
            }
            float respV = 0.7f + 0.3f * Mathf.Sin(t * pack.vaporVelocidadRespiro + p.fase);
            var c = Color.Lerp(Color.white, liq, pack.vaporMezclaLiquido);
            c.a = pack.vaporAlpha * pack.opacidadEfectos * respV * a;
            p.sr.color = c;

            if (!p.muriendo) p.y += h * pack.vaporSubida * pack.velocidadEfectos * dt;
            float x = p.x0 + Mathf.Sin(t * 0.9f + p.faseSway) * w * 0.04f * (0.4f + pack.vaporAnchoLinea);
            float crece = 1f + pack.vaporCrecimiento * Mathf.Clamp01(edad / p.vida);
            float baseTam = w * pack.vaporTamano * pack.tamanoEfectos * p.factorT;
            float sx = baseTam * p.fx, sy = baseTam * p.fy;
            p.y = Mathf.Min(p.y, tope - sy * 0.45f);
            p.sr.transform.localPosition = new Vector3(x, p.y, 0f);
            p.sr.transform.localScale = new Vector3(sx * crece, sy * crece, 1f);
            p.sr.transform.localRotation = Quaternion.Euler(0f, 0f,
                p.giro + Mathf.Sin(t * 0.5f + p.fase) * 8f);

            if (edad >= p.vida || (p.muriendo && a <= 0.001f)) LiberarPuff(i);
        }
    }

    void NacerBurbuja(float w, float h, float fondo)
    {
        var b = burbujasLibres.Count > 0 ? burbujasLibres[burbujasLibres.Count - 1] : CrearBurbuja();
        if (burbujasLibres.Count > 0) burbujasLibres.RemoveAt(burbujasLibres.Count - 1);
        else burbujas.Add(b);
        b.sr.gameObject.SetActive(true);

        float margen = w * pack.bubbleSize * 0.5f + w * 0.04f;
        b.x0 = Random.Range(-w * 0.5f + margen, w * 0.5f - margen);
        b.y = fondo + Random.Range(0.02f, 0.08f) * h;
        float tam = w * pack.bubbleSize * pack.tamanoEfectos * Random.Range(0.6f, 1.5f);
        b.sx = tam * Random.Range(0.85f, 1.2f);
        b.sy = tam * Random.Range(0.85f, 1.2f);

        bool delante = Random.value < pack.burbujaPorcentajeDelante;
        b.sr.transform.SetParent(delante ? capaDelante : capaAtras, false);
        // AMBAS capas en el MISMO order del liquido (0), para que el sprite
        // opaco del liquido NO las oculte. La diferencia delante/atras la
        // da el Z: atras -0.007 (detras de objetos), delante -0.019 (bagos
        // de objetos). El cristal (glassOrder=1) queda encima de ambas.
        b.sr.sortingOrder = liquid != null ? liquid.sortingOrder : 0;
        // Las de DELANTE se ven mas grandes al estar mas cerca: las achico
        // un poco para que no se noten desmedidas sobre los objetos.
        if (delante) { b.sx *= TamanoCapaDelante; b.sy *= TamanoCapaDelante; }
        float dado = Random.value;
        dado = delante
            ? Mathf.Lerp(dado, 1f, pack.burbujaSesgoProfundidad)
            : Mathf.Lerp(dado, 0f, pack.burbujaSesgoProfundidad);
        // Contraste extra de capas: las de DELANTE se refuerzan y las de
        // ATRAS se atenuan, para que se vea el doble plano del menu.
        float capa = delante ? (1f + pack.burbujaContrasteCapas) : (1f - pack.burbujaContrasteCapas);
        b.alphaPropia = Mathf.Clamp01(pack.bubbleAlpha * pack.opacidadEfectos * capa *
            Mathf.Lerp(1f - pack.burbujaVariacionAlpha, 1f + pack.burbujaVariacionAlpha, dado));
        b.mezclaPropia = Mathf.Clamp01(pack.burbujaMezclaLiquido *
            Random.Range(1f - pack.burbujaVariacionMezcla, 1f + pack.burbujaVariacionMezcla));
        b.respVel = pack.burbujaVelocidadRespiro *
            Random.Range(1f - pack.burbujaVariacionRespiro, 1f + pack.burbujaVariacionRespiro);
        b.fase = Random.value * Mathf.PI * 2f;
        b.nacido = Time.unscaledTime;
        b.pop = false;

        b.sr.sprite = Elegir(anillos, 4);
        b.sr.transform.localPosition = new Vector3(b.x0, b.y, 0f);
        b.sr.transform.localScale = new Vector3(b.sx * EscalaAnillo(b.sr.sprite), b.sy * EscalaAnillo(b.sr.sprite), 1f);
        var c = b.sr.color; c.a = 0f; b.sr.color = c;
    }

    void NacerLinea(float w, float h, float fondo, float superficie)
    {
        float y = Mathf.Lerp(superficie, fondo,
            Mathf.Clamp01(pack.vaporNacerAbajo * Random.Range(0.4f, 1f)));
        float medio = w * pack.vaporAnchoLinea * 0.5f;
        float faseSway = Random.value * Mathf.PI * 2f;
        float vidaLinea = Random.Range(2.2f, 4f);
        int n = Mathf.Max(1, pack.vaporPuffsPorLinea);
        for (int i = 0; i < n; i++)
        {
            float t01 = n == 1 ? 0.5f : (float)i / (n - 1);
            float x = Mathf.Lerp(-medio, medio, t01);
            if (pack.vaporIrregularidad > 0f)
                x += Random.Range(-1f, 1f) * w * 0.04f * pack.vaporIrregularidad;
            NacerPuff(x, y, faseSway, vidaLinea);
        }
    }

    void NacerPuff(float x0, float y0, float faseSway, float vidaLinea)
    {
        var p = puffsLibres.Count > 0 ? puffsLibres[puffsLibres.Count - 1] : CrearPuff();
        if (puffsLibres.Count > 0) puffsLibres.RemoveAt(puffsLibres.Count - 1);
        else puffs.Add(p);
        p.sr.gameObject.SetActive(true);
        p.x0 = x0; p.y = y0; p.faseSway = faseSway; p.vida = vidaLinea;
        p.fase = Random.value * Mathf.PI * 2f;
        p.giro = Random.Range(-12f, 12f);
        p.factorT = Random.Range(0.7f, 1.4f);
        p.fx = Random.Range(0.85f, 1.25f);
        p.fy = Random.Range(0.85f, 1.25f);
        p.nacido = Time.unscaledTime;
        p.muriendo = false;
        p.sr.sprite = Elegir(humo, 3);
        var c = p.sr.color; c.a = 0f; p.sr.color = c;
    }

    Burbuja CrearBurbuja()
    {
        var go = new GameObject("Burbuja");
        go.transform.SetParent(capaAtras, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = liquid != null ? liquid.sortingOrder : 0;
        return new Burbuja { sr = sr };
    }

    Puff CrearPuff()
    {
        var go = new GameObject("VaporPuff");
        go.transform.SetParent(capaVapor, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = liquid != null ? liquid.sortingOrder : 0;
        return new Puff { sr = sr };
    }

    void LiberarBurbuja(int i)
    {
        burbujas[i].sr.gameObject.SetActive(false);
        burbujasLibres.Add(burbujas[i]);
        burbujas.RemoveAt(i);
    }

    void LiberarPuff(int i)
    {
        puffs[i].sr.gameObject.SetActive(false);
        puffsLibres.Add(puffs[i]);
        puffs.RemoveAt(i);
    }

    Sprite Elegir(List<Sprite> lista, int variantes)
    {
        if (lista.Count == 0) Generar(lista, variantes);
        return lista[Random.Range(0, lista.Count)];
    }

    static void Generar(List<Sprite> dest, int variantes)
    {
        if (dest.Count > 0) return;
        bool esHumo = variantes == 3;
        int s = esHumo ? 96 : 64;
        float[] dureza = { 2.6f, 4.2f, 6.5f };
        float[] pos = { 0.62f, 0.70f, 0.76f, 0.84f };
        float[] grosor = { 7f, 5f, 6f, 4f };
        float[] relleno = { 0.30f, 0.22f, 0.16f, 0.26f };
        for (int v = 0; v < variantes; v++)
        {
            var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - s * 0.5f) / (s * 0.5f);
                    float dy = (y - s * 0.5f) / (s * 0.5f);
                    float dd = dx * dx + dy * dy;
                    float a;
                    if (esHumo) a = Mathf.Exp(-dd * dureza[v]);
                    else a = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sqrt(dd) - pos[v]) * grosor[v])
                             + Mathf.Clamp01(1f - Mathf.Sqrt(dd)) * relleno[v];
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            tex.Apply();
            dest.Add(Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f));
        }
    }

    void OnDestroy() { Limpiar(); }
}
