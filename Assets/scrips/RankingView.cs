using System;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

/// <summary>
/// Muestra un ranking (Normal o Infinito) instanciando el prefab de fila
/// (Player_Normal / Player_Infinito) dentro de un contenedor.
/// Adjuntar al panel del ranking y asignar prefab + contenedor en el Inspector.
/// </summary>
public class RankingView : MonoBehaviour
{
    public enum RankingType { Normal, Infinito }

    [Header("Tipo de ranking")]
    public RankingType type = RankingType.Normal;

    [Header("UI")]
    [Tooltip("Prefab de fila (Player_Normal o Player_Infinito).")]
    public GameObject rowPrefab;
    [Tooltip("Contenedor (Content de un ScrollRect) donde se instancian las filas.")]
    public Transform content;
    [Tooltip("Cantidad de entradas a mostrar.")]
    public int limit = 20;

    [Header("Leaderboards")]
    public string normalLeaderboardId = "ranked_normal";
    public string infinitoLeaderboardId = "ranked_infinito";

    [Serializable]
    public class NormalRankingMeta
    {
        public int total;
        public int wins30;
        public int wins40;
        public int wins50;
        public int wins60;
        public int wins70;
        public int wins80;
        public int wins90;
        public int wins100;
    }

    [Serializable]
    public class InfinitoRankingMeta
    {
        public int wins;
        public float bestTime;
        public int nearBest;
    }

