# NOTA — Pack "Caldero de Bruja" (arranque desde cero)

> Nota maestra del pack. Se acordó **borrar todo lo anterior** y reconstruir
> con un plan claro. Este documento es la fuente de verdad.

---

## 1. Concepto (confirmado)

Pack especial **"Caldero de Bruja"** para el juego de matching. Un cristal
(vaso) con líquido verde de bruja que sube por niveles; a cada nivel aparece
un objeto nuevo **y se acumulan** (no se reemplazan):

| Nivel | Líquido | Aparece |
|-------|---------|---------|
| 0 | Vacío | — |
| 1 | Fino (abajo) | **2 hongos** (los más chicos) |
| 2 | Medio | **+ el Ojo** (mediano, vivo: la iris mira a los lados) |
| 3 | Lleno | **+ el Hueso** (el más grande) |
| 4 | ¡BOOM! | Los objetos salen volando en **diagonales** aleatorias |

- Los objetos **se acumulan**: en nivel 3 conviven hongo + ojo + hueso.
- Los hongos van **DE A DOS** desde el nivel 1 (como la referencia de Photoshop).
- Al explotar (nivel 4) se lanzan hongos/ojos/huesos en diagonal y desaparecen.

## 2. Arte (confirmado)

- **Cristal**: SOLO el sprite `0tapa` de la tira `Assets/imagenes/liquido animacion.png`.
  `cristal-mejor.png` NO se usa nunca.
- **Líquido**: los frames REALES de la misma tira: `1a..1e` (nivel 1),
  `2a..2e` (nivel 2), `3a..3e` (nivel 3). Se reproducen **UNA vez** al subir
  de nivel y quedan congelados en el último frame (igual que las casillas del juego).
- **Objetos en escala de grises**, el color va por tinte de código:
  - **Ojo**: esclerótica **BLANCA** + **iris ROJA**. Iris ~2/3 del tamaño del
    ojo (0.68) para que se vea el anillo blanco alrededor. Movimiento lateral
    de la iris AMPLIO (×1.6 extra a los costados).
  - **Hongo**: ya existen los coloreados `hongo_color1.png` / `Hongo_color2.png`
    (colores base horneados en el arte, en `Assets/imagenes/`). Se muestran con
    **tinte blanco** (el arte manda); el arcoíris por código es OPCIONAL y viene
    APAGADO. (Los grises `Hongo_1`/`Hongo_2png` quedan sin uso por ahora.)
  - **Hueso**: tinte blanquecino fijo.
- **Jerarquía de tamaños** (anchos de mundo, cristal ≈ 2.5 u):
  - Hongo **0.45** (el más chico) — Ojo **0.75** (mediano) — Hueso **1.10** (el grande).

## 3. Fuera del juego (ACTUALIZADO)

- **NO hay builders ni menú de Herramientas** (se rechazaron): todo se arma
  **a mano en la escena** por el usuario; la IA solo escribe scripts.
- La vista del panel es **el botón en vivo**: `SwatchCaldero` llena el líquido
  (1a..1e → 2a..2e → 3a..3e, una pasada, congela lleno) cada vez que se abre
  Tienda/Personalización. Los objetos aparecen a su nivel y quedan flotando.
- La idea vieja de RenderTexture + cámara offscreen (`PackVistaTienda`) fue
  BORRADA y descartada. La vista "estática nivel 3" también: el llenado
  animado reemplaza todo eso.
- Los paneles están inactivos en la escena guardada: en Play el caldero solo
  se ve al abrir el panel. **No es un bug.**

## 4. Lecciones del intento anterior (no repetir)

1. El builder tomaba "el sprite más grande" de la tira como líquido → era el
   `0tapa`. → Ahora se cargan **por nombre**: `0tapa` y prefijos `1/2/3`.
2. Si el prefab se recrea, **cambia de GUID** → los selectores quedan rotos.
   → El builder re-vincula los selectores y se instancia por referencia fresca.
3. Con "Auto Save" del editor, los clones de preview se guardaban y se
   multiplicaban. → `DontSave` + limpiador `[InitializeOnLoad]`.
4. Los paneles están inactivos en la escena guardada: en Play el caldero solo
   aparece al abrir Tienda/Personalización. **No es un bug.**
5. Ajustar tamaños en la instancia de escena NO cambia los paneles: hay que
   **Apply al prefab** (o dejar los tamaños en el builder como constante).
6. `FindObjectsSortMode` vive en `UnityEngine`, no en `UnityEditor`.
7. El proyecto usa el **nuevo Input System** (`Mouse.current`, nunca `UnityEngine.Input`).
8. Tamaños de burbujas: factor visible (~0.25), color **blanco** sobre el verde.
9. Los PNG nuevos entran como textura **Default**: hay que poner
   `Texture Type = Sprite (2D and UI)` en el Inspector para poder arrastrarlos
   a los slots de Image (¡Apply después!).
10. El orden de dibujo de la jerarquía manda: **el código fuerza las capas**
    (Liquid primero, Crystal último) — no depender del orden manual.
