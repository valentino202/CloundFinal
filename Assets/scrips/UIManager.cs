using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de Navegación")]
    public GameObject authPanel;      // Contenedor padre de Auth (que agrupa mainMenu, login y register)
    public GameObject homeMenuPanel;  // El nuevo Menú Principal tras autenticarse
    public GameObject shopPanel;          // Tienda (lo creas tu manual)
    public GameObject customizationPanel; // Personalizacion (lo creas tu manual)

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

    [Header("Monedas (arrastra los 3 pares: Home, Tienda, Personalizacion)")]
    public TMP_Text[] coinsTexts;
    public TMP_Text[] gemsTexts;

    [Header("Modo de juego (seleccionar antes de darle a Jugar)")]
    [Tooltip("Modo que se usará al cargar GameScene.")]
    public GameManager.MatchMode selectedMatchMode = GameManager.MatchMode.Normal;
    [Range(30, 100)] public int selectedMaxTurns = 30;

    [Tooltip("Texto que muestra el número de turnos mientras mueves la rueda/slider.")]
    public TMP_Text turnsText;

    [Tooltip("Scrollbar que selecciona el límite de turnos.")]
    public Scrollbar maxTurnsScrollbar;

    const string PREF_MODE = "SelectedMatchMode";
    const string PREF_TURNS = "SelectedMaxTurns";

    private void LoadSelection()
    {
        if (PlayerPrefs.HasKey(PREF_MODE))
            selectedMatchMode = (GameManager.MatchMode)Mathf.Clamp(PlayerPrefs.GetInt(PREF_MODE), 0, 1);
        if (PlayerPrefs.HasKey(PREF_TURNS))
            selectedMaxTurns = Mathf.Clamp(PlayerPrefs.GetInt(PREF_TURNS), 30, 100);

        UpdateTurnsText();
    }

    private void UpdateTurnsText()
    {
        if (turnsText != null)
            turnsText.text = selectedMaxTurns.ToString();
    }

    private void SaveSelection()
    {
        PlayerPrefs.SetInt(PREF_MODE, (int)selectedMatchMode);
        PlayerPrefs.SetInt(PREF_TURNS, selectedMaxTurns);
        PlayerPrefs.Save();
    }

    /// <summary>Conecta el botón 'Modo Normal (30 turnos)' a este método.</summary>
    public void SetMatchModeNormal()
    {
        selectedMatchMode = GameManager.MatchMode.Normal;
        SaveSelection();
        Debug.Log("[Modo] Normal seleccionado.");
    }

    /// <summary>Conecta el botón 'Modo Infinito' a este método.</summary>
    public void SetMatchModeInfinito()
    {
        selectedMatchMode = GameManager.MatchMode.Infinito;
        SaveSelection();
        Debug.Log("[Modo] Infinito seleccionado.");
    }

    /// <summary>Lo conecta el botón 'Jugar'. Abre tu panel de selección de modo y oculta el anterior.</summary>
    public void OnClickPlay()
    {
        if (modeSelectionPanel == null)
        {
            Debug.LogWarning("[UI] Asigna un panel en 'Mode Selection Panel' del UIManager.");
            return;
        }

        // Oculta el panel anterior (Home o Auth) para que no quede detrás
        if (homeMenuPanel != null) homeMenuPanel.SetActive(false);
        if (authPanel != null) authPanel.SetActive(false);

        modeSelectionPanel.SetActive(true);
    }

    /// <summary>Lo conecta el botón 'Volver'. Oculta el panel de selección y muestra el anterior.</summary>
    public void HideModeSelectionPanel()
    {
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);

        var back = panelToShowOnBack != null ? panelToShowOnBack : homeMenuPanel;
        if (back != null) back.SetActive(true);
    }

    /// <summary>Elige el modo, guarda la selección y carga GameScene contra la IA.</summary>
    public void SelectModeAndPlay(GameManager.MatchMode mode, int turns = 30)
    {
        selectedMatchMode = mode;
        selectedMaxTurns = Mathf.Clamp(turns, 30, 100);
        SaveSelection();

        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    /// <summary>Selecciona Normal y carga GameScene (para botón sin parámetros).</summary>
    public void SelectNormalAndPlay()
    {
        SelectModeAndPlay(GameManager.MatchMode.Normal, selectedMaxTurns);
    }

    /// <summary>Selecciona Infinito y carga GameScene (para botón sin parámetros).</summary>
    public void SelectInfinitoAndPlay()
    {
        SelectModeAndPlay(GameManager.MatchMode.Infinito, selectedMaxTurns);
    }

    /// <summary>Llama tu rueda/slider para ajustar los turnos (30, 40, ... 100).</summary>
    public void SetMaxTurns(int turns)
    {
        selectedMaxTurns = Mathf.Clamp(turns, 30, 100);
        SaveSelection();
        UpdateTurnsText();
        Debug.Log($"[Turnos] Límite ajustado a {selectedMaxTurns}.");
    }

    public void SetMaxTurnsFromScrollbar(float normalizedValue)
    {
        int step = Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * 7f);
        SetMaxTurns(30 + step * 10);
    }

    [Header("Personalizacion de colores (todo con imagenes, sin texto)")]
    [Tooltip("El GameObject que tiene el script ColorWheel (arrastra el mismo, no la Image).")]
    public ColorWheel colorWheel;

    [Header("Previews (Image con color solido)")]
    [Tooltip("Image que muestra TU color.")]
    public UnityEngine.UI.Image playerColorPreview;
    [Tooltip("Image que muestra el color del ENEMIGO (siempre opuesto al tuyo).")]
    public UnityEngine.UI.Image enemyColorPreview;

    [Header("Panel emergente de selección de modo (opcional)")]
    [Tooltip("Si no asignas uno, se crea automático al darle a Jugar.")]
    public GameObject modeSelectionPanel;

    [Tooltip("Panel que se muestra al darle a Volver (por defecto HomeMenuPanel).")]
    public GameObject panelToShowOnBack;

    [Header("Botones de color (cada uno con el script ColorSwatch)")]
    [Tooltip("Cada uno es un GameObject con Button + ColorSwatch. El seleccionado se agranda.")]
    public ColorSwatch[] playerColorSwatches;

    [Header("Boton Personalizado (gris / rueda de colores)")]
    [Tooltip("El swatch gris que muestra el color elegido de la rueda en vivo.")]
    public ColorSwatch customColorSwatch;

    private void Awake()
    {
        // Auto-cableado: si no se asignaron a mano, se buscan en el panel de personalizacion.
        if (customizationPanel != null)
        {
            if (colorWheel == null) colorWheel = customizationPanel.GetComponentInChildren<ColorWheel>(true);
            if (playerColorSwatches == null || playerColorSwatches.Length == 0)
                playerColorSwatches = customizationPanel.GetComponentsInChildren<ColorSwatch>(true);
            if (customColorSwatch == null && playerColorSwatches != null && playerColorSwatches.Length > PlayerColorOptions.CustomIndex)
                customColorSwatch = playerColorSwatches[PlayerColorOptions.CustomIndex];
        }
    }

    private void Start()
    {
        LoadSelection(); // modo de juego elegido antes de darle a Jugar

        if (maxTurnsScrollbar != null)
        {
            maxTurnsScrollbar.onValueChanged.RemoveListener(SetMaxTurnsFromScrollbar);
            maxTurnsScrollbar.onValueChanged.AddListener(SetMaxTurnsFromScrollbar);
            maxTurnsScrollbar.SetValueWithoutNotify((selectedMaxTurns - 30f) / 70f);
        }

        if (authPanel == null) Debug.LogError("UIManager: authPanel NO asignado en el Inspector.");
        if (homeMenuPanel == null) Debug.LogError("UIManager: homeMenuPanel NO asignado en el Inspector.");
        if (mainMenuPanel == null || loginPanel == null || registerPanel == null)
        {
            Debug.LogError("UIManager: Paneles de UI no asignados en el Inspector.");
        }

        // Si ya hay sesion, ir directo al Home; si no, mostrar Auth + MainMenu
        if (UGSAuthManager.Instance != null && UGSAuthManager.Instance.IsSignedIn)
        {
            ShowHomeMenu();
        }
        else
        {
            ShowAuthScreen();
        }
    }

    private void OnEnable()
    {
        UGSAuthManager.AutoLoginSucceeded += HandleAutoLogin;
        if (colorWheel != null) colorWheel.OnColorChanged = OnWheelColorChanged;
    }

    private void OnDisable()
    {
        UGSAuthManager.AutoLoginSucceeded -= HandleAutoLogin;
        if (colorWheel != null) colorWheel.OnColorChanged = null;
    }

    private void HandleAutoLogin()
    {
        _ = FetchConfigAndEnterHome();
    }

    // --- NAVEGACIÓN ---
    /// <summary>Oculta todos los paneles para evitar solapamientos.</summary>
    private void HideAllPanels()
    {
        if (authPanel != null) authPanel.SetActive(false);
        if (homeMenuPanel != null) homeMenuPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (customizationPanel != null) customizationPanel.SetActive(false);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
    }

    public void ShowAuthScreen()
    {
        HideAllPanels();
        if (authPanel != null) authPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    public void ShowHomeMenu()
    {
        Debug.Log("[UI] Mostrando HomeMenuPanel.");
        HideAllPanels();
        if (homeMenuPanel != null) homeMenuPanel.SetActive(true);
        else Debug.LogWarning("[UI] homeMenuPanel es null, no se puede mostrar.");

        // Actualizar el nombre del jugador
        if (playerNameText != null && UGSAuthManager.Instance != null)
        {
            playerNameText.text = "Jugador: " + UGSAuthManager.Instance.GetPlayerName();
        }

        _ = RefreshCurrencyTexts();
    }

    public void ShowLoginPanel()
    {
        HideAllPanels();
        if (authPanel != null) authPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        HideAllPanels();
        if (authPanel != null) authPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
    }

    /// <summary>Tienda: apaga todo y prende Shop. Conecta tu boton Tienda a este metodo.</summary>
    public void ShowShop()
    {
        Debug.Log("[UI] Mostrando ShopPanel.");
        HideAllPanels();
        if (shopPanel != null) shopPanel.SetActive(true);
        else Debug.LogWarning("[UI] shopPanel es null, asignalo en el Inspector.");

        _ = RefreshCurrencyTexts();
    }

    /// <summary>Personalizacion: apaga todo y prende Custom. Conecta tu boton a este metodo.</summary>
    public void ShowCustomization()
    {
        Debug.Log("[UI] Mostrando CustomizationPanel.");
        HideAllPanels();
        if (customizationPanel != null) customizationPanel.SetActive(true);
        else Debug.LogWarning("[UI] customizationPanel es null, asignalo en el Inspector.");

        _ = RefreshCurrencyTexts();
        RefreshColorPreviews();
    }

    /// <summary>Lee balances de la nube y los escribe en los 3 pares de textos.</summary>
    public async System.Threading.Tasks.Task RefreshCurrencyTexts()
    {
        await UGSEconomy.RefreshAsync();
        if (coinsTexts != null)
            foreach (var t in coinsTexts)
                if (t != null) t.text = UGSEconomy.Coins.ToString();
        if (gemsTexts != null)
            foreach (var t in gemsTexts)
                if (t != null) t.text = UGSEconomy.Gems.ToString();
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (authPanel != null) authPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    // --- Personalizacion: color de tu ficha (el enemigo sale opuesto) ---
    // Rosa = 0, Naranja = 1, Personalizado = 2 (rueda). Conecta tus botones a estos.

    public void OnClickPlayerColorMode(int mode)
    {
        if (UGSProfileManager.Instance == null) return;
        if (playerColorSwatches == null || mode < 0 || mode >= playerColorSwatches.Length) return;
        var sw = playerColorSwatches[mode];
        if (sw == null) return;
        // Modos 0 y 1 usan los colores fijos del juego (Rosa / Naranja); el 2
        // (rueda) usa el color elegido del boton gris (actualizado en vivo por la rueda).
        if (mode == PlayerColorOptions.CustomIndex)
        {
            _ = UGSProfileManager.Instance.SetCustomColorAsync(sw.Color);
        }
        else
        {
            _ = UGSProfileManager.Instance.SetPlayerColorModeAsync(mode);
        }
        RefreshColorPreviews();
    }

    /// <summary>Lo llama la rueda al arrastrar. Activa modo Personalizado y pinta el boton gris en vivo.</summary>
    public void OnWheelColorChanged(Color c)
    {
        if (UGSProfileManager.Instance == null) return;
        // El boton gris muestra el color elegido mientras arrastras la rueda.
        if (customColorSwatch != null) customColorSwatch.SetColor(c);
        _ = UGSProfileManager.Instance.SetCustomColorAsync(c);
        RefreshColorPreviews();
    }

    /// <summary>Pinta los previews y marca el boton de color seleccionado.</summary>
    public void RefreshColorPreviews()
    {
        if (UGSProfileManager.Instance == null) return;

        if (playerColorPreview != null)
        {
            playerColorPreview.color = BoardFx.ColP1;
            playerColorPreview.raycastTarget = false;
        }
        if (enemyColorPreview != null)
        {
            enemyColorPreview.color = BoardFx.ColP2;
            enemyColorPreview.raycastTarget = false;
        }

        UpdateSwatchSelection();
    }

    // El seleccionado se agranda; los demas vuelven a escala normal.
    // Opcional: si el boton tiene un hijo llamado "SelectedFrame", se enciende.
    private void UpdateSwatchSelection()
    {
        if (playerColorSwatches == null) return;
        Color current = BoardFx.ColP1;
        for (int i = 0; i < playerColorSwatches.Length; i++)
        {
            var sw = playerColorSwatches[i];
            if (sw == null) continue;
            bool selected = ColorCloseEnough(sw.Color, current);
            sw.SetSelected(selected);

            var frame = sw.transform.Find("SelectedFrame");
            if (frame != null) frame.gameObject.SetActive(selected);
        }
    }

    private static bool ColorCloseEnough(Color a, Color b)
        => Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

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
            bool success = await UGSAuthManager.Instance.SwitchToAnonymousAsync();
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
            bool success = await UGSAuthManager.Instance.SwitchToUsernamePasswordAsync(loginUsername.text, loginPassword.text);
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
        Debug.Log("[UI] Click Cerrar Sesion.");
        if (UGSAuthManager.Instance != null)
        {
            UGSAuthManager.Instance.SignOut();
        }
        else
        {
            Debug.LogWarning("[UI] UGSAuthManager.Instance es null en Logout.");
        }
        if (loginPassword != null) loginPassword.text = "";
        if (registerPassword != null) registerPassword.text = "";

        HideAllPanels();
        if (authPanel != null) authPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        SetStatus("Sesión cerrada.");
    }

    /// <summary>Cierra la aplicacion (en Editor detiene el Play). Conecta tus botones de salir aquí.</summary>
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- MÉTODOS AUXILIARES ---
    private async System.Threading.Tasks.Task FetchConfigAndEnterHome()
    {
        await UGSRemoteConfig.FetchAsync();
        if (UGSProfileManager.Instance != null)
        {
            await UGSProfileManager.Instance.LoadProfileAsync();
        }
        ShowHomeMenu();
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
