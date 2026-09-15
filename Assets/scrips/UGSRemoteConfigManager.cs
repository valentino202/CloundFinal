using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.RemoteConfig;
public class UGSRemoteConfigManager : MonoBehaviour
{
    public static UGSRemoteConfigManager Instance;

    [Header("Parámetros del Tablero (Valores Por Defecto)")]
    public int gridSize = 6;
    public int maxCellLevel = 4;

    // Estructuras requeridas por Remote Config
    public struct UserAttributes { }
    public struct AppAttributes { }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Llama a este método justo después de iniciar sesión con Authentication
    public async Task FetchRemoteConfigValues()
    {
        // Verificaciones básicas
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

            // Suscribirse al evento de respuesta
            RemoteConfigService.Instance.FetchCompleted += ApplyRemoteSettings;

            // Solicitar configuración desde los servidores de UGS
            await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
        }
        catch (Exception e)
        {
            Debug.LogError($"[RemoteConfig] Error al consultar datos: {e.Message}");
            // Asegurar que no quede la suscripción si hubo error
            if (RemoteConfigService.Instance != null)
            {
                RemoteConfigService.Instance.FetchCompleted -= ApplyRemoteSettings;
            }
        }
    }

    private void ApplyRemoteSettings(ConfigResponse response)
    {
        // Desuscribir inmediatamente para evitar múltiples llamados
        if (RemoteConfigService.Instance != null)
        {
            RemoteConfigService.Instance.FetchCompleted -= ApplyRemoteSettings;
        }

        switch (response.status)
        {
            case ConfigRequestStatus.Success:
                // Leer las variables enviadas desde el Dashboard de Unity
                gridSize = RemoteConfigService.Instance.appConfig.GetInt("GRID_SIZE", 6);
                maxCellLevel = RemoteConfigService.Instance.appConfig.GetInt("MAX_CELL_LEVEL", 4);

                Debug.Log($"[RemoteConfig] Configuración cargada con éxito. GRID_SIZE: {gridSize}, MAX_CELL_LEVEL: {maxCellLevel}");
                break;

            case ConfigRequestStatus.Pending:
                Debug.Log("[RemoteConfig] Descarga pendiente...");
                break;

            case ConfigRequestStatus.Failed:
                Debug.LogError("[RemoteConfig] Falló la descarga de datos. Usando valores por defecto locales.");
                break;
        }
    }

    private void OnDestroy()
    {
        if (RemoteConfigService.Instance != null)
        {
            RemoteConfigService.Instance.FetchCompleted -= ApplyRemoteSettings;
        }
    }
}