    private void OnEnable()
    {
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {

        if (content.GetComponent<VerticalLayoutGroup>() == null)
        {
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
        }
        if (content.GetComponent<ContentSizeFitter>() == null)
        {
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        if (rowPrefab == null || content == null) return;
        ClearRows();
        if (!await EnsureServicesAsync()) return;

        try
        {
            if (type == RankingType.Normal)
                await LoadNormalAsync();
            else
                await LoadInfinitoAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Ranking] No se pudo cargar el ranking: {ex.Message}");
        }
    }

    private async Task<bool> EnsureServicesAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Ranking] UGS no inicializado: {ex.Message}");
            return false;
        }

        // El paquete de Leaderboards a veces no asigna su singleton automaticamente
        // cuando se usa un servicio por instancia. Lo inicializamos por reflexion.
        if (GetLeaderboardsInstance() == null)
        {
            try
            {
                Debug.LogWarning("[Ranking] State=" + UnityServices.State + " auth=" + (AuthenticationService.Instance != null));
                var asm = typeof(LeaderboardsService).Assembly;
                var initType = asm.GetType("Unity.Services.Leaderboards.LeaderboardsInitializer");
                var coreRegistry = GetCoreRegistryInstance();
                Debug.LogWarning("[Ranking] initType=" + (initType != null) + " coreRegistry=" + (coreRegistry != null) + " assemblies=" + AppDomain.CurrentDomain.GetAssemblies().Length);
                if (initType == null) Debug.LogWarning("[Ranking] LeaderboardsInitializer no encontrado.");
                if (coreRegistry == null) Debug.LogWarning("[Ranking] CoreRegistry.Instance es null.");
                if (initType != null && coreRegistry != null)
                {
                    var instance = Activator.CreateInstance(initType, true);
                    var method = initType.GetMethod("InitializeService", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (method == null) Debug.LogWarning("[Ranking] InitializeService no encontrado.");
                    if (method != null)
                    {
                        var service = method.Invoke(instance, new object[] { coreRegistry });
                        var field = typeof(LeaderboardsService).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (field != null) field.SetValue(null, service);
                        Debug.LogWarning("[Ranking] Leaderboards inicializado por reflexion: " + (service != null));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Ranking] No se pudo inicializar Leaderboards por reflexion: {ex.Message}");
            }
        }

        if (GetLeaderboardsInstance() == null)
        {
            Debug.LogWarning("[Ranking] The Leaderboards service has not been initialized. Please initialize Unity Services.");
            return false;
        }

        if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Ranking] Se requiere sesion para leer el leaderboard.");
            return false;
        }
        return true;
    }

    private static object GetCoreRegistryInstance()
    {
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("Unity.Services.Core.Internal.CoreRegistry");
                if (t == null) continue;
                var prop = t.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                {
                    var value = prop.GetValue(null);
                    if (value != null) return value;
                }
            }
        }
        catch { }
        return null;
    }
    private static ILeaderboardsService GetLeaderboardsInstance()
    {
        try
        {
            var field = typeof(LeaderboardsService).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            return field != null ? field.GetValue(null) as ILeaderboardsService : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task LoadNormalAsync()
    {
        var page = await LeaderboardsService.Instance.GetScoresAsync(
            normalLeaderboardId,
            new GetScoresOptions { Offset = 0, Limit = limit, IncludeMetadata = true });

        if (page == null)
        {
            Debug.LogWarning($"[Ranking] Respuesta vacía para {normalLeaderboardId}.");
            return;
        }
        if (page.Results == null)
        {
            Debug.LogWarning($"[Ranking] Results es null para {normalLeaderboardId}.");
            return;
        }
        Debug.Log($"[Ranking] {normalLeaderboardId}: {page.Results.Count} entradas recibidas.");

        foreach (var entry in page.Results)
        {
            var row = Instantiate(rowPrefab, content);
            SetText(row, "name", string.IsNullOrEmpty(entry.PlayerName) ? entry.PlayerId : entry.PlayerName);

            NormalRankingMeta meta = null;
            if (!string.IsNullOrEmpty(entry.Metadata))
            {
                try { meta = JsonUtility.FromJson<NormalRankingMeta>(entry.Metadata); }
                catch { meta = null; }
            }

            int total = meta != null ? meta.total : Mathf.RoundToInt((float)entry.Score);
            SetText(row, "Numero_victorias", total.ToString());

            for (int t = 30; t <= 100; t += 10)
            {
                int v = meta != null ? GetNormalWins(meta, t) : 0;
                SetText(row, "puntaje_" + t, v.ToString("00"));
            }
        }
    }

    private async Task LoadInfinitoAsync()
    {
        var page = await LeaderboardsService.Instance.GetScoresAsync(
            infinitoLeaderboardId,
            new GetScoresOptions { Offset = 0, Limit = limit, IncludeMetadata = true });

        if (page == null)
        {
            Debug.LogWarning($"[Ranking] Respuesta vacía para {infinitoLeaderboardId}.");
            return;
        }
        if (page.Results == null)
        {
            Debug.LogWarning($"[Ranking] Results es null para {infinitoLeaderboardId}.");
            return;
        }
        Debug.Log($"[Ranking] {infinitoLeaderboardId}: {page.Results.Count} entradas recibidas.");

        foreach (var entry in page.Results)
        {
            var row = Instantiate(rowPrefab, content);
            SetText(row, "name", string.IsNullOrEmpty(entry.PlayerName) ? entry.PlayerId : entry.PlayerName);

            InfinitoRankingMeta meta = null;
            if (!string.IsNullOrEmpty(entry.Metadata))
            {
                try { meta = JsonUtility.FromJson<InfinitoRankingMeta>(entry.Metadata); }
                catch { meta = null; }
            }

            int wins = meta != null ? meta.wins : 0;
            float bestTime = meta != null ? meta.bestTime : 0f;
            int near = meta != null ? meta.nearBest : 0;

            SetText(row, "victorias", wins.ToString());
            SetText(row, "mejor tiempo", FormatTime(bestTime));
            SetText(row, "numero_mrjores_tiempos", near.ToString());
        }
    }

    private int GetNormalWins(NormalRankingMeta meta, int turns)
    {
        if (meta == null) return 0;
        switch (turns)
        {
            case 30: return meta.wins30;
            case 40: return meta.wins40;
            case 50: return meta.wins50;
            case 60: return meta.wins60;
            case 70: return meta.wins70;
            case 80: return meta.wins80;
            case 90: return meta.wins90;
            case 100: return meta.wins100;
            default: return 0;
        }
    }

    private void ClearRows()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private void SetText(GameObject row, string childName, string value)
    {
        if (row == null) return;
        var child = row.transform.Find(childName);
        if (child == null) return;
        var tmp = child.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = value;
    }

    private string FormatTime(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return $"{s / 60}:{s % 60:D2}";
    }
}
