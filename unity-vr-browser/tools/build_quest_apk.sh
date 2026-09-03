#!/usr/bin/env bash
# Compila el APK para Quest sin abrir el Editor, y opcionalmente lo instala.
#
#   tools/build_quest_apk.sh                  # development APK
#   tools/build_quest_apk.sh --release        # release APK
#   tools/build_quest_apk.sh --install        # compila e instala en el visor
#   tools/build_quest_apk.sh --install-only   # instala un APK ya compilado
#   UNITY_PATH=/ruta/al/Unity tools/build_quest_apk.sh
set -euo pipefail

project="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
build_type="development"
install_after=0
build_first=1
output="Build/CaminaFelizVRBrowser.apk"

while [ $# -gt 0 ]; do
  case "$1" in
    --release) build_type="release" ;;
    --install) install_after=1 ;;
    --install-only) install_after=1; build_first=0 ;;
    --output)  output="$2"; shift ;;
    -h|--help) sed -n '2,8p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "Opción desconocida: $1" >&2; exit 2 ;;
  esac
  shift
done

apk_path() {
  if [ -f "${project}/${output}" ]; then printf '%s' "${project}/${output}"
  elif [ -f "${output}" ];           then printf '%s' "${output}"
  fi
}

# --- localizar el Editor ------------------------------------------------------
# La versión exacta la manda el proyecto: abrirlo con otra dispara una migración
# silenciosa que puede romper cosas sin avisar en batch mode.
wanted="$(sed -n 's/^m_EditorVersion: //p' "${project}/ProjectSettings/ProjectVersion.txt")"

find_unity() {
  if [ -n "${UNITY_PATH:-}" ]; then printf '%s' "${UNITY_PATH}"; return; fi

  local candidates=(
    "${HOME}/Unity/Hub/Editor/${wanted}/Editor/Unity"
    "/opt/unity/editors/${wanted}/Editor/Unity"
    "/Applications/Unity/Hub/Editor/${wanted}/Unity.app/Contents/MacOS/Unity"
    "/c/Program Files/Unity/Hub/Editor/${wanted}/Editor/Unity.exe"
  )
  local candidate
  for candidate in "${candidates[@]}"; do
    [ -x "${candidate}" ] && { printf '%s' "${candidate}"; return; }
  done

  # Cualquier versión instalada, avisando de que no es la del proyecto.
  local any
  any="$(ls -d "${HOME}"/Unity/Hub/Editor/*/Editor/Unity \
                /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | head -1 || true)"
  printf '%s' "${any}"
}

if [ ${build_first} -eq 1 ]; then

unity="$(find_unity)"

if [ -z "${unity}" ]; then
  cat >&2 <<MSG
No encuentro el Editor de Unity.

Instala Unity ${wanted} desde Unity Hub con el módulo "Android Build Support"
(incluidos OpenJDK y Android SDK & NDK Tools), o indica la ruta:

  UNITY_PATH=/ruta/al/Unity tools/build_quest_apk.sh
MSG
  exit 1
fi

case "${unity}" in
  *"${wanted}"*) ;;
  *) echo "AVISO: el proyecto pide Unity ${wanted} y voy a usar ${unity}" >&2 ;;
esac

# --- compilar -----------------------------------------------------------------
log="${project}/Build/unity-build.log"
mkdir -p "${project}/Build"

echo "Unity:   ${unity}"
echo "Tipo:    ${build_type}"
echo "Salida:  ${output}"
echo "Log:     ${log}"
echo
echo "Compilando (la primera vez tarda: importa todos los assets)..."

set +e
"${unity}" -quit -batchmode -nographics \
  -projectPath "${project}" \
  -logFile "${log}" \
  -executeMethod CaminaFeliz.VRBrowser.Editor.QuestBuildPipeline.BuildFromCommandLine \
  -apkOutput "${output}" \
  -buildType "${build_type}"
status=$?
set -e

if [ ${status} -ne 0 ]; then
  echo >&2
  echo "El build falló (código ${status}). Errores del log:" >&2
  grep -E "^\[VRBrowser\]|error CS[0-9]+|BuildFailedException|Exception:" "${log}" | tail -40 >&2 || true
  echo >&2
  echo "Log completo: ${log}" >&2
  exit ${status}
fi

fi   # fin del bloque de compilación

apk="$(apk_path)"

if [ -z "${apk}" ]; then
  echo "No encuentro ningún APK en ${output}." >&2
  echo "Compílalo primero: tools/build_quest_apk.sh" >&2
  exit 1
fi

echo
echo "APK listo: ${apk}"
ls -lh "${apk}" | awk '{print "Tamaño:  " $5}'

# --- instalar -----------------------------------------------------------------
if [ ${install_after} -eq 1 ]; then
  if ! command -v adb >/dev/null 2>&1; then
    echo "adb no está en el PATH; instala las Android Platform Tools." >&2
    exit 1
  fi

  if [ "$(adb devices | tail -n +2 | grep -c 'device$')" -eq 0 ]; then
    cat >&2 <<'MSG'
No hay ningún visor conectado y autorizado.

  1. Modo desarrollador activado en la app Meta Horizon del móvil.
  2. Visor conectado por USB-C y encendido.
  3. Acepta "Permitir depuración por USB" DENTRO del visor.
  4. Comprueba con: adb devices
MSG
    exit 1
  fi

  echo
  echo "Instalando en el visor..."
  adb install -r "${apk}"
  echo
  echo "Listo. En el visor: Biblioteca > Fuentes desconocidas > ${project##*/}"
fi
