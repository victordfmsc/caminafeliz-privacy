# Puesta en marcha en Meta Quest

## Versión de Unity

`ProjectVersion.txt` fija **2022.3 LTS**. Es la combinación mejor probada de la
base: el proyecto de referencia usa 2021.3, el plugin declara soporte 2021 / 2022
/ 6000, y el propio autor **desaconseja Unity 6000.x para Quest** porque el modo
de captura `HardwareBuffer` es inestable ahí.

Si hace falta Unity 6: se puede, pero hay que poner `CaptureMode` en `ByteBuffer`
y reimportar los recursos de TextMesh Pro (dan error de compilación al abrir).

## 1. Abrir el proyecto

```bash
./tools/bootstrap.sh          # comprueba requisitos y explica los pasos manuales
```

Abre `unity-vr-browser/` con Unity Hub. En el primer arranque el Package Manager
descarga `com.tlabaltoh.webview` y `com.tlabaltoh.vkeyborad` desde Git, así que
hace falta `git` en el PATH del sistema (no basta con el de Unity).

## 2. Ajustes de build

```
Tools ▸ CaminaFeliz VR Browser ▸ Apply Quest Build Settings
Tools ▸ CaminaFeliz VR Browser ▸ Validate Setup
```

Aplica y luego audita:

| Ajuste | Valor | Qué pasa si no |
|---|---|---|
| Plataforma | Android | — |
| Color Space | **Linear** | Todas las páginas se ven lavadas |
| Min API Level | 26 | El plugin no arranca |
| Target API Level | 33 | GeckoView no funciona (WebView sí) |
| Arquitectura | ARM64 | Quest rechaza el APK |
| Scripting Backend | IL2CPP | ARM64 lo exige |
| Graphics API | OpenGLES3 | Ver la nota de Vulkan más abajo |
| Internet permission | Forzada | **Panel negro y ni un error en el log** |
| Defines | `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC`, `..._ENABLE_CAMERA`, `..._ENABLE_MICROPHONE` | El manifiesto sale sin permisos |

`Validate Setup` no modifica nada: sirve para revisar antes de un build y es
llamable desde CI (`VrBrowserProjectSetup.Collect()`).

### La trampa del permiso de Internet

El plugin de OpenXR trae activado **"Force Remove Internet Permission"** en XR
Plug-in Management. Con eso el navegador no carga nada, no da error y el panel se
queda negro. Es el fallo más caro de diagnosticar de todo el montaje.

## 3. XR

1. `Project Settings ▸ XR Plug-in Management ▸ Android` → activar **OpenXR**.
2. En OpenXR, activar el grupo de features **Meta Quest** e **Interaction
   Profiles** de los mandos táctiles Oculus.
3. Importar los *Starter Assets* del XR Interaction Toolkit desde el Package
   Manager (traen el rig y los action maps).

Se usa OpenXR + XRI en vez del Meta XR All-in-One SDK a propósito: menos
acoplamiento de versión, y el proyecto de referencia ya trae una variante del
prefab preparada para XRI.

## 4. Escena

El plugin necesita un `BrowserManager` en la escena (recolecta las instancias
nativas). Ponlo en el mismo objeto que el `EventSystem`.

```
XR Origin (XR Rig)
├── Camera Offset ▸ Main Camera
└── Left/Right Controller  ← XR Ray Interactor

EventSystem                ← BrowserManager (TLabWebView)
                             XR UI Input Module

BrowserPanel  (Canvas World Space, TrackedDeviceGraphicRaycaster)
│                VrPanelPlacement
├── Surface   (RawImage)  ← VrBrowserPanel · VrPointerInput
├── Chrome    (barra URL + botones) ← VrBrowserChrome
└── Engine    ← TLabWebViewBackend + TLab.WebView.WebView
                 VrThumbstickScroll · VrKeyboardBridge · PrivacyController
```

Cableado mínimo:

- El `WebView` de TLab necesita su `rawImage` apuntando al `RawImage` de Surface.
- `TLabWebViewBackend.m_browser` → ese mismo `WebView`.
- `VrBrowserPanel.m_backend` y `.m_surface` → el backend y el `RawImage`.
- `VrPointerInput.m_backend`, `VrThumbstickScroll.m_backend`,
  `VrKeyboardBridge.m_backend`, `PrivacyController.m_backend` → el mismo backend.
- El `Canvas` debe ser **World Space** y llevar `TrackedDeviceGraphicRaycaster`,
  o el rayo del mando no llega al panel.

**Para trabajar en el Editor:** sustituye `TLabWebViewBackend` por
`SimulatedWebViewBackend` en los mismos huecos. Todo menos el contenido web real
funciona igual.

## 5. Build

```
File ▸ Build Settings ▸ Android ▸ Build And Run
```

Con el visor en modo desarrollador y conectado por USB. Para depurar:
`Window ▸ Analysis ▸ Android Logcat`, filtrando por `TLab` y `chromium`.

## Problemas conocidos

| Síntoma | Causa probable |
|---|---|
| Panel negro, sin errores | Internet permission eliminada por OpenXR |
| Páginas lavadas de color | Color Space en Gamma |
| Panel congelado en la primera imagen | Nadie llama a `UpdateFrame()` (el backend lo hace en `Update`) |
| La barra de URL nunca se actualiza | Falta `DispatchMessageQueue()`; los eventos de página salen de esa cola |
| Panel en negro solo en algunos dispositivos | Vulkan + `HardwareBuffer`: pasa a OpenGLES o a `ByteBuffer` |
| Arrastrar no hace scroll | El `downTime` del gesto no se propaga |
| Rechazo al publicar en Horizon Store | Páginas de memoria de 16 KB — ver rama `support-16kb` del plugin |
