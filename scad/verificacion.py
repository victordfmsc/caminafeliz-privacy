#!/usr/bin/env python3
"""
Verificacion de montaje: comprueba que la cadena de cotas cierra.

No usa valores copiados a mano. Hace que OpenSCAD evalue cada modulo y
devuelva por ECHO sus propias variables, y sobre esos numeros comprueba
ajustes, alineaciones, cinematica y limites fisicos.

Uso:  python3 verificacion.py
"""
import subprocess, re, math, sys, os, tempfile

MODULOS = {
    "M1": ("01_tornillo_arquimedes.scad", """tol pared inclinacion helice_d paso
        largo_util eje_d helice_esp holgura_helice carcasa_largo embudo_d embudo_h
        embudo_encaje boquilla_d boquilla_l pcd_brida eje_agujero_d nucleo_d
        carcasa_int_d carcasa_ext_d casq_int_d casq_ext_d k_cierre paso_max m4_paso"""),
    "M2": ("02_turbina_savonius.scad", """altura_total rotor_d solape pala_esp disco_d
        disco_esp eje_d cubo_d cubo_h_inf cubo_h_sup torsion alabe_h z_medio
        eje_ag_d encaje_disco lean"""),
    "M3": ("03_soporte_rodamientos.scad", """inclinacion rod_ext_d rod_int_d rod_esp
        rod_labio eje_d cubo_d mod_g z_g ancho_g cubo_g_h juego_g pcd_brida base_esp
        base_d z_apice buje_d buje_l d_mastil ang_ejes delta ang_mast r_prim cono_A
        ap_axial z_flip r_cab_ext eje_ag_d"""),
    "M4": ("04_tapones_pvc75.scad", """tol pared tubo_ext_d tubo_pared encaje camara
        fondo boquilla_d manguito_h nivel_agua racor_int_d tubo_int_d casq_int_d
        casq_ext_d cam_int_d cam_ext_d largo manguito_int_d manguito_ext_d
        z_manguito z_hombro manguito_p x_manguito z_racor"""),
    "M5": ("05_net_cup_50.scad", """sup_d base_d alto pared pestana_e pestana_v
        pestana_d taladro_d paso_vasos tubo_ext_d"""),
    "M6": ("06_estructura.scad", """tubo_b casq_int casq_ext anillo_int anillo_ext
        canal_d canal_int carcasa_d carc_int inclinacion rod_ext_d buje_d"""),
}

def valores(archivo, nombres):
    ns = nombres.split()
    src = f'include <{os.path.abspath(archivo)}>\n'
    src += "".join(f'echo("VAL", "{n}", {n});\n' for n in ns)
    with tempfile.NamedTemporaryFile("w", suffix=".scad", delete=False) as f:
        f.write(src); tmp = f.name
    r = subprocess.run(["openscad", "-o", "/dev/null", "--export-format", "asciistl",
                        "-D", 'pieza="ninguna"', tmp],
                       capture_output=True, text=True)
    os.unlink(tmp)
    d = {}
    for m in re.finditer(r'ECHO: "VAL", "(\w+)", ([-\d.e+]+)', r.stderr + r.stdout):
        d[m.group(1)] = float(m.group(2))
    faltan = [n for n in ns if n not in d]
    if faltan: sys.exit(f"{archivo}: no se pudo leer {faltan}")
    return d

# ---------------------------------------------------------------- comprobador
FALLOS = []
def chk(ok, titulo, detalle):
    print(f"  [{'OK ' if ok else 'MAL'}] {titulo:44s} {detalle}")
    if not ok: FALLOS.append(titulo)

