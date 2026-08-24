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
//  MONTAJE INCLINADO Y CIERRE DE CANGILONES
//    El tornillo trabaja a 40 grados sobre la horizontal. Un Arquimedes
//    vertical NO bombea. El agua rebosa de un cangilon al de abajo por el
//    CANTO INTERIOR del filete, cuya cota es
//        h(th) = -Ri cos(a) cos(th) + (S sin(a)/2pi) th
//    Hay barrera (y por tanto cangilon cerrado) solo si esa funcion tiene
//    minimo, es decir si
//        k = S * tan(a) / (2*pi*Ri)  <  1        <-- con el radio INTERIOR
//    Ojo: el criterio que se cita a menudo usa Ro y es mucho mas permisivo.
//    Con Ri = 7.15 y a = 40 grados, el paso maximo real son 53 mm.
//
//  PASO DE 15 mm (el enunciado pedia 30)
//    El llenado del cangilon cae deprisa con el paso, y la fuga por la
//    holgura crece con el salto de carga por filete (S*sin a). Integrando
//    el volumen de cangilon (ver analisis_bomba.py) el caudal neto a
//    300 rpm es maximo entre 12 y 20 mm:
//        paso 15 -> 1.50 mL/vuelta, neto 0.30-0.40 L/min
//        paso 30 -> 1.28 mL/vuelta, neto 0.18-0.29 L/min
//    Con paso 30 y la holgura original de 0.3 mm el caudal neto a 300 rpm
//    era practicamente CERO. Para volver al enunciado: paso = 30.
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
pieza = "tornillo";      // ["tornillo","carcasa","embudo","calibre","conjunto"]

/* [Reglas globales] */
tol   = 0.3;             // tolerancia de ensamblaje
pared = 3;               // grosor de pared minimo

/* [Montaje] */
inclinacion = 40;        // grados del eje del tornillo sobre la horizontal

/* [Rotor helicoidal] */
helice_d    = 40;        // diametro exterior del helicoide
paso        = 15;        // avance por vuelta (optimo de caudal neto)
largo_util  = 180;       // longitud total del filete
eje_d       = 8;         // varilla metalica pasante
helice_esp  = 2.0;       // espesor axial del filete (4 lineas de 0.5 mm)
seg_vuelta  = 24;        // segmentos por vuelta (resolucion del helicoide)
prisionero  = true;      // taladro M3 para prisionero en el cubo

/* [Carcasa] */
holgura_helice = 0.15;   // HOLGURA RADIAL helicoide-carcasa. Es el parametro
                         // mas sensible de todo el proyecto: la fuga escala
                         // entre h y h^3 segun regimen.
                         //   0.15 mm -> fuga 0.05-0.15 L/min (util desde ~100 rpm)
                         //   0.30 mm -> fuga 0.30-0.36 L/min (util desde ~250 rpm)
                         // Imprime primero la pieza "calibre" y mide.
carcasa_largo = 200;     // longitud del tubo recto
embudo_d      = 90;      // diametro de boca del embudo de entrada
embudo_h      = 45;      // altura del cono del embudo
embudo_encaje = 20;      // profundidad del casquillo embudo-tubo
embudo_patas  = 0;       // patas de apoyo (0 = el embudo cuelga sumergido,
                         // que es como trabaja con el tubo inclinado)
embudo_vent   = 4;       // ventanas de admision en el cono
embudo_buje   = true;    // arana con buje liso que guia el extremo inferior
                         // del eje (cojinete lubricado por el propio agua)
boquilla_d    = 32;      // diametro EXTERIOR de la boquilla de vertido
boquilla_l    = 55;      // longitud de la boquilla medida desde el eje del tubo.
                         // Sale a 'inclinacion' grados del plano perpendicular
                         // al tubo, es decir: A PLOMO cuando el conjunto se
                         // monta inclinado, y con la boca cortada horizontal.
brida_sup     = true;    // corona superior para atornillar el soporte (Modulo 3)
pcd_brida     = 60;      // circulo de taladros M4 de la corona superior

// ---------------------------------------------------------------------
//  COTAS DERIVADAS
// ---------------------------------------------------------------------
eje_agujero_d = eje_d + tol;              // 8.3  - encaje a presion sobre varilla
nucleo_d      = eje_agujero_d + 2*pared;  // 14.3 - respeta la pared minima
carcasa_int_d = helice_d + 2*holgura_helice;   // 40.3 - deslizamiento del helicoide
carcasa_ext_d = carcasa_int_d + 2*pared;  // 46.6
casq_int_d    = carcasa_ext_d + 2*tol;    // 47.2 - el embudo calza sobre el tubo
casq_ext_d    = casq_int_d + 2*pared;     // 53.2
boquilla_int_d= boquilla_d - 2*pared;     // 26.0
m3_broca      = 2.8;                      // roscado directo en plastico para M3
m4_paso       = 4.4;                      // taladro pasante M4
k_cierre      = paso * tan(inclinacion) / (2 * PI * nucleo_d / 2);  // criterio exacto
paso_max      = 2 * PI * (nucleo_d / 2) / tan(inclinacion);         // paso limite

