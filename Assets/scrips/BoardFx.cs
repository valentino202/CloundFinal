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
    /// Aplica el color del jugador. Modos clasicos: 0=Rosa, 1=Naranja y se
    /// respetan mutuamente (P1 Rosa -> P2 Naranja, P1 Naranja -> P2 Rosa).
    /// Solo el modo Personalizado (rueda) calcula al enemigo como opuesto HSV.
    /// </summary>
    public static void ApplyColors(int playerMode, Color custom)
    {
        ColP1 = PlayerColorOptions.Get(playerMode, custom);

        if (playerMode == PlayerColorOptions.RosaIndex)
            ColP2 = PlayerColorOptions.Naranja;
        else if (playerMode == PlayerColorOptions.NaranjaIndex)
            ColP2 = PlayerColorOptions.Rosa;
        else
            ColP2 = PlayerColorOptions.OppositeOf(ColP1);
    }

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
    /// Multiplicador de brillo. Los sprites del atlas estan desaturados y su
    /// brillo maximo es ~0.74, asi que el tint normal queda lavado. Con boost
    /// el pixel mas brillante llega a 1.0 y el color se ve vivo.
    /// </summary>
    public static float ColorBoost = 1.4f;

    public static Color Boost(Color c)
        => new Color(c.r * ColorBoost, c.g * ColorBoost, c.b * ColorBoost, c.a);

    /// <summary>
    /// Posicion del centro de la celda (x, y) en world space, centrado en origen.
    /// gridSize celdas por lado, y crece hacia arriba.
    /// </summary>
    public static Vector3 CellWorldPosition(int x, int y, int gridSize)
    {
        float half = (gridSize - 1) * 0.5f;
        return new Vector3((x - half) * Pitch, (y - half) * Pitch, 0f);
    }

    public static FxPack Current
    {
        get
        {
            var id = UGSProfileManager.Instance?.Profile.EquippedPackId;
            return FxPackCatalog.Get(id) ?? FxPackCatalog.Get("clasico");
        }
    }
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