#!/usr/bin/env bash
# Compila el proyecto contra stubs de Unity y ejecuta sus tests. Necesita dotnet.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project="$(cd "${here}/../.." && pwd)"
assets="${project}/Assets/CaminaFeliz"
out="$(mktemp -d)"
trap 'rm -rf "${out}"' EXIT

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Falta dotnet. En Ubuntu: sudo apt-get install -y dotnet-sdk-8.0" >&2
  exit 1
fi

# find exits non-zero when one of the roots is missing, which under `set -e`
# would kill the script before it could report anything useful.
find_first() {
  local name="$1" path_pattern="$2" root
  for root in /usr/lib/dotnet /usr/share/dotnet "${DOTNET_ROOT:-}"; do
    [ -d "${root}" ] || continue
    local hit
    hit="$(find "${root}" -name "${name}" -path "${path_pattern}" 2>/dev/null | sort | tail -1)"
    if [ -n "${hit}" ]; then
      printf '%s' "${hit}"
      return 0
    fi
  done
  return 0
}

csc="$(find_first csc.dll '*Roslyn*')"
runtime_ref="$(find_first System.Runtime.dll '*Microsoft.NETCore.App.Ref*')"

if [ -z "${csc}" ] || [ -z "${runtime_ref}" ]; then
  echo "No encuentro el compilador de Roslyn ni los ensamblados de referencia." >&2
  echo "Instala el SDK: sudo apt-get install -y dotnet-sdk-8.0" >&2
  exit 1
fi

refdir="$(dirname "${runtime_ref}")"

refs=()
for dll in "${refdir}"/*.dll; do refs+=("-r:${dll}"); done
quiet="-nowarn:CS0067,CS0414,CS0169,CS0649,CS0108,CS0660,CS0661"

build() {
  local name="$1"; shift
  echo "  compilando ${name}"
  dotnet "${csc}" -nologo -langversion:latest -target:library \
    -out:"${out}/${name}.dll" "${refs[@]}" ${quiet} "$@"
}

echo "Compilando"
build UnityStubs "${here}"/stubs/UnityEngine.cs "${here}"/stubs/UnityEditor.cs \
                 "${here}"/stubs/StubJson.cs
build TLabStub   -r:"${out}/UnityStubs.dll" "${here}/stubs/TLab.cs"

# Deliberadamente SIN -r:TLabStub.dll: si esto compila, la capa VR no toca el plugin.
build Runtime    -r:"${out}/UnityStubs.dll" \
                 "${assets}"/VRBrowser/Runtime/Core/*.cs \
                 "${assets}"/VRBrowser/Runtime/Vr/*.cs \
                 "${assets}"/VRBrowser/Runtime/Immersive/*.cs

build Integration -r:"${out}/UnityStubs.dll" -r:"${out}/TLabStub.dll" -r:"${out}/Runtime.dll" \
                  "${assets}"/VRBrowser/Runtime/Integration/*.cs
build Editor      -r:"${out}/UnityStubs.dll" -r:"${out}/Runtime.dll" \
                  "${assets}"/VRBrowser/Editor/*.cs

echo
echo "Ejecutando tests"
cat > "${out}/Tests.runtimeconfig.json" <<'JSON'
{ "runtimeOptions": { "tfm": "net8.0", "framework": { "name": "Microsoft.NETCore.App", "version": "8.0.0" } } }
JSON

dotnet "${csc}" -nologo -langversion:latest -target:exe -out:"${out}/Tests.dll" \
  "${refs[@]}" ${quiet} -r:"${out}/UnityStubs.dll" -r:"${out}/Runtime.dll" \
  "${here}"/runner/NUnitShim.cs "${here}"/runner/Runner.cs "${assets}"/VRBrowser/Tests/*.cs

cd "${out}" && dotnet Tests.dll
