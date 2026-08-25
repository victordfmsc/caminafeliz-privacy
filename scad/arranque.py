#!/usr/bin/env python3
"""
Por que un molino asi "apenas se mueve": balance de pares en el ARRANQUE.

El margen que se publico antes (x35 a x216) compara el par del rotor con el
par HIDRAULICO del tornillo, que es minusculo. No es ese el enemigo. Quien
decide si arranca es el ROZAMIENTO del tren: rodamientos, buje y engranajes.
Y con un Savonius el par disponible cae con el cuadrado de la velocidad del
viento, asi que un emplazamiento a media velocidad tiene un cuarto del par.
"""
import math

RHO = 1.225

# ---------------------------------------------------------------- par eolico
def par_rotor(v, D=0.120, H=0.150, Cts=0.30):
    """Par estatico del Savonius. Cts es el coeficiente de par de arranque:
       ~0.30 de media, pero en un rotor de palas RECTAS de dos alabes baja
       hasta ~0.05-0.10 en ciertas posiciones -> puntos muertos. Con el alabe
       helicoidal el minimo sube y practicamente desaparece la posicion en la
       que no arranca."""
    return 0.5 * RHO * D * H * v ** 2 * (D / 2) * Cts


# -------------------------------------------------------------- rozamientos
# Par de arranque, en N m. Los rodamientos 608ZZ nuevos van llenos de grasa:
# su par de arranque lo domina el batido de la grasa, no la rodadura.
ROZ = {
    "608ZZ de fabrica (grasa)":      500e-6,
    "608 desengrasado y aceite fino": 40e-6,
    "buje liso del embudo, seco":    400e-6,
    "buje liso del embudo, mojado":  200e-6,
    "engrane conico impreso":         50e-6,
}


def presupuesto(desengrasados, n_rod=3, mojado=True):
    r = n_rod * ROZ["608 desengrasado y aceite fino" if desengrasados
                    else "608ZZ de fabrica (grasa)"]
    r += ROZ["buje liso del embudo, mojado" if mojado else "buje liso del embudo, seco"]
    r += ROZ["engrane conico impreso"]
    return r


def v_arranque(T_roz, D=0.120, H=0.150, Cts=0.30):
    return math.sqrt(T_roz / (0.5 * RHO * D * H * (D / 2) * Cts))


def inercia(D=0.120, H=0.150, masa=0.25):
    """Momento de inercia aproximado del rotor (masa repartida cerca de R)."""
    return 0.5 * masa * (D / 2) ** 2


def deceleracion(T_roz, D=0.120, H=0.150, masa=0.25, n0=200):
    """Segundos que tarda en pararse desde n0 rpm girandolo a mano."""
    return inercia(D, H, masa) * (n0 * 2 * math.pi / 60) / T_roz


if __name__ == "__main__":
    print(f"{' PAR DISPONIBLE FRENTE A ROZAMIENTO ':=^70}")
    print(f"  {'viento':>7} {'par rotor':>11} {'Ø120x150':>10} {'Ø160x200':>10}")
    for v in (1.5, 2, 3, 4, 6):
        print(f"  {v:5.1f} m/s {par_rotor(v)*1e6:8.0f} uNm "
              f"{par_rotor(v)*1e6:9.0f} {par_rotor(v,0.160,0.200)*1e6:10.0f}")
    print()
    print("  Rozamiento del tren (par de arranque):")
    for k, val in ROZ.items():
        print(f"    {k:34s} {val*1e6:6.0f} uNm")
    print()
    print(f"{' VIENTO MINIMO PARA QUE ARRANQUE ':=^70}")
    for desg, etiqueta in ((False, "rodamientos de fabrica"), (True, "desengrasados")):
        T = presupuesto(desg)
        print(f"  {etiqueta:24s} rozamiento {T*1e6:4.0f} uNm  ->  "
              f"arranca con {v_arranque(T):4.1f} m/s"
              f"   (rotor Ø160x200: {v_arranque(T,0.160,0.200):.1f} m/s)")
    T = presupuesto(True)
    print(f"  {'idem, alabe recto':24s} (Cts 0.10 en el punto muerto)  ->  "
          f"arranca con {v_arranque(T, Cts=0.10):.1f} m/s")
    print()
    print(f"{' EFECTO DEL EMPLAZAMIENTO ':=^70}")
    print("  El par va con v^2, asi que perder velocidad de viento se paga doble:")
    for frac in (1.0, 0.7, 0.5, 0.35):
        print(f"    {frac*100:3.0f} % de la velocidad libre -> {frac**2*100:3.0f} % del par"
              f"   (arranca con {v_arranque(presupuesto(True))/frac:.1f} m/s de viento libre)")
    print()
    print(f"{' DOS PRUEBAS DE ACEPTACION ANTES DE MONTAR EL AGUA ':=^70}")
    for desg, etiqueta in ((False, "de fabrica"), (True, "desengrasados")):
        T = presupuesto(desg)
        m = T / (9.81 * 0.004)
        print(f"  rodamientos {etiqueta:14s}: para en {deceleracion(T):5.1f} s desde 200 rpm"
              f" | arranca con {m*1000:4.1f} g colgando de la varilla")
    print()
    print("  1) PARADA LIBRE: lanza el rotor a mano y cronometra. Si se para en")
    print("     menos de 10 s, hay demasiado rozamiento y no arrancara con brisa.")
    print("  2) PAR DE ARRANQUE: enrolla un hilo en la varilla de Ø8 y cuelga peso.")
    print("     Con 10 g debe arrancar. Si necesita 40 g, los rodamientos siguen")
    print("     con la grasa de fabrica y el molino no se movera con brisa.")
