// =====================================================================
//  MODULO 5 - Vaso de cultivo hidroponico ranurado (net cup Ø50)
//  Se apoya por la pestaña sobre el taladro del canal de PVC Ø75
// ---------------------------------------------------------------------
//  REGLAS GLOBALES: $fn = 100 | tolerancia 0.3 mm | sin librerias
//  NOTA de pared: la regla de 3 mm aplica a las piezas estancas
//  (carcasa, tapones). La canastilla es una celosia sin presion, por lo
//  que usa 2.5 mm: suficiente en PETG/ASA y deja pasar la raiz.
// ---------------------------------------------------------------------
//  IMPRESION: de pie, boca arriba, SIN soportes. La conicidad de 8.5
//  grados y el chaflan de 45 grados bajo la pestaña son autoportantes.
// ---------------------------------------------------------------------
//  PIEZAS
//    "vaso"     -> canastilla
//    "plantilla"-> galga para marcar el taladro Ø50.6 en el tubo PVC
//    "conjunto" -> vaso + tubo de referencia (NO exportar a STL)
// =====================================================================

$fn = 100;

/* [Pieza a renderizar] */
pieza = "vaso";          // ["vaso","plantilla","conjunto"]

/* [Reglas globales] */
tol = 0.3;

/* [Vaso] */
sup_d      = 50;         // diametro superior (pasa por el taladro del canal)
base_d     = 35;         // diametro de la base
alto       = 50;         // altura sin contar la pestaña
pared      = 2.5;        // espesor de la pared curva
fondo_esp  = 2.5;        // espesor del fondo
pestana_e  = 3;          // espesor de la pestaña de apoyo
pestana_v  = 5;          // vuelo radial de la pestaña
chaflan    = 5;          // chaflan a 45 grados bajo la pestaña (autoportante)

/* [Ranurado] */
ranura     = 2;          // anchura de ranura
n_vert     = 16;         // ranuras verticales en la pared
n_fondo    = 8;          // ranuras radiales en el fondo
anillo_inf = 3;          // anillo macizo en el canto inferior
dren_d     = 6;          // taladro central de drenaje

/* [Plantilla de taladro] */
tubo_ext_d = 75;
paso_vasos = 150;        // separacion entre vasos en el canal.
                         // 150 mm para lechuga (4 plantas por tramo de 600 mm,
                         // que es el limite de oxigeno con este caudal);
                         // 100-120 mm sirve para albahaca y hoja pequeña.

// ---------------------------------------------------------------------
//  COTAS DERIVADAS
// ---------------------------------------------------------------------
pestana_d  = sup_d + 2 * pestana_v;      // 60
taladro_d  = sup_d + 2 * tol;            // 50.6 taladro en el tubo PVC
z_ran_inf  = anillo_inf;
z_ran_sup  = alto - pestana_e - 1;
r_fondo_i  = dren_d / 2 + 1.5;
r_fondo_e  = base_d / 2 - pared - 1;

assert(base_d < sup_d, "La canastilla debe ser conica para desmoldear e imprimir.");
assert(z_ran_sup > z_ran_inf + 10, "Ranuras verticales demasiado cortas.");

echo(str("Vaso Ø", sup_d, "x", alto, " | pestaña Ø", pestana_d,
         " | taladro en el canal Ø", taladro_d));

// =====================================================================
//  VASO
// =====================================================================
module vaso() {
    difference() {
        union() {
            // Cuerpo conico hueco
            difference() {
                cylinder(d1 = base_d, d2 = sup_d, h = alto);
                translate([0, 0, fondo_esp])
                    cylinder(d1 = base_d - 2 * pared,
                             d2 = sup_d - 2 * pared, h = alto);
            }
            // Chaflan de transicion + pestaña de apoyo
            translate([0, 0, alto - pestana_e - chaflan])
                cylinder(d1 = sup_d, d2 = pestana_d, h = chaflan);
            translate([0, 0, alto - pestana_e])
                cylinder(d = pestana_d, h = pestana_e);
        }
        // Ranuras verticales en la pared
        for (i = [0 : n_vert - 1])
            rotate([0, 0, i * 360 / n_vert])
                translate([0, -ranura / 2, z_ran_inf])
                    cube([pestana_d, ranura, z_ran_sup - z_ran_inf]);
        // Ranuras radiales en el fondo (no llegan al canto: el anillo
        // exterior queda continuo y el fondo no se desarma)
        for (i = [0 : n_fondo - 1])
            rotate([0, 0, i * 360 / n_fondo + 180 / n_fondo])
                translate([r_fondo_i, -ranura / 2, -0.5])
                    cube([r_fondo_e - r_fondo_i, ranura, fondo_esp + 1]);
        // Drenaje central
        translate([0, 0, -0.5]) cylinder(d = dren_d, h = fondo_esp + 1);
        // Boca libre: vacia el chaflan y la pestaña hasta el Ø interior
        translate([0, 0, alto - pestana_e - chaflan - 0.01])
            cylinder(d = sup_d - 2 * pared, h = pestana_e + chaflan + 0.02);
    }
}

// =====================================================================
//  PLANTILLA DE TALADRO PARA EL CANAL
//  Se apoya sobre la generatriz del tubo y marca dos centros a 'paso'
// =====================================================================
module plantilla() {
    anc = 40; esp = 4;
    difference() {
        intersection() {
            translate([0, 0, -tubo_ext_d / 2])
                rotate([0, 90, 0])
                    difference() {
                        cylinder(d = tubo_ext_d + 2 * esp, h = paso_vasos + anc, center = true);
                        cylinder(d = tubo_ext_d + 2 * tol, h = paso_vasos + anc + 2, center = true);
                    }
            translate([-(paso_vasos + anc) / 2, -anc / 2, -esp - 1])
                cube([paso_vasos + anc, anc, esp + 2]);
        }
        for (x = [-paso_vasos / 2, paso_vasos / 2])
            translate([x, 0, -esp - 2]) cylinder(d = 3.5, h = esp + 4);
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "vaso") vaso();
else if (pieza == "plantilla") plantilla();
else if (pieza == "conjunto") {
    color("DimGray", 0.35)
        difference() {
            translate([0, 0, -tubo_ext_d / 2]) rotate([0, 90, 0])
                cylinder(d = tubo_ext_d, h = 200, center = true);
            translate([0, 0, -tubo_ext_d / 2]) rotate([0, 90, 0])
                cylinder(d = tubo_ext_d - 6, h = 202, center = true);
            translate([0, 0, -tubo_ext_d / 2 - 1]) cylinder(d = taladro_d, h = tubo_ext_d);
        }
    color("White") translate([0, 0, -pestana_e - 0.5]) vaso();
}