11. El script de objetos tiñe **TODAS las Images del objeto y de sus hijos**
    (`GetComponentsInChildren`): así funciona aunque el sprite esté en un hijo.
12. **Una excepción en Awake tumba TODO el botón.** El primer intento de vapor
    creaba su hijo sin `RectTransform` → excepción en `SwatchCaldero.Awake` →
    no llena, no burbujea, no activa objetos: solo se ve el PNG del último
    frame guardado. REGLA: cualquier hijo creado por código DEBE llevar su
    `RectTransform` desde el minuto uno, y si algo "se congela en un PNG",
    revisar la CONSOLA primero (una excepción roja explica todo). El vapor
    quedó EN PAUSA: se revirtió completo (el usuario lo pidió) hasta diseñar
    bien el paso.
13. **NUNCA `static` con referencias de escena.** El respiro de color de las
    burbujas guardaba el `Image` del líquido en un campo `static`: al cambiar
    de escena el Liquid viejo se destruye, la referencia queda muerta y cada
    `Update` tira excepción → burbujas cuadradas congeladas (el arreglo
    anti-cuadrados nunca llegaba a correr). REGLA: toda referencia a objetos
    de escena va en campo de INSTANCIA, y los accesos deben ser tolerantes a
    destrucción (Unity los ve como `null` y se re-buscan solos).
14. **Un recién nacido con alpha 0 NO puede morir por "alpha <= 0.001".**
    El vapor nacía invisible (el fade-in arranca en 0) y la condición de
    liberación lo mataba en su MISMO frame → nunca se vio, ni siquiera el
    modo prueba rojo. REGLA: liberar solo por fin de vida o despedida
    terminada; si algo "aparece y aparece pero nunca se ve", revisar las
    condiciones de muerte contra los valores de nacimiento.
15. **Guardar la escena DURANTE el Play hornea restos "cocidos".** Los hijos
    creados por código (VaporPuff/Burbuja) quedan en el archivo, apagados y
    con sprite muerto → el pool no los conoce y jamás se prenden. REGLA:
    limpiar restos al arrancar (y en OnValidate en editor) + regla de oro:
    guardar solo fuera del Play.
16. **El tamaño live necesita dados guardados por partícula.** Si el tamaño
    se calcula al nacer, mover la perilla en Play no cambia a los ya vivos.
    REGLA: guardar solo los dados aleatorios (factorT/factorFx/factorFy) y
    leer el resto (opciones del SwatchCaldero) EN VIVO cada frame.

## 5. Plan de reconstrucción (ACTUALIZADO — sin builders)

1. ✅ **Paso 1+2**: `SwatchCaldero.cs` (botón: tapa + llenado animado con los
   frames del juego) — funciona y aprobado. El mismo llenado se agregó a
   `ColorSwatch.cs` (los 3 swatches de color, campos `framesNivel1/2/3`).
2. ✅ **Paso 3 — objetos flotando (COMPLETO)**:
   - ✅ Nivel 1: los 2 hongos (sección 9).
   - ✅ Nivel 2: el ojo vivo con iris roja que mira (sección 9).
   - ✅ Nivel 3: el hueso grande, medio adentro y medio afuera del agua
     (técnica de las dos copias — sección 9).
   - Al aprobarlo todo: apagar los 3 modos de prueba (`detenerEnNivel1/2/3`)
     y el llenado queda completo con los tres objetos conviviendo.
3. ⏳ **Paso 4**: burbujas ✅ (sección 10) y vapor ✅ (sección 8b) — falta
   el ¡BOOM! diagonal al explotar (solo en el juego, arte `explocion.png`).
4. ⏳ **Paso 5**: vista en paneles Shop/Personalización (botón vivo, ya casi gratis).
5. ⛔ **Paso 6 REVERTIDO (emergencia)**: el port al juego se rompió y se
   deshizo COMPLETO (sección 8b-bis guarda el diseño para el reintento,
   que se hará paso a paso). El juego quedó como siempre.
6. ⏳ Compra/selección del pack (monedas, equipar) — la selección del MENÚ
   ya funciona (toggle + desequipar con los básicos).

## 6. Preguntas abiertas

1. ~~¿Frames del juego o rectángulos propios?~~ **RESUELTO**: frames del juego.
2. ~~¿2 hongos en nivel 1?~~ **RESUELTO**: sí, de a dos (referencia de Photoshop).
3. **Ojo (nivel 2)**: falta diseñar con el usuario (esclerótica blanca + iris
   roja ~0.68 + barrido lateral extra; a confirmar antes de codificar).

## 7. Acuerdo de trabajo (importante)

- **Yo (IA): SOLO código.** Scripts en `Assets/scrips/packs/`. Nada de
  builders de menú, nada de crear prefabs ni objetos desde herramientas.
- **Vos (usuario): la escena.** Creás los GameObjects, agregás los scripts,
  arrastrás los sprites y armás los prefabs a mano.
- Flujo: primero se arma **un botón suelto perfecto** en la escena; cuando
  esté bien hecho, lo convertís vos en prefab (`Selector_Caldero`).
