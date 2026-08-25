#!/usr/bin/env python3
"""
Analisis de los cuatro techos del sistema y de que interviene sobre cada uno.

Reutiliza el modelo de cangilon de analisis_bomba.py. La diferencia principal
respecto a aquel: la velocidad critica del tornillo se recalcula.

    La formula de Muysken (N = 50/D^(2/3)) esta calibrada para tornillos
    hidraulicos de obra. Extrapolada a D = 40 mm pide 4,1 g de aceleracion
    centripeta en el radio exterior, cuando entre 0,5 y 2 m se mantiene en
    1,3-1,4 g. El mecanismo del centrifugado exige N proporcional a D^(-1/2),
    no a D^(-2/3). Recalibrando sobre el punto de 1 m sale 250 rpm para Ø40,
    y el criterio puro de tambor (omega^2 R = g) da 211. Se trabaja con 230.
"""
import math, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from analisis_bomba import volumen, fuga, nivel, volumen_canal

GAP, ESP = 0.15, 2.0
LAM_ROTOR, R_ROTOR = 1.0, 0.060          # Savonius libre: N = lambda*v/R


def n_critica(Ro_mm, g_adm=1.40):
    """rpm a las que omega^2*R alcanza g_adm veces g."""
    R = Ro_mm / 1000
    return 9.5493 * math.sqrt(g_adm * 9.81 / R)


def rpm_libre(v):
    return LAM_ROTOR * v / R_ROTOR * 60 / (2 * math.pi)


def mejor_paso(Ro, Ri, alfa, gap=GAP):
    """Paso que maximiza el caudal neto a la velocidad critica."""
    mejor = None
    for S in [x / 2 for x in range(12, 90)]:
        V, k = volumen(Ro, Ri, S, alfa, esp=ESP, nr=44, nt=150, nz=44)
        if k >= 0.85:
            continue
        f0, f1 = fuga(gap, S, alfa, esp=ESP)
        Q = V * n_critica(Ro) / 1e6 - (f0 + f1) / 2
        if mejor is None or Q > mejor[0]:
            mejor = (Q, S, V, k, (f0 + f1) / 2)
    return mejor


def oxigeno(Q_lmin, T):
    """mg/h de O2 aportados y demanda de una lechuga adulta a T grados.
       Saturacion del agua dulce y Q10 = 2 para la respiracion radicular."""
    Cs = 14.6 - 0.41 * T + 0.008 * T ** 2          # mg/L, ajuste usual
    util = max(0.0, Cs - 5.0)                       # no bajar de 5 mg/L
    aporte = Q_lmin * 60 * util
    demanda = 15.0 * 2 ** ((T - 24) / 10)
    return aporte, demanda, Cs


if __name__ == "__main__":
    Ro, Ri, alfa = 20.0, 7.15, 40.0
    print(f"{' PUNTO DE PARTIDA ':=^72}")
    Q0, S0, V0, k0, f0 = mejor_paso(Ro, Ri, alfa)
    Nc0 = n_critica(Ro)
    print(f"  Ø40, 40 grados, paso {S0:.0f} mm")
    print(f"  velocidad critica {Nc0:.0f} rpm  (antes se usaba 427: extrapolacion mala)")
    print(f"  cangilon {V0/1000:.2f} mL  fuga {f0:.2f} L/min  ->  CAUDAL {Q0:.2f} L/min")
    print(f"  la turbina alcanza esas rpm con {Nc0/159:.1f} m/s: el tornillo esta")
    print(f"  saturado practicamente siempre")

    print(f"\n{' TECHO 1 · CAUDAL: DIAMETRO E INCLINACION ':=^72}")
    print(f"  {'Ø':>4} {'alfa':>5} {'paso':>5} {'Nc':>5} {'cangilon':>9} {'caudal':>8} {'factor':>7}")
    casos = [(20, 40), (20, 32), (20, 25), (25, 40), (25, 32), (25, 25), (30, 32)]
    for D2, a in casos:
        Q, S, V, k, f = mejor_paso(float(D2), Ri, float(a))
        print(f"  {2*D2:4.0f} {a:5.0f} {S:5.0f} {n_critica(D2):5.0f} "
              f"{V/1000:8.2f} mL {Q:6.2f} L/min {Q/Q0:6.2f}x")

    print(f"\n{' TECHO 1 · REDUCCION EN EL MASTIL ':=^72}")
    print(f"  Con 1:1 el tornillo pasa de {Nc0:.0f} rpm en cuanto sopla "
          f"{Nc0/159:.1f} m/s.")
    print(f"  Relacion que lo deja justo en el techo segun el viento medio del sitio:")
    for v in (2, 3, 4, 5, 6):
        print(f"    viento medio {v} m/s -> turbina {rpm_libre(v):4.0f} rpm -> "
              f"reduccion {rpm_libre(v)/Nc0:.1f}:1")

    print(f"\n{' TECHO 2 · PLANTAS: CAUDAL Y TEMPERATURA ':=^72}")
    print(f"  {'T':>4} {'Cs':>6} {'demanda':>9} | " +
          " ".join(f"{q:>6.2f}" for q in (0.25, 0.35, 0.50, 0.70)) + "  L/min")
    for T in (18, 20, 22, 24, 26, 28):
        _, dem, Cs = oxigeno(0.3, T)
        fila = f"  {T:4.0f} {Cs:6.2f} {dem:7.1f}mg/h | "
        for q in (0.25, 0.35, 0.50, 0.70):
            ap, dem, _ = oxigeno(q, T)
            fila += f"{ap/dem:6.1f} "
        print(fila + " plantas")

    print(f"\n{' TECHO 3 · RESERVA EN CALMA ':=^72}")
    Vc = volumen_canal()
    for T, n in ((24, 4), (20, 4), (20, 6)):
        _, dem, Cs = oxigeno(0.3, T)
        reserva = Vc * (Cs - 2.0) / (n * dem) * 60
        print(f"  {T} grados, {n} plantas: {reserva:.0f} min hasta hipoxia "
              f"(Cs={Cs:.1f} mg/L, demanda {n*dem:.0f} mg/h)")
    print("  La reserva apenas se mueve; lo que cambia con la temperatura es la")
    print("  tolerancia de la raiz a esa hipoxia, que escala con el mismo Q10.")

    print(f"\n{' TECHO 4 · VENTANA DE NIVEL ':=^72}")
    z_desc = 180 * math.sin(math.radians(alfa))
    r_carc = 23.15
    for nombre, run, encaje, sobre_eje in (
            ("actual: manguito de encaje", r_carc / math.cos(math.radians(alfa)), 14, 38.5),
            ("copa receptora en el tapon", r_carc / math.cos(math.radians(alfa)), 0, 37.5 + 10),
            ("recogedor anular en la corona", 6, 0, 37.5 + 10)):
        canal = z_desc - (run + encaje) - sobre_eje
        lamina = canal - 34.5 + 25
        n_min = 8 * math.sin(math.radians(alfa)) - Ro * math.cos(math.radians(alfa))
        print(f"  {nombre:32s} lamina {lamina:5.1f} mm  ventana {lamina-n_min:5.1f} mm")
    print("  Alternativa sin tocar geometria: valvula de flotador en el deposito,")
    print("  que convierte la ventana en un ajuste unico de puesta en marcha.")
