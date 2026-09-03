# Roadmap

Estado actual: **integración base + capa propia**. Compila la arquitectura, falta
montar la escena en el Editor y validarla en dispositivo.

## Fase 0 — Validar en Quest (siguiente paso, bloqueante)

Nada de lo de abajo tiene sentido hasta que una página real se pinte y se pueda
tocar en el visor.

- [ ] Montar la escena según [03-setup-quest.md](03-setup-quest.md).
- [ ] Build y deploy; cargar una página y pulsar un enlace con el rayo.
- [ ] Medir: fps del compositor, coste de `UpdateFrame` en el Profiler, y
      legibilidad real del texto a 1,2 m.
- [ ] Ajustar `viewSize` / `textureSize` / `widthMeters` con esa medida, no a ojo.
- [ ] Fijar el commit exacto del plugin en `Packages/manifest.json`.

## Fase 1 — Navegador usable

- [ ] Pestañas (el plugin admite varias instancias simultáneas; el coste es de
      memoria y de fill rate, hay que medirlo antes de decidir cuántas).
- [ ] Marcadores e historial persistentes.
- [ ] Página de inicio propia (HTML local vía `LoadHTML`).
- [ ] Zoom (`WebView.ZoomIn`/`ZoomOut`) en el joystick.
- [ ] Descargas con UI: el plugin ya emite `onDownloadStart/Finish/Error` y
      expone progreso; hoy nadie los escucha.

## Fase 2 — Ergonomía VR

- [ ] **Panel curvo.** Requiere un raycaster propio que invierta la curvatura
      antes de normalizar la posición del puntero; con el raycaster plano de uGUI
      el puntero se desalinea del contenido. Coste real, no cosmético.
- [ ] Multipanel: varias páginas colocadas alrededor del usuario.
- [ ] `CompositionLayers` con `CaptureMode.Surface`: el panel deja de pasar por
      el render de Unity y lo compone el runtime XR. Es la única vía para que el
      texto se vea nítido de verdad, y la razón por la que el plugin trae ese
      modo.
- [ ] Manos (hand tracking) además de mandos: pellizco como toque.
- [ ] Teclado: evaluar el overlay del sistema de Meta frente a TLabVKeyborad.

## Fase 3 — Producto

- [ ] Bloqueo de anuncios/rastreadores por interceptación de peticiones.
- [ ] Sincronizar marcadores con la cuenta de CaminaFeliz.
- [ ] Modo privado real: hoy `PrivacyController` borra al salir, que **no es**
      aislamiento de perfil. Si se promete privacidad, hay que implementarla o no
      prometerla.
- [ ] Soporte de páginas de 16 KB antes de publicar en Horizon Store.

## Decisiones aplazadas, con su disparador

| Decisión | Cuándo volver a mirarla |
|---|---|
| GeckoView en vez de WebView | Si hacen falta popups controlables o consistencia entre fabricantes. Cuesta ~50 MB de APK y API 33+ |
| Backend PCVR (UnityWebBrowser) | Si el producto sale también en SteamVR. La capa de abstracción ya lo admite |
| Comprar Vuplex 3D WebView | Si hacen falta 3+ plataformas con una sola API. Sale más barato que mantener dos backends |
| Forkear Wolvic | **Si WebXR pasa a ser requisito.** No es una fase, es otro proyecto: cambia la base entera |
