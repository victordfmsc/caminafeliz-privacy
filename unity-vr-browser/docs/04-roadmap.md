# Roadmap

Estado actual: **integración base + capa propia + reproductor 360 con mezcla de
passthrough**. Compila la arquitectura y la escena prototipo se genera desde el
Editor; falta validarla en dispositivo.

El producto es un **navegador con passthrough que además reproduce vídeo 360
mezclado con la realidad**. Esa mezcla es la función distintiva, así que se
valida antes que cualquier función de navegador.

## Fase 0 — Validar el prototipo en Quest (siguiente paso, bloqueante)

Nada de lo de abajo tiene sentido hasta que esto funcione en un visor real.

- [ ] `Build 360 + Passthrough Prototype Scene` y comprobar en el Editor que el
      deslizante cruza vídeo contra realidad simulada.
- [ ] Añadir `OVRCameraRig` + `OVRPassthroughLayer` + `MetaPassthroughController`
      y sustituir el controlador simulado.
- [ ] Build a dispositivo: **un .mp4 360 directo reproduciéndose con el
      deslizante moviendo la mezcla**. Ese es el hito del prototipo.
- [ ] Medir: fps del compositor, coste de decodificación a 4K y a 8K, memoria de
      la `RenderTexture`. Ajustar resolución con esa medida, no a ojo.
- [ ] Comprobar si el tinte del overlay sobre la barra de control molesta lo
      suficiente como para pasar a underlay.
- [ ] Fijar el commit exacto del plugin web en `Packages/manifest.json`.

## Fase 0.5 — Unir navegador y 360

- [ ] Montar la escena del navegador (docs/03) junto a la del reproductor.
- [ ] Verificar el puente `window.tlab` en dispositivo: abrir un .mp4 360 directo
      y que aparezca el botón de "ver en 360".
- [ ] Comprobar que pausar el vídeo de la página evita el audio doble.
- [ ] Mensaje claro y honesto cuando la página use MediaSource (YouTube), en vez
      de un botón que no hace nada.

## Fase 1 — Navegador usable

- [ ] Pestañas (el plugin admite varias instancias simultáneas; el coste es de
      memoria y de fill rate, hay que medirlo antes de decidir cuántas).
- [ ] Marcadores e historial persistentes.
- [ ] Página de inicio propia (HTML local vía `LoadHTML`).
- [ ] Zoom (`WebView.ZoomIn`/`ZoomOut`) en el joystick.
- [ ] Descargas con UI: el plugin ya emite `onDownloadStart/Finish/Error` y
      expone progreso; hoy nadie los escucha.

## Fase 1.5 — El 360 como producto

- [ ] Controles de reproducción en VR: barra de progreso, volumen, saltar.
- [ ] Recentrar el panorama sobre la mirada actual (`Video360Player.SetRotation`).
- [ ] Selector manual de layout cuando la heurística del nombre falla.
- [ ] Presets de mezcla en un botón del mando (solo vídeo / mixto / solo realidad).
- [ ] Vídeo local del visor y descargas desde el navegador al reproductor.

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
| Extractor de streams (YouTube) | Si reproducir 360 de YouTube pasa a ser requisito. Es otro proyecto, con preguntas legales propias |
| Passthrough en underlay | Si el tinte del overlay sobre la UI resulta inaceptable. Cuesta pelearse con transparencias |