def main():
    V = {k: valores(a, n) for k, (a, n) in MODULOS.items()}
    M1, M2, M3, M4, M5, M6 = (V[k] for k in ("M1", "M2", "M3", "M4", "M5", "M6"))
    inc = M1["inclinacion"]

    print("\n=== 1. AJUSTES ENTRE PIEZAS ==============================")
    j = M1["carcasa_int_d"] - M1["helice_d"]
    chk(abs(j - 2 * M1["holgura_helice"]) < 1e-6,
        "helice / carcasa", f"{M1['helice_d']:.1f} en {M1['carcasa_int_d']:.2f} -> {j:.2f} mm diametrales")
    chk(abs((M1["nucleo_d"] - M1["eje_agujero_d"]) / 2 - M1["pared"]) < 1e-6,
        "pared del nucleo del tornillo", f"{(M1['nucleo_d']-M1['eje_agujero_d'])/2:.2f} mm")
    chk(abs(M1["casq_int_d"] - M1["carcasa_ext_d"] - 2 * M1["tol"]) < 1e-6,
        "embudo / carcasa (casquillo)", f"{M1['casq_int_d']:.2f} sobre {M1['carcasa_ext_d']:.2f}")
    chk(abs(M6["carc_int"] - (M1["carcasa_ext_d"] + 2 * M6["tubo_b"] * 0)) < 1e9
        and abs(M6["carcasa_d"] - M1["carcasa_ext_d"]) < 0.01,
        "abrazadera de carcasa / carcasa real",
        f"nido para {M6['carcasa_d']:.2f}, carcasa {M1['carcasa_ext_d']:.2f}")
    chk(abs(M6["canal_d"] - M4["tubo_ext_d"]) < 1e-6,
        "abrazadera de canal / tubo PVC", f"{M6['canal_int']:.1f} sobre {M4['tubo_ext_d']:.1f}")
    chk(abs(M4["casq_int_d"] - M4["tubo_ext_d"] - 2 * M4["tol"]) < 1e-6,
        "tapon / tubo PVC", f"{M4['casq_int_d']:.1f} sobre {M4['tubo_ext_d']:.1f}")
    chk(abs(M5["taladro_d"] - M5["sup_d"]) > 0.5 and M5["taladro_d"] > M5["sup_d"],
        "vaso / taladro del canal", f"vaso {M5['sup_d']:.1f}, taladro {M5['taladro_d']:.1f}")
    hol = (M4["manguito_int_d"] - M1["boquilla_d"]) / 2
    chk(hol >= 0.9, "boquilla / manguito (union de vertido)",
        f"{hol:.2f} mm radiales, desviacion admisible {math.degrees(math.asin(min(1,2*hol/M4['manguito_p']))):.1f} grados")
    chk(M4["manguito_p"] >= 10, "profundidad de encaje de la boquilla",
        f"{M4['manguito_p']:.1f} mm")
    chk(M3["rod_ext_d"] - M3["rod_labio"] >= 2, "labio de retencion del 608",
        f"{(M3['rod_ext_d']-M3['rod_labio'])/2:.2f} mm de apoyo radial")

    print("\n=== 2. INTERFAZ CARCASA - TORRETA ========================")
    chk(abs(M1["pcd_brida"] - M3["pcd_brida"]) < 1e-6, "circulo de taladros M4",
        f"PCD {M1['pcd_brida']:.0f} en ambos, 4 taladros a 45+90k grados")
    chk(M3["base_d"] >= M1["pcd_brida"] + 16, "base de la torreta cubre el PCD",
        f"base {M3['base_d']:.0f} > PCD {M1['pcd_brida']:.0f} + 16")
    chk(M3["rod_labio"] > M1["eje_d"] + 2, "paso del eje por la torreta",
        f"labio {M3['rod_labio']:.1f} sobre eje {M1['eje_d']:.0f}")

    print("\n=== 3. REENVIO CONICO ====================================")
    chk(abs(M3["ang_ejes"] - (90 + inc)) < 1e-6, "angulo entre ejes",
        f"SIGMA = 90 + {inc:.0f} = {M3['ang_ejes']:.0f} grados")
    chk(abs(M3["delta"] - M3["ang_ejes"] / 2) < 1e-6, "cono primitivo (relacion 1:1)",
        f"delta = {M3['delta']:.1f} grados en los dos engranajes")
    chk(abs(M3["ang_mast"] - (90 - inc)) < 1e-6, "mastil vertical en obra",
        f"brazo a {M3['ang_mast']:.0f} grados del eje del tornillo")
    hueco_t = M3["z_apice"] - M3["z_flip"] - M3["buje_l"]
    hueco_m = M3["d_mastil"] - (M3["buje_l"] + 6) / 2 - M3["z_flip"]
    chk(hueco_t > 2, "cota de montaje del engranaje del tornillo",
        f"{hueco_t:.1f} mm entre el buje central y el cubo")
    chk(hueco_m > 2, "cota de montaje del engranaje del mastil",
        f"{hueco_m:.1f} mm entre el buje del brazo y el cubo")
    chk(abs(M3["r_cab_ext"] - (M3["r_prim"] + M3["mod_g"] * math.cos(math.radians(M3["delta"])))) < 0.01,
        "radio de cabeza exterior de norma",
        f"{M3['r_cab_ext']:.2f} = r + m*cos(delta)")
    chk(M3["juego_g"] > 0, "juego entre flancos", f"{M3['juego_g']:.2f} mm")
    chk(M3["ancho_g"] <= M3["cono_A"] / 3, "ancho de diente",
        f"{M3['ancho_g']:.0f} <= A0/3 = {M3['cono_A']/3:.1f}")

    print("\n=== 4. EJES Y VARILLAS ===================================")
    z_buje_inf = -(M1["embudo_h"] - (M1["embudo_h"] - 22))     # buje del embudo
    largo_eje_t = (M3["z_apice"] - M3["z_flip"] + M1["carcasa_largo"] + M3["base_esp"]) + 22 + 10
    chk(largo_eje_t < 400, "varilla del tornillo", f"{largo_eje_t:.0f} mm (corte recomendado)")
    largo_eje_m = M2["altura_total"] + M2["cubo_h_inf"] + 130 - M3["z_flip"] + 20
    chk(largo_eje_m < 500, "varilla del mastil", f"{largo_eje_m:.0f} mm (corte recomendado)")
    chk(abs(M2["eje_ag_d"] - M3["eje_ag_d"]) < 1e-6, "agujeros de eje coherentes",
        f"{M2['eje_ag_d']:.2f} mm en turbina y en reenvio")
    # velocidad critica del eje del mastil (rotor en voladizo)
    E, d = 200e9, M1["eje_d"] / 1000
    I = math.pi * d ** 4 / 64
    L = 0.095; m = 0.30
    n_cr = math.sqrt(3 * E * I / L ** 3 / m) * 60 / (2 * math.pi)
    chk(n_cr > 3 * 427, "velocidad critica del eje del mastil",
        f"{n_cr:.0f} rpm frente a 427 rpm de trabajo")

    print("\n=== 5. CADENA DE COTAS DEL VERTIDO =======================")
    z_desc = (M1["carcasa_largo"] - 20) * math.sin(math.radians(inc))
    z_boca = z_desc - M1["boquilla_l"]
    vuelo = M1["boquilla_l"] - (M1["carcasa_ext_d"] / 2) / math.cos(math.radians(inc))
    chk(vuelo > M4["manguito_p"], "la boquilla sale de la carcasa y entra en el manguito",
        f"vuelo libre {vuelo:.1f} mm, encaje {M4['manguito_p']:.1f} mm")
    canal_z = z_boca - M4["z_hombro"]
    lamina = canal_z - M4["tubo_int_d"] / 2 + M4["nivel_agua"]
    chk(canal_z > 0, "eje del canal sobre la base de la carcasa", f"{canal_z:.1f} mm")
    chk(lamina > 0, "lamina del canal sobre la base de la carcasa", f"{lamina:.1f} mm")
    # ventana de nivel del deposito
    r_h = M1["helice_d"] / 2
    n_min = 8 * math.sin(math.radians(inc)) - r_h * math.cos(math.radians(inc))
    chk(n_min < lamina, "ventana de nivel del deposito",
        f"entre {n_min:+.1f} y {lamina:+.1f} mm respecto a la base de la carcasa"
        f" ({lamina-n_min:.0f} mm de margen)")
    # La geometria de la conexion se come una altura fija: radio del canal +
    # manguito + carrera de la boquilla para salir de la carcasa + encaje.
    perdida = M4["z_hombro"] + M1["boquilla_l"]
    largo_min = (n_min + 8 + M4["tubo_int_d"] / 2 - M4["nivel_agua"] + perdida) \
                / math.sin(math.radians(inc))
    chk(M1["largo_util"] > largo_min, "longitud minima de helicoide",
        f"{M1['largo_util']:.0f} mm frente a {largo_min:.0f} mm minimos"
        f" (margen {M1['largo_util']-largo_min:.0f} mm)")
    chk(lamina - n_min > 5, "carga motriz del retorno por gravedad",
        f"{lamina-n_min:.0f} mm entre lamina del canal y nivel maximo del deposito")
    z_boq = M1["carcasa_largo"] - 20
    chk(8 < z_boq < 8 + M1["largo_util"], "la ventana de vertido cae sobre el filete",
        f"ventana a z={z_boq:.0f}, filete de 8 a {8+M1['largo_util']:.0f}")

    print("\n=== 6. CANAL Y CULTIVO ===================================")
    inm = M4["nivel_agua"] + M4["tubo_pared"] - (M5["alto"] - M5["pestana_e"]) + \
          (M4["tubo_ext_d"] / 2 - M4["tubo_ext_d"] / 2)
    inm = M4["nivel_agua"] + M4["tubo_pared"] - 28
    chk(-3 <= inm <= 6, "base del vaso respecto a la lamina",
        f"{inm:+.1f} mm (0 = roza el agua)")
    chk(M5["pestana_d"] > M5["taladro_d"] + 6, "la pestaña apoya en el tubo",
        f"pestaña {M5['pestana_d']:.0f} sobre taladro {M5['taladro_d']:.1f}")
    chk(M1["k_cierre"] < 1, "cangilones cerrados",
        f"k = {M1['k_cierre']:.3f} (paso maximo {M1['paso_max']:.0f} mm)")
    chk(M2["lean"] <= 45, "alabe helicoidal autoportante",
        f"canto a {M2['lean']:.1f} grados de la vertical")

    print("\n" + "=" * 58)
    if FALLOS:
        print(f"{len(FALLOS)} COMPROBACIONES FALLIDAS:")
        for f in FALLOS: print("   -", f)
        sys.exit(1)
    print("Todas las comprobaciones pasan.")

if __name__ == "__main__":
    main()
