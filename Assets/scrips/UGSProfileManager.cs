using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

/// <summary>
/// Agente 3: Perfil, progreso y dificultad adaptativa.
/// Guardado en Cloud Save (nube) + PlayerPrefs (local) + resolución de conflictos.
///
/// Uso:
///   await UGSProfileManager.Instance.LoadProfileAsync();   // nube primero, local de respaldo, conflicto si difieren
///   await UGSProfileManager.Instance.SaveProfileAsync();   // guarda en nube Y local
///   Suscribite a OnSaveConflictDetected para pedirle al usuario qué versión conserva.
/// </summary>
public class UGSProfileManager
{
    public static readonly UGSProfileManager Instance = new UGSProfileManager();

    [System.Serializable]
    public class PlayerProfile
    {
        public int saveVersion = 1;
        public long cloudTimestamp = 0;   // ticks cuando se guardo en la nube
        public long localTimestamp = 0;   // ticks cuando se guardo localmente

        // --- Configuración de usuario ---
        public string PlayerName = "";
        public List<string> SkinsOwned = new List<string>();
        public string EquippedSkin = "default";
        // --- Casillero del JUGADOR 1 (campos de siempre) ---
        public List<string> OwnedPackIds = new List<string> { "clasico" };
        public string EquippedPackId = "clasico";
        public int PlayerColorMode = 0;
        public Color PlayerCustomColor = new Color(1f, 0.15f, 0.6f);

        // --- Casillero del JUGADOR 2 (independiente del 1) ---
        // El pack guarda su propio sonido y burbujas (FxPack), asi que aca
        // solo vive QUÉ pack y QUÉ color eligio este jugador.
        public List<string> OwnedPackIds2 = new List<string> { "clasico" };
        public string EquippedPackId2 = "clasico";
        // -1 = AUTOMATICO: mientras el jugador 2 no elija su color, se
        // diferencia solo del jugador 1 (la regla de siempre). Al elegir
        // uno explicitamente, queda fijo e independiente.
        public int PlayerColorMode2 = -1;
        public Color PlayerCustomColor2 = PlayerColorOptions.Naranja;
        // Marca: false = color de P2 automatico (aunque haya un valor
        // viejo guardado en la nube). true = el jugador 2 eligio color.
        public bool ColorJugador2Elegido = false;

        // --- Progreso básico del juego ---
        public int TotalWins = 0;
        public int TotalMatches = 0;
        public long Coins = 0;
        public long Gems = 0;
    }

    public PlayerProfile Profile = new PlayerProfile();

    /// <summary>
    /// true cuando la carga del perfil (nube/local) termino. La UI usa
    /// esta marca para NO mostrar una seleccion provisional (ej: el
    /// color por defecto) antes de que lleguen los datos guardados.
    /// </summary>
    public static bool PerfilListo;

    // ---- Claves PlayerPrefs (guardado local, POR JUGADOR para no mezclar cuentas) ----
    string LocalProfileKey => "LocalPlayerProfile_v1_" + PlayerIdForLocal();
    string LocalTimestampKey => "LocalPlayerProfileTimestamp_v1_" + PlayerIdForLocal();

    static string PlayerIdForLocal()
    {
        try
        {
            return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn
                ? AuthenticationService.Instance.PlayerId
                : "";
        }
        catch { return ""; }
    }    // ---- Eventos ----
    public static event Action<SaveConflictInfo> OnSaveConflictDetected;
    public static event Action OnProfileLoaded;
    public static event Action OnProfileSaved;

    public class SaveConflictInfo
    {
        public PlayerProfile cloudProfile;
        public PlayerProfile localProfile;
        public bool cloudIsNewer;
        public bool localIsNewer;
        public bool cloudExists;
        public bool localExists;
    }

    private UGSProfileManager() { }

    public float WinRate => Profile.TotalMatches == 0 ? 0f : (float)Profile.TotalWins / Profile.TotalMatches;

    public BotAgent.Difficulty SuggestedDifficulty
    {
        get
        {
            if (Profile.TotalMatches < 3) return BotAgent.Difficulty.Medio;
            if (WinRate >= 0.65f) return BotAgent.Difficulty.Dificil;
            if (WinRate <= 0.35f) return BotAgent.Difficulty.Facil;
            return BotAgent.Difficulty.Medio;
        }
    }

    private bool CanUseCloudSave()
        => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn &&
           CloudSaveService.Instance != null;

