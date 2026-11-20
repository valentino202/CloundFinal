using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ruteo de animaciones SIN MonoBehaviour. El tablero pregunta aqui
/// que pack esta equipado y dispara tus hooks con la llave que toca:
/// - LevelFx: (celda, nivel 1-3, llave) al colocar un punto.
/// - ExplosionFx: (celdas en orden de cadena, llave boom) al explotar.
/// Tu arte se suscribe a esos hooks y reproduce su animacion con la llave.
/// Si no hay suscripcion, el tablero usa su look por codigo (fallback).
/// Pon SuppressFallback = true cuando tus animaciones ya cubran todo.
///
/// Tambien es la fuente unica de la paleta (GDD "Vibrant Neon Pop") y del
/// layout del tablero, para que GameManager y CellVisuals no se desincronicen.
/// </summary>
public static class BoardFx
{
    public static Action<Transform, int, string> LevelFx;
    public static Action<List<Vector2Int>, string> ExplosionFx;
    public static bool SuppressFallback = false;

    /// <summary>Quien provoco la ultima explosion (1 o 2). GameManager lo fija
    /// antes de disparar ExplosionFx para que el boom se tinte de su color.</summary>
    public static int LastAttacker = 1;

    // --- Paleta GDD (movida desde GameManager para no duplicarla) ---
    // ColP1/ColP2 NO son readonly: el jugador los cambia en Personalizacion.
    public static readonly Color ColNeutral = new Color(0.12f, 0.16f, 0.28f); // azul noche
    public static Color ColP1 = PlayerColorOptions.Rosa;   // rosado neon
    public static Color ColP2 = PlayerColorOptions.Naranja; // naranja electrico
    public static readonly Color ColBoom = new Color(1f, 0.85f, 0.3f);  // destello explosion

    /// <summary>
    /// Aplica los colores de los DOS casilleros, INDEPENDIENTES (paso 3):
    /// cada jugador elige su color (rosa, naranja, morado, rueda...).
    /// </summary>
    public static void ApplyColors(int mode1, Color custom1, int mode2, Color custom2)
    {
        ColP1 = PlayerColorOptions.Get(mode1, custom1);

        if (mode2 < 0)
        {
            // Casillero 2 sin color elegido: se diferencia solo del P1
            // (comportamiento original: rosa <-> naranja, o HSV opuesto
            // si P1 uso la rueda). Asi nunca quedan dos jugadores iguales.
            if (mode1 == PlayerColorOptions.RosaIndex)
                ColP2 = PlayerColorOptions.Naranja;
            else if (mode1 == PlayerColorOptions.NaranjaIndex)
                ColP2 = PlayerColorOptions.Rosa;
            else
                ColP2 = PlayerColorOptions.OppositeOf(ColP1);
        }
        else
        {
            // Color elegido explicitamente en el casillero 2: manda el.
            ColP2 = PlayerColorOptions.Get(mode2, custom2);
        }
    }

    /// <summary>
    /// Compatibilidad con llamadas viejas de un solo jugador: el rival
    /// queda naranja (el look de siempre).
    /// </summary>
    public static void ApplyColors(int playerMode, Color custom)
        => ApplyColors(playerMode, custom, PlayerColorOptions.NaranjaIndex, PlayerColorOptions.Naranja);

    // --- Layout del tablero en unidades de mundo (camara ortografica) ---
    /// <summary>Lado de una celda. Las animaciones son cuadradas, asi que 1x1.</summary>
    public const float CellSize = 1.2f;
    /// <summary>Hueco entre celdas. 0 = pegadas pared con pared.</summary>
    public const float CellGap = 0f;
    /// <summary>Distancia centro-a-centro entre celdas vecinas.</summary>
    public static float Pitch => CellSize + CellGap;

    /// <summary>Color de liquido segun el dueño de la celda.</summary>
    public static Color LiquidColorFor(int owner) => owner switch
    {
        1 => ColP1,
        2 => ColP2,
        _ => ColNeutral,
    };

    /// <summary>
    /// Color de liquido teniendo en cuenta el PACK (paso 1 del plan de
    /// packs individualistas). Si el pack trae su propio color, manda el;
    /// si no, cae al color por dueño de siempre. Con "clasico" (usaColorPropio
    /// = false) devuelve EXACTAMENTE lo mismo que la sobrecarga de arriba.
    /// </summary>
    public static Color LiquidColorFor(int owner, FxPack pack)
        => pack != null && pack.usaColorPropio
            ? pack.colorLiquidoPropio
            : LiquidColorFor(owner);
    // El brillo lo da el boost GLOBAL (ColorBoost), que es el que usan los
    // clasicos y que el usuario quiere vivo. No hace falta boost por pack.

    /// <summary>
    /// Multiplicador de brillo. Los sprites del atlas estan desaturados y su
    /// brillo maximo es ~0.74, asi que el tint normal queda lavado. Con boost
    /// el pixel mas brillante llega a 1.0 y el color se ve vivo.
    /// </summary>
    /// <summary>
    /// Brillo del tint. El PNG del liquido se exporto en GRIS OSCURO (la
    /// capa de tono/saturacion de Photoshop no se aplico al exportar), asi
    /// que hace falta un boost alto para que el color del jugador se vea
    /// vivo tanto en el juego como en el menu (ambos usan Boost).
    /// </summary>
    public static float ColorBoost = 1.4f;

