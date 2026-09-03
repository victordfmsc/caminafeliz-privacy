# Compilar el APK e instalarlo en tu Quest

Objetivo: `tools/build_quest_apk.sh --install` y tener el navegador puesto en el
visor.

```bash
tools/build_quest_apk.sh              # solo compila -> Build/CaminaFelizVRBrowser.apk
tools/build_quest_apk.sh --install    # compila e instala en el visor conectado
tools/build_quest_apk.sh --release    # build de release en vez de development
tools/build_quest_apk.sh --install-only   # instala un APK ya compilado, sin recompilar
```

`--install-only` existe porque el fallo más habitual no es el build: es que el
visor no estaba autorizado la primera vez. Recompilar entero para reintentar un
`adb install` de tres segundos no tiene sentido.

## Esto se ejecuta en TU máquina

El visor tiene que estar conectado por USB al ordenador donde corres el script.
No hay forma de instalar en un Quest desde una máquina remota: `adb` necesita el
puerto USB físico (o el visor en la misma red local, con `adb connect`).

El script no abre el Editor: lo lanza en `-batchmode`, ejecuta
`QuestBuildPipeline.BuildFromCommandLine`, y devuelve código distinto de cero si
el build falla, filtrando los errores del log. Un build roto tiene que llegar al
shell como fallo, o parece que ha ido bien.

## Requisitos, una sola vez

### 1. Unity con soporte Android

Instala **Unity 2022.3 LTS** desde Unity Hub con el módulo **Android Build
Support**, y dentro de él **OpenJDK** y **Android SDK & NDK Tools**. Sin esos dos
submódulos el build falla al final, después de importar todo.

Si tienes el Editor en otra ruta:

```bash
UNITY_PATH=/ruta/al/Unity tools/build_quest_apk.sh
```

### 2. Ajustes que el script no puede tocar

Dos cosas viven en assets del Editor y hay que hacerlas **una vez, a mano**, con
el proyecto abierto:

1. **`Project Settings ▸ XR Plug-in Management ▸ Android` → marcar `Oculus`.**
   Sin esto compila igual, arranca, y muestra una pantalla plana sin seguimiento
   de cabeza. Es el fallo más desconcertante de los dos.
2. **Menú de Meta ▸ `Update AndroidManifest.xml`** (con `Passthrough Support:
   Required` en el `OVRManager`). Añade
   `<uses-feature android:name="com.oculus.feature.PASSTHROUGH" android:required="true"/>`.
   Sin eso el passthrough no se activa y el deslizante no hace nada.

Lo demás —color space, API level, ARM64, IL2CPP, permiso de Internet, símbolos de
compilación— lo aplica el pipeline solo antes de cada build, y
`Tools ▸ CaminaFeliz VR Browser ▸ Validate Setup` lo audita sin tocar nada.

### 3. Modo desarrollador en el Quest

1. Necesitas una **organización de desarrollador** de Meta: crea una en
   <https://developer.meta.com/manage/organizations/>. Meta pide verificar la
   cuenta (teléfono o tarjeta) antes de dejarte activar el modo desarrollador.
2. En la app **Meta Horizon** del móvil: tu visor ▸ **Ajustes de desarrollador** ▸
   **Modo desarrollador: activado**.
3. Reinicia el visor.
4. Conéctalo por USB-C y **acepta "Permitir depuración por USB" dentro del
   visor** — el diálogo sale ahí, no en el ordenador. Marca "Permitir siempre".
5. Comprueba:

```bash
adb devices     # debe listar tu visor como "device", no "unauthorized"
```

`adb` viene en las Android Platform Tools; Unity también instala una copia bajo
`.../PlaybackEngines/AndroidPlayer/SDK/platform-tools/`.

## Qué produce el build

| | |
|---|---|
| Ruta | `Build/CaminaFelizVRBrowser.apk` |
| Package | `com.vertey.caminafelizvrbrowser` |
| Firma | Keystore de depuración de Unity (no hay claves en el repo) |
| Arquitectura | ARM64, IL2CPP |
| Formato | APK, nunca `.aab` — un app bundle no se puede instalar con `adb` |

En development build, `bundleVersionCode` sube en cada compilación, así que
`adb install -r` reinstala encima sin desinstalar antes.

Si `Build Settings` no tiene ninguna escena, el pipeline **genera la escena
prototipo** y la añade. Un primer build en un clon recién bajado produce algo que
puedes ponerte, no un error.

## Dónde aparece en el visor

**Biblioteca ▸ Fuentes desconocidas ▸ CaminaFeliz VR Browser.** No sale en la
biblioteca normal: las apps sideloadeadas van siempre a esa sección.

## Cuando algo falla

| Síntoma | Causa |
|---|---|
| `adb devices` dice `unauthorized` | No has aceptado el diálogo USB dentro del visor |
| `adb devices` no lista nada | Cable solo de carga, o modo desarrollador sin reiniciar |
| `INSTALL_FAILED_UPDATE_INCOMPATIBLE` | Firmado con otra clave: `adb uninstall com.vertey.caminafelizvrbrowser` |
| `INSTALL_FAILED_NO_MATCHING_ABIS` | El build no salió en ARM64 |
| Arranca plano, sin seguimiento de cabeza | Falta marcar `Oculus` en XR Plug-in Management |
| El deslizante no muestra realidad | Falta el `uses-feature` de passthrough en el manifiesto |
| Panel del navegador en negro, sin errores | Permiso de Internet eliminado por el plugin de XR |
| Build muy lento la primera vez | Normal: importa todos los assets. Las siguientes son mucho más rápidas |

Log completo del build en `Build/unity-build.log`. Para ver qué hace la app ya
instalada:

```bash
adb logcat -s Unity TLab chromium
```
