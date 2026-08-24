#!/usr/bin/env python3
"""
Plan de bandejas para Bambu Lab P2S (256 x 256 x 256 mm).

Lee los STL de ./stl (los genera render_stl.sh), aplica a cada pieza su
rotacion de impresion y empaqueta las SILUETAS REALES, no las cajas
envolventes: dos discos de Ø132 no caben lado a lado en 246 mm, pero si
en diagonal, y eso solo lo ve un empaquetador que trabaje con la forma.

La silueta se toma como envolvente convexa de la proyeccion en planta
(conservador: nunca ocupa menos de lo que la pieza ocupa de verdad), se
rasteriza a 1 mm y se dilata la mitad de la separacion pedida.

Salida:
    stl/bandeja_N.stl   un STL por bandeja con todo colocado.
    En Bambu Studio: importar y, si quieres ajustes por pieza,
    boton derecho -> Split to objects.

Uso:  python3 bandejas.py [--dir stl] [--gap 4]
"""
import math, os, sys, struct, argparse
import numpy as np

CAMA, MARGEN, ALTO = 256.0, 5.0, 250.0
DENSIDAD = 1.07      # g/cm3 (ASA; PETG 1.27)
CAUDAL   = 10.0      # mm3/s efectivos, boquilla 0.6 con muchos perimetros
CAPA     = 0.2       # mm

# (archivo, cantidad, rotacion de impresion, orden)
#   orden: "1" obliga a la primera bandeja (el calibre, que hay que medir
#             antes de imprimir lo que ajusta contra el)
#          "2" la prohibe: piezas que dependen de esa medida (tornillo y
#             carcasa) y las muy altas, para que la primera bandeja acabe
#             pronto y no se espere 10 h por una medida
#          ""  indiferente
PIEZAS = [
    ("01_tornillo_arquimedes_calibre",     1, (0, 0, 0),   "1"),
    ("01_tornillo_arquimedes_carcasa",     1, (0, 0, 0),   "2"),
    ("01_tornillo_arquimedes_tornillo",    1, (0, 0, 0),   "2"),
    ("01_tornillo_arquimedes_embudo",      1, (0, 0, 0),   ""),
    ("02_turbina_savonius_rotor",          1, (0, 0, 0),   "2"),
    ("02_turbina_savonius_disco_sup",      1, (0, 0, 0),   ""),
    ("02_turbina_savonius_disco_medio",    1, (0, 0, 0),   ""),
    ("03_soporte_rodamientos_soporte",     1, (0, 0, 0),   ""),
    ("03_soporte_rodamientos_alojamiento", 1, (-90, 0, 0), ""),
    ("03_soporte_rodamientos_engranaje",   2, (0, 0, 0),   ""),
    ("04_tapones_pvc75_tapon_a",           1, (0, 90, 0),  ""),
    ("04_tapones_pvc75_tapon_b",           1, (0, 90, 0),  ""),
    ("04_tapones_pvc75_racor",             2, (0, 0, 0),   ""),
    ("05_net_cup_50_vaso",                 4, (0, 0, 0),   ""),
    ("05_net_cup_50_plantilla",            1, (0, 0, 0),   ""),
    ("06_estructura_conector_3v",          6, (0, 0, 0),   ""),
    ("06_estructura_abraz_canal",          2, (0, 0, 0),   ""),
    ("06_estructura_abraz_carcasa",        1, (0, 0, 0),   ""),
    ("06_estructura_sop_mastil",           1, (0, 0, 0),   ""),
]

def leer_stl(path):
    with open(path, "rb") as f:
        cab = f.read(84)
        if cab[:5] == b"solid" and b"facet" in cab:
            f.seek(0); v = []; tris = []
            for linea in f.read().decode(errors="ignore").splitlines():
                p = linea.split()
                if p and p[0] == "vertex":
                    v.append([float(x) for x in p[1:4]])
                    if len(v) == 3: tris.append(v); v = []
            return np.array(tris, dtype=float)
        n = struct.unpack("<I", cab[80:84])[0]
        d = np.frombuffer(f.read(50 * n), dtype=np.uint8).reshape(n, 50)
        v = d[:, 12:48].copy().view("<f4").reshape(n, 3, 3).astype(float)
        return v

