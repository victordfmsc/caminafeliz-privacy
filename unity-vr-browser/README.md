# CaminaFeliz VR Browser

Navegador web en Unity para **Meta Quest standalone**, construido sobre bases
open source en lugar de desde cero.

- Motor web: `WebView` de Android (Chromium del sistema) o `GeckoView`, vía
  [TLabWebView](https://github.com/TLabAltoh/TLabWebView) (MIT).
- Base VR de referencia:
  [TLabWebViewVR](https://github.com/TLabAltoh/TLabWebViewVR) (MIT).
- XR: OpenXR + XR Interaction Toolkit.

## Por dónde empezar

| Quiero… | Documento |
|---|---|
| Saber por qué esta base y no Wolvic o CEF | [docs/01-analisis-bases-opensource.md](docs/01-analisis-bases-opensource.md) |
| Entender cómo encaja el código | [docs/02-arquitectura.md](docs/02-arquitectura.md) |
| Compilar y ejecutar en el visor | [docs/03-setup-quest.md](docs/03-setup-quest.md) |
| Ver qué falta | [docs/04-roadmap.md](docs/04-roadmap.md) |

```bash
./tools/bootstrap.sh    # comprueba el entorno y lista los pasos manuales
```

## Qué hay aquí

```
Assets/CaminaFeliz/VRBrowser/
├── Runtime/
│   ├── Core/          IWebViewBackend, UrlUtility, BrowserSession,
│   │                  SimulatedWebViewBackend   (sin dependencias del motor)
│   ├── Vr/            Panel, colocación, puntero, teclado, chrome, privacidad
│   └── Integration/   TLabWebViewBackend, scroll con joystick  (única capa acoplada)
├── Editor/            Aplicar y auditar los ajustes de build de Quest
└── Tests/             EditMode: resolución de URLs e historial
```

La idea central: **una sola frontera con el motor web**
(`IWebViewBackend`, ~15 métodos). Por encima de ella nadie sabe que existe
Android. Eso permite (a) iterar en el Editor con `SimulatedWebViewBackend`, algo
imposible con el plugin desnudo, y (b) cambiar de motor —PCVR, Vuplex, un fork de
Wolvic— escribiendo una clase, no reescribiendo el proyecto.

## Estado

Integración y capa propia escritas y documentadas. **Sin verificar en
dispositivo todavía**: no se ha compilado en Unity ni desplegado en un Quest, y
la escena está descrita en la documentación pero no montada como asset. La
lógica pura (resolución de URLs, historial) tiene tests EditMode; el resto exige
el visor. Primer paso pendiente: la Fase 0 del roadmap.

## Licencias de terceros

Ver [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Todo lo integrado es MIT;
`GeckoView`, si se activa, es MPL-2.0 y añade sus propias obligaciones.
