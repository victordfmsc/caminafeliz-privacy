# Análisis: qué base open source usar

Objetivo: un navegador web en Unity para **Meta Quest en modo standalone** (el APK
corre dentro del visor, Android arm64), sin escribir un motor web.

Regla que descarta candidatos antes que ninguna otra: **nadie escribe un motor de
renderizado web**. Blink y Gecko son ~30 millones de líneas y un ciclo de
seguridad permanente. Lo que se elige aquí no es "qué motor", sino **cómo
incrustar un motor existente en una textura de Unity**.

## Candidatos evaluados

### 1. Wolvic (Igalia) — descartado como base, imprescindible como referencia

- Repo: <https://github.com/Igalia/wolvic> · Licencia MPL-2.0 · Muy activo
  (Gecko 1.9 / Chromium 1.3, mayo 2026).
- Heredero directo de Firefox Reality. Corre en Quest, Pico, Magic Leap 2.
  Backends Gecko y Chromium.

**Por qué no es la base:** Wolvic *es una aplicación Android completa*, no una
librería. Su UI, su gestor de sesiones WebXR y su compositor son suyos; no hay
punto de extensión para "dame la página como textura y yo la pinto en mi escena
Unity". Integrarlo significaría o bien portar nuestra UI a Android nativo
(abandonando Unity), o bien forkear Chromium y exponerle una superficie a Unity:
semanas de trabajo de plataforma antes de la primera línea de producto.

**Para qué sirve igualmente:** es la mejor referencia de diseño de interacción VR
web que existe, y la única implementación open source seria de WebXR en visor.
Si el producto acaba necesitando WebXR real (páginas inmersivas dentro del
navegador), Wolvic es el camino, y eso cambia el proyecto entero. Ver
[04-roadmap.md](04-roadmap.md).

### 2. UnityWebBrowser (Voltstro) — descartado por plataforma

- Repo: <https://github.com/Voltstro-Studios/UnityWebBrowser> · MIT · CEF/Chromium.
- Arquitectura muy limpia: proceso externo con el motor + IPC + render a
  `Texture2D`. Motor web de primera (Chromium completo).

**Por qué no:** solo tiene builds de escritorio (Windows x64, Linux x64, macOS
x64/arm64). **No hay soporte Android**, y el modelo de proceso externo no se
traslada a un APK de Quest. Es la elección correcta si el objetivo cambiara a
PCVR; por eso el proyecto se diseña con una capa de abstracción que lo admitiría
como segundo backend sin tocar nada por encima.

### 3. TLabWebView + TLabWebViewVR (tlabaltoh) — **base elegida**

- Plugin: <https://github.com/TLabAltoh/TLabWebView> · MIT · v1.0.8
- Proyecto VR de ejemplo: <https://github.com/TLabAltoh/TLabWebViewVR> · MIT
- Plugin Java: <https://github.com/TLabAltoh/TLabWebViewPlugin>

Envuelve el componente de navegador de Android (`WebView` de Chromium, o
`GeckoView` de Mozilla) y lo entrega como `Texture2D` a Unity. Tres modos de
captura: `HardwareBuffer` (más rápido), `ByteBuffer` (más estable, el que trae
por defecto) y `Surface` (para CompositionLayers).

Cubre ya: entrada táctil, teclado, descargas (incluidos blob y data URL), resize,
ejecución de JavaScript, varias instancias simultáneas, y widgets nativos
(select, date, color, alert, auth).

**Por qué gana:** es el único candidato que (a) corre en Quest standalone,
(b) entrega la página como textura, requisito para pintarla en un panel 3D, y
(c) tiene un proyecto VR de referencia ya montado con XR Interaction Toolkit y
Meta XR SDK. Es literalmente "no empezar de cero".

**Lo que no es:** no es un producto. `TLabWebViewVR` es una *demo*: un panel, un
campo de URL y un puente de teclado (`XRBrowserInputField.cs`, 130 líneas). No
tiene pestañas, ni historial navegable, ni scroll con joystick, ni marcadores, ni
gestión de privacidad, ni forma alguna de iterar en el Editor. Todo eso es
nuestro trabajo, y es el trabajo que aporta valor.

### 4. Vuplex 3D WebView — alternativa comercial de referencia

184 € por plataforma (Android/Gecko). Cubre Windows, macOS, Android, iOS,
visionOS, WebGL y UWP con una API unificada, y tiene ejemplo oficial para Quest.

No se elige por ser de pago y de código cerrado, pero marca el listón: si el
proyecto necesitara mañana el mismo navegador en escritorio y en móvil con una
sola API, comprar Vuplex sale más barato que mantener dos backends. Nuestra capa
`IWebViewBackend` mantiene esa puerta abierta: sería un `VuplexWebViewBackend`
más, sin tocar el resto.

## Veredicto

| Criterio | Wolvic | UnityWebBrowser | **TLabWebView** | Vuplex |
|---|---|---|---|---|
| Quest standalone | Sí (como app) | **No** | **Sí** | Sí |
| Página como textura en Unity | No | Sí (escritorio) | **Sí** | Sí |
| Licencia | MPL-2.0 | MIT | **MIT** | Comercial |
| Ejemplo VR listo | n/a | No | **Sí** | Sí |
| Motor web | Gecko / Chromium | Chromium (CEF) | **Chromium (WebView) o Gecko** | Chromium / Gecko |
| WebXR dentro del navegador | **Sí** | No | No | No |

**Base: `TLabWebViewVR` + `TLabWebView`, instalados como paquetes UPM**, con
nuestra capa propia encima. Wolvic queda como referencia de diseño y como plan B
si WebXR pasa a ser requisito.

## Riesgos asumidos, con nombre y apellidos

1. **Un solo mantenedor.** TLabWebView lo mantiene una persona y no publica
   releases etiquetadas (solo ramas `upm`/`master`). Mitigación: fijar el commit
   exacto en `Packages/manifest.json` en cuanto haya una versión que funcione, y
   tener un fork propio del repo por si desaparece. Al ser MIT, forkear es libre.
2. **Sin WebXR.** El plugin no expone la API WebXR. Las páginas inmersivas no
   funcionarán. Si eso es requisito, la base es la equivocada.
3. **Nada funciona en el Editor.** El plugin solo renderiza en dispositivo. Sin
   contramedidas, cada cambio de UI cuesta un build+deploy completo. Por eso el
   proyecto incluye `SimulatedWebViewBackend`.
4. **Páginas de 16 KB.** Android 15 y la Horizon Store exigen soporte de páginas
   de memoria de 16 KB para librerías nativas. El repo tiene una rama
   `support-16kb` sin fusionar: **hay que verificarlo antes de publicar**, no
   antes de prototipar.
5. **Vulkan + HardwareBuffer.** La combinación más rápida está confirmada en
   Quest pero deja el panel en negro en algunos Adreno. Y con Unity 6000.x el
   propio autor reporta `HardwareBuffer` inestable. Ver
   [03-setup-quest.md](03-setup-quest.md).
6. **GeckoView pesa.** Añade ~50 MB al APK y exige API 33+ y dependencias
   AndroidX vía `mainTemplate.gradle`. Empezamos con `WebView` (Chromium del
   sistema, 0 MB) y solo pasamos a Gecko si hacen falta sus popups o su
   consistencia entre dispositivos.
