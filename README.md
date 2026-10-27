# Reaction Domino

Juego de **reacción en cadena** para dos jugadores, hecho en Unity 6 con Unity Gaming Services (UGS). Proyecto del curso **Cloud 2026**, cuyo objetivo es aprender las bases del **multijugador online por turnos** usando Claude Code.

> Estado actual: se juega **Humano vs. Bot** en local. El modo online por turnos (dos jugadores en dos equipos) está **pendiente**; ver [`docs/TAREAS.md`](docs/TAREAS.md).

## Cómo se juega

- Tablero de 6×6 (configurable por Remote Config). Cada jugador pone **un punto por turno** en una celda vacía o propia.
- Cuando una celda llega al **nivel 4** explota: pasa a 0 y lanza un punto a sus 4 vecinas, que **pasan a ser del atacante** y pueden explotar en cadena.
- Gana quien deja al rival **sin celdas** (solo cuenta cuando los dos ya han jugado al menos una vez) o, al llegar al tope de turnos (30 por defecto), quien **tenga más celdas**. Si hay igualdad, es empate.
- Fuera de la partida hay monedas y gemas, tienda de packs de animaciones, personalización de color y un bot que ajusta su dificultad según tu porcentaje de victorias.

## Requisitos

- **Unity 6000.3.24f1** (ver `ProjectSettings/ProjectVersion.txt`).
- Una cuenta de Unity con un proyecto de UGS vinculado (el `cloudProjectId` está en `ProjectSettings/ProjectSettings.asset`; si usas otro proyecto de UGS, vincúlalo desde *Edit → Project Settings → Services*).

## Cómo ejecutar

1. Clona el repositorio y ábrelo con Unity Hub (la primera vez tarda mientras importa).
2. Abre `Assets/Scenes/SampleScene.unity` (menú y login) y pulsa Play. `GameScene` se carga desde el botón de jugar.
3. Entra como **invitado** o con usuario y contraseña.

## Configuración necesaria en el dashboard de UGS

El juego **no funciona completo** sin esto. Anota aquí los valores reales de tu proyecto cuando los tengas:

| Servicio | Qué hay que crear | Estado |
|---|---|---|
| Authentication | Activar *Anonymous* y *Username/Password* | ☐ |
| Cloud Save | Se usa para el perfil del jugador; hoy todo se guarda desde el cliente en la clase *Default* (claves `PlayerName`, `Coins`, `Gems`, `TotalWins`, `TotalMatches`, `EquippedPack`, `OwnedPacks`, `SkinsOwned`, `PlayerColor`, `PlayerCustomColor`) | ☐ |
| Remote Config | `GRID_SIZE` (6), `MAX_CELL_LEVEL` (4), `BOT_DIFFICULTY` (1), `MAX_TURNS` (30) | ☐ |
| Cloud Code | Desplegar `Assets/CloudCode/ValidateMatchResult.js` (paquete *Deployment* del editor o `ugs deploy`) | ☐ |
| Leaderboards | Crear el leaderboard `ranked_mmr` | ☐ |

## Estructura del proyecto

```
Assets/
├── scrips/          ← código C# (los nombres de carpeta se pueden renombrar más adelante)
│   ├── GameLogic.cs        reglas del juego, sin MonoBehaviour
│   ├── BotAgent.cs         oponente local con 3 dificultades
│   ├── MatchController.cs  junta GameLogic + BotAgent
│   ├── GameManager.cs      vista y flujo de la partida (GameScene)
│   ├── CellVisuals.cs, BoardFx.cs, FxPacks.cs   capa visual y animaciones
│   ├── UGSAuthManager.cs, AuthUIController.cs, UIManager.cs   login y menús
│   ├── UGSProfileManager.cs, UGSEconomy.cs      perfil y monedas (Cloud Save)
│   ├── UGSRemoteConfig.cs                       configuración remota
│   └── UGSCloudCodeManager.cs                   llamada a Cloud Code
├── CloudCode/ValidateMatchResult.js   endpoint de Cloud Code
├── Scenes/          SampleScene (menú) y GameScene (partida)
└── animetion/, imagenes/, particulas/, musica y sonidos/   arte y sonido
docs/
├── TAREAS.md        qué falta y en qué orden hacerlo
└── MENSAJES.md      plantilla del protocolo de mensajes del modo online
```

## Seguridad

- **No subas credenciales ni claves** al repositorio (ni de UGS ni de ningún otro servicio). Los identificadores de proyecto de Unity que aparecen en `ProjectSettings` no son secretos.
- Las monedas, victorias y puntos de ranking deben calcularse en el **servidor** (Cloud Code), no en el cliente, y guardarse en la clase de acceso **Protected** de Cloud Save (la clase *Default* la puede escribir el propio jugador con su token). Hoy no es así; ver `docs/TAREAS.md`.

## Cómo colaborar

- Trabaja en una rama por tarea (`feature/...`, `fix/...`) y abre un Pull Request hacia `main`.
- Mensajes de commit que digan **qué cambió y por qué** (por ejemplo: `GameLogic: validar índices fuera de rango`), no `cambios pequeños`.
