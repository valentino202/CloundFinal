using System.Collections.Generic;

/// <summary>
/// Pack de animaciones SIN MonoBehaviour (solo datos).
/// Un pack = 4 llaves: anim de nivel 1, 2, 3 y explosion (4).
/// Las llaves son strings que TU defines (nombre de clip, prefab,
/// sprite, addressable...). Ej: pack "acuario" -> anim1..animBoom.
/// El jugador solo elige el pack; el tablero rutea cada llave a su nivel.
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

    public FxPack(string id, string displayName, string anim1, string anim2, string anim3, string animBoom)
    {
        this.id = id; this.displayName = displayName;
        this.anim1 = anim1; this.anim2 = anim2; this.anim3 = anim3; this.animBoom = animBoom;
    }

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
        new FxPack("clasico", "Clasico", "", "", "", ""),
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
}
