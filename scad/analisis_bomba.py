#!/usr/bin/env python3
"""
Analisis fisico y agronomico de la bomba de Arquimedes eolica.
Ejecutar:  python3 analisis_bomba.py        (requiere numpy)

MODELO DEL CANGILON
El agua de cada cangilon esta limitada por la carcasa, el nucleo, dos caras
del filete y un plano horizontal. El nivel maximo lo fija el PUNTO DE SILLA
de la helice del canto interior del filete (r = Ri), que es por donde el agua
rebosa al cangilon de abajo:

    h(th) = -Ri cos(a) cos(th) + (S sin(a) / 2pi) th
    h'(th) = 0  ->  sin(th) = -k ,   k = S tan(a) / (2 pi Ri)

Si k >= 1 no existe ese minimo: la helice del canto sube de forma monotona,
no hay barrera y EL CANGILON NO CIERRA. El criterio exacto de cierre es por
tanto  S tan(a) / (2 pi Ri) < 1, con el radio INTERIOR, no con el exterior.

VALIDACION
Con paso tendiendo a cero el llenado tiene solucion analitica: el agua ocupa
la parte del anillo con x >= Ri, es decir un segmento circular. El modelo
numerico converge a ese valor (0.3180 para Ro=20, Ri=7.15).
"""
import math
try:
    import numpy as np
except ImportError:
    raise SystemExit("Necesita numpy:  pip install numpy")

# ------------------------------------------------------------------ geometria
Ro, Ri = 20.0, 7.15       # radios exterior e interior del helicoide (mm)
S      = 15.0             # paso (mm)
L      = 180.0            # longitud del helicoide (mm)
ALFA   = 40.0             # inclinacion sobre la horizontal (grados)
ESP    = 2.0             # espesor axial del filete (mm)
GAP    = 0.15             # holgura radial helicoide-carcasa (mm)
RHO, G, MU = 1000.0, 9.81, 1.0e-3

# ------------------------------------------------------------------- hidraulica
def nivel(Ri, S, a):
    ar = math.radians(a)
    A, B = Ri * math.cos(ar), S * math.sin(ar) / (2 * math.pi)
    k = B / A
    if k >= 1:
        return None, k
    th = -math.asin(k)
    return -A * math.cos(th) + B * th, k

def volumen(Ro, Ri, S, a, esp=ESP, nr=60, nt=200, nz=60):
    """Volumen de agua por cangilon, en mm3."""
    c, k = nivel(Ri, S, a)
    if c is None:
        return 0.0, k
    ar = math.radians(a); ca, sa = math.cos(ar), math.sin(ar)
    r = Ri + (np.arange(nr) + 0.5) * (Ro - Ri) / nr
    t = -math.pi + (np.arange(nt) + 0.5) * 2 * math.pi / nt
    R, T = np.meshgrid(r, t, indexing="ij")
    dA = R * ((Ro - Ri) / nr) * (2 * math.pi / nt)
    V = 0.0
    for j in range(nz):
        w = (j + 0.5) * S / nz
        if w < esp:
            continue
        H = -ca * R * np.cos(T) + sa * (S * T / (2 * math.pi) + w)
        V += float((dA * (H <= c)).sum()) * (S / nz)
    return V, k

def fuga(gap, S, a, esp=ESP):
    """Fuga por la holgura radial: modelo viscoso (ranura) y de orificio.
       Salto de carga entre cangilones contiguos = S sin(a)."""
    dh = S * math.sin(math.radians(a)) / 1000.0
    dp = RHO * G * dh
    w  = math.pi * Ro / 1000.0            # arco mojado ~ media circunferencia
    h, Lp = gap / 1000.0, esp / 1000.0
    q_vis = w * h ** 3 * dp / (12 * MU * Lp)
    q_orf = 0.6 * w * h * math.sqrt(2 * G * dh)
    return min(q_vis, q_orf) * 60000.0, max(q_vis, q_orf) * 60000.0   # L/min

def n_critica(Ro_mm=Ro, g_adm=1.40):
    """Velocidad a la que el agua deja de quedarse en el fondo del cangilon.

    La formula de Muysken (N = 50/D^(2/3)) esta calibrada para tornillos
    hidraulicos de obra, con D del orden del metro. Extrapolada a D = 40 mm
    pide 4,1 g de aceleracion centripeta en el radio exterior, cuando entre
    0,5 y 2 m se mantiene en 1,3-1,4 g: el exponente no es coherente con el
    mecanismo. El centrifugado exige omega^2*R < k*g, es decir N ~ D^(-1/2).
    Recalibrando sobre el punto de 1 m sale 250 rpm para Ø40; el criterio
    puro de tambor (omega^2*R = g) da 211. Se trabaja con 1,40 g -> 230-250.
    """
    return 9.5493 * math.sqrt(g_adm * G / (Ro_mm / 1000))

# --------------------------------------------------------------------- eolica
def savonius(v, D=0.120, H=0.150, Cp=0.20, rho=1.225):
    return 0.5 * rho * D * H * v ** 3 * Cp                    # W en el eje

def par_estatico(v, D=0.120, H=0.150, Cts=0.30, rho=1.225):
    return 0.5 * rho * D * H * v ** 2 * (D / 2)               # N m (Cts=1 -> *Cts)

def rpm_libre(v, lam=1.0, R=0.060):
    return lam * v / R * 60 / (2 * math.pi)

