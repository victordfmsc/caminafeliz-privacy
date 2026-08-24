#!/usr/bin/env python3
"""
Genera montaje.html: la guia de montaje con las imagenes incrustadas.

Las vistas salen de la geometria real (08_montaje.scad sobre los STL) y las
cotas de verificacion.py, que a su vez las lee de los propios modulos. Aqui
solo se maqueta: si cambia un parametro, se regenera todo y los numeros
siguen cuadrando.

Uso:  ./render_stl.sh && python3 infografia.py
"""
import base64, os, re, subprocess, sys

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, "montaje.html")


def img(ruta):
    with open(os.path.join(AQUI, ruta), "rb") as f:
        return "data:image/png;base64," + base64.b64encode(f.read()).decode()


def paso(n):
    return img(f"img/montaje/paso_{n}.png")


def pieza(nombre):
    return img(f"img/piezas/{nombre}.png")


# --------------------------------------------------------------- inventario
# (archivo del thumbnail, nombre, cantidad, modulo, con que casa)
PIEZAS = [
    ("01_tornillo_arquimedes_calibre", "Calibre", "1", "M1",
     "Anillo de carcasa y tramo de tornillo. Se imprime el primero y se mide."),
    ("01_tornillo_arquimedes_tornillo", "Tornillo", "1", "M1",
     "Helicoide &Oslash;40, paso 15, 12 vueltas. Sobre varilla &Oslash;8, dentro de la carcasa."),
    ("01_tornillo_arquimedes_carcasa", "Carcasa", "1", "M1",
     "Tubo &Oslash;40,3 interior. Recibe embudo abajo y torreta arriba."),
    ("01_tornillo_arquimedes_embudo", "Embudo", "1", "M1",
     "Boca &Oslash;90 con ventanas y ara&ntilde;a de buje. Calza sobre la carcasa."),
    ("02_turbina_savonius_rotor", "Rotor Savonius", "1", "M2",
     "&Oslash;120 &times; 150, &aacute;labe helicoidal 120&deg;. Sobre el m&aacute;stil."),
    ("02_turbina_savonius_disco_sup", "Disco superior", "1", "M2",
     "Ranura helicoidal ciega. Se encola sobre el canto del &aacute;labe."),
    ("02_turbina_savonius_disco_medio", "Disco intermedio", "1", "M2",
     "Ranura pasante. Se <em>enrosca</em> hasta media altura."),
    ("03_soporte_rodamientos_soporte", "Torreta", "1", "M3",
     "Los dos alojamientos 608, a 90&deg; y a 50&deg;. Atornilla a la corona."),
    ("03_soporte_rodamientos_engranaje", "Engranaje c&oacute;nico", "2", "M3",
     "1:1, m&oacute;dulo 2, z=20, cono primitivo 65&deg;. Uno por eje."),
    ("03_soporte_rodamientos_alojamiento", "Chumacera 608", "1", "M3",
     "Segundo apoyo del m&aacute;stil, sobre el travesa&ntilde;o del bastidor."),
    ("03_soporte_rodamientos_acople", "Acople r&iacute;gido", "0-1", "M3",
     "Alternativa al par c&oacute;nico si alineas los ejes. No se usa en este montaje."),
    ("04_tapones_pvc75_tapon_a", "Tap&oacute;n A &mdash; entrada", "1", "M4",
     "Manguito de vertido y placa aireadora de 7 agujeros."),
    ("04_tapones_pvc75_tapon_b", "Tap&oacute;n B &mdash; rebose", "1", "M4",
     "Asiento del racor y dos respiraderos a 45&deg;."),
    ("04_tapones_pvc75_racor", "Racor de rebose", "1+1", "M4",
     "Brida interior: la presi&oacute;n lo aprieta contra su asiento."),
    ("05_net_cup_50_vaso", "Vaso de cultivo", "4", "M5",
     "&Oslash;50 ranurado. Cuelga del taladro &Oslash;50,6 del canal."),
    ("05_net_cup_50_plantilla", "Plantilla", "1", "M5",
     "Galga curva para marcar dos taladros al paso elegido."),
    ("06_estructura_conector_3v", "Conector 3 v&iacute;as", "6", "M6",
     "Nudo de esquina para bastidor de tubo &Oslash;25."),
    ("06_estructura_abraz_canal", "Abrazadera de canal", "2", "M6",
     "Cuna de 200&deg;: el canal &Oslash;75 entra a presi&oacute;n."),
    ("06_estructura_abraz_carcasa", "Abrazadera de carcasa", "1", "M6",
     "Cuna ya girada a 40&deg;. Es la que fija el &aacute;ngulo de trabajo."),
    ("06_estructura_sop_mastil", "Soporte de m&aacute;stil", "1", "M6",
     "Abrazadera con alojamiento 608 de eje vertical."),
]

