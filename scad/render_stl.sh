#!/usr/bin/env bash
# Exporta todos los STL a ./stl  ·  uso: ./render_stl.sh [directorio_salida]
set -euo pipefail
OUT="${1:-stl}"
mkdir -p "$OUT"
render() {   # render <archivo.scad> <pieza>
  echo ">> $2"
  openscad -o "$OUT/$(basename "$1" .scad)_$2.stl" -D "pieza=\"$2\"" "$1"
}
render 01_tornillo_arquimedes.scad tornillo
render 01_tornillo_arquimedes.scad carcasa
render 01_tornillo_arquimedes.scad embudo
render 02_turbina_savonius.scad    rotor
render 02_turbina_savonius.scad    disco_sup
render 02_turbina_savonius.scad    disco_medio
render 03_soporte_rodamientos.scad soporte
render 03_soporte_rodamientos.scad alojamiento
render 03_soporte_rodamientos.scad engranaje
render 03_soporte_rodamientos.scad acople
render 04_tapones_pvc75.scad       tapon_a
render 04_tapones_pvc75.scad       tapon_b
render 05_net_cup_50.scad          vaso
render 05_net_cup_50.scad          plantilla
render 06_estructura.scad          conector_3v
render 06_estructura.scad          abraz_canal
render 06_estructura.scad          abraz_carcasa
render 06_estructura.scad          sop_mastil
echo "Listo: $(ls -1 "$OUT" | wc -l) archivos en $OUT/"
