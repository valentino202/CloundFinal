# TIENDA DE PACKS — Manual y plantilla de prefab

> Documento del sistema de compra (Paso 5). Complemento de `NOTA_MenuA_Juego.md`.
> Objetivo: que el caldero se pueda **comprar**, y que esta tarjeta sirva
> de **plantilla (prefab) para cualquier pack futuro** (acuario, metal, etc.).

---

## 1. IDEAS CLAVE (no olvidar)

1. **NO se copia el pack**. El pack ya existe como DATOS en `FxPackCatalog`
   (id `"caldero"`). La tienda solo modifica el PERFIL del jugador.
2. **Un solo flujo de datos**:
   ```
   Comprar → SpendCurrencyAsync(precio) → UnlockPackAsync(id) → Cloud Save
   Personalizacion → EquipPackAsync(id) → Cloud Save
   Juego → lee EquippedPackId → muestra el pack
   ```
3. **La propiedad es del PERFIL**, no de la tarjeta visual:
   `Profile.OwnedPackIds` (guardado en Cloud Save clave `OwnedPacks` + local).
   Cada cuenta (PlayerId) tiene lo suyo. Otra cuenta NO tiene el pack.
4. **Economía intacta**: la tienda NO toca `UGSEconomy` ni los textos
   generales (`CoinsText`/`GemsText` de UIManager). Solo descuenta del saldo
   global con `SpendCurrencyAsync` (que ya guarda en la nube) y muestra el
   PRECIO en los textos de la tarjeta.
5. **Sin emojis en TMP**: la fuente LiberationSans no tiene ✅ (lo reemplaza
   por □ y warning). Mensajes de estado solo texto plano.

---

## 2. SCRIPT `TiendaPacks.cs` (lo que ya está hecho)

### Campos del Inspector

| Campo | Qué es | Ejemplo |
|---|---|---|
| `packId` | Id del pack en el catálogo | `caldero` |
| `nombrePack` | Nombre展示 | `Caldero de Bruja` |
| `precioMonedas` | Costo en monedas | `1000` |
| `precioGemas` | Costo en gemas (0 = no requiere) | `500` |
| `botonComprar` | Button "COMPRAR" | `Boton_comprar` |
| `textoBoton` | TMP del botón → COMPRAR/COMPRADO | |
| `textoEstado` | TMP de mensajes ("Disponible...", "insuficientes", "COMPRADO...") | `comprado` |
| `textoMonedas` / `textoGemas` | TMP que muestran el **PRECIO** (no el saldo) | `CoinsText_cald` / `GemasText_cald` |
| `textoPrecioMonedas` / `textoPrecioGemas` | Etiquetas fijas de precio (monedas/gemas) | `oro_cald` / `diamante_cald` |
| `selectorCaldero` | GameObject del pack animado (vista previa) | `Selector_Caldero` |
| `slotEnTienda` | RectTransform donde se muestra en la tienda | slot dentro de `ShopPanel` |
| `slotEnPersonalizacion` | RectTransform original en `CustomizationPanel` | |

### Métodos públicos
- `Comprar()` → se conecta solo en `Start()` al `onClick` del botón (y también se puede llamar desde un botón OnClick: el flag `comprando` evita doble compra).
- `ActualizarUI()` → refresca precio, botón, estado. Llamar al abrir (`OnEnable`).
- `DevolverCaldero()` → **estático**. Lo llama `UIManager.ShowCustomization()` para devolver el pack a su slot al salir de la tienda. Funciona aunque la tienda esté apagada (referencia estática de instancia en `Awake`).

### Comportamiento importante
- **Reembolso**: si `UnlockPackAsync` falla después de cobrar, devuelve las monedas/gemas (`GrantCurrencyAsync`). Nunca se pierde dinero.
- **Estado persistente**: una vez comprado, siempre dice "COMPRADO - equipable en Personalizacion".
- **Saldo insuficiente**: "Monedas/gemas insuficientes", no se descuenta nada.
- **Vitrina**: en la tienda el pack se ve **siempre** (aunque no lo tengas); en personalización está oculto hasta comprarlo. Lo decide `SwatchCaldero.EnTienda` (compara su padre con `slotEnTienda`).
- **NO se puede equipar en la tienda**: `SwatchCaldero.OnPointerUp` y `SeleccionarPack` hacen `return` si `EnTienda`. Solo se selecciona en Personalización.

---

## 3. CÓMO ARMAR LA TARJETA (prefabs futuros) — PASO A PASO

Esta tarjeta está pensada para **duplicarla y cambiar 4 cosas** por pack nuevo.

### 3.1 Estructura recomendada del GameObject tarjeta

