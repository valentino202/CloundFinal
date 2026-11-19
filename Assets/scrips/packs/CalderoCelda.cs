using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Objetos flotando DENTRO de una casilla del tablero (world-space).
/// Receta tomada de la jerarquia del menu (boton 100px => fraccion de
/// celda de 1.2 unidades):
///   Nivel 1: dos hongos (14px y 7px).
///   Nivel 2: + ojo vivo con iris miradora (23px) + hongo grande (17px).
/// El ojo NO se sincroniza con los demas: cada objeto tiene su fase y
/// ritmo propio, y al subir de nivel los viejos conservan su paseo.
/// </summary>
public class CalderoCelda : MonoBehaviour
{
    class Objeto
    {
        public Transform tr;
        public SpriteRenderer sr;
        public Vector3 destino;
        public float proximoDestino;
        public float fase;
        public float escala;       // multiplicador de localScale (ya por nivel)
        public float escalaBase;   // escala calibrada
        public float diametro;     // tamano real en unidades (ya por nivel)
        public float diametroBase; // tamano calibrado
        public float giro;
        public float pulso;
        public float bob;
        public int nivelMinimo = 1;

        // Iris del ojo (5 miradas: central, 2 laterales espejadas, 2 verticales)
        public bool esOjo;
        public SpriteRenderer iris;
        public float irisEscala;
        public Vector3 irisBase, irisActual, irisDestino;
        public Sprite[] miradas;
        public bool[] espejoX, espejoY;
        public bool[] lateral;
        public float proximoMirada;

        // Movimiento del hueso (no gira: se meca) y su copia sumergida
        public bool sinGiro;
        public float oscilacion;
        public float velVaiven;
        public bool pasearPorBordes;
        public SpriteRenderer copia;
        public float copiaFase;
        public float z; // profundidad segun el orden de hermanos del menu
    }

    // Camara 2D en z=-10 mirando a +Z: "mas cerca" es z MENOR.
    // Misma capa que el liquido, por delante por Z; el vidrio (order 1)
    // sigue quedando encima de todo.
    const float ZDelanteDelLiquido = -0.01f;
    // Orden de hermanos del menu (atras -> adelante):
    // Hongo3, Hongo2, ojo2, hueso, Hongo1, copia sumergida, ojo.
    const float ZHongo3 = -0.011f;
    const float ZHongo2 = -0.012f;
    const float ZOjo2   = -0.013f;
    const float ZHueso  = -0.014f;
    const float ZHongo1 = -0.015f;
    const float ZOjo    = -0.017f;
    const float FondoVerdeFraccion = 0.42f; // el verde empieza mas arriba del borde

    /// <summary>Factor de homotecia: escala tamaños Y posiciones relativas
    /// desde el centro de la celda (origen 0,0 de este objeto).</summary>
    float K => Mathf.Max(0.01f, pack != null ? pack.objetosEscalaGlobal : 1f);

    readonly List<Objeto> objetos = new List<Objeto>();
    FxPack pack;
    SpriteRenderer liquid;
    SpriteMask mask; // recorte de la copia sumergida al shape del liquido
    int nivel;
    float celda;
    float topeY;
    float sup1, sup2, sup3;

    public void Configurar(FxPack pack, SpriteRenderer liquid, Vector3 centroLocal,
                           float superficie1, float superficie2, float superficie3)
    {
        this.pack = pack;
        this.liquid = liquid;
        celda = BoardFx.CellSize;
        sup1 = superficie1; sup2 = superficie2; sup3 = superficie3;
        transform.localPosition = centroLocal;

        if (pack.hongo1Sprite != null)
            CrearObjeto("Hongo1", pack.hongo1Sprite, pack.hongo1Tamano,
                pack.hongosGiro, pack.hongo1Respiro, 0.02f, 1, ZHongo1);
        if (pack.hongo2Sprite != null)
            CrearObjeto("Hongo2", pack.hongo2Sprite, pack.hongo2Tamano,
                -pack.hongosGiro, 0.161f, 0.04f, 1, ZHongo2);
        if (pack.hongoGrandeSprite != null)
            CrearObjeto("HongoGrande", pack.hongoGrandeSprite, pack.hongoGrandeTamano,
                pack.hongoGrandeGiro, pack.hongoGrandeRespiro, 0.04f, 2, ZHongo3);
        if (pack.ojoSprite != null)
            CrearOjo(false, pack.ojoTamano, pack.irisColor, ZOjo);
        if (pack.huesoSprite != null)
            CrearHueso();
        if (pack.ojoSprite != null)
            CrearOjo(true, pack.ojo2Tamano, pack.irisColor2, ZOjo2);

        SetNivel(0);
    }