    // =========================================================================
    // 1. NUBE (Cloud Save SDK)
    // =========================================================================

    /// <summary>Carga el perfil desde Cloud Save. Retorna true si encontró datos.</summary>
    public async Task<bool> LoadProfileFromCloudAsync()
    {
        if (!CanUseCloudSave())
        {
            Debug.LogWarning("[Profile] No se puede cargar de la nube sin sesión o CloudSave.");
            return false;
        }

        try
        {
            var keys = new HashSet<string>
            {
                "saveVersion", "cloudTimestamp",
                "PlayerName", "SkinsOwned", "EquippedSkin", "TotalWins", "TotalMatches",
                "OwnedPacks", "EquippedPack", "Coins", "Gems",
                "PlayerColor", "PlayerCustomColor",
                // Casillero del jugador 2
                "OwnedPacks2", "EquippedPack2", "PlayerColor2", "PlayerCustomColor2",
                "Color2Elegido"
            };

            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data == null || data.Count == 0)
                return false;

            bool loaded = false;

            if (data.TryGetValue("saveVersion", out var v)) Profile.saveVersion = v.Value.GetAs<int>();
            if (data.TryGetValue("cloudTimestamp", out var ts)) Profile.cloudTimestamp = ts.Value.GetAs<long>();
            if (data.TryGetValue("PlayerName", out var n)) { Profile.PlayerName = n.Value.GetAs<string>(); loaded = true; }
            if (data.TryGetValue("EquippedSkin", out var s)) Profile.EquippedSkin = s.Value.GetAs<string>();
            if (data.TryGetValue("TotalWins", out var w)) Profile.TotalWins = w.Value.GetAs<int>();
            if (data.TryGetValue("TotalMatches", out var m)) Profile.TotalMatches = m.Value.GetAs<int>();
            if (data.TryGetValue("Coins", out var c)) Profile.Coins = c.Value.GetAs<long>();
            if (data.TryGetValue("Gems", out var g)) Profile.Gems = g.Value.GetAs<long>();
            if (data.TryGetValue("PlayerColor", out var pc)) Profile.PlayerColorMode = pc.Value.GetAs<int>();
            if (data.TryGetValue("PlayerCustomColor", out var pcc))
            {
                string csv = pcc.Value.GetAs<string>();
                if (!string.IsNullOrEmpty(csv))
                {
                    var parts = csv.Split(',');
                    if (parts.Length == 3 &&
                        int.TryParse(parts[0], out int rr) && int.TryParse(parts[1], out int gg) && int.TryParse(parts[2], out int bb))
                        Profile.PlayerCustomColor = new Color(rr / 255f, gg / 255f, bb / 255f);
                }
            }
            // --- Casillero del jugador 2 ---
            if (data.TryGetValue("PlayerColor2", out var pc2)) Profile.PlayerColorMode2 = pc2.Value.GetAs<int>();
            if (data.TryGetValue("Color2Elegido", out var c2e)) Profile.ColorJugador2Elegido = c2e.Value.GetAs<bool>();
            if (data.TryGetValue("PlayerCustomColor2", out var pcc2))
            {
                string csv = pcc2.Value.GetAs<string>();
                if (!string.IsNullOrEmpty(csv))
                {
                    var parts = csv.Split(',');
                    if (parts.Length == 3 &&
                        int.TryParse(parts[0], out int rr) && int.TryParse(parts[1], out int gg) && int.TryParse(parts[2], out int bb))
                        Profile.PlayerCustomColor2 = new Color(rr / 255f, gg / 255f, bb / 255f);
                }
            }
            if (data.TryGetValue("SkinsOwned", out var sk))
            {
                string skinsJson = sk.Value.GetAs<string>();
                if (!string.IsNullOrEmpty(skinsJson))
                    Profile.SkinsOwned = JsonUtility.FromJson<SkinList>(skinsJson)?.skins ?? new List<string>();
            }
            if (data.TryGetValue("OwnedPacks", out var op))
            {
                string packsJson = op.Value.GetAs<string>();
                if (!string.IsNullOrEmpty(packsJson))
                    Profile.OwnedPackIds = JsonUtility.FromJson<SkinList>(packsJson)?.skins ?? new List<string>();
            }
            if (Profile.OwnedPackIds.Count == 0) Profile.OwnedPackIds = new List<string> { "clasico" };
            if (data.TryGetValue("EquippedPack", out var ep) && !string.IsNullOrEmpty(ep.Value.GetAs<string>()))
                Profile.EquippedPackId = ep.Value.GetAs<string>();
            // Casillero del jugador 2
            if (data.TryGetValue("OwnedPacks2", out var op2))
            {
                string packs2Json = op2.Value.GetAs<string>();
                if (!string.IsNullOrEmpty(packs2Json))
                    Profile.OwnedPackIds2 = JsonUtility.FromJson<SkinList>(packs2Json)?.skins ?? new List<string>();
            }
            if (Profile.OwnedPackIds2.Count == 0) Profile.OwnedPackIds2 = new List<string> { "clasico" };
            if (data.TryGetValue("EquippedPack2", out var ep2) && !string.IsNullOrEmpty(ep2.Value.GetAs<string>()))
                Profile.EquippedPackId2 = ep2.Value.GetAs<string>();

            ApplyColors();

            Debug.Log($"[Profile] Cargado desde nube. WinRate={WinRate:P0}");
            OnProfileLoaded?.Invoke();
            return loaded;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Profile] Error load cloud: {e.Message}");
            return false;
        }
    }

    /// <summary>Guarda el perfil actual en Cloud Save.</summary>
    public async Task<bool> SaveProfileToCloudAsync()
    {
        if (!CanUseCloudSave())
        {
            Debug.LogWarning("[Profile] No se puede guardar en la nube sin sesión o CloudSave.");
            return false;
        }

        try
        {
            Profile.cloudTimestamp = DateTime.UtcNow.Ticks;

            var data = new Dictionary<string, object>
            {
                { "saveVersion", Profile.saveVersion },
                { "cloudTimestamp", Profile.cloudTimestamp },
                { "PlayerName", Profile.PlayerName },
                { "EquippedSkin", Profile.EquippedSkin },
                { "TotalWins", Profile.TotalWins },
                { "TotalMatches", Profile.TotalMatches },
                { "EquippedPack", Profile.EquippedPackId },
                { "Coins", Profile.Coins },
                { "Gems", Profile.Gems },
                { "PlayerColor", Profile.PlayerColorMode },
                { "PlayerCustomColor", $"{Mathf.RoundToInt(Profile.PlayerCustomColor.r * 255)},{Mathf.RoundToInt(Profile.PlayerCustomColor.g * 255)},{Mathf.RoundToInt(Profile.PlayerCustomColor.b * 255)}" },
                { "SkinsOwned", JsonUtility.ToJson(new SkinList { skins = Profile.SkinsOwned }) },
                { "OwnedPacks", JsonUtility.ToJson(new SkinList { skins = Profile.OwnedPackIds }) },
                // Casillero del jugador 2
                { "OwnedPacks2", JsonUtility.ToJson(new SkinList { skins = Profile.OwnedPackIds2 }) },
                { "EquippedPack2", Profile.EquippedPackId2 },
                { "PlayerColor2", Profile.PlayerColorMode2 },
                { "Color2Elegido", Profile.ColorJugador2Elegido },
                { "PlayerCustomColor2", $"{Mathf.RoundToInt(Profile.PlayerCustomColor2.r * 255)},{Mathf.RoundToInt(Profile.PlayerCustomColor2.g * 255)},{Mathf.RoundToInt(Profile.PlayerCustomColor2.b * 255)}" }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("[Profile] Guardado en nube OK.");
            OnProfileSaved?.Invoke();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Profile] Error save cloud: {e.Message}");
            return false;
        }
    }

    // =========================================================================
    // 2. LOCAL (PlayerPrefs)
    // =========================================================================

    /// <summary>Guarda el perfil actual en PlayerPrefs (local).</summary>
    public void SaveProfileToLocal()
    {
        try
        {
            Profile.localTimestamp = DateTime.UtcNow.Ticks;
            string json = JsonUtility.ToJson(Profile);
            PlayerPrefs.SetString(LocalProfileKey, json);
            PlayerPrefs.SetString(LocalTimestampKey, Profile.localTimestamp.ToString());
            PlayerPrefs.Save();
            Debug.Log("[Profile] Guardado local OK.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Profile] Error save local: {e.Message}");
        }
    }

    /// <summary>Carga el perfil desde PlayerPrefs (local). Retorna true si encontró datos.</summary>
    public bool LoadProfileFromLocal()
    {
        try
        {
            if (!PlayerPrefs.HasKey(LocalProfileKey)) return false;

            string json = PlayerPrefs.GetString(LocalProfileKey);
            var loaded = JsonUtility.FromJson<PlayerProfile>(json);
            if (loaded == null) return false;

            Profile = loaded;

            if (PlayerPrefs.HasKey(LocalTimestampKey))
                long.TryParse(PlayerPrefs.GetString(LocalTimestampKey), out Profile.localTimestamp);

            ApplyColors();
            Debug.Log("[Profile] Cargado desde local OK.");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Profile] Error load local: {e.Message}");
            return false;
        }
    }

    // =========================================================================
    // 3. CONFLICTOS NUBE vs LOCAL
    // =========================================================================

    /// <summary>Compara versiones y timestamps para detectar conflicto.</summary>
    public SaveConflictInfo CheckConflict()
    {
        var info = new SaveConflictInfo
        {
            cloudExists = Profile.cloudTimestamp > 0,
            localExists = Profile.localTimestamp > 0,
            cloudProfile = new PlayerProfile { saveVersion = Profile.saveVersion, cloudTimestamp = Profile.cloudTimestamp },
            localProfile = new PlayerProfile { saveVersion = Profile.saveVersion, localTimestamp = Profile.localTimestamp }
        };

        if (!info.cloudExists && !info.localExists)
        {
            info.cloudIsNewer = false;
            info.localIsNewer = false;
            return info;
        }

        if (!info.cloudExists) { info.localIsNewer = true; return info; }
        if (!info.localExists) { info.cloudIsNewer = true; return info; }

        info.cloudIsNewer = Profile.cloudTimestamp > Profile.localTimestamp ||
                            (Profile.cloudTimestamp == Profile.localTimestamp && Profile.saveVersion > info.localProfile.saveVersion);
        info.localIsNewer = Profile.localTimestamp > Profile.cloudTimestamp;

        return info;
    }

    /// <summary>Resuelve el conflicto: keepCloud=true conserva la nube, false conserva lo local.</summary>
    public async Task<bool> ResolveConflictAsync(bool keepCloud)
    {
        var conflict = CheckConflict();

        if (keepCloud)
        {
            if (!conflict.cloudExists) return false;
            bool ok = await LoadProfileFromCloudAsync();
            if (ok) SaveProfileToLocal(); // sincroniza local con la nube elegida
            return ok;
        }
        else
        {
            if (!conflict.localExists) return false;
            bool ok = LoadProfileFromLocal();
            if (ok) await SaveProfileToCloudAsync(); // sube la versión local a la nube
            return ok;
        }
    }

    // =========================================================================
    // 4. API UNIFICADA
    // =========================================================================

    /// <summary>
    /// Carga perfil: intenta nube primero, si falla usa local.
    /// Si ambos existen y difieren, dispara OnSaveConflictDetected y espera decisión de la UI.
    /// </summary>
    public async Task LoadProfileAsync()
    {
        // Limpia el perfil en memoria: cada cuenta empieza desde cero y solo
        // hereda datos de SUS claves (nube por jugador + local por jugador).
        PerfilListo = false;
        Profile = new PlayerProfile();

        bool cloudOk = await LoadProfileFromCloudAsync();
        bool localOk = LoadProfileFromLocal();

        if (cloudOk && localOk)
        {
            var conflict = CheckConflict();
            if (conflict.cloudIsNewer || conflict.localIsNewer)
            {
                OnSaveConflictDetected?.Invoke(conflict);

                // Por defecto (sin decisión de UI): se queda con la más reciente.
                if (conflict.cloudIsNewer)
                    await LoadProfileFromCloudAsync();
                else
                    LoadProfileFromLocal();
            }
        }
        else if (!cloudOk && localOk)
        {
            // Solo hay local, ya se cargó en LoadProfileFromLocal
        }
        // si ninguno existe, Profile queda con valores por defecto
        PerfilListo = true;
        OnProfileLoaded?.Invoke(); // avisa a la UI que ya puede mostrar la seleccion
    }

    /// <summary>Guarda perfil en nube y local.
    /// CON DEBOUNCE: si llegan muchos cambios seguidos (ej: arrastrar la
    /// rueda de colores dispara SetCustomColorAsync por cada frame), se
    /// agrupan en UN solo guardado a la nube. Sin esto UGS responde
    /// request_rate_limited. El guardado LOCAL es inmediato (no tiene limite).
    /// </summary>
    const int saveDebounceMs = 1500;    // tiempo que espera para agrupar cambios
    bool guardadoNubeEnMarcha;          // hay un ciclo de guardado corriendo
    bool cambiosPendientes;             // llegaron cambios mientras guardaba

    public async Task SaveProfileAsync()
    {
        SaveProfileToLocal(); // local: instantaneo y sin limite de ritmo

        if (guardadoNubeEnMarcha) { cambiosPendientes = true; return; }
        guardadoNubeEnMarcha = true;
        try
        {
            do
            {
                cambiosPendientes = false;
                // Espera el debounce: si mientras esperan siguen llegando
                // cambios, la espera se reinicia (la rafaga se agrupa).
                await Task.Delay(saveDebounceMs);
                await SaveProfileToCloudAsync();
            }
            while (cambiosPendientes);
        }
        finally { guardadoNubeEnMarcha = false; }
    }

    public async Task RecordMatchResultAsync(bool won)
    {
        Profile.TotalMatches++;
        if (won) Profile.TotalWins++;
        await SaveProfileAsync();
    }

    /// <summary>Limpia el perfil en memoria (al cerrar sesion) para que la proxima cuenta empiece desde sus propios datos.</summary>
    public void ResetProfile()
    {
        PerfilListo = false;
        Profile = new PlayerProfile();
        ApplyColors();
    }

    // --- API por jugador: 1 = casillero del humano, 2 = casillero del rival ---
    // Los defaults (jugador = 1) conservan TODAS las llamadas viejas del menu.
    List<string> PacksDelJugador(int jugador)
        => jugador == 2 ? Profile.OwnedPackIds2 : Profile.OwnedPackIds;

    public bool OwnsPack(string packId, int jugador = 1) => PacksDelJugador(jugador).Contains(packId);

    public async Task<bool> UnlockPackAsync(string packId, int jugador = 1)
    {
        var lista = PacksDelJugador(jugador);
        if (FxPackCatalog.Get(packId) == null || lista.Contains(packId)) return false;
        lista.Add(packId);
        await SaveProfileAsync();
        return true;
    }

    public async Task<bool> EquipPackAsync(string packId, int jugador = 1)
    {
        if (FxPackCatalog.Get(packId) == null || !OwnsPack(packId, jugador)) return false;
        if (jugador == 2) Profile.EquippedPackId2 = packId;
        else Profile.EquippedPackId = packId;
        await SaveProfileAsync();
        return true;
    }

    public async Task GrantCurrencyAsync(long coins, long gems)
    {
        Profile.Coins = System.Math.Max(0, Profile.Coins + coins);
        Profile.Gems = System.Math.Max(0, Profile.Gems + gems);
        await SaveProfileAsync();
    }

    public async Task<bool> SpendCurrencyAsync(long coins, long gems)
    {
        if (Profile.Coins < coins || Profile.Gems < gems) return false;
        Profile.Coins -= coins;
        Profile.Gems -= gems;
        await SaveProfileAsync();
        return true;
    }

    public void ApplyColors()
        => BoardFx.ApplyColors(Profile.PlayerColorMode, Profile.PlayerCustomColor,
                               Profile.ColorJugador2Elegido ? Profile.PlayerColorMode2 : -1,
                               Profile.PlayerCustomColor2);

    public async Task SetPlayerColorModeAsync(int mode, int jugador = 1)
    {
        mode = Mathf.Clamp(mode, 0, PlayerColorOptions.CustomIndex);
        if (jugador == 2)
        {
            Profile.PlayerColorMode2 = mode;
            Profile.ColorJugador2Elegido = true; // deja de ser automatico
        }
        else Profile.PlayerColorMode = mode;
        ApplyColors();
        await SaveProfileAsync();
    }

    public async Task SetCustomColorAsync(Color c, int jugador = 1)
    {
        if (jugador == 2)
        {
            Profile.PlayerCustomColor2 = c;
            Profile.PlayerColorMode2 = PlayerColorOptions.CustomIndex;
            Profile.ColorJugador2Elegido = true; // deja de ser automatico
        }
        else
        {
            Profile.PlayerCustomColor = c;
            Profile.PlayerColorMode = PlayerColorOptions.CustomIndex;
        }
        ApplyColors();
        await SaveProfileAsync();
    }

    [System.Serializable] private class SkinList { public List<string> skins; }
}
