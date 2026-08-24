// =====================================================================
//  MODULO 1 - Tornillo de Arquimedes corto + carcasa
//  Bomba de elevacion de agua para canal hidroponico horizontal
// ---------------------------------------------------------------------
//  REGLAS GLOBALES DEL PROYECTO
//    * $fn = 100 en todas las superficies curvas
//    * Grosor de pared minimo ........ 3.0 mm
//    * Tolerancia de ensamblaje ...... 0.3 mm
//        - union deslizante / rotativa : diametro + 2*tol  (0.3 mm radial)
//        - encaje a presion            : diametro + 1*tol  (0.3 mm diametral)
//    * Sin librerias externas: el archivo renderiza tal cual
// ---------------------------------------------------------------------
//  PIEZAS
//    "tornillo" -> rotor helicoidal          (imprimir de pie, sin soportes)
//    "carcasa"  -> tubo + boquilla de vertido (imprimir de pie, boca abajo)
//    "embudo"   -> copa/embudo de entrada     (imprimir de pie, boca arriba)
//    "conjunto" -> vista de montaje (NO exportar a STL)
//
//  La carcasa se divide en tubo (200 mm) + embudo (57 mm) para que ambas
//  piezas quepan de pie en camas de 220-250 mm sin partir el conducto.
// =====================================================================

$fn = 100;

/* [Pieza a renderizar] */
pieza = "tornillo";      // ["tornillo","carcasa","embudo","conjunto"]

/* [Reglas globales] */
tol   = 0.3;             // tolerancia de ensamblaje
pared = 3;               // grosor de pared minimo

/* [Rotor helicoidal] */
helice_d    = 40;        // diametro exterior del helicoide
paso        = 30;        // avance por vuelta
largo_util  = 180;       // longitud total del filete
eje_d       = 8;         // varilla metalica pasante
helice_esp  = 2.2;       // espesor axial del filete
seg_vuelta  = 24;        // segmentos por vuelta (resolucion del helicoide)
prisionero  = true;      // taladro M3 para prisionero en el cubo

/* [Carcasa] */
carcasa_largo = 200;     // longitud del tubo recto
embudo_d      = 90;      // diametro de boca del embudo de entrada
embudo_h      = 45;      // altura del cono del embudo
embudo_encaje = 20;      // profundidad del casquillo embudo-tubo
embudo_patas  = 12;      // patas que separan la boca del fondo del deposito
embudo_buje   = true;    // arana con buje liso que guia el extremo inferior
                         // del eje (cojinete lubricado por el propio agua)
boquilla_d    = 32;      // diametro EXTERIOR de la boquilla de vertido
boquilla_ang  = 25;      // inclinacion del tramo oblicuo respecto a la horizontal
boquilla_vuelo= 55;      // salida horizontal desde el eje del tubo
bajante_h     = 30;      // tramo vertical final que entra en el Tapon A (Modulo 4)
brida_sup     = true;    // corona superior para atornillar el soporte (Modulo 3)
pcd_brida     = 60;      // circulo de taladros M4 de la corona superior

// ---------------------------------------------------------------------
//  COTAS DERIVADAS
// ---------------------------------------------------------------------
eje_agujero_d = eje_d + tol;              // 8.3  - encaje a presion sobre varilla
nucleo_d      = eje_agujero_d + 2*pared;  // 14.3 - respeta la pared minima
carcasa_int_d = helice_d + 2*tol;         // 40.6 - deslizamiento del helicoide
carcasa_ext_d = carcasa_int_d + 2*pared;  // 46.6
casq_int_d    = carcasa_ext_d + 2*tol;    // 47.2 - el embudo calza sobre el tubo
casq_ext_d    = casq_int_d + 2*pared;     // 53.2
boquilla_int_d= boquilla_d - 2*pared;     // 26.0
m3_broca      = 2.8;                      // roscado directo en plastico para M3
m4_paso       = 4.4;                      // taladro pasante M4