def girar(tris, r):
    for eje, ang in zip((0, 1, 2), r):
        if ang % 360 == 0: continue
        c, s = math.cos(math.radians(ang)), math.sin(math.radians(ang))
        a, b = [(1, 2), (2, 0), (0, 1)][eje]
        x, y = tris[..., a].copy(), tris[..., b].copy()
        tris[..., a], tris[..., b] = x * c - y * s, x * s + y * c
    return tris

def volumen(tris):
    """Volumen encerrado por la malla (suma de tetraedros con signo)."""
    a, b, c = tris[:, 0], tris[:, 1], tris[:, 2]
    return abs(np.einsum("ij,ij->i", a, np.cross(b, c)).sum()) / 6.0

def envolvente(pts):
    p = np.unique(np.round(pts, 1), axis=0)
    p = p[np.lexsort((p[:, 1], p[:, 0]))]
    def media(pts):
        h = []
        for q in pts:
            while len(h) >= 2:
                u, v = h[-1] - h[-2], q - h[-2]
                if u[0] * v[1] - u[1] * v[0] > 0: break
                h.pop()
            h.append(q)
        return h
    return np.array(media(p)[:-1] + media(p[::-1])[:-1])

def rasterizar(poly, gap):
    mn = poly.min(0); pol = poly - mn
    w, h = int(np.ceil(pol[:, 0].max())) + 1, int(np.ceil(pol[:, 1].max())) + 1
    yy, xx = np.mgrid[0:h, 0:w]
    dentro = np.ones((h, w), bool)
    n = len(pol)
    for i in range(n):
        a, b = pol[i], pol[(i + 1) % n]
        dentro &= ((b[0] - a[0]) * (yy + .5 - a[1]) - (b[1] - a[1]) * (xx + .5 - a[0])) >= -0.5
    g = int(round(gap))
    if g:
        m = np.zeros((h + g, w + g), bool)
        for dy in range(g + 1):
            for dx in range(g + 1):
                m[dy:dy + h, dx:dx + w] |= dentro
        dentro = m
    return dentro

def empaquetar(items, lado, paso=1):
    """Coloca primero las piezas atadas a un orden concreto y luego el resto
       por area decreciente, en la primera posicion libre (abajo-izquierda)."""
    bandejas = []
    pend = sorted(items, key=lambda i: (0 if i["orden"] else 1,
                                        -i["mask0"].sum()))
    while pend:
        cama = np.zeros((lado, lado), bool)
        puestos, restan = [], []
        for it in pend:
            colocado = False
            for k in range(4):
                m = np.rot90(it["mask0"], k)
                h, w = m.shape
                if h > lado or w > lado: continue
                for y in range(0, lado - h + 1, paso):
                    fila = cama[y:y + h]
                    for x in range(0, lado - w + 1, paso):
                        if not (fila[:, x:x + w] & m).any():
                            cama[y:y + h, x:x + w] |= m
                            it2 = dict(it); it2["pos"] = (x, y, k); puestos.append(it2)
                            colocado = True; break
                    if colocado: break
                if colocado: break
            if not colocado: restan.append(it)
        if not puestos: sys.exit("Hay una pieza que no cabe en la bandeja")
        bandejas.append(puestos)
        # el resto conserva el orden, pero ya sin la atadura de bandeja
        pend = sorted(restan, key=lambda i: -i["mask0"].sum())
    return bandejas

