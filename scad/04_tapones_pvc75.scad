// =====================================================================
//  MODULO 4 - Tapones para el canal horizontal (tubo PVC Ø75)
//    Tapon A (entrada) : recibe la boquilla del tornillo (Modulo 1)
//    Tapon B (salida)  : rebose que fija la lamina de agua de reserva
// ---------------------------------------------------------------------
//  REGLAS GLOBALES: $fn = 100 | pared min. 3 mm | tolerancia 0.3 mm
//  Sin librerias externas.
// ---------------------------------------------------------------------
//  El eje del tubo es el eje X y la vertical es +Z, de modo que las
//  cotas de altura de agua se leen directamente sobre el modelo.
//  IMPRESION: girar 90 grados en el laminador (eje del tubo vertical,
//  fondo sobre la cama). El manguito del Tapon A queda horizontal y
//  pide soporte ligero solo bajo su vuelo.
// ---------------------------------------------------------------------
//  PIEZAS
//    "tapon_a" | "tapon_b" | "conjunto"
// =====================================================================

$fn = 100;

/* [Pieza a renderizar] */
pieza = "tapon_a";       // ["tapon_a","tapon_b","conjunto"]

/* [Reglas globales] */
tol   = 0.3;
pared = 3;

/* [Tubo PVC comercial] */
tubo_ext_d  = 75;        // diametro exterior nominal
tubo_pared  = 3;         // MEDIR el tubo real (evacuacion Ø75: 1.8 o 3.0 mm)
modo_encaje = "exterior";// ["exterior","interior"]  casquillo sobre el tubo
                         // o tapon macho dentro del tubo

/* [Geometria del tapon] */
encaje   = 30;           // longitud de solape con el tubo
camara   = 35;           // camara util (aloja el manguito o el racor)
fondo    = 4;            // espesor del fondo

/* [Tapon A - entrada] */
boquilla_d = 32;         // Ø EXTERIOR de la boquilla del Modulo 1
manguito_h = 22;         // vuelo del manguito sobre la generatriz del tubo
manguito_p = 20;         // profundidad de encaje de la boquilla

/* [Tapon B - salida / rebose] */
nivel_agua = 25;         // lamina de agua sobre la base interior del tubo
racor_int_d= 12;         // paso libre del racor de salida
racor_l    = 28;         // vuelo del racor
racor_pua  = true;       // espigas para manguera flexible

// ---------------------------------------------------------------------
//  COTAS DERIVADAS
// ---------------------------------------------------------------------
tubo_int_d  = tubo_ext_d - 2 * tubo_pared;     // 69
casq_int_d  = tubo_ext_d + 2 * tol;            // 75.6  (encaje exterior)
casq_ext_d  = casq_int_d + 2 * pared;          // 81.6
macho_ext_d = tubo_int_d - 2 * tol;            // 68.4  (encaje interior)
macho_int_d = macho_ext_d - 2 * pared;         // 62.4
cam_int_d   = tubo_int_d;                      // 69
cam_ext_d   = cam_int_d + 2 * pared;           // 75
largo       = encaje + camara + fondo;         // 69

manguito_int_d = boquilla_d + 2 * tol;         // 32.6 encaje deslizante
manguito_ext_d = manguito_int_d + 2 * pared;   // 38.6
z_manguito     = cam_ext_d / 2 + manguito_h;   // cota de la boca del manguito
x_manguito     = encaje + camara / 2;

// Eje del racor: su generatriz inferior queda a 'nivel_agua' del suelo
z_racor    = -tubo_int_d / 2 + nivel_agua + racor_int_d / 2;
racor_ext_d= racor_int_d + 2 * pared;          // 18

assert(nivel_agua + racor_int_d < tubo_int_d, "El racor no cabe en el tubo.");
assert(camara > manguito_ext_d * 0.8, "Camara corta para el manguito de entrada.");

echo(str("Tapon: L=", largo, " casquillo Øint=", casq_int_d, " Øext=", casq_ext_d));
echo(str("Tapon B: eje del racor a z=", z_racor,
         " (lamina de agua de ", nivel_agua, " mm)"));

// =====================================================================
//  CUERPO COMUN
//    encaje (casquillo o macho) + camara + fondo, eje = X
// =====================================================================
module tubo_x(d, x0, l) { translate([x0, 0, 0]) rotate([0, 90, 0]) cylinder(d = d, h = l); }

module cuerpo() {
    difference() {
        union() {
            if (modo_encaje == "exterior") {
                tubo_x(casq_ext_d, 0, encaje);
                // transicion a 45 grados: autoportante al imprimir de pie
                translate([encaje, 0, 0]) rotate([0, 90, 0])
                    cylinder(d1 = casq_ext_d, d2 = cam_ext_d, h = (casq_ext_d - cam_ext_d) / 2);
            } else {
                tubo_x(macho_ext_d, 0, encaje);
                tubo_x(casq_ext_d, encaje - 4, 4);   // brida de tope contra el canto
            }
            tubo_x(cam_ext_d, encaje, camara + fondo);
        }
        // vaciados: el escalon entre encaje y camara hace de tope del tubo
        if (modo_encaje == "exterior") tubo_x(casq_int_d, -0.5, encaje + 0.5);
        else                           tubo_x(macho_int_d, -0.5, encaje + 0.5);
        tubo_x(cam_int_d, encaje, camara);
    }
}

// =====================================================================
//  TAPON A - ENTRADA
// =====================================================================
module tapon_a() {
    difference() {
        union() {
            cuerpo();
            // Manguito vertical que recibe la boquilla del Modulo 1
            translate([x_manguito, 0, 0])
                cylinder(d = manguito_ext_d, h = z_manguito);
        }
        // Encaje de la boquilla + paso libre hacia la camara
        translate([x_manguito, 0, z_manguito - manguito_p])
            cylinder(d = manguito_int_d, h = manguito_p + 0.01);
        translate([x_manguito, 0, 0])
            cylinder(d = manguito_int_d - 2 * pared, h = z_manguito - manguito_p + 0.01);
        // La camara se vacia despues del manguito para dejarlo hueco
        tubo_x(cam_int_d, encaje, camara);
    }
}

// =====================================================================
//  TAPON B - SALIDA / REBOSE
// =====================================================================
module racor() {
    l_recto = racor_l - (racor_pua ? 9 : 0);
    cylinder(d = racor_ext_d, h = l_recto);
    if (racor_pua)
        for (i = [0 : 2])
            translate([0, 0, l_recto - 9 + i * 3])
                cylinder(d1 = racor_ext_d + 3, d2 = racor_ext_d, h = 3);
}

module tapon_b() {
    difference() {
        union() {
            cuerpo();
            // Racor axial a la altura de rebose
            translate([largo, 0, z_racor]) rotate([0, 90, 0]) racor();
        }
        // Paso del rebose: atraviesa el fondo
        translate([encaje + camara - 6, 0, z_racor]) rotate([0, 90, 0])
            cylinder(d = racor_int_d, h = fondo + racor_l + 8);
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "tapon_a") tapon_a();
else if (pieza == "tapon_b") tapon_b();
else {
    color("SeaGreen") tapon_a();
    color("Teal") translate([420, 0, 0]) mirror([1, 0, 0]) tapon_b();
    // referencia: tramo de tubo PVC
    color("Gainsboro", 0.35)
        difference() {
            tubo_x(tubo_ext_d, encaje, 420 - 2 * encaje);
            tubo_x(tubo_int_d, encaje - 1, 420 - 2 * encaje + 2);
        }
}
