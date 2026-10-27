# Protocolo de mensajes del modo online (plantilla para rellenar)

> Rellénalo **antes** de programar la Tarea 4. Borra las instrucciones en cursiva a medida que lo completes.

## 1. Mensajes cliente → servidor

*Un mensaje por fila. Todos llevan `requestId`. Las jugadas llevan además `expectedTurnNumber`. Nunca envíes "quién soy" ni "quién ganó".*

| Mensaje | Parámetros | Qué hace el servidor | Respuesta |
|---|---|---|---|
| `CreateMatch` | | | |
| `JoinMatch` | | | |
| `PlacePoint` | | | |
| `GetMatch` | | | |
| `Forfeit` (opcional) | | | |

## 2. Estado de la partida

*¿Qué campos tiene lo que guarda el servidor y lo que recibe cada cliente? Piensa en: código de partida, número de turno, tablero (`owner` y `level`), turno actual, fase (esperando / jugando / terminada), hora límite, tu asiento, ganador.*

## 3. Resultados de cada jugada (`outcome`)

*Rechazos normales, no excepciones: por ejemplo `applied`, `replayed`, `not_started`, `stale`, `not_your_turn`, `invalid_cell`, `cell_owned_by_rival`, `match_finished`, `conflict`. ¿Cuál mostraría la pantalla y cómo?*

## 4. Diagrama de secuencia de un turno

*Dibuja (a mano o con texto) el flujo: Jugador 1 coloca un punto → servidor → Jugador 2 se entera. Incluye el caso "Jugador 1 no recibe respuesta y reintenta con el mismo `requestId`".*

## 5. Preguntas de diseño que debes justificar

1. ¿Quién es la autoridad (servidor o anfitrión) y por qué?
2. ¿Dónde vive el estado de la partida?
3. ¿Sondeo o push para enterarse del turno del rival?
4. ¿Cuánto dura un turno y qué pasa al agotarse?
5. ¿Qué pasa si un jugador se desconecta y vuelve?
6. ¿Quién da los premios y cuándo? ¿Cómo evitas que se paguen dos veces?