```
Tienda_Caldero                      ← GameObject con TiendaPacks
├── SlotVistaPrevia (RectTransform) ← slotEnTienda
├── TextoBoton (TMP)                ← textoBoton
├── Boton_comprar (Button)          ← botonComprar
│   └── Label (TMP)
├── TextoEstado (TMP)               ← textoEstado
├── PrecioFila
│   ├── oro_cald (TMP/Image)        ← textoPrecioMonedas
│   ├── CoinsText_cald (TMP)        ← textoMonedas (PRECIO)
│   ├── diamante_cald (TMP/Image)   ← textoPrecioGemas
│   └── GemasText_cald (TMP)        ← textoGemas (PRECIO)
└── (el pack animado se MUEVE acá desde CustomizationPanel)
```

### 3.2 Hacer el prefab (lo hacés vos en Unity)
1. Armá la tarjeta como arriba y poné el componente `Tienda Packs`.
2. Arrastrá todos los campos del Inspector.
3. **Importante**: el `selectorCaldero` y los slots son referencias a objetos
   **fuera de la tarjeta** (viven en `CustomizationPanel`). Para que el prefab
   sea autocontenido, lo ideal es que el **pack animado también sea un prefab**
   y se instancie en ambos paneles — pero hoy se comparte el MISMO GameObject
   moviéndolo (ver §5, limitación conocida).
4. Guardá la tarjeta como prefab (`Assets/Prefabs/Tienda/Tienda_Caldero.prefab`).

### 3.3 Duplicar para otro pack (ej. Acuario)
1. Duplicá el prefab → `Tienda_Acuario`.
2. Cambiá en el Inspector:
   - `packId` = `acuario`
   - `nombrePack` = `Acuario`
   - `precioMonedas` / `precioGemas`
   - los textos de precio y el `selectorAcuario` +
     `slotEnTienda_Acuario` / `slotEnPersonalizacion_Acuario`
3. Registrá el pack en `FxPackCatalog` (igual que se hizo con `caldero`).
4. En `UIManager.ShowCustomization()` agregá la devolución del pack nuevo:
   ```csharp
   TiendaPacks.DevolverCaldero();          // existente
   TiendaPacks.DevolverPack("acuario");    // ← nuevo (ver §5 mejora futura)
   ```

---

## 4. FLUJO DE PRUEBA (demo Cloud Save)

1. Menú → **Tienda** → ver el pack animado + PRECIO 1000/500 + saldo arriba.
2. Clic **COMPRAR** → se descuenta del saldo global (ej. 2160→1160, 1070→570).
3. Consola: `[Tienda] Pack 'caldero' comprado y guardado en la nube...`
   + `[Profile] Guardado local OK.` + `[Profile] Guardado en nube OK.`
4. Estado → "COMPRADO - equipable en Personalizacion".
5. **Personalización** → el pack aparece (antes oculto). Clic → equipa.
6. Salir y volver a entrar → sigue comprado/equipado ☁️.
7. **Otra cuenta** (invitado/registro nuevo) → NO tiene el pack → dice COMPRAR
   y el pack está oculto en personalización. ✅ Esto demuestra Cloud Save por jugador.

---

## 5. LIMITACIONES CONOCIDAS / MEJORAS FUTURAS

- **Un solo `TiendaPacks.instancia` estático**: hoy `DevolverCaldero()` es
  estático y apunta a una sola tarjeta. Si hay varias tarjetas en la tienda,
  hay que cambiarlo a un registro por `packId`
  (ej. `Dictionary<string, TiendaPacks>` con `Registrar(this)` en `Awake` y
  `DevolverPack(string id)`). Es la mejora Nº1 para hacer el prefab realmente
  multi-pack.
- **El pack animado se comparte** (se mueve entre paneles) en vez de
  instanciarse dos veces. Ventaja: no hay copia ni desincronización.
  Desventaja: el prefab de la tarjeta no es 100% autocontenido.
- **Los mensajes de estado** se comparan con `Contains("insuficientes")` para
  no sobrescribir el aviso — frágil si se cambia el texto. Mejor: un enum de estado.
- **Precio fijo en el Inspector** de la tarjeta (no en `FxPack` / Remote Config).
  Para producción, mover el precio a `FxPack` o Remote Config.

---

## 6. ARCHIVOS RELACIONADOS

- `Assets/scrips/packs/TiendaPacks.cs` — lógica de compra.
- `Assets/scrips/packs/SwatchCaldero.cs` — vista previa, bloqueo y equipado.
- `Assets/scrips/UGSProfileManager.cs` — `SpendCurrencyAsync`,
  `UnlockPackAsync`, `EquipPackAsync`, `OwnsPack` (Cloud Save).
- `Assets/scrips/UIManager.cs` — `ShowShop`, `ShowCustomization`
  (llama a `TiendaPacks.DevolverCaldero()`).
- `NOTA_MenuA_Juego.md` — manual general de menú → juego.