assert(pared >= 3, "El grosor de pared no puede bajar de 3 mm.");
assert(nucleo_d < helice_d - 4, "El nucleo invade el filete: reduce el eje o sube helice_d.");

echo(str("Carcasa: Dint=", carcasa_int_d, " Dext=", carcasa_ext_d));
echo(str("Tornillo: nucleo=", nucleo_d, " agujero eje=", eje_agujero_d,
         " vueltas=", largo_util/paso));

// =====================================================================
//  UTILIDADES
// =====================================================================

// Sector circular macizo de espesor t (rebanada elemental del helicoide)
module sector(r, ang, t, pasos = 8) {
    linear_extrude(height = t)
        polygon(concat([[0, 0]],
                       [for (i = [0 : pasos])
                            let (a = ang * i / pasos) [r * cos(a), r * sin(a)]]));
}

// Helicoide macizo generado por barrido de sectores encadenados con hull().
// Se construye asi (y no con linear_extrude+twist) porque el twist adelgaza
// el filete a medida que crece el radio; con hull() el espesor axial es
// constante en todo el radio y por tanto imprimible y resistente.
module helicoide(r, largo, paso, esp, seg) {
    dz    = paso / seg;              // avance por rebanada
    dang  = 360 / seg;               // giro por rebanada
    n     = round(largo / dz);
    for (i = [0 : n - 1])
        hull() {
            translate([0, 0, i * dz])
                rotate([0, 0, i * dang]) sector(r, dang, esp);
            translate([0, 0, (i + 1) * dz])
                rotate([0, 0, (i + 1) * dang]) sector(r, dang, esp);
        }
}

// =====================================================================
//  PIEZA A - ROTOR (TORNILLO DE ARQUIMEDES)
// =====================================================================
module tornillo() {
    difference() {
        union() {
            // Nucleo / cubo sobre la varilla
            cylinder(d = nucleo_d, h = largo_util);
            // Filete helicoidal, solapado 0.5 mm con el nucleo
            intersection() {
                helicoide(helice_d / 2, largo_util, paso, helice_esp, seg_vuelta);
                cylinder(d = helice_d, h = largo_util);   // recorta a Ø exterior exacto
            }
            // Collarines de apoyo en los extremos (topes contra rodamientos)
            cylinder(d = nucleo_d + 4, h = 4);
            translate([0, 0, largo_util - 4]) cylinder(d = nucleo_d + 4, h = 4);
        }
        // Agujero pasante para la varilla
        translate([0, 0, -1]) cylinder(d = eje_agujero_d, h = largo_util + 2);
        // Prisioneros M3 radiales en ambos extremos
        if (prisionero)
            for (z = [8, largo_util - 8])
                translate([0, 0, z]) rotate([0, 90, 0])
                    cylinder(d = m3_broca, h = nucleo_d);
    }
}

// =====================================================================
//  PIEZA B - CARCASA (TUBO + EMBUDO + BOQUILLA)
// =====================================================================

// Boquilla de vertido: tramo oblicuo + codo + bajante vertical que encaja
// dentro del Tapon A del Modulo 4.
module boquilla_solida(d) {
    l_obl = boquilla_vuelo / cos(boquilla_ang);
    z_cod = -boquilla_vuelo * tan(boquilla_ang);
    // tramo oblicuo (hacia +X y hacia abajo)
    rotate([0, 90 + boquilla_ang, 0]) cylinder(d = d, h = l_obl);
    // codo
    translate([boquilla_vuelo, 0, z_cod]) sphere(d = d);
    // bajante vertical
    translate([boquilla_vuelo, 0, z_cod - bajante_h]) cylinder(d = d, h = bajante_h);
}