    Objeto CrearObjeto(string nombre, Sprite sprite, float fraccionTamano,
                       float giro, float pulso, float bob, int nivelMinimo, float z)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = liquid != null ? liquid.sortingOrder : 0;

        // El PNG no mide 1 unidad: normalizamos por bounds para que la
        // fraccion signifique fraccion REAL de la celda.
        float diametro = celda * fraccionTamano * pack.objetosEscalaGlobal;
        float nativo = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        float escala = nativo > 0.01f ? diametro / nativo : diametro;
        go.transform.localScale = Vector3.one * escala;

        var o = new Objeto
        {
            tr = go.transform, sr = sr,
            escala = escala, escalaBase = escala,
            diametro = diametro, diametroBase = diametro,
            fase = Random.value * Mathf.PI * 2f,
            giro = giro, pulso = pulso, bob = bob,
            nivelMinimo = nivelMinimo, z = z,
        };
        objetos.Add(o);
        return o;
    }

    /// <summary>El ojo plus su iris hija con las 5 miradas del menu.</summary>
    void CrearOjo(bool segundo, float tamano, Color colorIris, float z)
    {
        var o = CrearObjeto(segundo ? "Ojo2" : "Ojo", pack.ojoSprite, tamano,
            pack.hongosGiro, 0f, 0.05f, segundo ? 3 : 2, z);
        o.esOjo = true;

        var irisGO = new GameObject("Iris");
        irisGO.transform.SetParent(o.tr, false);
        // Apenas por delante de SU ojo (0.0005), no 0.01: si no, la iris
        // del ojo 2 se adelanta al hueso y parece flotar separada.
        irisGO.transform.localPosition = new Vector3(0f, 0f, -0.0005f);
        var isr = irisGO.AddComponent<SpriteRenderer>();
        isr.sprite = pack.irisCentral != null ? pack.irisCentral : pack.ojoSprite;
        isr.color = colorIris; // rojo (P1) o azul (P2): no lo tiñe el liquido
        isr.sortingOrder = o.sr.sortingOrder;

        // La iris es HIJA del ojo: su localScale se multiplica por la
        // escala del padre. Hay que dividirla para que el tamano MUNDIAL
        // sea el 68% real del ojo (antes quedaba al ~27%, un puntito).
        float irisDiametro = o.diametro * pack.irisFraccion;
        float nativoIris = isr.sprite != null
            ? Mathf.Max(isr.sprite.bounds.size.x, isr.sprite.bounds.size.y) : 1f;
        float escalaMundial = nativoIris > 0.01f ? irisDiametro / nativoIris : irisDiametro;
        o.irisEscala = escalaMundial / Mathf.Max(0.0001f, o.escala);
        irisGO.transform.localScale = Vector3.one * o.irisEscala;

        Sprite c = pack.irisCentral != null ? pack.irisCentral : pack.ojoSprite;
        Sprite l = pack.irisLateral != null ? pack.irisLateral : c;
        Sprite v = pack.irisVertical != null ? pack.irisVertical : c;
        o.miradas = new[] { c, l, l, v, v };
        o.espejoX = new[] { false, false, true, false, false };
        o.espejoY = new[] { false, false, false, false, true };
        o.lateral = new[] { false, true, true, false, false };
        o.iris = isr;
        o.irisBase = irisGO.transform.localPosition;
        o.proximoMirada = Time.unscaledTime + Random.Range(0.3f, 0.9f);
    }

    static Sprite mascaraBlancaCache;
    static Sprite MascaraBlanca()
    {
        if (mascaraBlancaCache != null) return mascaraBlancaCache;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        mascaraBlancaCache = Sprite.Create(tex, new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f), 1f);
        return mascaraBlancaCache;
    }

    /// <summary>
    /// Hueso del nivel 3: pasea por los BORDES, no gira (se meca) y
    /// lleva una copia sumergida que sigue su transform y se tiñe de verde.
    /// </summary>
    void CrearHueso()
    {
        var o = CrearObjeto("Hueso", pack.huesoSprite, pack.huesoTamano,
            0f, 0f, pack.huesoBob, 3, ZHueso);
        o.sinGiro = true;
        o.oscilacion = pack.huesoOscilacion;
        o.velVaiven = pack.huesoVelVaiven;
        o.pasearPorBordes = true;

        // Mascara con las medidas REALES de 'ZonaAgua' del menu:
        // 100 x 78.77 px de un boton de 100px, centrada en y=-11.3.
        // En el juego el boton = 1 celda (BoardFx.CellSize), asi que
        // recorta exactamente la misma zona (bajo el agua).
        var maskGO = new GameObject("ZonaAgua_Mask");
        maskGO.transform.SetParent(transform, false);
        maskGO.transform.localPosition = new Vector3(0f, -celda * 0.113f, 0f);
        maskGO.transform.localScale = new Vector3(celda * 1f, celda * 0.7877f, 1f);
        mask = maskGO.AddComponent<SpriteMask>();
        mask.sprite = MascaraBlanca();
        mask.alphaCutoff = 0.1f;

        var copiaGO = new GameObject("Hueso_Agua");
        copiaGO.transform.SetParent(o.tr, false);
        // Z MENOR = mas cerca (camara en -10). -0.002 deja la copia
        // DELANTE del hueso (-0.014 -> -0.016) pero detras del ojo (-0.017),
        // igual que ZonaAgua esta entre el hueso y base_ojo en el menu.
        copiaGO.transform.localPosition = new Vector3(0f, 0f, -0.002f);
        var cr = copiaGO.AddComponent<SpriteRenderer>();
        cr.sprite = pack.huesoSprite;
        cr.sortingOrder = o.sr.sortingOrder;
        cr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        copiaGO.transform.localScale = Vector3.one; // copia exacta del padre
        o.copia = cr;
        o.copiaFase = Random.value * Mathf.PI * 2f;
    }

    /// <summary>Cada objeto aparece en su nivel; los vivos NO se reinician.</summary>
    public void SetNivel(int nivelActual)
    {
        nivel = nivelActual;
        float superficie = nivelActual <= 1 ? sup1 : nivelActual == 2 ? sup2 : sup3;
        // x_nuevo = 0 + K * (x_original - 0): el techo del paseo se
        // acerca al centro con el mismo K que los tamaños.
        topeY = K * (-celda * 0.5f + celda * superficie - celda * 0.01f);
        float factorNivel = nivelActual <= 1 ? pack.factorTamanoNivel1 : 1f;

        foreach (var o in objetos)
        {
            bool visible = nivelActual >= o.nivelMinimo;
            bool yaVivo = o.tr.gameObject.activeSelf;
            if (!visible) { o.tr.gameObject.SetActive(false); continue; }

            o.diametro = o.diametroBase * factorNivel;
            o.escala = o.escalaBase * factorNivel;
            o.tr.localScale = Vector3.one * o.escala;

            if (!yaVivo)
            {
                // Objeto NUEVO: arranca dentro del liquido con fase propia.
                o.destino = DestinoAleatorio(o);
                o.tr.localPosition = o.destino;
                o.proximoDestino = Time.unscaledTime + Random.Range(1.2f, 3.5f);
            }
            else
            {
                // Ya vivo: conserva paseo/fase/ritmo; solo se recorta a los
                // limites del nuevo nivel.
                Vector3 p = o.tr.localPosition;
                Limitar(o, ref p);
                o.tr.localPosition = p;
            }
            o.tr.gameObject.SetActive(true);
        }
    }

    void Limitar(Objeto o, ref Vector3 p)
    {
        float mitad = o.diametro * 0.5f;
        // Homotecia: los LIMITES se escalan por K; el radio del objeto
        // (mitad) ya viene escalado porque diametro = K * diametroBase.
        float fondo = -celda * FondoVerdeFraccion * K + mitad;
        p.x = Mathf.Clamp(p.x, -celda * 0.45f * K + mitad, celda * 0.45f * K - mitad);
        p.y = Mathf.Clamp(p.y, fondo, Mathf.Max(fondo, topeY - mitad));
    }

    Vector3 DestinoAleatorio(Objeto o)
    {
        float mitad = o.diametro * 0.5f;
        float minX = -celda * 0.45f * K + mitad, maxX = celda * 0.45f * K - mitad;
        float x;
        if (o.pasearPorBordes)
        {
            // Hueso: como una cuchara, se pega a las paredes del vaso.
            float margen = (maxX - minX) * 0.25f;
            x = Random.value < 0.5f
                ? Random.Range(minX, minX + margen)
                : Random.Range(maxX - margen, maxX);
        }
        else x = Random.Range(minX, maxX);

        float fondo = -celda * FondoVerdeFraccion * K + mitad;
        float y = Random.Range(fondo, Mathf.Max(fondo, topeY - mitad));
        return new Vector3(x, y, o.z);
    }

    void Update()
    {
        if (nivel < 1 || objetos.Count == 0) return;
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        Color liq = liquid != null ? liquid.color : Color.clear;

        foreach (var o in objetos)
        {
            if (!o.tr.gameObject.activeSelf) continue;

            if (t >= o.proximoDestino)
            {
                o.destino = DestinoAleatorio(o);
                o.proximoDestino = t + Random.Range(1.2f, 3.5f);
            }
            // El bob (desplazamiento) y la velocidad tambien son longitudes
            // por segundo: se escalan con K para conservar la homotecia.
            Vector3 objetivo = o.destino + Vector3.up * (Mathf.Sin(t * 1.3f + o.fase) * o.bob * K);
            float vel = (o.pasearPorBordes ? pack.huesoVelocidadPaseo : pack.hongosVelocidadPaseo) * K;
            o.tr.localPosition = Vector3.MoveTowards(o.tr.localPosition, objetivo, vel * dt);
            // El hueso NO gira: se apoya y se meca (vaiven del menu).
            float rotZ = o.sinGiro
                ? Mathf.Sin(t * o.velVaiven + o.fase) * o.oscilacion
                : t * o.giro;
            o.tr.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            if (o.pulso > 0f)
                o.tr.localScale = Vector3.one * (o.escala *
                    (1f + o.pulso * Mathf.Sin(t * (o.nivelMinimo == 2 ? 0.5f : 0.9f) + o.fase)));

            // Hongos: ARCOIRIS + tinte sumergido con respiro. Ojo: su
            // esclerotica tambien se tiñe, pero la iris NO (tenirHijos=0).
            if (o.sr != null)
            {
                if (o.sinGiro)
                {
                    // Hueso: arriba al AIRE se ve normal (sumergido=0),
                    // solo su COPIA recibe el velo verde mas abajo.
                    o.sr.color = Color.white;
                }
                else
                {
                    Color baseC = pack.hongosArcoiris && !o.esOjo
                        ? Color.HSVToRGB(Mathf.Repeat(t * pack.hongosVelocidadArcoiris + o.fase, 1f),
                                         0.8f, 0.9f + 0.1f * Mathf.Sin(t * 2.2f + o.fase))
                        : Color.white;
                    Color c = baseC;
                    if (liq.a > 0f)
                    {
                        float fuerza = pack.hongosFuerzaSumergido *
                            (0.75f + 0.25f * Mathf.Sin(t * pack.hongosRespiroColor + o.fase));
                        c = Color.Lerp(baseC, liq * baseC, fuerza);
                    }
                    c.a = pack.hongosOpacidad;
                    o.sr.color = c;
                }
            }

            // Copia SUMERGIDA del hueso: sigue su transform (es hija) y
            // recibe el color del liquido con su propio respiro.
            if (o.copia != null && liq.a > 0f)
            {
                float fuerza = pack.huesoCopiaFuerza *
                    (0.75f + 0.25f * Mathf.Sin(t * pack.huesoCopiaVelRespiro + o.copiaFase));
                Color c = Color.Lerp(Color.white, liq, fuerza);
                c.a = pack.huesoCopiaOpacidad;
                o.copia.color = c;
            }

            if (o.esOjo) ActualizarIris(o, t, dt);
        }
    }

    void ActualizarIris(Objeto o, float t, float dt)
    {
        if (o.iris == null || o.miradas == null) return;
        if (t >= o.proximoMirada)
        {
            o.proximoMirada = t + Random.Range(pack.irisMinCambio, pack.irisMaxCambio);
            int i = Random.Range(0, o.miradas.Length);
            o.iris.sprite = o.miradas[i];
            o.iris.transform.localScale = new Vector3(
                o.irisEscala * (o.espejoX[i] ? -1f : 1f),
                o.irisEscala * (o.espejoY[i] ? -1f : 1f), 1f);

            // Amplitud en coordenadas LOCALES del ojo (su scale la aplica
            // Unity al hijo): world = local * o.escala.
            float amp = o.diametro * pack.irisDesplazamiento /
                        Mathf.Max(0.01f, o.escala);
            if (o.lateral[i]) amp *= pack.irisExtraLateral;
            Vector2 dir = i == 1 ? Vector2.right : i == 2 ? Vector2.left
                        : i == 3 ? Vector2.up : i == 4 ? Vector2.down : Vector2.zero;
            o.irisDestino = new Vector3(dir.x * amp, dir.y * amp, 0f)
                            + (Vector3)(Random.insideUnitCircle * amp * 0.25f);
        }
        o.irisActual = Vector3.Lerp(o.irisActual, o.irisDestino,
            dt / Mathf.Max(0.02f, pack.irisSuavidad));
        o.iris.transform.localPosition = o.irisBase + o.irisActual;
    }

    void OnDestroy()
    {
        foreach (var o in objetos)
            if (o.tr != null) Destroy(o.tr.gameObject);
        objetos.Clear();
    }
}
