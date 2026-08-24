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
//    "tapon_a" | "tapon_b" | "racor" | "conjunto"
//
//  IMPRESION SIN SOPORTES
//    El racor de rebose va SUELTO. Integrado obligaba a elegir entre
//    apoyar el tapon sobre la punta del racor o puentear un fondo de
//    Ø69: separandolo, el Tapon B se imprime como un vaso boca arriba y
//    no lleva ni un gramo de soporte. La brida del racor asienta por
//    DENTRO, contra la cara interior del fondo, asi que la presion del
//    agua lo aprieta contra su asiento en vez de expulsarlo. Ademas la
//    junta queda justo en la linea de agua, con carga practicamente
//    nula. Solo el manguito del Tapon A necesita soporte.
// =====================================================================

$fn = 100;

/* [Pieza a renderizar] */
pieza = "tapon_a";       // ["tapon_a","tapon_b","racor","conjunto"]

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
// El agua no cae por un solo agujero sino por una placa perforada: el
// chorro se rompe en gotas y se airea. No es un adorno: con 0.34 L/min y
// 4 lechugas adultas el oxigeno aportado (61 mg/h) iguala justo la demanda
// (60 mg/h), asi que el agua tiene que entrar saturada. Ver analisis_bomba.py
aireador_n = 6;          // agujeros perifericos (mas uno central)
aireador_d = 5;          // diametro de cada agujero
boquilla_d = 32;         // Ø EXTERIOR de la boquilla del Modulo 1
manguito_h = 15;         // vuelo del manguito sobre la generatriz del tubo
// La profundidad de encaje NO se fija a mano: se deriva del hombro de
// tope, que se situa 4 mm por encima de la generatriz superior del
// conducto. Fijarla a mano hacia caer el hombro justo en la tangente de
// la camara y generaba una degeneracion (solido no valido).

/* [Tapon B - salida / rebose] */
// Respiradero: sin el, el canal queda estanco y las raices aereas (las que
// quedan por encima de la lamina) no tienen renovacion de aire. Es lo que
// da autonomia durante las calmas: el agua del canal se queda sin oxigeno
// en unos 6 minutos, y a partir de ahi la planta respira por arriba.
// Son dos taladros a +-45 grados sobre la generatriz alta: asi no entra
// lluvia de frente y, al imprimir el tapon con el eje vertical, quedan
// como simples agujeros horizontales que no piden soporte.
respiradero_d = 6;
respiradero_a = 45;      // separacion angular respecto a la vertical
brida_racor_d = 24;      // brida interior del racor
brida_racor_e = 3;
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
z_hombro       = cam_int_d / 2 + 4;            // tope de la boquilla
manguito_p     = z_manguito - z_hombro;        // profundidad de encaje
x_manguito     = encaje + camara / 2;

// Eje del racor: su generatriz inferior queda a 'nivel_agua' del suelo
z_racor    = -tubo_int_d / 2 + nivel_agua + racor_int_d / 2;
racor_ext_d= racor_int_d + 2 * pared;          // 18

assert(nivel_agua + racor_int_d < tubo_int_d, "El racor no cabe en el tubo.");
assert(camara > manguito_ext_d * 0.8, "Camara corta para el manguito de entrada.");
assert(manguito_p >= 12, "Encaje de la boquilla demasiado corto: sube manguito_h.");

echo(str("Tapon: L=", largo, " casquillo Øint=", casq_int_d, " Øext=", casq_ext_d));
echo(str("Tapon B: eje del racor a z=", z_racor,
         " (lamina de agua de ", nivel_agua, " mm)"));
echo(str("Aireador: ", aireador_n + 1, " agujeros de ", aireador_d, " mm"));
echo(str("Boca del manguito a ", z_manguito, " mm sobre el eje del canal; ",
         "la boquilla del Modulo 1 debe terminar a ", z_manguito - manguito_p,
         " mm sobre ese eje."));

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
        // Encaje de la boquilla
        translate([x_manguito, 0, z_hombro])
            cylinder(d = manguito_int_d, h = manguito_p + 0.01);
        // Placa aireadora: rompe el chorro en gotas al entrar en la camara
        translate([x_manguito, 0, 0]) {
            translate([0, 0, cam_int_d / 2 - 6])
                cylinder(d = aireador_d, h = z_hombro - cam_int_d / 2 + 6.01);
            for (i = [0 : aireador_n - 1])
                rotate([0, 0, i * 360 / aireador_n])
                    translate([8, 0, cam_int_d / 2 - 6])
                        cylinder(d = aireador_d,
                                 h = z_hombro - cam_int_d / 2 + 6.01);
        }
        // La camara se vacia despues del manguito para dejarlo hueco
        tubo_x(cam_int_d, encaje, camara);
    }
}

// =====================================================================
//  TAPON B - SALIDA / REBOSE
// =====================================================================
// Racor de rebose SUELTO. Se introduce desde dentro del tapon antes de
// montarlo sobre el tubo: la brida queda contra la cara interior del fondo
// y el agua la aprieta contra su asiento.
module racor() {
    paso_d = racor_int_d + 2 * pared;          // 18, atraviesa el fondo
    l_pua  = racor_pua ? 9 : 0;
    difference() {
        union() {
            cylinder(d = brida_racor_d, h = brida_racor_e);          // brida
            cylinder(d = paso_d - tol, h = brida_racor_e + fondo);   // paso
            translate([0, 0, brida_racor_e + fondo])
                cylinder(d = racor_ext_d, h = racor_l - l_pua);
            if (racor_pua)
                for (i = [0 : 2])
                    translate([0, 0, brida_racor_e + fondo + racor_l - l_pua + i * 3])
                        cylinder(d1 = racor_ext_d + 3, d2 = racor_ext_d, h = 3);
        }
        translate([0, 0, -0.5])
            cylinder(d = racor_int_d, h = brida_racor_e + fondo + racor_l + 1);
    }
}

module tapon_b() {
    x_resp = encaje + camara * 0.6;
    difference() {
        cuerpo();
        // Paso del racor a traves del fondo, a la altura de rebose
        translate([encaje + camara - 1, 0, z_racor]) rotate([0, 90, 0])
            cylinder(d = racor_int_d + 2 * pared + tol, h = fondo + 2);
        // Asiento de la brida, por dentro
        translate([encaje + camara - brida_racor_e, 0, z_racor]) rotate([0, 90, 0])
            cylinder(d = brida_racor_d + tol, h = brida_racor_e + 0.01);
        // Respiraderos
        for (sy = [-1, 1])
            rotate([respiradero_a * sy, 0, 0])
                translate([x_resp, 0, 0])
                    cylinder(d = respiradero_d, h = cam_ext_d);
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "tapon_a") tapon_a();
else if (pieza == "tapon_b") tapon_b();
else if (pieza == "racor") racor();
else if (pieza == "conjunto") {
    color("SeaGreen") tapon_a();
    color("Teal") translate([420, 0, 0]) mirror([1, 0, 0]) tapon_b();
    color("DarkCyan") translate([420 - encaje - camara + brida_racor_e, 0, z_racor])
        rotate([0, 90, 0]) racor();
    // referencia: tramo de tubo PVC
    color("Gainsboro", 0.35)
        difference() {
            tubo_x(tubo_ext_d, encaje, 420 - 2 * encaje);
            tubo_x(tubo_int_d, encaje - 1, 420 - 2 * encaje + 2);
        }
}
