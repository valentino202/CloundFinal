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
    public bool IsSignedIn => isInitialized && AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
    public static event Action AutoLoginSucceeded;
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

    private bool _busy = false;

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("UGS Inicializado Correctamente.");
            SetupEvents();
            isInitialized = true;
            await TryAutoLoginAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Error al inicializar UGS: " + e.Message);
        }
    }

    private async Task TryAutoLoginAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (AuthenticationService.Instance == null || AuthenticationService.Instance.IsSignedIn)
                return;

            // Sin cuenta guardada: quedarse en AuthPanel para elegir registro/login/invitado.
            if (!AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log("[AUTH] Sin sesion guardada. Esperando en AuthPanel.");
                return;
            }

            // Con sesion guardada: entrar solo (restaura la ultima cuenta).
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[AUTH] Auto-login transparente OK. PlayerID: {AuthenticationService.Instance.PlayerId}");
            AutoLoginSucceeded?.Invoke();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogWarning($"[AUTH] Auto-login no disponible: {ex.Message}. El usuario puede loguearse manual.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AUTH] Auto-login fallo: {ex.Message}");
        }
        finally { _busy = false; }
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
            await EnsurePlayerNameAsync(username);
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Error al iniciar sesión: {ex.Message}");
            return false;
        }
    }

    // Cambio explicito de cuenta desde un boton (cierra sesion previa primero).
    public async Task<bool> SwitchToAnonymousAsync()
    {
        if (!isInitialized || AuthenticationService.Instance == null) return false;
        if (_busy) { Debug.LogWarning("[AUTH] Operacion en curso, espera."); return false; }
        _busy = true;
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[AUTH] Cambiando de cuenta: cerrando sesion previa...");
                AuthenticationService.Instance.SignOut();
            }
            // Sin esto, el token de la cuenta con contrasena queda en disco
            // y SignInAnonymously restaura esa misma cuenta en vez del invitado.
            AuthenticationService.Instance.ClearSessionToken();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[AUTH] Sesion invitado OK. PlayerID: {AuthenticationService.Instance.PlayerId}");
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Error al cambiar a invitado: {ex.Message}");
            return false;
        }
        finally { _busy = false; }
    }

    public async Task<bool> SwitchToUsernamePasswordAsync(string username, string password)
    {
        if (!isInitialized || AuthenticationService.Instance == null) return false;
        if (_busy) { Debug.LogWarning("[AUTH] Operacion en curso, espera."); return false; }
        _busy = true;
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[AUTH] Cambiando de cuenta: cerrando sesion previa...");
                AuthenticationService.Instance.SignOut();
            }
            AuthenticationService.Instance.ClearSessionToken();
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log($"Inicio de sesión exitoso con Usuario/Contraseña! PlayerID: {AuthenticationService.Instance.PlayerId}");
            await EnsurePlayerNameAsync(username);
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Error al iniciar sesión: {ex.Message}");
            return false;
        }
        finally { _busy = false; }
    }

    // Si la cuenta no tiene PlayerName (creada antes o desde dashboard),
    // lo repara con el username del login para no mostrar Invitado_XXXX.
    private async Task EnsurePlayerNameAsync(string username)
    {
        try
        {
            if (AuthenticationService.Instance == null) return;
            if (!string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName)) return;
            if (string.IsNullOrEmpty(username)) return;
            await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
            Debug.Log($"[AUTH] PlayerName reparado: {username}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AUTH] No se pudo reparar PlayerName: {ex.Message}");
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
        // Cerrar sesion borra el token: al reabrir se queda en AuthPanel.
        AuthenticationService.Instance.ClearSessionToken();
    }
}