# ------------------------------------------------------------------- pasos
# (n, titulo, entra, cuerpo, cota critica o None, aviso o None)
PASOS = [
    (1, "Varilla y tornillo",
     "Varilla &Oslash;8 &middot; Tornillo",
     "Pasa la varilla por el n&uacute;cleo del tornillo y aprieta los dos prisioneros M3, "
     "situados a 8&nbsp;mm de cada extremo. El agujero es de &Oslash;8,3: entra a presi&oacute;n suave, "
     "no debe bailar.",
     "La varilla asoma 30 mm por debajo del tornillo",
     "Ese saliente es el que luego enhebra el buje del embudo. Si lo cortas al ras, "
     "el extremo inferior del eje se queda sin gu&iacute;a."),
    (2, "El tornillo entra en la carcasa",
     "Carcasa",
     "Se introduce desde arriba, por la boca de la corona. El conjunto queda "
     "flotando dentro del tubo hasta que se monte el rodamiento.",
     "Holgura radial 0,15 mm &mdash; &Oslash;40,0 en &Oslash;40,3",
     "Gira el tornillo a mano una vuelta completa: tiene que girar libre en todo el "
     "recorrido. Si roza, el culpable casi siempre es la costura de capa; l&iacute;jala."),
    (3, "Embudo por debajo",
     "Embudo",
     "Enhebra la varilla que asoma por el buje de la ara&ntilde;a y empuja el embudo hasta "
     "el tope: el casquillo de 20&nbsp;mm calza sobre el tubo de la carcasa.",
     "Casquillo &Oslash;46,9 sobre carcasa &Oslash;46,3",
     "Es un ajuste deslizante, no aprieta. Fija el embudo con dos tornillos M3 "
     "autorroscantes a trav&eacute;s del casquillo, o no aguantar&aacute; el tir&oacute;n al sacarlo del agua."),
    (4, "Torreta sobre la corona",
     "Torreta &middot; Rodamiento 608",
     "Cuatro M4 en el c&iacute;rculo de &Oslash;60. El rodamiento entra por arriba en el buje "
     "central y apoya en el labio; as&iacute; el empuje de la bomba lo aprieta contra su "
     "asiento en vez de expulsarlo.",
     None,
     None),
    (5, "Engranaje del tornillo",
     "Engranaje c&oacute;nico (1 de 2)",
     "Va sobre la varilla del tornillo, con el dentado mirando hacia arriba. Su "
     "posici&oacute;n axial no es libre: define d&oacute;nde cae el &aacute;pice de los conos, y con &eacute;l todo "
     "el engrane.",
     "4,7 mm entre la cara alta del buje central y la base del cubo",
     "Mide con una galga o un trozo de varilla de 4,7&nbsp;mm antes de apretar el prisionero. "
     "Si lo montas a ojo, el par entra forzado o con juego excesivo."),
    (6, "Engranaje y varilla del m&aacute;stil",
     "Engranaje c&oacute;nico (2 de 2) &middot; Varilla &Oslash;8",
     "Segundo rodamiento en el brazo inclinado y engranaje enfrentado. Los dos ejes "
     "se cortan en un punto com&uacute;n &mdash;el &aacute;pice&mdash; y cada cono primitivo vale la mitad del "
     "&aacute;ngulo entre ejes.",
     "14,7 mm entre la cara del buje del brazo y la base del cubo",
     "Antes de apretar, gira el conjunto una vuelta entera con la mano. Debe girar "
     "suave y sin puntos duros: el juego entre flancos de dise&ntilde;o es 0,30&nbsp;mm."),
    (7, "Rotor Savonius",
     "Rotor &middot; Disco superior &middot; Disco intermedio",
     "El disco intermedio no se desliza: se <em>enrosca</em>, porque el &aacute;labe est&aacute; "
     "torsionado 120&deg;. El superior encaja en su ranura ciega y se encola.",
     "Cubo D-shaft: cota D 7,0 mm",
     "Lima un plano en la varilla, o cambia <code>cubo_tipo</code> a redondo y f&iacute;ate solo "
     "del prisionero. Con el par que mueve esto sobra, pero el plano evita que el cubo "
     "se muerda con el tiempo."),
    (8, "Canal y tapones",
     "Tap&oacute;n A &middot; Tap&oacute;n B &middot; Racor",
     "Taladra el canal con la plantilla. El racor se introduce desde <em>dentro</em> del "
     "Tap&oacute;n B antes de calzarlo sobre el tubo: la brida queda contra la cara interior "
     "del fondo.",
     "Taladros &Oslash;50,6 cada 150 mm",
     "La separaci&oacute;n de 150&nbsp;mm no es est&eacute;tica: es el l&iacute;mite de ox&iacute;geno para lechuga. "
     "Con albahaca puedes bajar a 100-120&nbsp;mm."),
    (9, "Vasos de cultivo",
     "4 &times; Vaso",
     "Entran por el taladro y apoyan por la pesta&ntilde;a. La base del vaso queda "
     "exactamente a la altura de la l&aacute;mina de agua: la ra&iacute;z baja al agua y la parte "
     "alta del cepell&oacute;n respira en aire h&uacute;medo.",
     "Base del vaso a 0 mm de la l&aacute;mina",
     "Para pl&aacute;ntula, sube <code>nivel_agua</code> a 28&nbsp;mm y la base queda 3&nbsp;mm sumergida."),
    (10, "Bastidor, nivelaci&oacute;n y agua",
     "Conectores &middot; Abrazaderas &middot; Dep&oacute;sito",
     "La abrazadera de carcasa fija el &aacute;ngulo de 40&deg;. El canal se coloca <em>despu&eacute;s</em>, "
     "desliz&aacute;ndolo hasta que el manguito quede a plomo bajo la boquilla; nunca al rev&eacute;s.",
     "L&aacute;mina del dep&oacute;sito entre &minus;10 y +12 mm respecto a la base de la carcasa",
     "Es la cota que decide si el invento funciona. Por debajo de &minus;10 el primer "
     "cangil&oacute;n no coge agua; por encima de +12 el retorno del canal deja de bajar por "
     "gravedad."),
]