module carcasa() {
    z_boq = carcasa_largo - 20;      // altura del eje de la boquilla
    difference() {
        union() {
            // Tubo principal
            cylinder(d = carcasa_ext_d, h = carcasa_largo);
            // Boquilla de vertido
            translate([0, 0, z_boq]) boquilla_solida(boquilla_d);
            // Corona superior de anclaje del soporte (Modulo 3)
            if (brida_sup)
                translate([0, 0, carcasa_largo - 6])
                    cylinder(d = pcd_brida + 14, h = 6);
        }
        // ---- vaciados ----
        // Conducto principal
        translate([0, 0, -1]) cylinder(d = carcasa_int_d, h = carcasa_largo + 2);
        // Conducto de la boquilla (perfora la pared del tubo)
        translate([0, 0, z_boq]) boquilla_solida(boquilla_int_d);
        // Taladros M4 de la corona superior
        if (brida_sup)
            for (a = [45 : 90 : 359])
                rotate([0, 0, a]) translate([pcd_brida / 2, 0, carcasa_largo - 7])
                    cylinder(d = m4_paso, h = 8);
    }
}

// =====================================================================
//  PIEZA C - EMBUDO / COPA DE ENTRADA
//  Calza a presion sobre el extremo inferior de la carcasa (casquillo de
//  47.2 mm) y mantiene la boca separada del fondo del deposito.
// =====================================================================
// Arana de 3 nervios con buje liso para el extremo inferior del eje.
// Los nervios llevan el canto inferior a 45 grados y el buje termina en
// punta conica: todo autoportante imprimiendo el embudo de pie.
module arana() {
    z_top  = embudo_h;
    buje_d = eje_d + 2 * tol + 2 * pared;     // 14.6
    // Volumen interior del embudo dilatado 2 mm: recorta los nervios
    // haciendo que empotren en la pared del cono en vez de quedar tangentes
    module interior() {
        cylinder(d1 = embudo_d - 2 * pared + 4, d2 = casq_int_d + 4, h = embudo_h);
        translate([0, 0, embudo_h]) cylinder(d = casq_int_d + 4, h = embudo_encaje);
    }
    difference() {
        intersection() {
            union() {
                translate([0, 0, z_top - 14]) cylinder(d = buje_d, h = 20);
                translate([0, 0, z_top - 22])
                    cylinder(d1 = 1, d2 = buje_d, h = 8);      // punta a 45 grados
                for (a = [0 : 120 : 359])
                    rotate([0, 0, a]) translate([0, 1.5, 0]) rotate([90, 0, 0])
                        linear_extrude(height = 3)
                            polygon([[5, z_top - 25], [5, z_top], [30, z_top]]);
            }
            interior();
        }
        translate([0, 0, z_top - 23]) cylinder(d = eje_d + 2 * tol, h = 40);
    }
}

module embudo() {
    if (embudo_buje) arana();
    difference() {
        union() {
            // Cono de captacion
            cylinder(d1 = embudo_d, d2 = casq_ext_d, h = embudo_h);
            // Casquillo de union con el tubo
            translate([0, 0, embudo_h]) cylinder(d = casq_ext_d, h = embudo_encaje);
            // Patas
            for (a = [0 : 120 : 359])
                rotate([0, 0, a])
                    translate([embudo_d / 2 - 3, 0, -embudo_patas])
                        cylinder(d = 12, h = embudo_patas + 6);
        }
        // Conducto interior: boca ancha -> casquillo
        translate([0, 0, -1])
            cylinder(d1 = embudo_d - 2 * pared, d2 = casq_int_d, h = embudo_h + 1);
        translate([0, 0, embudo_h])
            cylinder(d = casq_int_d, h = embudo_encaje + 1);
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "tornillo") tornillo();
else if (pieza == "carcasa") carcasa();
else if (pieza == "embudo") embudo();
else {
    carcasa();
    color("Silver") translate([0, 0, -embudo_h]) embudo();
    color("SteelBlue") translate([0, 0, 8]) tornillo();
}