- El script `SwatchCaldero` está hecho para eso: si le asignás a mano los
  hijos `Liquid` y `Crystal` en el Inspector, los usa; si no existen, los
  crea solo al darle Play (como ColorSwatch). Pero mejor armálos a mano
  para verlos en el editor.

## 8. Estado actual

- ✅ `Assets/scrips/packs/SwatchCaldero.cs` — botón caldero (llenado animado,
  objetos por nivel con sus franjas, modos de prueba `detenerEnNivel1/2/3`,
  auto-agrega `BurbujasCalderoUI`).
- ✅ `Assets/scrips/packs/ObjetoEnLiquidoUI.cs` — paseo libre sumergido (sección 9).
- ✅ `Assets/scrips/packs/OjoFlotanteUI.cs` — el ojo vivo (5 miradas, iris roja).
- ✅ `Assets/scrips/packs/ParteSumergidaUI.cs` — copia sumergida del hueso.
- ✅ `Assets/scrips/packs/BurbujasCalderoUI.cs` — burbujas UI (sección 10),
  con profundidad de DOS CAPAS (delante marcadas / atrás tenues).
- ✅ `Assets/scrips/packs/VaporCalderoUI.cs` — vapor en LÍNEAS (sección 8b),
  aprobado. Nace en la mitad del verde (0.5), columna concentrada (ancho
  0.5), techo relativo a la superficie del nivel.
- ✅ `Assets/scrips/packs/SwatchCaldero.cs` — botón del MENÚ, SOLO VISTA
  (llenado + burbujas + vapor + objetos). SIN selección: todo el código de
  equipar/quitar fue ELIMINADO en la reversión (emergencia). El clic solo
  hace la animación de hover/press, como siempre.
- ✅ `Assets/scrips/ColorSwatch.cs` — ORIGINAL (sin desequipar pack; eso
  también se eliminó en la reversión). Solo sus botones básicos de siempre
  + el llenado animado que ya estaba aprobado.
- ⛔ **REVERTIDO (emergencia)**: `CellVisuals.cs` y `FxPacks.cs` originales,
  `CalderoCelda.cs` borrado. El juego vuelve a funcionar como siempre.
- ⏭ Siguiente: BASE SEGURA = todo el menú aprobado. Cuando se reintente el
  port al juego (sección 8b-bis), va en PASOS CHICOS con prueba por paso.
- ✅ `Assets/scrips/ColorSwatch.cs` — llenado animado en los 3 swatches de color.
- ✅ `Assets/scrips/UGSProfileManager.cs` — debounce del guardado a la nube
  (la rueda de colores disparaba ~30 saves/seg → `request_rate_limited`).
- ✅ Arte: `hongo_color1.png`, `Hongo_color2.png` (convertidos a Sprite 2D and UI).
- ⏭ Siguiente: PROBAR el paso 6 en GameScene (sección 8b-bis: `Pack Id`
  = caldero + arrastrar sprites + BOOM volando en el juego). Después:
  paso 5 (compra/selección del pack).

## 8b. BASE SEGURA (checkpoint validado — punto de origen)

**Fecha: 2026-11-17 (ACTUALIZADO — vapor en líneas calibrado, objetos extra
nivel 2/3, respiro del tamaño). Este estado funciona y lo aprobó el usuario.
Es la BASE DE TODO: cualquier cambio nuevo arranca desde acá, y si sale mal,
se revierte a este origen como lo seguro.**

Lo que incluye la base (TODO esto probado y funcionando):

- **Llenado**: tapa + `1a..1e → 2a..2e → 3a..3e`, UNA pasada, congelado en
  el último frame. Sin loop, sin boom en paneles.
- **Objetos por nivel** (se acumulan): hongos (nivel 1), ojo vivo (nivel 2,
  iris roja por código, mira cada 0.5–1.5s), hueso (nivel 3, dos copias con
  `ZonaAgua`/RectMask2D, en sincronía total, modo cuchara, franja propia
  1.25, asoma sin salir del vidrio).
- **Tope duro global**: NINGÚN objeto sale del rect del botón.
- **Franjas por nivel**: 0.25 / 0.5 / 0.85 (calibradas al verde pintado).
- **Burbujas** (`BurbujasCalderoUI`, auto-agregada): nacen abajo, suben,
  crecen 0.85→1.15, pop en la superficie, tamaños MUY variados (0.6–1.5),
  formas no cuadradas (4 anillos generados + achatado aleatorio), alpha 0.4,
  RESPIRAN el color REAL del líquido (mezcla 0.55, fase propia), seguro
  anti-cuadrados (casilla vacía → anillo generado; self-heal por frame).
- **Profundidad de burbujas — DOS CAPAS** (aprobado 2026-11-17):
  `porcentajeDelante` (0.3) nace DELANTE de los objetos en el contenedor
  "BurbujasFrente" (justo antes del Crystal); `sesgoProfundidad` (0.7) sesga
  la transparencia por plano: las de DELANTE tienden a MARCADAS, las de
  ATRÁS a TENUES (sin ser determinista — queda orgánico).
