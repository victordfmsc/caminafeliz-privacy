# Passthrough + vídeo 360

Objetivo del prototipo: abrir un vídeo 360 y verlo mezclado con las cámaras del
visor, con un deslizante que va de **solo vídeo** a **solo realidad**.

## Camino más corto (10 minutos, sin visor)

Abre el proyecto: **la escena se genera sola la primera vez**. Para rehacerla:

```
Tools ▸ CaminaFeliz VR Browser ▸ Create or Rebuild Main Scene
Tools ▸ CaminaFeliz VR Browser ▸ Report Installed Packages
```

Monta rig, passthrough, navegador y reproductor 360 con lo que encuentre
instalado, y el log te dice pieza por pieza qué es real y qué es simulado. El deslizante funciona: cruza el vídeo contra un color
de "habitación" simulada. No es tu salón, y no pretende serlo — sirve para
validar que la curva de mezcla se siente bien y que el cableado es correcto, que
son las dos cosas que cuestan ciclos de build.

Si el Meta XR SDK está resuelto, el compositor pone ya el `OVRCameraRig` y el
`MetaPassthroughController` reales; si no, deja los simulados y lo dice en el
log. Volver a lanzar `Create or Rebuild Main Scene` tras instalar el SDK cambia
las piezas.

## Cómo se hace la mezcla

**El compositor del visor hace la mezcla, no un shader nuestro.**
`OVRPassthroughLayer.textureOpacity` va de 0 (solo vídeo) a 1 (solo realidad), y
`RealityMix` la escribe directamente.

Eso importa por tres razones concretas:

1. El compositor mezcla **después de la reproyección**, a la frecuencia del
   panel. La realidad queda clavada a tu cabeza aunque nuestro frame rate caiga,
   cosa que un fundido hecho en la escena no consigue.
2. No cuesta un dibujado transparente a pantalla completa.
3. **Esquiva un problema conocido**: materiales transparentes mezclados contra un
   passthrough en *underlay* fallan de formas feas —rectángulos negros—,
   especialmente bajo URP. Por eso el proyecto está en Built-in y el passthrough
   va en **overlay**.

Lo que `RealityMix` añade encima es la otra mitad del fundido. Subir el
passthrough a secas apila una habitación luminosa sobre un vídeo luminoso, y el
centro del deslizante se convierte en una sopa ilegible. Así que el vídeo se
atenúa exactamente lo mismo que se está cubriendo (`_Exposure` del skybox).

**Contrapartida del overlay:** la barra de control también se tiñe según sube el
deslizante. Si molesta, el campo `Placement` del `MetaPassthroughController` a `Underlay`,
cámara en Solid Color con alfa 0 y post-procesado desactivado. Está previsto,
pero es el camino con más aristas.

## Cómo se reproduce el 360

`Video360Player` usa `VideoPlayer` → `RenderTexture` → **skybox**, con el shader
de serie `Skybox/Panoramic`. Ese shader ya resuelve 360 vs 180, mono vs
over-under vs side-by-side y la selección por ojo, así que el prototipo **no
necesita ningún shader propio**.

El layout se adivina del nombre del archivo (`_TB`, `_SBS`, `180`, `_LR`), porque
los productores lo codifican ahí mucho más fiablemente que en los metadatos del
contenedor. Si sale mal se ve al instante: imagen doble o mundo aplastado. Se
puede forzar a mano con `Video360Player.Layout`.

## Sacar el vídeo del navegador

`WebVideoDetector` inyecta un script en la página que recorre el DOM —incluidos
los *shadow roots*, que es donde se esconden casi todos los reproductores— y
devuelve las fuentes `<video>` por el puente `window.tlab.unitySendMessage`.
`ImmersiveModeController` elige la mejor candidata (prioriza fotogramas 2:1, que
es lo que parece el material equirectangular) y la manda al reproductor.

### Lo que esto NO puede hacer, dicho claro

**YouTube y Vimeo no van a funcionar.** No es un fallo a corregir: esos sitios
sirven el vídeo por DASH/HLS segmentado sobre MediaSource, y el `currentSrc` que
la página expone es un handle `blob:` que solo esa página puede leer. El
`VideoPlayer` de Unity no puede abrirlo. Hacerlo funcionar significa integrar un
extractor de streams, que es otro proyecto y con sus propias preguntas legales.

`WebVideoDetector` detecta ese caso y lo reporta por `VideosBlocked` en vez de
dejar que reviente más tarde en el reproductor.

**Lo que sí funciona:** URLs directas a `.mp4` (que es como se distribuye casi
todo el material 360 de prueba), páginas que sirven ficheros progresivos, y
archivos locales del visor.

Otra limitación: el puente `window.tlab` **solo existe en el motor `WebView`**,
no en `GeckoView`. Con Gecko seleccionado, el detector no encuentra nada.

## Lo que genera el compositor

```
OVRCameraRig                    ← OVRPassthroughLayer + MetaPassthroughController
Directional Light
EventSystem                     ← BrowserManager (TLab)
360 Video Player                ← VideoPlayer + Video360Player + PrototypeAutoPlay
Reality Mix                     ← RealityMix (passthrough + player)
Immersive Bar (Canvas)          ← Slider → RealityMix.SetMix, play/pausa, presets
Browser                         ← VrPanelPlacement, VrKeyboardBridge, PrivacyController
├── Panel (Canvas World Space)
│   ├── Surface (RawImage)      ← VrBrowserPanel + VrPointerInput
│   ├── Chrome                  ← VrBrowserChrome (atrás/adelante/recargar/inicio)
│   ├── Ver en 360              ← se enciende solo si la página tiene vídeo servible
│   └── Engine                  ← TLab WebView + TLabWebViewBackend (o simulado)
└── Web Video Detector          ← WebVideoDetector (nombre ÚNICO en la escena)
Immersive Mode Controller       ← ata las dos mitades
```

La barra de URL no se genera: un `TMP_InputField` necesita los recursos de
TextMesh Pro importados y una fuente, y un campo a medio montar es peor que
ninguno. Añádelo a mano y asígnalo a `VrBrowserChrome.m_addressField`; el resto
del chrome funciona sin él.

Dos detalles que cuestan una tarde si se pasan por alto:

- El `WebVideoDetector` recibe las respuestas por `UnitySendMessage`, que
  **direcciona por nombre de GameObject**. Ese nombre tiene que ser único en la
  escena y el objeto tiene que estar activo, o las respuestas se pierden en
  silencio.
- Al entrar en modo inmersivo hay que pausar el vídeo de la página o **suena dos
  veces**. `ImmersiveModeController` lo hace, pero solo si tiene asignado el
  backend.

## Rendimiento

Sin medir todavía. Los números a vigilar en el Profiler y en OVR Metrics:

- Decodificación de vídeo: 4K30 h.264 es apuesta segura en Quest 2/3. 8K es
  donde empiezan los problemas y hay que medirlo, no suponerlo.
- Con la `RenderTexture` a 4096×2048 el coste de memoria ya es notable; bajarla
  es la primera palanca si el frame rate no llega.
- El passthrough tiene su propio coste en el compositor, y solo aparece con
  `RealityMix` por encima de 0.
