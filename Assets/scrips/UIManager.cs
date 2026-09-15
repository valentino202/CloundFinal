using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de Navegación")]
    public GameObject authPanel;      // Contenedor padre de Auth (que agrupa mainMenu, login y register)
    public GameObject homeMenuPanel;  // El nuevo Menú Principal tras autenticarse

    [Header("Sub-Paneles de Autenticación")]
    public GameObject mainMenuPanel;
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Referencias de Inputs y Textos")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;
    public TMP_InputField registerUsername;
    public TMP_InputField registerPassword;
    public TMP_Text statusText;
    public TMP_Text playerNameText;    // Texto en HomeMenuPanel donde se mostrará el nombre

    private void Start()
    {
        if (mainMenuPanel == null || loginPanel == null || registerPanel == null)
        {
            Debug.LogError("UIManager: Paneles de UI no asignados en el Inspector.");
        }

        ShowAuthScreen();
        ShowMainMenu();
    }

    // --- NAVEGACIÓN ---
    public void ShowAuthScreen()
    {
        if (authPanel != null) authPanel.SetActive(true);
        if (homeMenuPanel != null) homeMenuPanel.SetActive(false);
    }

    public void ShowHomeMenu()
    {
        if (authPanel != null) authPanel.SetActive(false);
        if (homeMenuPanel != null) homeMenuPanel.SetActive(true);

        // Actualizar el nombre del jugador usando la nueva función de UGSAuthManager
        if (playerNameText != null && UGSAuthManager.Instance != null)
        {
            playerNameText.text = "Jugador: " + UGSAuthManager.Instance.GetPlayerName();
        }
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    public void ShowLoginPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
    }

    // --- ACCIONES DE AUTENTICACIÓN Y TRANSICIÓN AL HOME ---
    public async void OnClickGuest()
    {
        SetStatus("Conectando como invitado...");

        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: autenticador no inicializado.");
            return;
        }

        try
        {
            bool success = await UGSAuthManager.Instance.SignInAnonymouslyAsync();
            if (success)
            {
                SetStatus("Cargando configuración...");
                await FetchConfigAndEnterHome();
            }
            else
            {
                SetStatus("Error en login invitado.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickGuest: {ex}");
            SetStatus("Error al conectar como invitado.");
        }
    }

    public async void OnClickSubmitLogin()
    {
        if (loginUsername == null || loginPassword == null)
        {
            SetStatus("Campos de login no asignados.");
            return;
        }

        if (string.IsNullOrEmpty(loginUsername.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            SetStatus("Por favor ingresa usuario y contraseña.");
            return;
        }

        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: autenticador no inicializado.");
            return;
        }

        SetStatus("Iniciando sesión...");
        try
        {
            bool success = await UGSAuthManager.Instance.SignInWithUsernamePasswordAsync(loginUsername.text, loginPassword.text);
            if (success)
            {
                await FetchConfigAndEnterHome();
            }
            else
            {
                SetStatus("Error de credenciales.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickSubmitLogin: {ex}");
            SetStatus("Error al iniciar sesión.");
        }
    }

    public async void OnClickSubmitRegister()
    {
        if (registerUsername == null || registerPassword == null)
        {
            SetStatus("Campos de registro no asignados.");
            return;
        }

        if (string.IsNullOrEmpty(registerUsername.text) || string.IsNullOrEmpty(registerPassword.text))
        {
            SetStatus("Por favor completa usuario y contraseña.");
            return;
        }

        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: autenticador no inicializado.");
            return;
        }

        SetStatus("Creando cuenta...");
        try
        {
            bool success = await UGSAuthManager.Instance.SignUpWithUsernamePasswordAsync(registerUsername.text, registerPassword.text);
            if (success)
            {
                await FetchConfigAndEnterHome();
            }
            else
            {
                SetStatus("Error al crear cuenta.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickSubmitRegister: {ex}");
            SetStatus("Error al crear cuenta.");
        }
    }

    public async void OnClickLinkAccount()
    {
        if (registerUsername == null || registerPassword == null)
        {
            SetStatus("Campos de registro no asignados.");
            return;
        }

        if (string.IsNullOrEmpty(registerUsername.text) || string.IsNullOrEmpty(registerPassword.text))
        {
            SetStatus("Por favor completa usuario y contraseña.");
            return;
        }

        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: autenticador no inicializado.");
            return;
        }

        SetStatus("Vinculando cuenta...");
        try
        {
            bool success = await UGSAuthManager.Instance.LinkAnonymousToUsernamePasswordAsync(registerUsername.text, registerPassword.text);
            if (success)
            {
                SetStatus("¡Cuenta unificada con éxito!");
                ShowHomeMenu();
            }
            else
            {
                SetStatus("Error al vincular cuenta.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickLinkAccount: {ex}");
            SetStatus("Error al vincular cuenta.");
        }
    }

    public void OnClickLogout()
    {
        if (UGSAuthManager.Instance != null)
        {
            UGSAuthManager.Instance.SignOut();
        }
        ShowAuthScreen();
        ShowMainMenu();
        SetStatus("Sesión cerrada.");
    }

    // --- MÉTODOS AUXILIARES ---
    private async System.Threading.Tasks.Task FetchConfigAndEnterHome()
    {
        if (UGSRemoteConfigManager.Instance != null)
        {
            await UGSRemoteConfigManager.Instance.FetchRemoteConfigValues();
        }
        ShowHomeMenu();
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
