#!/usr/bin/env bash
# Comprueba que el entorno puede abrir el proyecto y recuerda los pasos manuales.
# No instala Unity ni descarga paquetes: eso lo hace el Package Manager al abrir.
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
problems=0

say()  { printf '  %s\n' "$1"; }
fail() { printf '  [!] %s\n' "$1"; problems=$((problems + 1)); }

printf '\nCaminaFeliz VR Browser - comprobación de entorno\n\n'

printf 'Requisitos del sistema\n'
if command -v git >/dev/null 2>&1; then
  say "git: $(git --version)"
else
  fail 'git no está en el PATH. El Package Manager lo necesita para las dependencias por URL de Git.'
fi

if [ -n "${ANDROID_HOME:-}" ] || [ -n "${ANDROID_SDK_ROOT:-}" ]; then
  say "Android SDK: ${ANDROID_HOME:-$ANDROID_SDK_ROOT}"
else
  say 'ANDROID_HOME sin definir (correcto si usas el SDK que instala Unity Hub).'
fi

if command -v adb >/dev/null 2>&1; then
  devices="$(adb devices | tail -n +2 | grep -c device || true)"
  say "adb disponible, dispositivos conectados: ${devices}"
else
  say 'adb no está en el PATH (opcional; hace falta para desplegar y para logcat).'
fi

printf '\nProyecto\n'
for required in Packages/manifest.json ProjectSettings/ProjectVersion.txt Assets/CaminaFeliz; do
  if [ -e "${project_root}/${required}" ]; then
    say "ok: ${required}"
  else
    fail "falta: ${required}"
  fi
done

say "Unity esperado: $(sed -n 's/^m_EditorVersion: //p' "${project_root}/ProjectSettings/ProjectVersion.txt")"

printf '\nPasos manuales (no automatizables desde fuera del Editor)\n'
say '1. Abrir el proyecto con Unity Hub y esperar a que resuelva los paquetes de Git.'
say '2. Tools > CaminaFeliz VR Browser > Apply Quest Build Settings'
say '3. Tools > CaminaFeliz VR Browser > Validate Setup'
say '4. XR Plug-in Management > Android > OpenXR + feature group Meta Quest.'
say '5. Package Manager > XR Interaction Toolkit > importar los Starter Assets.'
say '6. Montar la escena siguiendo docs/03-setup-quest.md'

printf '\n'
if [ "${problems}" -gt 0 ]; then
  printf 'Problemas encontrados: %s\n\n' "${problems}"
  exit 1
fi

printf 'Entorno correcto.\n\n'
