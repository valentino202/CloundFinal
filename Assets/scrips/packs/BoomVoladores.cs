using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BOOM del caldero EN EL JUEGO (el premio visual que solo se ve aca):
/// al explotar una casilla, varios objetos del pack (hongos/ojo/hueso)
/// salen volando en DIAGONALES LATERALES al azar, giran, caen un poco y
/// se desvanecen hasta desaparecer.
///
/// Responsabilidad: SOLO los voladores. El clip de explosion, el sonido y
/// el Destello los maneja CellVisuals.PlayBoom como siempre.
/// Se auto-destruye cuando todos los voladores terminan.
/// </summary>
public class BoomVoladores : MonoBehaviour
{
    class Volador
    {
        public SpriteRenderer sr;
        public Vector2 vel;
        public float giro;
        public float nacido, vida;
        public float escala;
    }

    readonly List<Volador> voladores = new List<Volador>();
    FxPack pack;
    Color tinte;
    float celda;

    /// <summary>Configura el boom. 'tinte' = color del liquido del dueno.</summary>
    public void Configurar(FxPack pack, Vector3 centroLocal, Color tinte)
    {
        this.pack = pack;
        this.tinte = tinte;
        celda = BoardFx.CellSize;
        transform.localPosition = centroLocal;

        var sprites = new List<Sprite>();
        if (pack.hongo1Sprite != null) sprites.Add(pack.hongo1Sprite);
        if (pack.hongo2Sprite != null) sprites.Add(pack.hongo2Sprite);
        if (pack.hongoGrandeSprite != null) sprites.Add(pack.hongoGrandeSprite);
        if (pack.ojoSprite != null) sprites.Add(pack.ojoSprite);
        if (pack.huesoSprite != null) sprites.Add(pack.huesoSprite);

        int cantidad = Mathf.Max(1, pack.boomObjetos);
        for (int i = 0; i < cantidad; i++)
        {
            Sprite sp = sprites.Count > 0 ? sprites[Random.Range(0, sprites.Count)] : null;
            CrearVolador(sp);
        }
    }

    void CrearVolador(Sprite sp)
    {
        var go = new GameObject("Volador");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        // Arte REAL del objeto (blanco = colores del PNG), NO el tinte del
        // liquido: asi se distingue de la explosion (que si va teñida) y
        // se ven como hongos/ojo/hueso volando, no como destellos verdes.
        sr.color = Color.white;
        sr.sortingOrder = explosionOrderBoom;

        // Diagonal LATERAL marcada: salen hacia los costados y arriba,
        // bien separados del centro de la explosion (no se mezclan con el
        // destello). Van mas rapido que el clip para que se vean volar.
        float lado = Random.value < 0.5f ? -1f : 1f;
        float vx = lado * Random.Range(pack.boomVelocidad * 0.8f, pack.boomVelocidad * 1.4f);
        float vy = Random.Range(pack.boomVelocidad * 0.6f, pack.boomVelocidad * 1.2f);
        float escala = celda * Random.Range(0.08f, 0.16f);
        // Normalizar el sprite (PNG no mide 1 unidad), igual que los objetos.
        if (sp != null)
        {
            float nativo = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
            if (nativo > 0.01f) escala /= nativo;
        }
        go.transform.localScale = Vector3.one * escala;

        voladores.Add(new Volador
        {
            sr = sr,
            vel = new Vector2(vx, vy),
            giro = Random.Range(-240f, 240f),
            nacido = Time.unscaledTime,
            vida = Random.Range(pack.boomVida * 0.7f, pack.boomVida * 1.3f),
            escala = escala,
        });
    }

    // Entre el vidrio (1) y la explosion (3): por encima del cristal para
    // que se vean, pero por DEBAJO del destello para que no se mezclen con
    // la animacion 4 y se distingan como objetos separados volando.
    const int explosionOrderBoom = 2;

    void Update()
    {
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;
        for (int i = voladores.Count - 1; i >= 0; i--)
        {
            var v = voladores[i];
            float edad = t - v.nacido;
            if (edad >= v.vida) { Destroy(v.sr.gameObject); voladores.RemoveAt(i); continue; }

            // Gravedad leve: la subida se frena y cae un poco.
            v.vel.y -= celda * 1.2f * dt;
            v.sr.transform.localPosition += (Vector3)(v.vel * dt);
            v.sr.transform.localRotation = Quaternion.Euler(0f, 0f, v.giro * edad);

            // Se desvanecen al final de su vida (conservando el arte real).
            float k = Mathf.Clamp01(edad / v.vida);
            var c = v.sr.color;
            c.a = 1f - k * k;
            v.sr.color = c;
        }

        if (voladores.Count == 0) Destroy(gameObject);
    }
}
