using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pack de animaciones SIN MonoBehaviour (solo datos).
/// Un pack = 4 llaves: anim de nivel 1, 2, 3 y explosion (4).
/// Las llaves son strings que TU defines (nombre de clip, prefab,
/// sprite, addressable...). Ej: pack "acuario" -> anim1..animBoom.
/// El jugador solo elige el pack; el tablero rutea cada llave a su nivel.
///
/// Ademas cada pack puede llevar las referencias reales de arte (glassPrefab
/// + los 4 AnimationClip). CellVisuals inyecta esas refs al arrancar, asi que
/// el catalogo de abajo solo define el esqueleto: id, nombre y llaves.
/// </summary>
[System.Serializable]
public class FxPack
{
    public string id;
    public string displayName;
    public string anim1;
    public string anim2;
    public string anim3;
    public string animBoom;

    // --- Arte real del pack (lo asigna CellVisuals desde el Inspector) ---
    /// <summary>Cristal/vaso que envuelve al liquido. NUNCA se tinta: va en gris.</summary>
    public GameObject glassPrefab;
    public AnimationClip clip1;
    public AnimationClip clip2;
    public AnimationClip clip3;
    public AnimationClip clipBoom;

    // Velocidad de reproduccion por nivel. Menos de 1 = mas lento (mas legible).
    public float speed1 = 1f;
    public float speed2 = 1f;
    public float speed3 = 1f;
    public float speedBoom = 1f;

    public FxPack(string id, string displayName, string anim1, string anim2, string anim3, string animBoom)
    {
        this.id = id; this.displayName = displayName;
        this.anim1 = anim1; this.anim2 = anim2; this.anim3 = anim3; this.animBoom = animBoom;
    }

    /// <summary>Clip real para un nivel (1-3). 4 = boom. Null si el pack no lo trae.</summary>
    public AnimationClip ClipForLevel(int level) => level switch
    {
        1 => clip1,
        2 => clip2,
        3 => clip3,
        _ => clipBoom,
    };

    /// <summary>
    /// Las animaciones son de UNA PASADA: al terminar quedan congeladas en el
    /// ultimo frame (WrapMode.ClampForever), que es justo el estado "lleno"
    /// de la casilla. Nunca repiten.
    /// </summary>
    public static bool LoopsForLevel(int level) => false;

    /// <summary>Velocidad de reproduccion para un nivel.</summary>
    public float SpeedForLevel(int level) => level switch
    {
        1 => speed1,
        2 => speed2,
        3 => speed3,
        _ => speedBoom,
    };

    public bool HasArt =>
        glassPrefab != null && clip1 != null && clip2 != null && clip3 != null && clipBoom != null;

    public string KeyForLevel(int level) => level switch
    {
        1 => anim1,
        2 => anim2,
        3 => anim3,
        _ => animBoom, // 4 = explosion
    };
}

/// <summary>
/// Catalogo de packs. El "clasico" trae llaves vacias = look por codigo.
/// Agrega los tuyos AQUI una sola vez (ej. acuario) o con RegisterPack
/// desde tu propio loader. El jugador los cambia sin tocar codigo.
/// </summary>
public static class FxPackCatalog
{
    private static readonly List<FxPack> packs = new List<FxPack>
    {
        new FxPack("clasico", "Clasico", "clasico_1", "clasico_2", "clasico_3", "clasico_boom"),
        // Ejemplo para cuando hagas tus animaciones:
        // new FxPack("acuario", "Acuario", "acuario_1", "acuario_2", "acuario_3", "acuario_boom"),
    };

    public static void RegisterPack(FxPack pack)
    {
        if (pack == null || string.IsNullOrEmpty(pack.id)) return;
        packs.RemoveAll(p => p.id == pack.id);
        packs.Add(pack);
    }

    public static FxPack Get(string id) => packs.Find(p => p.id == id);
    public static List<FxPack> All() => new List<FxPack>(packs);

    /// <summary>
    /// Adjunta el arte real a un pack existente sin pisar id/nombre/llaves.
    /// Lo llama CellVisuals en Awake con lo que hay en su Inspector.
    /// </summary>
    public static void AttachArt(string packId, GameObject glass, AnimationClip c1, AnimationClip c2, AnimationClip c3, AnimationClip cBoom, float s1, float s2, float s3, float sBoom)
    {
        var pack = Get(packId);
        if (pack == null) return;
        if (glass != null) pack.glassPrefab = glass;
        if (c1 != null) pack.clip1 = c1;
        if (c2 != null) pack.clip2 = c2;
        if (c3 != null) pack.clip3 = c3;
        if (cBoom != null) pack.clipBoom = cBoom;
        pack.speed1 = s1;
        pack.speed2 = s2;
        pack.speed3 = s3;
        pack.speedBoom = sBoom;
    }
}