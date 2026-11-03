# UGSProfileManager - Perfil, Guardado y Conflictos

## Archivo
`Assets/scrips/UGSProfileManager.cs` — clase estática pura (singleton `Instance`), no MonoBehaviour.

## PlayerProfile (datos del jugador)
- `saveVersion` (int) — versión del esquema de guardado
- `cloudTimestamp` (long) — ticks del último guardado en nube
- `localTimestamp` (long) — ticks del último guardado local
- **Configuración de usuario**: PlayerName, SkinsOwned, EquippedSkin, OwnedPackIds, EquippedPackId, PlayerColorMode, PlayerCustomColor
- **Progreso básico**: TotalWins, TotalMatches, Coins, Gems

## Guardado (Cloud Save + PlayerPrefs)

### Nube (Cloud Save SDK)
- `LoadProfileFromCloudAsync()` → Task<bool> — carga desde UGS Cloud Save. Retorna true si encontró datos.
- `SaveProfileToCloudAsync()` → Task<bool> — guarda en UGS Cloud Save con timestamp UTC.

### Local (PlayerPrefs)
- `SaveProfileToLocal()` — guarda Profile como JSON en PlayerPrefs
- `LoadProfileFromLocal()` → bool — carga Profile desde PlayerPrefs

### Unificado
- `LoadProfileAsync()` — nube primero, si falla usa local. Si ambos existen y difieren, dispara `OnSaveConflictDetected`.
- `SaveProfileAsync()` — guarda en nube Y local.

## Conflictos Nube vs Local
- `CheckConflict()` → SaveConflictInfo — compara cloudTimestamp vs localTimestamp y saveVersion
- `ResolveConflictAsync(bool keepCloud)` — keepCloud=true conserva nube (sincroniza local), false conserva local (sube a nube)
- Evento `OnSaveConflictDetected` (Action<SaveConflictInfo>) — la UI se suscribe para mostrar diálogo

## Eventos
- `OnSaveConflictDetected` — conflicto entre nube y local
- `OnProfileLoaded` — perfil cargado
- `OnProfileSaved` — perfil guardado

## Otros métodos públicos
- `RecordMatchResultAsync(bool won)` — suma TotalMatches/TotalWins y guarda
- `OwnsPack(string packId)` → bool
- `UnlockPackAsync(string packId)` → Task<bool>
- `EquipPackAsync(string packId)` → Task<bool>
- `GrantCurrencyAsync(long coins, long gems)` — suma monedas/gemas y guarda
- `SpendCurrencyAsync(long coins, long gems)` → Task<bool> — gasta si alcanza
- `ApplyColors()` — llama BoardFx.ApplyColors(PlayerColorMode, PlayerCustomColor)
- `SetPlayerColorModeAsync(int mode)` — modo 0=Rosa, 1=Naranja, 2=Personalizado
- `SetCustomColorAsync(Color c)` — color libre de la rueda, activa modo 2

## Integración con Auth
- `CanUseCloudSave()` → bool — verifica AuthenticationService.IsSignedIn && CloudSaveService.Instance != null
- Sin sesión, todo opera en PlayerPrefs (local only)

## Flujo típico
1. Login → `LoadProfileAsync()` (nube primero, local de respaldo)
2. Cambio de color/skin → `SetPlayerColorModeAsync()` / `UnlockPackAsync()` (guarda nube+local)
3. Fin de partida → `RecordMatchResultAsync()` + `GrantCurrencyAsync()` (recompensas del servidor)
4. UI se suscribe a `OnSaveConflictDetected` para resolver manualmente o se queda con el más reciente

## Notas para IA
- `UGSProfileManager.Instance` es singleton estático, no necesita GameObject en escena
- `UGSEconomy.RefreshAsync()` lee de `UGSProfileManager.Instance.Profile` (Coins, Gems)
- `UGSCloudCodeManager.SubmitMatchResultAsync()` llama `GrantCurrencyAsync` + `RecordMatchResultAsync` cuando el servidor valida
- Cloud Save keys: "PlayerName", "SkinsOwned", "EquippedSkin", "TotalWins", "TotalMatches", "OwnedPacks", "EquippedPack", "Coins", "Gems", "PlayerColor", "PlayerCustomColor", "saveVersion", "cloudTimestamp"