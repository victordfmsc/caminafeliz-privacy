# Licencias de terceros

Ningún componente de terceros está copiado en este repositorio: todos se
resuelven en tiempo de instalación (Package Manager o Gradle). Este archivo
documenta qué se trae y bajo qué condiciones.

## Instalados vía UPM (`Packages/manifest.json`)

| Componente | Licencia | Obligaciones |
|---|---|---|
| [TLabWebView](https://github.com/TLabAltoh/TLabWebView) | MIT | Conservar aviso de copyright y licencia |
| [TLabVKeyborad](https://github.com/TLabAltoh/TLabVKeyborad) | MIT | Ídem |
| Paquetes de Unity (XRI, OpenXR, Input System, TMP) | Unity Companion License | Uso ligado al Editor de Unity |

## Motores web

| Motor | Licencia | Nota |
|---|---|---|
| Android `WebView` | Componente del sistema operativo | No se distribuye: lo aporta el dispositivo. Sin obligaciones de distribución |
| `GeckoView` (opcional) | MPL-2.0 | **Si se activa**: los ficheros bajo MPL modificados deben publicarse. Se enlaza sin modificar, así que basta con el aviso. Añade ~50 MB al APK |

## Referencias consultadas, no integradas

| Proyecto | Licencia | Uso |
|---|---|---|
| [Wolvic](https://github.com/Igalia/wolvic) | MPL-2.0 | Referencia de diseño de interacción. Sin código copiado |
| [UnityWebBrowser](https://github.com/Voltstro-Studios/UnityWebBrowser) | MIT | Evaluado y descartado (sin soporte Android) |

## Sobre el código propio

`Assets/CaminaFeliz/` es original. `VrPointerInput` reproduce el cálculo de
coordenadas normalizadas del `BaseInputListener` de TLabWebView (MIT), citado en
el propio fichero, porque es la parte fácil de equivocar y conviene que
coincida exactamente con lo que el motor espera.
