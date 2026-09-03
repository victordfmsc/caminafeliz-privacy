# Comprobación de compilación sin Unity

Compila el código C# del proyecto y ejecuta sus tests **sin instalar Unity**,
contra stubs de la API de Unity y una copia fiel de la API pública de
TLabWebView.

```bash
tools/compilecheck/run.sh
```

## Qué comprueba de verdad

- Que los seis ensamblados compilan: sintaxis, `using`, firmas, versión de C#.
- Que **`CaminaFeliz.VRBrowser.Runtime` compila sin ninguna referencia a
  TLabWebView**. Esa es la afirmación central de la arquitectura, y aquí deja de
  ser una promesa: si alguien mete una llamada al plugin en la capa VR, esto
  falla.
- Que la lógica pura (resolución de URLs, historial de navegación) hace lo que
  dicen sus tests, ejecutándolos de verdad.

## Qué NO comprueba

Los stubs codifican **mi lectura** de la API de Unity y de TLabWebView, no la API
real. Que esto compile no garantiza que Unity compile: una firma que yo haya
transcrito mal pasa aquí y falla allí. Tampoco valida nada de lo que solo existe
en ejecución — renderizado, escenas, prefabs, el plugin de Android.

Es un filtro previo barato, no un sustituto de abrir el proyecto.
