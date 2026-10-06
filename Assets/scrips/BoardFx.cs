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
    public static readonly Color ColNeutral = new Color(0.12f, 0.16f, 0.28f); // azul noche
    public static readonly Color ColP1 = new Color(1f, 0.15f, 0.6f);   // rosado neon
    public static readonly Color ColP2 = new Color(1f, 0.45f, 0.05f);  // naranja electrico
    public static readonly Color ColBoom = new Color(1f, 0.85f, 0.3f);  // destello explosion

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