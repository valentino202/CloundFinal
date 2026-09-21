using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

public class AuthUIController : MonoBehaviour
{
    [Header("Referencias de UI - Login")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;

    [Header("Referencias de UI - Registro / Vincular")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;

    [Header("Estado General")]
    public TMP_Text statusText;

    // Referencia opcional al UIManager para cambiar de pantalla al autenticar
    [Header("Controlador de UI")]
    public UIManager uiManager;

    // =========================================================================
    // 1. INICIAR SESIÓN COMO INVITADO
    // =========================================================================
    public async void OnClickAnonymousLogin()
    {
        SetStatus("Iniciando sesión como invitado...");

        if (!CheckAuthManager()) return;

        try
        {
            bool success = await UGSAuthManager.Instance.SignInAnonymouslyAsync();
            if (success)
            {
                SetStatus("Bienvenido (Invitado)");
                OnAuthSuccess();
            }
            else
            {
                SetStatus("Error en Login Anónimo.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickAnonymousLogin: {ex}");
            SetStatus("Error al iniciar sesión como invitado.");
        }
    }

    // =========================================================================
    // 2. INICIAR SESIÓN CON CREDENCIALES (LOGIN)
    // =========================================================================
    public async void OnClickLoginWithCredentials()
    {
        if (!ValidateInputs(loginUsernameInput, loginPasswordInput)) return;
        if (!CheckAuthManager()) return;

        SetStatus("Iniciando sesión...");
        try
        {
            bool success = await UGSAuthManager.Instance.SignInWithUsernamePasswordAsync(loginUsernameInput.text, loginPasswordInput.text);
            if (success)
            {
                SetStatus($"Bienvenido, {loginUsernameInput.text}!");
                OnAuthSuccess();
            }
            else
            {
                SetStatus("Error de credenciales o usuario no encontrado.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickLoginWithCredentials: {ex}");
            SetStatus("Error al iniciar sesión.");
        }
    }

    // =========================================================================
    // 3. REGISTRAR CUENTA NUEVA
    // =========================================================================
    public async void OnClickRegister()
    {
        if (!ValidateInputs(registerUsernameInput, registerPasswordInput)) return;
        if (!CheckAuthManager()) return;

        SetStatus("Registrando cuenta...");
        try
        {
            bool success = await UGSAuthManager.Instance.SignUpWithUsernamePasswordAsync(registerUsernameInput.text, registerPasswordInput.text);
            if (success)
            {
                SetStatus("¡Cuenta creada exitosamente!");
                OnAuthSuccess();
            }
            else
            {
                SetStatus("Error al crear la cuenta. Intenta con otro usuario.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickRegister: {ex}");
            SetStatus("Error al crear cuenta.");
        }
    }

    // =========================================================================
    // 4. UNIFICAR / VINCULAR CUENTA ANÓNIMA
    // =========================================================================
    public async void OnClickLinkAccount()
    {
        if (!ValidateInputs(registerUsernameInput, registerPasswordInput)) return;
        if (!CheckAuthManager()) return;

        SetStatus("Vinculando datos de invitado a tu nueva cuenta...");
        try
        {
            bool success = await UGSAuthManager.Instance.LinkAnonymousToUsernamePasswordAsync(registerUsernameInput.text, registerPasswordInput.text);
            if (success)
            {
                SetStatus("¡Cuenta vinculada con éxito!");
                OnAuthSuccess();
            }
            else
            {
                SetStatus("Error al vincular la cuenta.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickLinkAccount: {ex}");
            SetStatus("Error al vincular cuenta.");
        }
    }

    // =========================================================================
    // VALIDACIONES Y MÉTODOS AUXILIARES
    // =========================================================================
    private bool ValidateInputs(TMP_InputField userInput, TMP_InputField passInput)
    {
        if (userInput == null || passInput == null)
        {
            SetStatus("Campos de entrada no asignados en el Inspector.");
            Debug.LogError("Campos TMP_InputField no asignados en AuthUIController.");
            return false;
        }

        if (string.IsNullOrEmpty(userInput.text) || string.IsNullOrEmpty(passInput.text))
        {
            SetStatus("Por favor ingresa usuario y contraseña.");
            return false;
        }

        return true;
    }

    private bool CheckAuthManager()
    {
        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: Autenticador no inicializado.");
            Debug.LogError("UGSAuthManager.Instance es null");
            return false;
        }
        return true;
    }

    private async void OnAuthSuccess()
    {
        // UIManager es la fuente de verdad para navegacion y carga (RemoteConfig + Profile).
        // Se replica aqui para no depender del orden de botones en escena.
        if (UGSRemoteConfigManager.Instance != null)
        {
            await UGSRemoteConfigManager.Instance.FetchRemoteConfigValues();
        }
        if (UGSProfileManager.Instance != null)
        {
            await UGSProfileManager.Instance.LoadProfileAsync();
        }

        // Cambiar la pantalla al HomeMenu
        if (uiManager != null)
        {
            uiManager.ShowHomeMenu();
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