def escribir(items, path, gap):
    with open(path, "w") as f:
        f.write("solid bandeja\n")
        for it in items:
            x0, y0, k = it["pos"]
            t = it["tris"].copy()
            t[..., 0] -= it["mn"][0]; t[..., 1] -= it["mn"][1]; t[..., 2] -= it["mn"][2]
            for _ in range(k):        # rotaciones de 90 grados en planta
                ancho = t[..., 1].max()
                x, y = t[..., 0].copy(), t[..., 1].copy()
                t[..., 0], t[..., 1] = ancho - y, x
            t[..., 0] += x0 + MARGEN + gap / 2
            t[..., 1] += y0 + MARGEN + gap / 2
            for tri in t:
                f.write("facet normal 0 0 0\n  outer loop\n")
                for v in tri: f.write("    vertex %.4f %.4f %.4f\n" % tuple(v))
                f.write("  endloop\nendfacet\n")
        f.write("endsolid bandeja\n")

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dir", default="stl"); ap.add_argument("--gap", type=float, default=4)
    a = ap.parse_args()
    lado = int(CAMA - 2 * MARGEN - a.gap)   # la mascara ya lleva la separacion
    items = []
    for nombre, n, r, orden in PIEZAS:
        f = os.path.join(a.dir, nombre + ".stl")
        if not os.path.exists(f): sys.exit(f"Falta {f}. Ejecuta ./render_stl.sh")
        tris = girar(leer_stl(f), r)
        mn = tris.reshape(-1, 3).min(0); mx = tris.reshape(-1, 3).max(0)
        if mx[2] - mn[2] > ALTO: sys.exit(f"{nombre} no cabe en altura")
        mask = rasterizar(envolvente(tris.reshape(-1, 3)[:, :2]), a.gap)
        vol = volumen(tris)
        for _ in range(n):
            items.append(dict(idx=len(items), nombre=nombre, tris=tris, mn=mn,
                              orden=orden, mask0=mask, h=mx[2] - mn[2],
                              w=mx[0] - mn[0], d=mx[1] - mn[1], v=vol))
    # fase 1: primera bandeja sin las piezas que dependen del calibre
    fase1 = [i for i in items if i["orden"] != "2"]
    b1 = empaquetar(fase1, lado)[:1]
    puestos = {i["idx"] for i in b1[0]}
    resto = [i for i in items if i["idx"] not in puestos]
    bandejas = b1 + empaquetar(resto, lado)

    print(f"Bandeja util {lado} x {lado} mm, separacion {a.gap:.0f} mm")
    print(f"Estimacion: {DENSIDAD} g/cm3, {CAUDAL:.0f} mm3/s efectivos, capa {CAPA} mm\n")
    tot_v = tot_t = 0
    for n, b in enumerate(bandejas, 1):
        ocup = sum(i["w"] * i["d"] for i in b) / lado ** 2 * 100
        vb = sum(i["v"] for i in b)
        alto = max(i["h"] for i in b)
        # tiempo = extrusion + sobrecoste por capa (cambio de altura y viajes)
        horas = (vb / CAUDAL + (alto / CAPA) * len(b) * 1.2) / 3600
        tot_v += vb; tot_t += horas
        print(f"BANDEJA {n}   {len(b)} piezas | altura {alto:.0f} mm"
              f" | ocupacion {ocup:.0f} % | {vb/1000*DENSIDAD:.0f} g"
              f" | ~{horas:.0f} h")
        c = {}
        for i in b: c[i["nombre"]] = c.get(i["nombre"], 0) + 1
        for k, v in sorted(c.items()):
            i0 = next(i for i in b if i["nombre"] == k)
            print(f"     {v} x {k[3:]:34s} {i0['w']:5.0f} x {i0['d']:5.0f} x {i0['h']:5.0f}")
        escribir(b, os.path.join(a.dir, f"bandeja_{n}.stl"), a.gap)
        print()
    print(f"TOTAL {len(bandejas)} bandejas | {tot_v/1000*DENSIDAD:.0f} g"
          f" | ~{tot_t:.0f} h -> {a.dir}/bandeja_N.stl")

if __name__ == "__main__":
    main()