- **Objetos extra (más dinámico, aprobado 2026-11-17)**: NIVEL 2 entra un
  **hongo grande** (`hongo3`, clon del hongo 1 con escala ~20; el usuario lo
  crea duplicando) junto al ojo; NIVEL 3 entra un **SEGUNDO OJO** (`ojo2`,
  clon) junto al hueso. La iris de cada ojo tiene SU color: primer ojo rojo,
  segundo **azul** (`Tinte Iris` por instancia, `Iris Roja Por Codigo` ✅
  fija el color al arrancar y nadie más lo toca).
- **Respiro del tamaño** (`ObjetoEnLiquidoUI`, aprobado): el hongo grande
  PALPITA chico↔grande (`respirarTamano` ✅ en su componente, amplitud 0.15,
  velocidad 0.9) — pulsa alrededor de SU escala calibrada, fase propia por
  objeto, APAGADO por defecto para los demás.
- **Vapor** (`VaporCalderoUI`, auto-agregado): EN LÍNEAS — cada
  `lineasPorSegundo` (0.6) nace una fila recta de `puffsPorLinea` (4) puffs
  a la MISMA altura, con vaivén COMPARTIDO (misma fase) y vida compartida:
  suben como bloque y mueren juntos. Nace en el CUERPO del brebaje
  (`nacerAbajo` 0.5 = la MITAD del verde del nivel, con azar), sube DETRÁS
  del vidrio (`sobreVidrio` ❌), ANCHO 0.5 (columna concentrada al centro,
  el vaivén escala con el ancho), techo RELATIVO a la superficie del nivel
  (`topeVapor` 0.25: nivel 1/2 apenas sobresalen de su franja; nivel 3 lo
  corta el borde del vidrio y llega arriba — inalterable por el valor).
  Tope duro total: el centro jamás pasa del techo ni de los bordes (menos
  la mitad del puff). Al tocar el techo se congela y se apaga ahí.
  Tamaño leído EN VIVO (dados `factorT/factorFx/factorFy` por puff).
- **Opciones de efectos en `SwatchCaldero`** (se GUARDAN con la escena y se
  reenvían en vivo cada frame): burbujas y vapor on/off, knobs finos de
  ambos, multiplicadores maestros (densidad/tamaño/velocidad/opacidad).
- **Referencias de escena NUNCA en campos `static`** (lección 13 — fue la
  causa de que las burbujas volvieran a ser cuadradas).
- **Escena se guarda FUERA del Play** (lección 15 — guardas durante Play
  hornean restos "cocidos" que no se prenden nunca).

**Regla de trabajo a partir de ahora:**
1. Antes de tocar código, la base queda intacta (no se "mejora" lo aprobado
   sin avisar).
2. Si un cambio nuevo rompe algo (burbujas cuadradas, objetos congelados,
   excepciones en consola), PRIMERO se revierte a esta base, después se
   diagnostica con la consola.
3. Cada vez que el usuario apruebe un avance nuevo, se actualiza esta
   sección 8b con la fecha (el checkpoint avanza).


## 9. Proceso del paso 3 — Nivel 1: hongos en el líquido (lo aprendido)

> **⚠️ ESTO DEBE verse TAMBIÉN EN EL JUEGO (casillas reales, `CellVisuals`)**
> — el líquido del nivel 1 con los hongos adentro, no solo en el botón de
> los paneles. La receta de abajo es la que se porta al juego en el paso 6.

### Armado (a mano, el usuario)

- Hijos del botón entre `Liquid` y `Crystal`: `Hongo1`, `Hongo2`
  (sprites `hongo_color1` / `Hongo_color2`, escala 14 y 7 — calibrada).
- Cada uno con `ObjetoEnLiquidoUI`, DESACTIVADOS en el editor; se arrastran
  a los campos `Hongo 1` / `Hongo 2` del `SwatchCaldero`.
- El llenado los enciende justo al terminar el nivel 1 (frame `1e`).

### La receta del "dentro del líquido" (5 efectos juntos)

1. **Capas por código**: `SwatchCaldero` fuerza `Liquid` PRIMERO (detrás de
   los objetos) y `Crystal` ÚLTIMO (vidrio encima de todo). No confiar en el
   orden manual de la jerarquía.
2. **Deambular**: deriva lateral (11 px) + bob vertical chico (4 px) con
   velocidades y fases aleatorias distintas → ruta orgánica, nunca un círculo,
   y los dos hongos nunca van sincronizados.
3. **Contención**: el movimiento se recorta contra el rectángulo del `Liquid`
   (`limitarAlLiquido`, `franjaAltura` ~0.24 — el nivel 1 es una franja fina).
   El objeto NUNCA sale del líquido ni flota en la superficie.
4. **Tinte sumergido**: el color se lee del Image del `Liquid` (respaldo:
   `BoardFx.Boost(colorLiquido)` del `SwatchCaldero`). NADA de verde fijo en
   código — si cambia el color del caldero, los objetos se adaptan solos
   (`fuerzaSumergido` ~0.5). El script tiñe TODAS las Images del objeto y de
   sus hijos.
5. **Opacidad** (~0.82): el objeto medio translúcido "deja pasar" el líquido
   — la transparencia vende la profundidad (idea del usuario, confirmada).

