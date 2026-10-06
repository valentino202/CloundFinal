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

    // UGS 3.7.4 no expone IdentityToken: la persistencia del invitado la lleva
    // el session token, que SignInAnonymouslyAsync() ya reutiliza solo. El truco
    // es NO llamar ClearSessionToken() y ademas aislar cada cuenta en un Profile,
    // porque cada profile guarda su propio session token en PlayerPrefs.
    // - "guest": cuenta anonima (una sola por instalacion)
    // - "user" : cuentas con usuario/contrasena
    private const string GuestProfile = "guest";
    private const string UserProfile = "user";

    // Marca que el usuario cerro sesion a proposito. Evita usar ClearSessionToken()
    // para eso, que destruiria el invitado y crearia un PlayerID nuevo al reiniciar.
    private const string SignedOutFlag = "UGS_SignedOutOnPurpose";

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

            // El usuario cerro sesion a proposito: quedarse en AuthPanel pero
            // SIN borrar el session token, para no perder la cuenta existente.
            if (PlayerPrefs.GetInt(SignedOutFlag, 0) == 1)
            {
                Debug.Log("[AUTH] Sesion cerrada manualmente. Esperando en AuthPanel.");
                return;
            }

            // Con sesion guardada: entrar solo (restaura la ultima cuenta).
            // UGS reutiliza el session token del profile activo, asi que esto
            // devuelve siempre el MISMO jugador, no uno nuevo.
            if (AuthenticationService.Instance.SessionTokenExists)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[AUTH] Auto-login transparente OK ({AuthenticationService.Instance.Profile}). PlayerID: {AuthenticationService.Instance.PlayerId}");
                AutoLoginSucceeded?.Invoke();
                return;
            }

            // Sin sesion guardada: quedarse en AuthPanel para elegir registro/login/invitado.
            Debug.Log("[AUTH] Sin sesion guardada. Esperando en AuthPanel.");
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
            // El invitado vive en su propio profile: asi su session token no se
            // pisa con el de una cuenta con contrasena.
            SwitchToProfile(GuestProfile);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                // Si el profile ya tiene session token, UGS devuelve el MISMO
                // jugador. Solo crea uno nuevo la primera vez.
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Login Anónimo Exitoso!");
            }

            PlayerPrefs.SetInt(SignedOutFlag, 0);
            PlayerPrefs.Save();
            return true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Error en Login Anónimo: {ex.Message}");
            return false;
        }
    }

    // SwitchProfile exige estar desconectado, asi que cierra sesion conservando
    // las credenciales del profile actual (clearCredentials=false por defecto).
    private static void SwitchToProfile(string profile)
    {
        if (AuthenticationService.Instance == null) return;
        if (AuthenticationService.Instance.IsSignedIn)
            AuthenticationService.Instance.SignOut();
        if (AuthenticationService.Instance.Profile != profile)
            AuthenticationService.Instance.SwitchProfile(profile);
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
        }        catch (AuthenticationException ex)
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
            Debug.Log("[AUTH] Cambiando al invitado...");
            // Sin ClearSessionToken: el profile "guest" conserva su session token,
            // y UGS devuelve SIEMPRE el mismo invitado. Ese era el bug que
            // generaba un PlayerID nuevo en cada pulsacion del boton.
            SwitchToProfile(GuestProfile);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            PlayerPrefs.SetInt(SignedOutFlag, 0);
            PlayerPrefs.Save();
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
            Debug.Log("[AUTH] Cambiando de cuenta: cerrando sesion previa...");
            // El profile "user" es independiente del "guest": el invitado no se pisa.
            SwitchToProfile(UserProfile);
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            PlayerPrefs.SetInt(SignedOutFlag, 0);
            PlayerPrefs.Save();
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
                // La cuenta del profile "guest" ya tiene contrasena. El token
                // sigue sirviendo (mismo PlayerId), asi que se deja intacto:
                // "Invitado" seguira llevando a ESTA cuenta, no a otra.
                PlayerPrefs.SetInt(SignedOutFlag, 0);
                PlayerPrefs.Save();
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
            // SignOut() sin clearCredentials conserva el session token: la cuenta
            // NO se pierde y no se crea un PlayerID nuevo al volver a entrar.
            AuthenticationService.Instance.SignOut();
            Debug.Log("Usuario desconectado.");
        }

        // Marca la intention de stay en AuthPanel. Antes esto se hacia con
        // ClearSessionToken(), que destruia la cuenta anonima: por eso salia
        // una cuenta "Invitado" nueva en cada arranque.
        PlayerPrefs.SetInt(SignedOutFlag, 1);
        PlayerPrefs.Save();
        Debug.Log("[AUTH] Sesion cerrada. La cuenta se conserva para el proximo acceso.");
    }

    // Borra la cuenta anonima de verdad (crea una nueva la proxima vez).
    // Solo para depurar/testing; el flujo normal ya no lo necesita.
    public void ResetGuest()
    {
        if (AuthenticationService.Instance == null) return;
        if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();
        if (AuthenticationService.Instance.Profile != GuestProfile)
            AuthenticationService.Instance.SwitchProfile(GuestProfile);
        AuthenticationService.Instance.ClearSessionToken();
        PlayerPrefs.DeleteKey(SignedOutFlag);
        PlayerPrefs.Save();
        Debug.LogWarning("[AUTH] Invitado borrado. El proximo acceso creara una cuenta nueva.");
    }
}