# ------------------------------------------------------------ verificacion
def verificacion():
    """Ejecuta verificacion.py y devuelve las comprobaciones agrupadas."""
    r = subprocess.run([sys.executable, os.path.join(AQUI, "verificacion.py")],
                       capture_output=True, text=True, cwd=AQUI)
    grupos, actual = [], None
    for linea in r.stdout.splitlines():
        m = re.match(r"=== \d+\. (.+?) =+$", linea)
        if m:
            actual = (m.group(1).strip(), [])
            grupos.append(actual)
            continue
        m = re.match(r"\s*\[(OK |MAL)\] (.{1,44}?)\s{2,}(.+)$", linea)
        if m and actual:
            actual[1].append((m.group(1).strip() == "OK", m.group(2), m.group(3)))
    if not grupos:
        sys.exit("verificacion.py no devolvio comprobaciones:\n" + r.stdout + r.stderr)
    return grupos, all(ok for _, gs in grupos for ok, _, _ in gs)


def main():
    grupos, todo_ok = verificacion()
    n_chk = sum(len(g[1]) for g in grupos)

    tpl = open(os.path.join(AQUI, "montaje.tpl.html"), encoding="utf-8").read()

    inventario = "\n".join(
        f'''<figure class="pieza">
      <img src="{pieza(f)}" alt="{n}" loading="lazy">
      <figcaption><span class="pz-cant">{c}&times;</span><span class="pz-nom">{n}</span>
      <span class="pz-mod">{m}</span><p>{d}</p></figcaption>
    </figure>''' for f, n, c, m, d in PIEZAS)

    pasos = []
    for n, tit, entra, cuerpo, cota, aviso in PASOS:
        bloque = f'''<section class="paso" id="paso-{n}">
      <div class="paso-img"><img src="{paso(n)}" alt="Paso {n}: {tit}" loading="lazy"></div>
      <div class="paso-txt">
        <p class="paso-n">Paso {n:02d}</p>
        <h3>{tit}</h3>
        <p class="entra">Entra: {entra}</p>
        <p>{cuerpo}</p>'''
        if cota:
            bloque += f'\n        <p class="cota"><span>Cota</span>{cota}</p>'
        if aviso:
            bloque += f'\n        <p class="aviso">{aviso}</p>'
        bloque += "\n      </div>\n    </section>"
        pasos.append(bloque)

    filas = []
    for titulo, checks in grupos:
        filas.append(f'<tr class="grupo"><th colspan="3">{titulo}</th></tr>')
        for ok, que, val in checks:
            marca = "OK" if ok else "MAL"
            filas.append(f'<tr><td class="est {"si" if ok else "no"}">{marca}</td>'
                         f'<td>{que}</td><td class="val">{val}</td></tr>')

    html = (tpl.replace("{{INVENTARIO}}", inventario)
               .replace("{{PASOS}}", "\n".join(pasos))
               .replace("{{VERIFICACION}}", "\n".join(filas))
               .replace("{{N_CHECKS}}", str(n_chk))
               .replace("{{ESTADO}}", "todas pasan" if todo_ok else "CON FALLOS")
               .replace("{{HERO}}", paso(10)))
    open(SALIDA, "w", encoding="utf-8").write(html)
    print(f"{SALIDA}  ({os.path.getsize(SALIDA)/1e6:.2f} MB, {n_chk} comprobaciones)")


if __name__ == "__main__":
    main()