### Opciones del flotador

- `girar` ✅ (vueltas continuas, `velocidadGiro` 22°/s; negativo = al revés)
  o vaivén de lado a lado si se apaga.
- `arcoiris` opcional, APAGADO por defecto (el arte ya viene a color).
- Diagnóstico: clic derecho en el componente → "Diagnosticar Tinte".

### Modo de prueba

- `SwatchCaldero.detenerEnNivel1` ✅: el llenado se congela en `1e` (franja
  verde) para calibrar los hongos. **APAGAR** al pasar al nivel 2 (el ojo).

### Nivel 2 — el ojo (HECHO, con lo aprendido)

- **Arte**: esclerótica `base_ojo_color.png` (blanca con venas rojas) + iris
  APARTE: `ojo_iris_central/lateral/vertical.png` (ya convertidos a sprite).
- **OjoFlotanteUI**: 5 miradas — central (base normal), lateral ESPEJADA
  (izq/der) y vertical ESPEJADA (arriba/abajo); si falta un sprite, la central
  cubre todo. Cambios de mirada aleatorios cada 0.5–1.5 s, movimiento
  suavizado, barridos laterales ×1.4 y un toque de azar (nunca mecánico).
- **Iris ROJA POR CODIGO** (`Iris Roja Por Codigo` ✅, `(1, 0.13, 0.13)`):
  fijada al arrancar y nadie mas la toca — el verde del líquido NUNCA se la
  come. `Teñir Hijos` ❌ en el ojo: solo la esclerótica recibe el velo.
- **PASEO LIBRE (cambio importante)**: los objetos NO vuelven a un punto
  fijo — eligen destinos aleatorios dentro del líquido y van de uno a otro.
  Ritmo RAPIDO (el juego es rapido): hongos velocidadPaseo ~22, destinos
  0.5–1.4 s, giro 34°/s; ojo ~34, destinos 0.4–1.1 s, giro 40°/s. El bob
  mece mientras pasean. Todo por campos, calibrable en Inspector.
- **Espacio creciente**: al completar el nivel 2 la franja de paseo crece
  (`alturaFranjaNivel2` 0.6) y al 3 llega a 1 — mas liquido = mas libertad.
- **Respiro** del tinte ±25% (75%↔100%) con fase propia por objeto.

### Nivel 3 — el hueso (HECHO — la excepción: parte afuera del agua)

- **Arte**: `Huesito.png` (GRIS) → tinte blanquecino por campo `Tinte`
  (~`(1, 0.96, 0.88)`) en el ORIGINAL. El mas grande (~34), rotacion base
  ~150 (cabeza arriba), aparece al terminar el nivel 3 (3e), se acumula.
- **La tecnica de las DOS COPIAS** (parte al aire + parte sumergida):
  - `Hueso` (original): pasea y se ve NORMAL — `ObjetoEnLiquidoUI` con
    `Sumergido ❌` y `Opacidad 1` (el verde NO lo tiñe a el).
  - `ZonaAgua` (hijo del boton, DESPUES del hueso en la jerarquia, antes de
    Crystal): GameObject con **RectMask2D**. Su rect = la zona del agua:
    ancho del vidrio, borde superior = la linea del agua (se calibra a mano).
  - `Hueso_Agua` (hijo de ZonaAgua): Image `Huesito` MISMO tamaño y pivot
    (0.5, 0.5) + **`ParteSumergidaUI`** — copia al original frame a frame
    (posicion/rotacion/escala en espacio MUNDO) y solo se ve lo que queda
    dentro de ZonaAgua: la parte sumergida, teñida con el color REAL del
    liquido (con respiro) y translucida, tapando al original ahi.
  - Vinculos: `ParteSumergidaUI.Original` → Hueso (o busqueda automatica
    por nombre, sin importar mayusculas) y `ObjetoEnLiquidoUI.Copia
    Sumergida` → Hueso_Agua (la enciende con el hueso y la apaga con el).
- **Reglas de oro de las copias** (por las que nos trabamos):
  - La fisica va SOLO en el original. La copia NUNCA tiene
    `ObjetoEnLiquidoUI` (si le queda uno, ParteSumergidaUI lo elimina solo
    con warning) — si no, cada uno se mueve por su lado.
  - El nombre del hueso importa para la busqueda automatica: `Hueso`.
  - La copia se dibuja DESPUES del original (ZonaAgua despues del hueso),
    sino el original la tapa y parece que "no esta unido".
- **Movimiento tipo CUCHARA**: `Pasear Por Bordes ✅` (destinos pegados a
  los bordes/esquinas, de lado a lado), `Girar ❌` (vaiven de apoyado:
  Oscilacion ~15, Velocidad Vaiven ~0.4-0.5), Paseo ~14 (pesado), destinos
  1.0–2.5 s. `Usar Franja Propia ✅` + `Franja Altura ~1.25` → asoma por
  encima del agua pero NUNCA fuera del vidrio.
- **TOPE DURO (regla global)**: el rect del boton `Selector_Caldero` recorta
  a TODOS los objetos — nadie sale del vidrio jamas, ni siquiera el hueso.
