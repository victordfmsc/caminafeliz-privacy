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
| Ver qué falta | [docs/04-roadmap.md](docs/04-roadmap.md) |

```bash
./tools/bootstrap.sh              # comprueba el entorno y lista los pasos manuales
./tools/compilecheck/run.sh       # compila y pasa los tests SIN Unity (necesita dotnet)
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

**Prototipo en 10 minutos, sin visor:** abre el proyecto y usa
`Tools ▸ CaminaFeliz VR Browser ▸ Build 360 + Passthrough Prototype Scene`.
Genera la escena cableada con un clip de prueba; al darle a Play, el deslizante
cruza el vídeo contra una "realidad" simulada. En dispositivo solo cambia una
pieza: el controlador de passthrough.

## Qué hay aquí

```
Assets/CaminaFeliz/VRBrowser/
├── Runtime/
│   ├── Core/          IWebViewBackend, UrlUtility, BrowserSession,
│   │                  SimulatedWebViewBackend   (sin dependencias del motor)
│   ├── Vr/            Panel, colocación, puntero, teclado, chrome, privacidad
│   ├── Immersive/     Reproductor 360, mezcla realidad/vídeo, detector de
│   │                  vídeos de la página, modo inmersivo
│   └── Integration/   TLabWebViewBackend, scroll con joystick  (única capa acoplada)
├── Editor/            Ajustes de build de Quest + generador de la escena prototipo
└── Tests/             EditMode: resolución de URLs e historial

Assets/CaminaFeliz/App/
└── MetaPassthroughController.cs   (fuera de asmdef a propósito: ver el fichero)
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
