using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UGSAuthManager : MonoBehaviour
{
    public static UGSAuthManager Instance;
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
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

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("UGS Inicializado Correctamente.");
            SetupEvents();
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error al inicializar UGS: " + e.Message);
        }
    }

    private void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"[AUTH] Sesión iniciada. PlayerID: {AuthenticationService.Instance.PlayerId}");
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("[AUTH] Sesión cerrada.");
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.LogError($"[AUTH] Error de autenticación: {err.ErrorCode} - {err.Message}");
        };
    }

    // =========================================================================
    // 1. LOGIN ANÓNIMO (INVITADO)
    // =========================================================================
    public async Task<bool> SignInAnonymouslyAsync()
    {
        if (!isInitialized)
        {
            Debug.LogError("Intento de SignInAnonymously antes de inicializar UGS.");
            return false;
        }

        if (AuthenticationService.Instance == null)
        {
            Debug.LogError("AuthenticationService.Instance es null en SignInAnonymouslyAsync.");
            return false;
        }

        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Login Anónimo Exitoso!");
            }
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Error en Login Anónimo: {ex.Message}");
            return false;
        }
    }

    // =========================================================================
    // 2. USERNAME & PASSWORD (REGISTRO E INICIO DE SESIÓN)
    // =========================================================================
    public async Task<bool> SignUpWithUsernamePasswordAsync(string username, string password)
    {
        if (!isInitialized || AuthenticationService.Instance == null)
        {
            Debug.LogError("Intento de registro antes de inicializar UGS o AuthenticationService no disponible.");
            return false;
        }

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            // Asignamos el nombre de usuario como PlayerName en el perfil de UGS
            await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
            Debug.Log("Usuario registrado con éxito!");
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Error al registrar usuario: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SignInWithUsernamePasswordAsync(string username, string password)
    {
        if (!isInitialized || AuthenticationService.Instance == null)
        {
            Debug.LogError("Intento de inicio de sesión antes de inicializar UGS o AuthenticationService no disponible.");
            return false;
        }

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("Inicio de sesión exitoso con Usuario/Contraseña!");
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Error al iniciar sesión: {ex.Message}");
            return false;
        }
    }

    // =========================================================================
    // 3. UNIFICAR USUARIOS (LINKING ACCOUNTS)
    // =========================================================================
    public async Task<bool> LinkAnonymousToUsernamePasswordAsync(string username, string password)
    {
        if (!isInitialized || AuthenticationService.Instance == null)
        {
            Debug.LogError("Intento de vincular antes de inicializar UGS.");
            return false;
        }

        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);
                await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
                Debug.Log("¡Cuenta Anónima unificada con éxito a Usuario/Contraseña!");
                return true;
            }
            else
            {
                Debug.LogWarning("Debes estar en una sesión anónima antes de unificar.");
                return false;
            }
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Error al unificar cuenta: {ex.Message}");
            return false;
        }
    }

    // =========================================================================
    // OB TENER NOMBRE DEL JUGADOR
    // =========================================================================
    public string GetPlayerName()
    {
        if (!isInitialized || AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn) return "Sin Sesión";

        string playerName = AuthenticationService.Instance.PlayerName;

        // Si no tiene un PlayerName registrado (caso invitado), mostramos "Invitado_XXXX"
        if (string.IsNullOrEmpty(playerName))
        {
            string pid = AuthenticationService.Instance.PlayerId ?? "";
            string shortId = pid.Length >= 5 ? pid.Substring(0, 5) : pid;
            return $"Invitado_{shortId}";
        }

        // Limpiamos la etiqueta de discriminador que Unity añade por defecto (ej. User#1234 -> User)
        if (playerName.Contains("#"))
        {
            playerName = playerName.Split('#')[0];
        }

        return playerName;
    }

    public void SignOut()
    {
        if (!isInitialized || AuthenticationService.Instance == null)
        {
            Debug.LogWarning("Intento de SignOut cuando UGS no está inicializado o AuthenticationService no disponible.");
            return;
        }

        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("Usuario desconectado.");
        }
    }
}
