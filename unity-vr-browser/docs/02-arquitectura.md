# Arquitectura

## La decisión que sostiene todo lo demás

Nada por encima de `IWebViewBackend` sabe que existen Android, JNI o
TLabWebView. Toda la comunicación con el motor pasa por una interfaz de ~15
métodos.

```
        VrBrowserChrome        VrPointerInput      VrThumbstickScroll
        (URL, botones)         (rayo → toque)      (joystick → scroll)
               │                     │                     │
               └──────────┬──────────┴──────────┬──────────┘
                          ▼                     ▼
                   VrBrowserPanel         BrowserSession
                   (tamaño, DPI)          (historial, búsqueda)
                          │
                          ▼
                  ┌───────────────┐
                  │IWebViewBackend│   ← única frontera con el motor
                  └───────┬───────┘
              ┌───────────┴────────────┐
              ▼                        ▼
    TLabWebViewBackend        SimulatedWebViewBackend
    (Quest, Android)          (Editor, sin dispositivo)
```

Esto compra dos cosas concretas:

**1. Se puede trabajar en el Editor.** El plugin de Android no pinta nada fuera
del dispositivo: en el Editor el panel es un rectángulo negro. Sin
`SimulatedWebViewBackend`, cada ajuste de la barra de URL o del mapeo del rayo
cuesta un ciclo build → deploy → ponerse el visor (varios minutos). Con él, el
backend simulado dibuja una rejilla, desplaza esa rejilla al hacer scroll y pinta
un punto donde apuntas: si el punto no cae donde apuntas, el mapeo está mal, y lo
ves a 90 fps sin visor. Solo el contenido web real necesita dispositivo.

**2. Cambiar de motor no es reescribir.** Pasar a PCVR
(`UnityWebBrowser`/CEF), comprar Vuplex, o migrar a un fork de Wolvic, es
escribir una clase nueva. Los componentes VR, la UI y la lógica de navegación no
se tocan.

## Qué aporta cada pieza sobre la demo original

| Componente | Qué resuelve | Por qué no estaba resuelto |
|---|---|---|
| `IWebViewBackend` / `WebViewBackend` | Frontera única con el motor | La demo llama al plugin desde la UI |
| `TLabWebViewBackend` | Bombeo por frame, `downTime` del gesto, borrado de datos por motor | El plugin deja las tres cosas al host y no avisa |
| `SimulatedWebViewBackend` | Iterar sin dispositivo | Imposible con la demo |
| `BrowserSession` | `CanGoBack`/`CanGoForward`, historial, buscar vs. navegar | El motor no expone si hay historial |
| `UrlUtility` | "unity.com" → URL, "cómo hacer X" → búsqueda | La demo carga literalmente lo que escribas |
| `VrBrowserPanel` | Píxeles CSS / píxeles de textura / metros, por separado | Números mezclados y fijos en el prefab |
| `VrPanelPlacement` | Seguimiento perezoso + recentrar | El panel está clavado en la escena |
| `VrPointerInput` | Rayo XR → toque web, agnóstico del motor | Acoplado al plugin |
| `VrThumbstickScroll` | Leer sin arrastrar la página con el gatillo | No existe |
| `VrKeyboardBridge` | Cualquier teclado (TLab, XRI, sistema Meta, físico) | Acoplado al teclado XRI de los samples |
| `PrivacyController` | Borrar caché/cookies/historial, DNT, borrado al salir | No existe |
| `VrBrowserProjectSetup` | Ajustes de build aplicables y auditables | Una checklist de clics en el README |

## Detalles que cuesta caro descubrir a mano

Tres cosas que el plugin exige y no documenta en su API, resueltas dentro de
`TLabWebViewBackend`:

- **El motor no se bombea solo.** Hay que llamar a `UpdateFrame()` y
  `DispatchMessageQueue()` cada frame desde fuera. Sin lo primero el panel es una
  imagen congelada; sin lo segundo **no se dispara ni un solo callback de
  página** (`onPageStart`/`onPageFinish` salen de esa cola). Solo aparece en los
  scripts de ejemplo, no en la interfaz.
- **El `downTime` del gesto.** `TouchEvent()` devuelve la marca de tiempo del
  DOWN, y ese mismo valor tiene que viajar en todos los MOVE y en el UP. Si no,
  Android ve toques sueltos en vez de un arrastre, y la página no hace scroll ni
  selecciona texto.
- **`Reload()` no existe.** Se implementa con `EvaluateJS("location.reload();")`,
  que además preserva la restauración de scroll, cosa que un `LoadUrl(GetUrl())`
  destruiría.

## Ensamblados

| Ensamblado | Depende de | Motivo |
|---|---|---|
| `CaminaFeliz.VRBrowser.Runtime` | uGUI, TextMeshPro | Todo lo agnóstico del motor. **No** referencia TLab: compila sin el plugin instalado |
| `CaminaFeliz.VRBrowser.Integration` | Runtime, `TLab.WebView.Runtime`, Input System | Todo el acoplamiento vive aquí |
| `CaminaFeliz.VRBrowser.Editor` | Runtime | Solo Editor |

Esa separación es la que hace verificable la afirmación "el motor está aislado":
si alguien mete una llamada a TLab en la capa VR, **no compila**.

## Sobre paneles curvos

Un panel curvo se lee mejor, y es lo primero que se pide. No está implementado a
propósito: el raycast de uGUI es plano, así que curvar la malla desalinea el
puntero del contenido — apuntas a un enlace y pulsas el de al lado. Hacerlo bien
exige un raycaster propio que invierta la curvatura antes de normalizar la
posición. Está en el roadmap con ese coste explícito, no olvidado.