- **Franjas por nivel ajustadas al VERDE PINTADO** (no al rect del sprite):
  nivel 2 = 0.5, nivel 3 = 0.85 (calibrar mirando el frame). El nivel 1
  respeta la de cada objeto.

> **⚠️ PENDIENTE PARA EL JUEGO (paso 6)**: TODO esto debe verse en las
> casillas reales (`CellVisuals`) — líquido por nivel + objetos paseando
> sumergidos + ojo vivo. La receta es la misma; se porta al juego cuando
> el botón esté completo.

### Puntos de calibración que surgieron

- El hongo en la superficie: su posición es la que le da el usuario — bajarlo
  en la escena (fuera de Play). Con el paseo libre ya no hay punto de reposo.
- Tinte "no se ve": verificar que el componente esté en el objeto con Image
  (o hijos con Image), `sumergido` ✅, y subir `fuerzaSumergido`. Solo se ve
  en Play.
- La iris gris/verdosa: se arregló con `Iris Roja Por Codigo` en OjoFlotanteUI.

## 10. Proceso del paso 4 — Burbujas (portadas del juego, lo aprendido)

- **Origen**: en el juego son un ParticleSystem de mundo (`CellVisuals`,
  `SetupBubbles`/`UpdateBubbleZone`, textura `BubbleTexture` generada por
  codigo). En el boton UI eso no sirve igual: se porto el COMPORTAMIENTO
  exacto a `BurbujasCalderoUI` (version UI con Images).
- **Comportamiento portado**: nacen en una franja del FONDO, suben parejas
  con deriva minima, crecen al subir (0.85 -> 1.15), REVIENTAN al llegar a
  la superficie (pop rapido), fade in 0.12s, blancas translucidas.
- **La tasa escala con el nivel** (2.5/s al nivel 3): nivel 1 pocas, nivel 3
  burbujea a full. La superficie de cada nivel usa las MISMAS franjas
  calibradas del llenado (`alturaFranjaNivel1/2/3`).
- **Mas aleatorias** (a pedido del usuario): tamano 0.6–1.5 (bien variado),
  forma no cuadrada (sx/sy independientes 0.85–1.2) + 4 variantes de anillo
  generadas por codigo (posicion/grosor/relleno distintos); cada burbuja
  elige una al azar y se queda con ella toda su vida.
- **Respiran el color del liquido** (a pedido del usuario): el color base es
  blanco lavado con el color REAL del Liquid (mezcla 0.55), y su fuerza
  sube/baja suave (0.75–1.0) con fase propia por burbuja. Al cambiar el
  color del caldero, TODAS las burbujas cambian con el.
- **Seguro anti-cuadrados** (Image sin sprite = cuadrado blanco de Unity):
  1. `ElegirSprite()` descarta casillas vacias del arreglo `Sprites` y cae
     a un anillo generado.
  2. Self-heal por frame: si una burbuja pierde el sprite, se le asigna
     otro al instante.
- **Integracion**: la auto-agrega `SwatchCaldero` (cero armado manual);
  `Configurar(Liquid)` + `SetNivel(nivel, franja)` al completar cada nivel
  + `Reiniciar()` al rellenar.
- **Bug clave resuelto (lecciones 12/13)**: el campo `static Image liquidImg`
  del respiro quedaba apuntando a un Liquid destruido al cambiar de escena
  → excepcion por frame → burbujas cuadradas congeladas. Solucion: campo de
  INSTANCIA + re-busqueda tolerante a destruccion.
- **Sprites del pack (opcional)**: `Assets/particulas/brubujas.png` es una
  plancha con burbujas reales de todos los tamanos — se puede cortar con el
  Sprite Editor (Sprite Mode: Multiple → Slice) y arrastrar los pedazos al
  campo `Sprites` de `BurbujasCalderoUI`.
- `Assets/particulas/efectos.png` (30MB) NO sirve como sprites: es UNA sola
  textura difusa con texto cocido adentro; Unity la reescala feo.

## 8b-bis. PASO 6 — Port al JUEGO (⛔ REVERTIDO — emergencia 2026-11-17)

