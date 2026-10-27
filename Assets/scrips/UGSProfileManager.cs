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
        public List<string> OwnedPackIds = new List<string> { "clasico" };
        public string EquippedPackId = "clasico";
        public int PlayerColorMode = 0;
        public Color PlayerCustomColor = new Color(1f, 0.15f, 0.6f);

        // --- Progreso básico del juego ---
        public int TotalWins = 0;
        public int TotalMatches = 0;
        public long Coins = 0;
        public long Gems = 0;
    }

    public PlayerProfile Profile = new PlayerProfile();

    // ---- Claves PlayerPrefs (guardado local) ----
    const string LOCAL_PROFILE_KEY = "LocalPlayerProfile_v1";
    const string LOCAL_TIMESTAMP_KEY = "LocalPlayerProfileTimestamp_v1";

    // ---- Eventos ----
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
                "PlayerColor", "PlayerCustomColor"
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
                { "OwnedPacks", JsonUtility.ToJson(new SkinList { skins = Profile.OwnedPackIds }) }
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
            PlayerPrefs.SetString(LOCAL_PROFILE_KEY, json);
            PlayerPrefs.SetString(LOCAL_TIMESTAMP_KEY, Profile.localTimestamp.ToString());
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
            if (!PlayerPrefs.HasKey(LOCAL_PROFILE_KEY)) return false;

            string json = PlayerPrefs.GetString(LOCAL_PROFILE_KEY);
            var loaded = JsonUtility.FromJson<PlayerProfile>(json);
            if (loaded == null) return false;

            Profile = loaded;

            if (PlayerPrefs.HasKey(LOCAL_TIMESTAMP_KEY))
                long.TryParse(PlayerPrefs.GetString(LOCAL_TIMESTAMP_KEY), out Profile.localTimestamp);

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
    }

    /// <summary>Guarda perfil en nube y local.</summary>
    public async Task SaveProfileAsync()
    {
        await SaveProfileToCloudAsync();
        SaveProfileToLocal();
    }

    public async Task RecordMatchResultAsync(bool won)
    {
        Profile.TotalMatches++;
        if (won) Profile.TotalWins++;
        await SaveProfileAsync();
    }

    public bool OwnsPack(string packId) => Profile.OwnedPackIds.Contains(packId);

    public async Task<bool> UnlockPackAsync(string packId)
    {
        if (FxPackCatalog.Get(packId) == null || OwnsPack(packId)) return false;
        Profile.OwnedPackIds.Add(packId);
        await SaveProfileAsync();
        return true;
    }

    public async Task<bool> EquipPackAsync(string packId)
    {
        if (FxPackCatalog.Get(packId) == null || !OwnsPack(packId)) return false;
        Profile.EquippedPackId = packId;
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
        => BoardFx.ApplyColors(Profile.PlayerColorMode, Profile.PlayerCustomColor);

    public async Task SetPlayerColorModeAsync(int mode)
    {
        Profile.PlayerColorMode = Mathf.Clamp(mode, 0, PlayerColorOptions.CustomIndex);
        ApplyColors();
        await SaveProfileAsync();
    }

    public async Task SetCustomColorAsync(Color c)
    {
        Profile.PlayerCustomColor = c;
        Profile.PlayerColorMode = PlayerColorOptions.CustomIndex;
        ApplyColors();
        await SaveProfileAsync();
    }

    [System.Serializable] private class SkinList { public List<string> skins; }
}
