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
/// </summary>
public static class BoardFx
{
    public static Action<Transform, int, string> LevelFx;
    public static Action<List<Vector2Int>, string> ExplosionFx;
    public static bool SuppressFallback = false;

    public static FxPack Current
    {
        get
        {
            var id = UGSProfileManager.Instance?.Profile.EquippedPackId;
            return FxPackCatalog.Get(id) ?? FxPackCatalog.Get("clasico");
        }
    }
}
