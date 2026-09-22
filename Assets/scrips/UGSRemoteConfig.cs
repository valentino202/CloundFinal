using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.RemoteConfig;

/// <summary>
/// Configuracion remota SIN MonoBehaviour (clase estatica pura).
/// Uso: UGSRemoteConfig.gridSize / await UGSRemoteConfig.FetchAsync();
/// IMPORTANTE: borra el viejo componente UGSRemoteConfigManager de la escena
/// (quedara como Missing Script tras este cambio).
/// </summary>
public static class UGSRemoteConfig
{
    public static int gridSize = 6;
    public static int maxCellLevel = 4;
    public static int botDifficulty = 1; // 0=Facil 1=Medio 2=Dificil
    public static int maxTurns = 30;

    public static event Action Applied;

    // Estructuras requeridas por Remote Config
    public struct UserAttributes { }
    public struct AppAttributes { }

    // Llamar justo despues de iniciar sesion con Authentication.
    public static async Task FetchAsync()
    {
        if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[RemoteConfig] Se requiere iniciar sesión antes de consultar la nube.");
            return;
        }

        if (RemoteConfigService.Instance == null)
        {
            Debug.LogError("[RemoteConfig] RemoteConfigService no disponible.");
            return;
        }

        try
        {
            Debug.Log("[RemoteConfig] Solicitando configuración remota...");
            RemoteConfigService.Instance.FetchCompleted -= ApplySettings;
            RemoteConfigService.Instance.FetchCompleted += ApplySettings;
            await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
        }
        catch (Exception e)
        {
            Debug.LogError($"[RemoteConfig] Error al consultar datos: {e.Message}");
            if (RemoteConfigService.Instance != null)
                RemoteConfigService.Instance.FetchCompleted -= ApplySettings;
        }
    }

    private static void ApplySettings(ConfigResponse response)
    {
        if (RemoteConfigService.Instance != null)
            RemoteConfigService.Instance.FetchCompleted -= ApplySettings;

        switch (response.status)
        {
            case ConfigRequestStatus.Success:
                gridSize = RemoteConfigService.Instance.appConfig.GetInt("GRID_SIZE", 6);
                maxCellLevel = RemoteConfigService.Instance.appConfig.GetInt("MAX_CELL_LEVEL", 4);
                botDifficulty = RemoteConfigService.Instance.appConfig.GetInt("BOT_DIFFICULTY", 1);
                maxTurns = RemoteConfigService.Instance.appConfig.GetInt("MAX_TURNS", 30);
                Debug.Log($"[RemoteConfig] OK. GRID_SIZE: {gridSize}, MAX_CELL_LEVEL: {maxCellLevel}, BOT_DIFFICULTY: {botDifficulty}, MAX_TURNS: {maxTurns}");
                Applied?.Invoke();
                break;
            case ConfigRequestStatus.Pending:
                Debug.Log("[RemoteConfig] Descarga pendiente...");
                break;
            case ConfigRequestStatus.Failed:
                Debug.LogError("[RemoteConfig] Falló la descarga. Usando valores por defecto locales.");
                break;
        }
    }
}