> **⛔ ESTA SECCION ES SOLO DISEÑO GUARDADO — EL CODIGO FUE REVERTIDO.**
> Al probar el port el juego se rompio (emergencia: "regresa todo, no toques
> nada del juego"). REVERTIDO COMPLETO: `CellVisuals.cs` y `FxPacks.cs`
> vuelven a su estado original, `CalderoCelda.cs` BORRADO. El juego queda
> EXACTAMENTE como antes del paso 6. **Lo unico del juego que sobrevive:
> nada.** Todo lo del MENU (boton, vapor, burbujas, objetos, seleccion)
> queda intacto.
>
> **Leccion 17 (emergencia)**: el paso 6 se intento en UNA sola pasada
> grande (CalderoCelda + cableado de CellVisuals + auto-armado) y rompio
> el juego completo. REGLA: el port al juego se hace en PASOS CHICOS con
> prueba en Play despues de CADA uno, nunca en un bloque gigante.
>
> El diseño de abajo queda guardado para el reintento, paso a paso:
> (guardo el plan completo: CalderoCelda en mundo, pack "caldero" en el
> catalogo con arte de objetos, knobs en CellVisuals, ordenes de capa
> calderoGlass 4 / numero 5 / boom glass+1, auto-armado por ruta, y el
> toggle de seleccion en el boton del menu + desequipar con los botones
> basicos — eso ultimo ya funciona y es solo del menu).

- **`Assets/scrips/packs/CalderoCelda.cs`** (nuevo): la celda de caldero en
  mundo. Por casilla crea: hongos (nivel 1), hongo grande + ojo vivo con
  iris tintada (nivel 2), hueso cuchara + segundo ojo azul (nivel 3) —
  paseo libre rápido, tinte sumergido con el color REAL del Liquid (se lee
  cada frame), tope duro a la celda, miradas del ojo (5, con espejos).
  Vapor en líneas (misma receta: nace en la mitad del verde, ancho 0.5,
  techo relativo, congelado al morir, tamaño en vivo; textura del puff
  generada por código). **BOOM**: `Explosionar()` lanza
  `calderoBoomObjetos` sprites aleatorios (hongos/ojo/hueso) en diagonales
  LATERALES al azar, con giro y gravedad leve, que se desvanecen y
  desaparecen — se dispara desde `PlayBoom` junto al clipBoom de siempre.
- **`FxPacks.cs`**: el pack `"caldero"` ya está en el catálogo; `FxPack`
  tiene los campos de arte (`hongo1Sprite`, `hongo2Sprite`,
  `hongoGrandeSprite`, `ojoSprite`, `irisSprites`, `irisColor1` rojo /
  `irisColor2` azul, `huesoSprite`) + `HasCalderoObjects` +
  `FxPackCatalog.AttachCalderoArt` (lo llama CellVisuals en Awake).
- **`CellVisuals.cs`**: knobs nuevos bajo "Pack caldero —" (arte, tamaños,
  movimiento, tinte, vapor, boom, órdenes de capa). Cuando el pack activo
  tiene objetos (`UsingCaldero`): el vidrio sube a `calderoGlassOrder` (4),
  el número a `calderoNumeroOrder` (5) y el boom a glassOrder+1 — objetos
  order 1, vapor order 2 (detrás del vidrio, como el botón). Cada celda
  crea su `CalderoCelda` y `ApplyState` le pasa el nivel.
- **Diferencias aceptadas vs el botón** (por ser mundo, no UI):
  el hueso NO usa la técnica de dos copias (RectMask2D no existe en
  SpriteRenderer) — se tiñe igual que los demás y asoma con su franja
  extendida; los objetos aparecen al CAMBIO de nivel (sin animación
  frame a frame; el llenado animado ya lo hace el clip del liquido).
- **LO QUE HACE EL USUARIO**: NADA 🎉 — **AUTO-ARMADO** (`CellVisuals.
  autoArmarPackCaldero` ✅): el codigo llena SOLO los campos del pack
  caldero con el arte del proyecto por RUTA (`Assets/imagenes/`:
  hongo_color1, Hongo_color2, base_ojo_color, ojo_iris_central/lateral/
  vertical, Huesito) y pone `Pack Id = caldero`. Corre en Awake (Play lo
  aplica al toque) y en OnValidate en modo edicion; al guardar la escena
  las referencias quedan serializadas para el build. Solo respeta lo
  asignado a mano. El glassPrefab y los 4 clips ya venian del clasico.
  1. El BOOM se prueba jugando: subir una casilla a nivel 4 → explosión
     del clip + objetos volando en diagonales que desaparecen.
- ⛔ **SELECCIÓN DEL PACK TAMBIÉN REVERTIDA (emergencia)**: el toggle del
  botón (`SeleccionarPack` + estado "equipado" en Update) y el desequipar
  de los botones básicos (`desequiparPackAlClic`) fueron ELIMINADOS por
  completo — cero rastro de caldero en scripts que el juego comparte
  (`ColorSwatch`, `SwatchCaldero`). El diseño queda en esta nota para el
  reintento. `UGSProfileManager.EquippedPackId` sigue existiendo (es del
  sistema de perfiles, de antes) y `BoardFx` cae a "clasico" si el valor
  guardado es un pack que no existe.
- Los knobs se leen EN VIVO cada frame (patrón SwatchCaldero): ajustar en
  Play funciona (tamaños, vapor, velocidades).

### 8b-ter. DISEÑO V2 — PACKS INDIVIDUALISTAS POR JUGADOR (aprendizaje de la emergencia)

> Diagnóstico del usuario (CORRECTO, confirmado con el código) de por qué
> el port v1 se sentía mal aunque "funcionara":
>
> 1. **El pack es GLOBAL y compartido**: `BoardFx.Current` devuelve UN
>    pack para TODO el tablero y `CellVisuals` lo usa en todas las
>    casillas — tuyas y del bot. Solo el TINTE cambia por dueño
>    (`LiquidColorFor(owner)`). Por eso el enemigo tambien tenia hongos,
>    ojos, huesos y burbujas.
> 2. **El verde no aparecia**: `colorLiquido` vivia SOLO en el boton del
>    menu (`SwatchCaldero`). El pack del juego NO trae color propio — el
>    liquido de la casilla se tinta con el color del DUEÑO (rosa/naranja).
>    Leccion: un pack especial DEBE llevar su propio color de liquido.
> 3. **Objetos deformes**: el menu es UI (canvas, fracciones del tamaño
>    del BOTON); el juego es world-space (celdas de 1.2 unidades). Las
>    proporciones calibradas en el canvas no significan nada en la celda.
>    Leccion: las proporciones de un pack se calibran DENTRO de una
>    celda del juego (una celda de prueba), nunca copiadas del menu.

**La meta a futuro (decisión del usuario)**: cada jugador elige SU color
Y SU pack para diferenciarse. Pack del jugador 1 ≠ pack del jugador 2.

**Cómo funciona (el mecanismo ya existe a medias)**:
- `CellVisual` ya guarda `owner` y `SyncAll` re-tinta por dueño hoy.
- Extender "color por dueño" → "PACK por dueño":
  - casilla de P1 → pack equipado de P1 (su `EquippedPackId`, perfil ☁️)
  - casilla de P2 (bot) → pack definido para el bot (ej: siempre "clasico")
  - conquista → `owner` cambia → la casilla se reconstruye con el pack
    del NUEVO dueño (sobrescritura, como pidió el usuario).

**Plan de reintento por pasos (lección 17 aplicada — probar en Play tras
CADA paso; con "clasico" no debe verse NINGUN cambio)**:
- **Paso A**: `FxPack` gana `liquidColor` propio; `CellVisuals` tintea con
  el color del pack en vez del global.
- **Paso B**: `BoardFx.Current` → `BoardFx.PackFor(owner)`; ambos jugadores
  = "clasico" (cero cambio visible).
- **Paso C**: registrar "caldero" con arte calibrado EN el juego (una celda
  de prueba primero); solo P1 lo equipa → sus casillas caldero, las del bot
  clasicas. `FxPackCatalog.RegisterPack` ya existe para esto.
- **Paso D**: sobrescritura del pack por conquista (ya re-tinta hoy; se
  extiende al pack completo).

## 11. Proceso del paso 4b — Vapor (APROBADO, lo aprendido)

- **Concepto final (a pedido del usuario)**: humo en **LÍNEAS RECTAS** —
  cada línea nace junta (fila de puffs a la MISMA altura), sube como
  bloque con vaivén COMPARTIDO (misma fase) y vida COMPARTIDA: muere junta.
  Respiro y forma siguen individuales dentro del bloque (textura orgánica).
- **Nacimiento**: DENTRO del líquido verde (`nacerAbajo` = 0.12 del alto,
  bajo la superficie de la franja calibrada del nivel).
- **Capa**: DETRÁS del vidrio (`sobreVidrio` ❌) — el vapor dentro del pote.
- **Techo por nivel (la clave)**: `tope = superficie del nivel + techo×alto`,
  y JAMÁS pasa del borde del vidrio. Nivel 1 y 2: apenas sobresale de su
  franja; nivel 3: la superficie ya está casi al borde → el vidrio lo corta
  → llega arriba SIEMPRE, sin importar el valor de `topeVapor` (0.25).
  Una sola perilla escala los 3 niveles.
- **Tope duro total**: centro del puff clavado a techo y bordes (menos la
  mitad de su tamaño); al tocar el techo se CONGELA y se desvanece ahí
  (antes seguía subiendo durante la despedida y salía del botón).
- **Tamaño EN VIVO**: dados `factorT/factorFx/factorFy` guardados por puff;
  el resto se lee cada frame de las opciones → `Vapor Tamano` y amigos
  responden al instante en Play (lección 16). `nacerAbajo` solo afecta
  puffs nuevos.
- **El bug que ocultó el vapor días** (lección 14): la condición de
  liberación `alpha <= 0.001` mataba al puff en su primer frame (el
  fade-in nace en alpha 0) → nacía, se pintaba y moría invisible, en loop.
  Arreglo: liberar solo por fin de vida o despedida terminada.
- **Restos "cocidos"** (lección 15): escena guardada durante Play → puffs/
  burbujas horneadas apagadas con sprite muerto que nunca se prenden.
  Limpieza al arrancar (`LimpiezaDeRestos`) + OnValidate en editor.
- **Diagnóstico que funciona**: `modoPruebaVapor` (puffs gigantes ROJOS) +
  logs `[VaporCalderoUI] nivel X — vapor encendido` + leer la consola roja
  PRIMERO (lección 12).
- **El usuario edita SOLO el `SwatchCaldero`**: los campos del componente
  `VaporCalderoUI`/`BurbujasCalderoUI` se pisan cada frame con las opciones
  — moverlos en Play no hace nada.
- **Gotcha serializado**: los defaults del código NO actualizan la escena
  del usuario — avisarle SIEMPRE qué valor poner a mano (`Vapor Techo` 0.25).
