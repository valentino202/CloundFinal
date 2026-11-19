using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos del pack caldero dentro de una casilla (world-space):
///   - BURBUJAS con DOS PLANOS: las de atras quedan detras de los objetos,
///     las de delante pasan por encima (pero detras del vidrio), con
///     variacion de tamano/alpha/mezcla/ritmo por burbuja y 'pop' al morir.
///   - VAPOR en LINEAS: bloques de puffs que nacen JUNTOS dentro del verde,
///     suben como una columna, crecen, respiran el color real y se congelan
///     al llegar al techo antes de desvanecerse.
/// Receta y comportamiento copiados de BurbujasCalderoUI / VaporCalderoUI.
/// </summary>
public class EfectosCalderoCelda : MonoBehaviour
{
    class Burbuja
    {
        public SpriteRenderer sr;
        public float x0, y, tamano, fase, nacido;
        public float sx, sy, alphaPropia, mezclaPropia, respiroVel;
        public bool pop, delante;
        public float popT;
    }

    class Puff
    {
        public SpriteRenderer sr;
        public float x0, y, fase, faseSway, giro, nacido, vida;
        public float factorT, factorFx, factorFy;
        public bool muriendo;
        public float muerteT;
    }

    // Camara en z=-10: mas negativo = mas cerca. El vidrio (glassOrder=1)
    // va encima de todo por sortingOrder.
    const float ZBurbujaAtras = -0.005f;   // detras de los objetos (-0.011)
    const float ZVapor = -0.006f;          // detras de los objetos
    const float ZBurbujaDelante = -0.018f; // por delante del ojo (-0.017)

    readonly List<Burbuja> burbujas = new List<Burbuja>();
    readonly List<Burbuja> burbujasLibres = new List<Burbuja>();
    readonly List<Puff> puffs = new List<Puff>();
    readonly List<Puff> puffsLibres = new List<Puff>();

    static readonly List<Sprite> anillos = new List<Sprite>();
    static readonly List<Sprite> puffsSuaves = new List<Sprite>();

    /// <summary>Escala para que un sprite de 64/96px con PPU 100 mida
    /// exactamente las unidades que pide el menu (sx/sy en pixeles del
    /// boton de 100 = fraccion de celda).</summary>
    static float EscalaSprite(Sprite sp)
    {
        if (sp == null) return 1f;
        float nativo = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
        return nativo > 0.01f ? 1f / nativo : 1f;
    }

    FxPack pack;
    SpriteRenderer liquid;
    Transform padreLiquido, padreFrente, padreVapor;
    int nivel;
    float sup1, sup2, sup3;
    float acumBurbujas, acumVapor;

    public void Configurar(FxPack pack, SpriteRenderer liquid, Vector3 centroLocal,
                           float s1, float s2, float s3)
    {
        this.pack = pack;
        this.liquid = liquid;
        transform.localPosition = centroLocal;
        sup1 = s1; sup2 = s2; sup3 = s3;

        padreLiquido = CrearContenedor("BurbujasAtras", ZBurbujaAtras);
        padreFrente = CrearContenedor("BurbujasFrente", ZBurbujaDelante);
        padreVapor = CrearContenedor("Vapor", ZVapor);
    }

