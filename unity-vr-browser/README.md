# CaminaFeliz VR Browser

Navegador web **con passthrough** en Unity para **Meta Quest standalone**, que
además reproduce vídeo 360 mezclado con las cámaras del visor mediante un
deslizante de realidad/vídeo. Construido sobre bases open source en lugar de
desde cero.

- Motor web: `WebView` de Android (Chromium del sistema) o `GeckoView`, vía
  [TLabWebView](https://github.com/TLabAltoh/TLabWebView) (MIT).
- Base VR de referencia:
  [TLabWebViewVR](https://github.com/TLabAltoh/TLabWebViewVR) (MIT).
- Passthrough: Meta XR Core SDK (`OVRPassthroughLayer`).
- XR: plugin Oculus + XR Interaction Toolkit.

## Por dónde empezar

| Quiero… | Documento |
|---|---|
| Saber por qué esta base y no Wolvic o CEF | [docs/01-analisis-bases-opensource.md](docs/01-analisis-bases-opensource.md) |
| Entender cómo encaja el código | [docs/02-arquitectura.md](docs/02-arquitectura.md) |
| Compilar y ejecutar en el visor | [docs/03-setup-quest.md](docs/03-setup-quest.md) |
| **Passthrough y vídeo 360** | [docs/05-passthrough-360.md](docs/05-passthrough-360.md) |
| **Compilar el APK e instalarlo en el visor** | [docs/06-build-apk-quest.md](docs/06-build-apk-quest.md) |
| Ver qué falta | [docs/04-roadmap.md](docs/04-roadmap.md) |

```bash
./tools/bootstrap.sh              # comprueba el entorno y lista los pasos manuales
./tools/compilecheck/run.sh       # compila y pasa los tests SIN Unity (necesita dotnet)
./tools/build_quest_apk.sh --install   # compila el APK y lo instala en el Quest
```

## Cómo importarlo en Unity

**Opción A — proyecto completo (recomendada).** Descomprime
`caminafeliz-vr-browser-unity-project.zip` y abre la carpeta con Unity Hub. Trae
`Packages/manifest.json`, así que el Package Manager resuelve TLabWebView, el
Meta XR Core SDK y el XR Interaction Toolkit al abrir. Hace falta `git` en el
PATH del sistema para las dependencias por URL de Git.

**Opción B — sobre un proyecto que ya tengas.** `Assets ▸ Import Package ▸
Custom Package` y elige `CaminaFelizVRBrowser.unitypackage`. Trae solo
`Assets/CaminaFeliz/`: las dependencias las añades tú al manifest (mira
`Packages/manifest.json` de este repo). Es la vía si quieres montarlo encima de
`TLabWebViewVR`.

Los dos artefactos se regeneran con:

```bash
python3 tools/make_unity_package.py --metas --package CaminaFelizVRBrowser.unitypackage
```

## Ponerlo en el visor

```bash
tools/build_quest_apk.sh --install
```

Lanza Unity en `-batchmode`, compila el APK y lo instala por `adb`. Si Build
Settings está vacío, genera la escena prototipo antes de compilar, para que un
primer build en un clon recién bajado produzca algo que puedas ponerte.

Antes del primer build hay **dos ajustes que hay que hacer a mano una vez** —
marcar `Oculus` en XR Plug-in Management, y el `Update AndroidManifest.xml` de
Meta para el passthrough. Sin el primero la app arranca plana; sin el segundo el
deslizante no muestra realidad. Los dos, y el modo desarrollador del visor, en
[docs/06-build-apk-quest.md](docs/06-build-apk-quest.md).

**La escena se crea sola al abrir el proyecto por primera vez.** Un proyecto
clonado sin ningún asset de escena abre en una escena vacía sin título y con
Build Settings vacío, y eso se lee como "aquí no hay nada" aunque estén todos los
scripts. Para rehacerla:

```
Tools ▸ CaminaFeliz VR Browser ▸ Create or Rebuild Main Scene
Tools ▸ CaminaFeliz VR Browser ▸ Report Installed Packages
```

Monta rig, passthrough, navegador y reproductor 360 con lo que encuentre
instalado. Con el Meta XR SDK resuelto pone el `OVRCameraRig` y el passthrough
reales; sin él, los simulados — y el log dice pieza por pieza cuál es cuál.

## Qué hay aquí

```
Assets/CaminaFeliz/VRBrowser/
├── Runtime/
│   ├── Core/          IWebViewBackend, UrlUtility, BrowserSession,
│   │                  SimulatedWebViewBackend   (sin dependencias del motor)
│   ├── Vr/            Panel, colocación, puntero, teclado, chrome, privacidad
│   ├── Immersive/     Reproductor 360, mezcla realidad/vídeo, passthrough de
│   │                  Meta, detector de vídeos de la página, modo inmersivo
│   └── Integration/   TLabWebViewBackend, scroll con joystick  (única capa acoplada)
├── Editor/            Ajustes de build, compositor de escena y pipeline del APK
├── Scenes/            La escena principal (generada, versionada)
└── Tests/             EditMode: resolución de URLs e historial
```

La idea central, aplicada dos veces: **una frontera pequeña por cada cosa que
necesita hardware**. `IWebViewBackend` para el motor web, `PassthroughController`
para las cámaras del visor. Cada una tiene una implementación real y otra
simulada, así que el navegador, el reproductor 360 y el deslizante de mezcla se
pueden probar enteros en el Editor — imposible con los plugins desnudos, que no
renderizan nada fuera del dispositivo.

La mezcla realidad/vídeo la hace **el compositor del visor**
(`OVRPassthroughLayer.textureOpacity`), no un shader nuestro: se mantiene
enganchada a la cabeza aunque baje el frame rate, y esquiva el problema conocido
de las transparencias contra passthrough en underlay.

## Estado

Navegador, reproductor 360 y mezcla con passthrough escritos y documentados.

**Verificado sin Unity** (`tools/compilecheck/run.sh`): los seis ensamblados
compilan y los 29 tests pasan. Esa comprobación demuestra además que
`CaminaFeliz.VRBrowser.Runtime` compila **sin ninguna referencia a TLabWebView**,
que es la afirmación central de la arquitectura. Lo que no demuestra: los stubs
codifican mi lectura de la API de Unity y del plugin, no la API real — una firma
mal transcrita pasa ahí y falla en el Editor.

**Sin verificar en Unity ni en dispositivo**: no se ha abierto el proyecto en el
Editor ni desplegado en un Quest. La escena prototipo se genera desde el Editor y es
jugable ahí con los backends simulados; la escena completa con el navegador
sigue siendo montaje manual. La lógica pura (resolución de URLs, historial)
tiene tests EditMode; lo demás exige el visor.

Limitación que conviene saber antes de probar: **YouTube y Vimeo no se pueden
reproducir en 360** desde el navegador. Sirven el vídeo por MediaSource y la URL
que exponen no es abrible desde fuera de la página. Funcionan las URLs directas
a `.mp4` y los archivos locales. El detalle está en
[docs/05-passthrough-360.md](docs/05-passthrough-360.md).

## Licencias de terceros

Ver [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Todo lo integrado es MIT;
`GeckoView`, si se activa, es MPL-2.0 y añade sus propias obligaciones.