# ------------------------------------------------------------------- agronomia
def volumen_canal(D_int=69.0, h=25.0, largo=600.0):
    R = D_int / 2
    a = R ** 2 * math.acos((R - h) / R) - (R - h) * math.sqrt(2 * R * h - h ** 2)
    return a * largo / 1e6                                    # litros

if __name__ == "__main__":
    an = math.pi * (Ro ** 2 - Ri ** 2)
    print(f"{' VALIDACION DEL MODELO ':=^70}")
    seg = Ro ** 2 * math.acos(Ri / Ro) - Ri * math.sqrt(Ro ** 2 - Ri ** 2)
    print(f"  Llenado analitico con paso -> 0 : {seg/an:.4f}")
    for Sx in (3.0, 1.5, 0.6):
        V, k = volumen(Ro, Ri, Sx, ALFA, esp=0.0, nz=40)
        print(f"    numerico con S={Sx:4.1f} mm      : {V/(an*Sx):.4f}")

    print(f"\n{' CIERRE DE CANGILONES ':=^70}")
    print(f"  Criterio exacto  k = S tan(a) / (2 pi Ri) < 1")
    for Sp in (15, 30, 45, 60):
        _, k = nivel(Ri, Sp, ALFA)
        print(f"    paso {Sp:2d} mm a {ALFA:.0f} deg -> k = {k:.3f}"
              f"   {'CIERRA' if k < 1 else 'NO CIERRA'}")

    print(f"\n{' PASO OPTIMO (caudal neto a 300 rpm, alfa=40) ':=^70}")
    print(f"  {'paso':>5} {'V/vuelta':>10} {'llenado':>8} {'fuga L/min':>12} {'neto L/min':>14}")
    for Sp in (10, 12, 15, 20, 24, 30, 36):
        V, k = volumen(Ro, Ri, float(Sp), ALFA, nr=50, nt=160, nz=50)
        f0, f1 = fuga(GAP, Sp, ALFA)
        Q = V * 300 / 1e6
        print(f"  {Sp:5d} {V/1000:8.2f} mL {V/(an*Sp):8.3f} {f0:5.2f}-{f1:<6.2f}"
              f" {max(0,Q-f1):6.2f} - {max(0,Q-f0):<6.2f}")

    print(f"\n{' EFECTO DE LA HOLGURA (paso ' + str(int(S)) + ' mm) ':=^70}")
    V, k = volumen(Ro, Ri, S, ALFA)
    for g in (0.10, 0.15, 0.20, 0.30, 0.50):
        f0, f1 = fuga(g, S, ALFA)
        print(f"  holgura radial {g:.2f} mm -> fuga {f0:5.2f}-{f1:<5.2f} L/min"
              f"   rpm minimas utiles {f0*1e6/V:4.0f}-{f1*1e6/V:<4.0f}")

    print(f"\n{' CAUDAL Y VIENTO ':=^70}")
    print(f"  Volumen por vuelta: {V/1000:.2f} mL   |   velocidad critica del"
          f" tornillo: {n_critica():.0f} rpm")
    print(f"  {'v m/s':>6} {'P eje':>8} {'rpm libre':>10} {'rpm util':>9}"
          f" {'caudal neto L/min':>19}")
    for v in (2, 3, 4, 6):
        N = min(rpm_libre(v), n_critica())
        f0, f1 = fuga(GAP, S, ALFA)
        Q = V * N / 1e6
        print(f"  {v:6.1f} {savonius(v)*1000:6.0f}mW {rpm_libre(v):10.0f}"
              f" {N:9.0f} {max(0,Q-f1):8.2f} - {max(0,Q-f0):<8.2f}")

    print(f"\n{' PAR DISPONIBLE FRENTE A PAR EXIGIDO ':=^70}")
    H_turn = S * math.sin(math.radians(ALFA)) / 1000.0
    M_req = RHO * G * (V / 1e9) * H_turn / (2 * math.pi)
    for v in (2, 3, 5):
        M = par_estatico(v) * 0.30
        print(f"  v={v} m/s: par estatico del rotor {M*1000:6.2f} mNm  frente a"
              f" {M_req*1000:.3f} mNm del tornillo  -> x{M/M_req:.0f}")
    print("  (falta sumar rozamiento de 3 rodamientos 608 y del buje: del orden"
          "\n   de 0.3-1 mNm en total, aun asi con margen amplio)")

    print(f"\n{' BALANCE AGRONOMICO ':=^70}")
    Vc = volumen_canal()
    f0, f1 = fuga(GAP, S, ALFA)
    Qn = max(0, V * n_critica() / 1e6 - (f0 + f1) / 2)
    print(f"  Volumen del canal (Ø69, 25 mm de lamina, 600 mm) : {Vc:.2f} L")
    print(f"  Caudal neto en saturacion ({n_critica():.0f} rpm)          : {Qn:.2f} L/min")
    print(f"  Renovacion del canal                             : {Vc/Qn:.1f} min")
    print(f"  Oxigeno aportado (delta 3 mg/L utiles)           : {Qn*60*3:.0f} mg/h")
    print(f"  Plantas que sostiene (15 mg/h por lechuga)       : {Qn*60*3/15:.1f}")
    print(f"  Reserva disuelta en el canal (8 mg/L)            : {Vc*8:.1f} mg")
    n_pl = Qn * 60 * 3 / 15
    print(f"  Autonomia sin viento                             : {Vc*8/(n_pl*15)*60:.0f} min")