    Transform CrearContenedor(string nombre, float z)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        return go.transform;
    }

    public void SetNivel(int nivelActual)
    {
        nivel = nivelActual;
        if (nivel <= 0) Reiniciar();
    }

    void Reiniciar()
    {
        acumBurbujas = acumVapor = 0f;
        foreach (var b in burbujas) { b.sr.gameObject.SetActive(false); burbujasLibres.Add(b); }
        burbujas.Clear();
        foreach (var p in puffs) { p.sr.gameObject.SetActive(false); puffsLibres.Add(p); }
        puffs.Clear();
    }

    float Superficie()
    {
        float f = nivel <= 1 ? sup1 : nivel == 2 ? sup2 : sup3;
        return -BoardFx.CellSize * 0.5f + BoardFx.CellSize * f;
    }

    /// <summary>Fondo REAL del liquido (borde inferior de la celda), igual
    /// que el menu usa -h*0.5 del rect del Liquid.</summary>
    float FondoLiquido() => -BoardFx.CellSize * 0.5f;

    void Update()
    {
        if (nivel <= 0 || pack == null)
        {
            // Celda vacia / sin pack: si quedaron puffs o burbujas vivas,
            // LIMITARLOS (no congelarlos como en el menu, que siempre esta
            // lleno). Soluciona el humo acumulado en celdas nivel 1/2.
            for (int i = puffs.Count - 1; i >= 0; i--)
                Liberar(puffs, puffsLibres, i, puffs[i].sr);
            for (int i = burbujas.Count - 1; i >= 0; i--)
                Liberar(burbujas, burbujasLibres, i, burbujas[i].sr);
            return;
        }
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        Color liq = liquid != null ? liquid.color : Color.white;
        float celda = BoardFx.CellSize;
        float superficie = Superficie();
        float fondo = FondoLiquido();
        // El usuario lo confirmo con el Inspector: todos los valores son
        // IGUALES en los 3 niveles, solo cambia la franja del liquido.
        // Los multiplicadores generales (densidad/tamano/velocidad/opacidad)
        // estan en 1, asi que la tasa es FIJA (15/s burbujas, 1.94/s vapor).

        // --- BURBUJAS ---
        acumBurbujas += pack.bubbleRate * pack.densidadEfectos * dt;
        while (acumBurbujas >= 1f) { acumBurbujas -= 1f; NacerBurbuja(liq); }

        for (int i = burbujas.Count - 1; i >= 0; i--)
        {
            var b = burbujas[i];
            float edad = t - b.nacido;
            if (b.pop)
            {
                b.popT += dt;
                float k = Mathf.Clamp01(b.popT / 0.15f);
                float esc = 1.15f + 0.45f * k;
                float eb = EscalaSprite(b.sr.sprite);
                b.sr.transform.localScale = new Vector3(b.sx * esc * eb, b.sy * esc * eb, 1f);
                var cp = b.sr.color;
                cp.a = b.alphaPropia * (1f - k);
                b.sr.color = cp;
                if (k >= 1f) Liberar(burbujas, burbujasLibres, i, b.sr);
                continue;
            }

            float subida = celda * pack.bubbleRise * pack.velocidadEfectos;
            b.y += subida * dt;
            if (b.y >= superficie) { b.pop = true; b.popT = 0f; }
            float x = b.x0 + Mathf.Sin(t * 1.5f + b.fase) * celda * pack.bubbleDrift;
            float prog = Mathf.Clamp01((b.y - fondo) / Mathf.Max(0.01f, superficie - fondo));
            float crece = Mathf.Lerp(0.85f, 1.15f, prog);
            float resp = 0.75f + 0.25f * Mathf.Sin(t * b.respiroVel + b.fase);

            var c = Color.Lerp(Color.white, liq, b.mezclaPropia);
            c.a = b.alphaPropia * resp * Mathf.Clamp01(edad / 0.12f);
            b.sr.color = c;
            b.sr.transform.localPosition = new Vector3(x, b.y, 0f);
            float eb = EscalaSprite(b.sr.sprite);
            b.sr.transform.localScale = new Vector3(b.sx * crece * eb, b.sy * crece * eb, 1f);
        }

        // --- VAPOR EN LINEAS ---
        if (pack.vaporOn && pack.vaporLineas > 0f)
        {
            acumVapor += pack.vaporLineas * pack.densidadEfectos * dt;
            while (acumVapor >= 1f) { acumVapor -= 1f; NacerLinea(superficie, fondo); }
        }

        // EXACTAMENTE como el menu (VaporCalderoUI.Update, linea 211): el
        // vapor sube solo un poco POR ENCIMA de la superficie (topeVapor
        // = 0.08). Sale de un sitio LIMITADO junto al agua, NO de todo el
        // envase. El borde del vidrio es el tope duro.
        float tope = Mathf.Min(superficie + celda * pack.vaporTecho, celda * 0.5f);
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
            float resp = 0.7f + 0.3f * Mathf.Sin(t * pack.vaporVelocidadRespiro + p.fase);
            var c = Color.Lerp(Color.white, liq, pack.vaporMezclaLiquido);
            c.a = pack.vaporAlpha * pack.opacidadEfectos * resp * a;
            p.sr.color = c;

            if (!p.muriendo) p.y += celda * pack.vaporSubida * pack.velocidadEfectos * dt;
            float x = p.x0 + Mathf.Sin(t * 0.9f + p.faseSway) * celda * 0.04f * (0.4f + pack.vaporAnchoLinea);
            float crece = 1f + pack.vaporCrecimiento * Mathf.Clamp01(edad / p.vida);
            float baseTam = celda * pack.vaporTamano * pack.tamanoEfectos * p.factorT;
            float sx = baseTam * p.factorFx, sy = baseTam * p.factorFy;
            p.y = Mathf.Min(p.y, tope - sy * 0.45f);
            p.sr.transform.localPosition = new Vector3(x, p.y, 0f);
            float ep = EscalaSprite(p.sr.sprite);
            p.sr.transform.localScale = new Vector3(sx * crece * ep, sy * crece * ep, 1f);
            p.sr.transform.localRotation = Quaternion.Euler(0f, 0f,
                p.giro + Mathf.Sin(t * 0.5f + p.fase) * 8f);

            if (edad >= p.vida || (p.muriendo && a <= 0.001f))
                Liberar(puffs, puffsLibres, i, p.sr);
        }
    }

    void NacerBurbuja(Color liq)
    {
        var b = burbujasLibres.Count > 0 ? burbujasLibres[burbujasLibres.Count - 1] : CrearBurbuja();
        if (burbujasLibres.Count > 0) burbujasLibres.RemoveAt(burbujasLibres.Count - 1);
        else burbujas.Add(b);
        b.sr.gameObject.SetActive(true);

        float celda = BoardFx.CellSize;
        float margen = celda * pack.bubbleSize * 0.5f + celda * 0.04f;
        b.x0 = Random.Range(-celda * 0.45f + margen, celda * 0.45f - margen);
        b.y = FondoLiquido() + Random.Range(0.02f, 0.08f) * celda;
        float tam = celda * pack.bubbleSize * pack.tamanoEfectos * Random.Range(0.6f, 1.5f);
        b.sx = tam * Random.Range(0.85f, 1.2f);
        b.sy = tam * Random.Range(0.85f, 1.2f);

        bool delante = Random.value < pack.burbujaPorcentajeDelante;
        b.delante = delante;
        b.sr.transform.SetParent(delante ? padreFrente : padreLiquido, false);
        float dado = Random.value;
        dado = delante
            ? Mathf.Lerp(dado, 1f, pack.burbujaSesgoProfundidad)
            : Mathf.Lerp(dado, 0f, pack.burbujaSesgoProfundidad);
        b.alphaPropia = Mathf.Clamp01(pack.bubbleAlpha * pack.opacidadEfectos *
            Mathf.Lerp(1f - pack.burbujaVariacionAlpha, 1f + pack.burbujaVariacionAlpha, dado));
        b.mezclaPropia = Mathf.Clamp01(pack.burbujaMezclaLiquido *
            Random.Range(1f - pack.burbujaVariacionMezcla, 1f + pack.burbujaVariacionMezcla));
        b.respiroVel = pack.burbujaVelocidadRespiro *
            Random.Range(1f - pack.burbujaVariacionRespiro, 1f + pack.burbujaVariacionRespiro);
        b.fase = Random.value * Mathf.PI * 2f;
        b.nacido = Time.unscaledTime;
        b.pop = false;

        b.sr.sprite = Elegir(anillos, 4);
        b.sr.transform.localPosition = new Vector3(b.x0, b.y, 0f);
        b.sr.transform.localScale = new Vector3(b.sx * EscalaSprite(b.sr.sprite), b.sy * EscalaSprite(b.sr.sprite), 1f);
        var c = b.sr.color; c.a = 0f; b.sr.color = c;
    }

    void NacerLinea(float superficie, float fondo)
    {
        float celda = BoardFx.CellSize;
        float y = Mathf.Lerp(superficie, fondo,
            Mathf.Clamp01(pack.vaporNacerAbajo * Random.Range(0.4f, 1f)));
        float medio = celda * pack.vaporAnchoLinea * 0.5f;
        float faseSway = Random.value * Mathf.PI * 2f;
        float vida = Random.Range(2.2f, 4f);
        int n = Mathf.Max(1, pack.vaporPuffsPorLinea);
        for (int i = 0; i < n; i++)
        {
            float t01 = n == 1 ? 0.5f : (float)i / (n - 1);
            float x = Mathf.Lerp(-medio, medio, t01);
            if (pack.vaporIrregularidad > 0f)
                x += Random.Range(-1f, 1f) * celda * 0.04f * pack.vaporIrregularidad;
            NacerPuff(x, y, faseSway, vida);
        }
    }

    void NacerPuff(float x0, float y0, float faseSway, float vida)
    {
        var p = puffsLibres.Count > 0 ? puffsLibres[puffsLibres.Count - 1] : CrearPuff();
        if (puffsLibres.Count > 0) puffsLibres.RemoveAt(puffsLibres.Count - 1);
        else puffs.Add(p);
        p.sr.gameObject.SetActive(true);
        p.x0 = x0; p.y = y0; p.faseSway = faseSway; p.vida = vida;
        p.fase = Random.value * Mathf.PI * 2f;
        p.giro = Random.Range(-12f, 12f);   // menu
        p.factorT = Random.Range(0.7f, 1.4f); // menu
        p.factorFx = Random.Range(0.8f, 1.25f);
        p.factorFy = Random.Range(0.8f, 1.25f);
        p.nacido = Time.unscaledTime;
        p.muriendo = false;
        p.sr.sprite = Elegir(puffsSuaves, 3);
    }

    Burbuja CrearBurbuja()
    {
        var go = new GameObject("Burbuja");
        go.transform.SetParent(padreLiquido, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = liquid != null ? liquid.sortingOrder : 0;
        var b = new Burbuja { sr = sr };
        return b; // NacerBurbuja lo agrega a la lista de vivas
    }

    Puff CrearPuff()
    {
        var go = new GameObject("VaporPuff");
        go.transform.SetParent(padreVapor, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = liquid != null ? liquid.sortingOrder : 0;
        var p = new Puff { sr = sr };
        return p; // NacerPuff lo agrega a la lista de vivas
    }

    void Liberar<T>(List<T> vivas, List<T> libres, int i, SpriteRenderer sr)
    {
        sr.gameObject.SetActive(false);
        libres.Add(vivas[i]);
        vivas.RemoveAt(i);
    }

    Sprite Elegir(List<Sprite> lista, int variantes)
    {
        if (lista.Count == 0) Generar(lista, variantes);
        return lista[Random.Range(0, lista.Count)];
    }

    static void Generar(List<Sprite> dest, int variantes)
    {
        if (dest.Count > 0) return;
        bool vapor = variantes == 3;
        int s = vapor ? 96 : 64;
        // VAPOR: EXACTAMENTE como VaporCalderoUI.Puffs() del menu:
        // gaussiana exp(-d*k), 3 durezas (2.6 / 4.2 / 6.5), sin ruido.
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
                    if (vapor) a = Mathf.Exp(-dd * dureza[v]); // nube densa al centro
                    else
                    {
                        float d = Mathf.Sqrt(dd);
                        a = Mathf.Clamp01(1f - Mathf.Abs(d - pos[v]) * grosor[v])
                            + Mathf.Clamp01(1f - d) * relleno[v];
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            tex.Apply();
            dest.Add(Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f));
        }
    }

    void OnDestroy()
    {
        Reiniciar();
    }
}
