# 🖥️ UIManager - Administrador de Interfaz

## Campos principales
- `authPanel` - Contenedor padre de Auth (agrupa mainMenu, login, register)
- `homeMenuPanel` - Menú principal tras autenticarse
- `shopPanel` - Tienda
- `customizationPanel` - Panel de personalización
- `mainMenuPanel`, `loginPanel`, `registerPanel` - Sub-paneles de autenticación
- `loginUsername`, `loginPassword`, `registerUsername`, `registerPassword` - Inputs
- `statusText` - Texto de estado
- `playerNameText` - Nombre del jugador en Home
- `coinsTexts[]`, `gemsTexts[]` - Textos de monedas/gemas (3 pares: Home, Tienda, Personalización)
- `colorWheel` - Rueda de colores (auto-cableado desde customizationPanel)
- `playerColorSwatches[]` - Botones de color (ColorSwatch, auto-cableado)
- `customColorSwatch` - Botón gris/personalizado
- `modeSelectionPanel` - Panel emergente de selección de modo
- `turnsText` - Texto del límite de turnos

## Navegación
- `ShowAuthScreen()` - Muestra auth + mainMenu, oculta home/shop/custom
- `ShowHomeMenu()` - Muestra home, oculta auth/shop/custom
- `ShowShop()` - Muestra tienda
- `ShowCustomization()` - Muestra personalización
- `ShowMainMenu()` / `ShowLoginPanel()` / `ShowRegisterPanel()` - Sub-paneles de auth
- `OnClickLogout()` - Cierra sesión y vuelve a auth
- `OnClickQuit()` - Cierra la aplicación

## Modo de juego
- `OnClickPlay()` - Abre el panel de selección de modo
- `HideModeSelectionPanel()` - Cierra el panel y vuelve al anterior
- `SetMatchModeNormal()` / `SetMatchModeInfinito()` - Guarda el modo en PlayerPrefs
- `SetMaxTurns(int)` - Guarda el límite de turnos (30-100, de 10 en 10)
- `SelectModeAndPlay(mode, turns)` - Guarda modo+turnos y carga GameScene
- `SelectNormalAndPlay()` / `SelectInfinitoAndPlay()` - Atajos sin parámetros
- `PlaySelectedMode()` - Carga GameScene con los valores guardados

## Personalización de colores
- `OnClickPlayerColorMode(int mode)` - Modo 0=Rosa, 1=Naranja, 2=Personalizado
- `OnWheelColorChanged(Color c)` - Lo llama la rueda al arrastrar (actualiza el botón gris en vivo)
- `RefreshColorPreviews()` - Actualiza previews y marca el botón seleccionado

## Auto-cableado (Awake)
- Si `colorWheel` es null, busca en `customizationPanel`
- Si `playerColorSwatches` está vacío, busca `ColorSwatch` hijos
- Si `customColorSwatch` es null, usa el índice CustomIndex del array

## Notas
- `HideAllPanels()` oculta todos los paneles antes de mostrar uno (evita solapamientos)
- `FetchConfigAndEnterHome()` carga RemoteConfig + Profile y va al Home
- `RefreshCurrencyTexts()` actualiza coins/gems en todos los pares de textos