    public static Color Boost(Color c)
        => new Color(c.r * ColorBoost, c.g * ColorBoost, c.b * ColorBoost, c.a);

    // --- Aclarado del PNG gris del liquido (sin tocar el archivo) ---
    static readonly System.Collections.Generic.Dictionary<Sprite, Sprite> liquidosAclarados
        = new System.Collections.Generic.Dictionary<Sprite, Sprite>();
    static readonly System.Collections.Generic.HashSet<Sprite> yaAclarados
        = new System.Collections.Generic.HashSet<Sprite>();

    /// <summary>
    /// El PNG del liquido es GRIS OSCURO (la capa de tono/saturacion de
    /// Photoshop no se exporto). Multiplicado por rosa/naranja da colores
    /// apagados, y subir el boost los quema. Aqui convertimos el gris a
    /// GRIS CLARO (0.45..1.0) manteniendo el relieve de las olas y el alpha,
    /// para que el tint del jugador de el color exacto con boost 1.4.
    /// Se aplica UNA vez por sprite y se cachea.
    /// </summary>
    public static Sprite AclararLiquido(Sprite original)
    {
        if (original == null) return null;
        if (yaAclarados.Contains(original)) return original; // ya esta aclarado
        if (liquidosAclarados.TryGetValue(original, out var ya)) return ya;

        var tex = original.texture;
        // Leer pixels de la textura original (Unity permite GetPixels si es readable).
        Color[] px;
        try { px = tex.GetPixels(); }
        catch { return original; } // textura no readable: se usa tal cual

        float min = 1f, max = 0f;
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a < 0.02f) continue;
            float g = px[i].r;
            if (g < min) min = g;
            if (g > max) max = g;
        }
        float rango = Mathf.Max(0.01f, max - min);
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a < 0.02f) continue;
            float g = Mathf.Clamp01((px[i].r - min) / rango); // 0..1 normalizado
            g = Mathf.Lerp(0.45f, 1f, g);                      // gris claro
            px[i] = new Color(g, g, g, px[i].a);
        }
        var nueva = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        nueva.SetPixels(px);
        nueva.Apply();
        var sp = Sprite.Create(nueva, original.rect, original.pivot,
            original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        liquidosAclarados[original] = sp;
        yaAclarados.Add(sp);
        return sp;
    }

    /// <summary>
    /// Posicion del centro de la celda (x, y) en world space, centrado en origen.
    /// gridSize celdas por lado, y crece hacia arriba.
    /// </summary>
    public static Vector3 CellWorldPosition(int x, int y, int gridSize)
    {
        float half = (gridSize - 1) * 0.5f;
        return new Vector3((x - half) * Pitch, (y - half) * Pitch, 0f);
    }

    // Pack de respaldo si el casillero tiene un id que ya no existe.
    public const string BotPackId = "clasico";

    /// <summary>
    /// Pack que le corresponde a un DUEÑO de casilla (paso 2). Cada jugador
    /// tiene SU casillero en el perfil (nube): 1 = EquippedPackId,
    /// 2 = EquippedPackId2. Si el id no existe en el catalogo, cae al
    /// "clasico" (asi el valor viejo "caldero" no rompe nada).
    /// </summary>
    public static FxPack PackFor(int owner)
    {
        string id = owner == 2
            ? UGSProfileManager.Instance?.Profile.EquippedPackId2
            : owner == 1 ? UGSProfileManager.Instance?.Profile.EquippedPackId : null;
        return FxPackCatalog.Get(id) ?? FxPackCatalog.Get(BotPackId);
    }

    /// <summary>Pack del jugador humano (compatibilidad con llamadas viejas).</summary>
    public static FxPack Current => PackFor(1);
}

/// <summary>
/// Color del jugador. El enemigo NO se elige: sale opuesto automaticamente.
/// Modos: 0 = Rosa, 1 = Naranja, 2 = Personalizado (rueda de colores).
/// </summary>
public static class PlayerColorOptions
{
    public static readonly Color Rosa = new Color(1f, 0.15f, 0.6f);
    public static readonly Color Naranja = new Color(1f, 0.45f, 0.05f);

    public const int RosaIndex = 0;
    public const int NaranjaIndex = 1;
    public const int CustomIndex = 2;

    public static readonly string[] Names = { "Rosa", "Naranja", "Personalizado" };

    /// <summary>Color del jugador segun modo. En 2 usa el color de la rueda.</summary>
    public static Color Get(int mode, Color custom) => Mathf.Clamp(mode, 0, CustomIndex) switch
    {
        RosaIndex => Rosa,
        NaranjaIndex => Naranja,
        _ => custom,
    };

    /// <summary>
    /// Enemigo opuesto: matiz +180°. Con saturación y valor mínimos para que
    /// nunca quede lavado ni igual al jugador.
    /// </summary>
    public static Color OppositeOf(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        float hue = (h + 0.5f) % 1f;
        return Color.HSVToRGB(hue, Mathf.Max(s, 0.55f), Mathf.Max(v, 0.65f));
    }
}