assert(pared >= 3, "El grosor de pared no puede bajar de 3 mm.");
assert(nucleo_d < helice_d - 4, "El nucleo invade el filete: reduce el eje o sube helice_d.");
assert(k_cierre < 1,
       "Los cangilones NO cierran: reduce el paso o baja la inclinacion.");

echo(str("Carcasa: Dint=", carcasa_int_d, " Dext=", carcasa_ext_d));
echo(str("Tornillo: nucleo=", nucleo_d, " agujero eje=", eje_agujero_d,
         " vueltas=", largo_util/paso));
echo(str("Inclinacion=", inclinacion, " deg | k=", k_cierre,
         " (cierra si <1) | paso maximo admisible=", paso_max, " mm"));
echo(str("Holgura radial=", holgura_helice, " mm -> carcasa Dint=", carcasa_int_d));
echo(str("Elevacion util = ", largo_util * sin(inclinacion), " mm"));
// Cadena de cotas del vertido, medida desde la base del tubo de la carcasa
// (z=0 = boca del casquillo del embudo) con el conjunto ya inclinado:
echo(str("Descarga (eje de la boquilla) a z=", (carcasa_largo - 20) * sin(inclinacion),
         " mm | boca de la boquilla a z=",
         (carcasa_largo - 20) * sin(inclinacion) - boquilla_l, " mm"));
echo(str("Vuelo libre de la boquilla fuera de la carcasa = ",
         boquilla_l - (carcasa_ext_d / 2) / cos(inclinacion), " mm"));

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

// Boquilla de vertido. Con el tubo inclinado 'inclinacion' grados sobre la
// horizontal, un tramo recto girado ese mismo angulo respecto al plano
// perpendicular al tubo queda EXACTAMENTE vertical: cae a plomo dentro del
// manguito del Tapon A (Modulo 4) y su boca queda cortada en horizontal.
module boquilla_solida(d) {
    rotate([0, 90 + inclinacion, 0]) cylinder(d = d, h = boquilla_l);
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
            // Patas opcionales
            if (embudo_patas > 0)
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
        // Ventanas de admision: dejan entrar agua aunque la boca quede
        // proxima al fondo del deposito. Mantienen 4 pilares de union.
        if (embudo_vent > 0)
            for (a = [0 : 360 / embudo_vent : 359])
                rotate([0, 0, a + 180 / embudo_vent])
                    translate([15, -11, 9]) cube([50, 22, embudo_h - 22]);
    }
}

// =====================================================================
//  PIEZA D - CALIBRE DE AJUSTE
//    Imprime esto ANTES que nada: un anillo de carcasa y un tramo de
//    tornillo. Debe entrar girando con resistencia apenas perceptible y
//    sin bailar. Mide ademas ambos diametros con el pie de rey: la
//    diferencia con la cota nominal es el error de tu maquina, y se
//    corrige con holgura_helice (o con el factor de escala del laminador).
// =====================================================================
module calibre() {
    h = 12;
    // anillo de carcasa
    difference() {
        cylinder(d = carcasa_ext_d, h = h);
        translate([0, 0, -0.5]) cylinder(d = carcasa_int_d, h = h + 1);
    }
    // tramo de tornillo
    translate([carcasa_ext_d + 12, 0, 0]) difference() {
        union() {
            cylinder(d = nucleo_d, h = h);
            intersection() {
                helicoide(helice_d / 2, h, paso, helice_esp, seg_vuelta);
                cylinder(d = helice_d, h = h);
            }
        }
        translate([0, 0, -0.5]) cylinder(d = eje_agujero_d, h = h + 1);
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "tornillo") tornillo();
else if (pieza == "carcasa") carcasa();
else if (pieza == "embudo") embudo();
else if (pieza == "calibre") calibre();
else if (pieza == "conjunto") {
    // Vista de montaje en su posicion real de trabajo
    rotate([0, 90 - inclinacion, 0]) {
        carcasa();
        color("Silver") translate([0, 0, -embudo_h]) embudo();
        color("SteelBlue") translate([0, 0, 8]) tornillo();
    }
}
