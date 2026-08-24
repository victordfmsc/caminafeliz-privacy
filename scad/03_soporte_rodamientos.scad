// =====================================================================
//  MODULO 3 - Soporte mecanico, alojamientos 608 y reenvio conico 1:1
//  Une el eje de la turbina (Modulo 2) con el eje del tornillo (Modulo 1)
// ---------------------------------------------------------------------
//  REGLAS GLOBALES: $fn = 100 | pared min. 3 mm | tolerancia 0.3 mm
//  Sin librerias externas: el perfil de evolvente se genera aqui mismo.
// ---------------------------------------------------------------------
//  PIEZAS
//    "soporte"     -> torreta con los DOS alojamientos a presion 608
//                     (uno vertical para el tornillo, uno horizontal
//                      para la turbina, ejes a 90 grados)
//    "alojamiento" -> chumacera suelta 608 para el extremo libre de la
//                     turbina (imprimir tumbada sobre la cara trasera)
//    "engranaje"   -> engranaje conico 1:1 (imprimir DOS unidades)
//    "acople"      -> acople rigido 8-8 mm, alternativa al par conico
//                     cuando los ejes se montan alineados
//    "conjunto"    -> vista de montaje (NO exportar a STL)
// =====================================================================

$fn = 100;

/* [Pieza a renderizar] */
pieza = "soporte";       // ["soporte","alojamiento","engranaje","acople","conjunto"]

/* [Reglas globales] */
tol   = 0.3;
pared = 3;

/* [Rodamiento 608] */
rod_ext_d = 22.2;        // alojamiento a presion (Ø22 nominal + ajuste)
rod_int_d = 8;
rod_esp   = 7;
rod_labio = 19.5;        // diametro del labio de retencion (toca la pista ext.)

/* [Eje] */
eje_d     = 8;
cubo_tipo = "D";         // ["D","redondo"]
d_shaft   = 7.0;
cubo_d    = 18;

/* [Engranaje conico 1:1 (conos primitivos a 45 grados)] */
mod_g     = 2;           // modulo
z_g       = 20;          // numero de dientes
ang_pres  = 20;          // angulo de presion
ancho_g   = 8;           // ancho de diente medido sobre la generatriz
cubo_g_h  = 14;          // longitud del cubo

/* [Torreta] */
pcd_brida = 60;          // circulo de taladros M4 (coincide con Modulo 1)
base_esp  = 6;
base_d    = 80;
z_apice   = 42;          // altura del apice comun de los conos primitivos
x_pared   = 48;          // posicion del muro que sostiene el eje horizontal
pared_esp = 6;
buje_d    = 34;          // diametro de los bujes que alojan los rodamientos
buje_l    = 14;

// ---------------------------------------------------------------------
//  COTAS DERIVADAS
// ---------------------------------------------------------------------
eje_ag_d = eje_d + tol;                    // 8.3
plano_d  = d_shaft - eje_d / 2 + tol / 2;  // plano del D-shaft
m3_broca = 2.8;
m4_paso  = 4.4;
m4_cab   = 8;

r_prim   = mod_g * z_g / 2;                // radio primitivo = 20
cono_A   = r_prim / sin(45);               // distancia de cono exterior
esc_g    = (cono_A - ancho_g) / cono_A;    // factor de reduccion hacia el apice
alt_g    = ancho_g * cos(45);              // altura axial del dentado
apice_g  = (cono_A - ancho_g) * cos(45);   // apice por debajo del extremo menor

assert(rod_ext_d - rod_labio >= 2, "El labio no retiene la pista exterior.");
assert(buje_d >= rod_ext_d + 2 * pared, "Pared insuficiente alrededor del rodamiento.");

echo(str("Engranaje: Dprim=", 2 * r_prim, " Dext=", 2 * (r_prim + mod_g),
         " escala=", esc_g, " altura dentado=", alt_g));
echo(str("Montaje: apice de los conos a z=", z_apice,
         " sobre la base; eje horizontal a la misma altura."));

// =====================================================================
//  PERFIL DE EVOLVENTE (generado sin librerias)
//    inv(a) = tan(a) - a        [radianes]  ->  en grados:
// =====================================================================
function inv_deg(a) = tan(a) * 180 / PI - a;

module diente_2d(m, z, pa, pasos = 12) {
    rp = m * z / 2;
    rb = rp * cos(pa);
    ra = rp + m;              // addendum  = 1.00 m
    rf = rp - 1.25 * m;       // dedendum  = 1.25 m
    r0 = max(rb, rf);
    fi0 = 90 / z + inv_deg(pa);   // semiespesor angular + retroceso de evolvente
    der = [for (i = [0 : pasos])
             let (r = r0 + (ra - r0) * i / pasos,
                  a = fi0 - inv_deg(acos(rb / r)))
             [r * cos(a), r * sin(a)]];
    izq = [for (i = [pasos : -1 : 0])
             let (r = r0 + (ra - r0) * i / pasos,
                  a = -(fi0 - inv_deg(acos(rb / r))))
             [r * cos(a), r * sin(a)]];
    polygon(concat([[0, 0]], der, izq));
}

module engranaje_2d(m, z, pa) {
    rf = m * z / 2 - 1.25 * m;
    union() {
        circle(r = rf);
        for (i = [0 : z - 1]) rotate([0, 0, i * 360 / z]) diente_2d(m, z, pa);
    }
}

// =====================================================================
//  AGUJERO DE EJE Y CUBO
// =====================================================================
module agujero_eje(h) {
    if (cubo_tipo == "D")
        intersection() {
            cylinder(d = eje_ag_d, h = h);
            translate([-eje_d, -eje_d, 0]) cube([2 * eje_d, eje_d + plano_d, h]);
        }
    else
        cylinder(d = eje_ag_d, h = h);
}

// =====================================================================
//  ENGRANAJE CONICO (orientacion de IMPRESION)
//    Extremo menor sobre la cama: el dentado crece a 45 grados, que es
//    el limite autoportante -> se imprime sin soportes.
// =====================================================================
module engranaje() {
    difference() {
        union() {
            translate([0, 0, alt_g]) mirror([0, 0, 1])
                linear_extrude(height = alt_g, scale = esc_g, slices = 20)
                    engranaje_2d(mod_g, z_g, ang_pres);
            translate([0, 0, alt_g]) cylinder(d = cubo_d, h = cubo_g_h);
        }
        translate([0, 0, -0.01]) agujero_eje(alt_g + cubo_g_h + 0.02);
        translate([0, 0, alt_g + cubo_g_h / 2]) rotate([0, 90, 0])
            cylinder(d = m3_broca, h = cubo_d);
    }
}

// Engranaje colocado con el APICE en el origen y el eje sobre +Z
module engranaje_montado() {
    translate([0, 0, apice_g]) engranaje();
}

// =====================================================================
//  ALOJAMIENTOS DE RODAMIENTO
//    Cajera de 22.2 x 7.4 mm + labio de retencion de Ø19.5
// =====================================================================
module cajera_608(h_total) {
    // se resta desde z=0 hacia arriba: cajera arriba, labio abajo
    translate([0, 0, h_total - rod_esp - 0.4])
        cylinder(d = rod_ext_d, h = rod_esp + 0.5);
    translate([0, 0, -0.01]) cylinder(d = rod_labio, h = h_total + 0.02);
}

// Chumacera suelta (segundo apoyo del eje de la turbina)
module alojamiento() {
    anc = 46; prof = 16; h_eje = 26; base_e = 6;
    difference() {
        union() {
            hull() {
                translate([0, 0, h_eje]) rotate([-90, 0, 0])
                    cylinder(d = buje_d, h = prof);
                translate([-anc / 2, 0, 0]) cube([anc, prof, base_e]);
            }
        }
        // asiento del rodamiento orientado hacia -Y
        translate([0, prof, h_eje]) rotate([90, 0, 0]) cajera_608(prof);
        // taladros M4 de sujecion
        for (x = [-anc / 2 + 8, anc / 2 - 8])
            translate([x, prof / 2, -1]) cylinder(d = m4_paso, h = base_e + 2);
    }
}

// =====================================================================
//  TORRETA CON LOS DOS ALOJAMIENTOS A 90 GRADOS
// =====================================================================
module soporte() {
    difference() {
        union() {
            // Base circular que atornilla a la corona de la carcasa
            hull() {
                cylinder(d = base_d, h = base_esp);
                translate([x_pared - 4, -30, 0])
                    cube([pared_esp + 20, 60, base_esp]);
            }
            // Buje vertical (eje del tornillo)
            cylinder(d = buje_d, h = buje_l);
            // Muro vertical + buje horizontal (eje de la turbina)
            translate([x_pared, -30, 0]) cube([pared_esp, 60, z_apice + 24]);
            translate([x_pared - 4, 0, z_apice]) rotate([0, 90, 0])
                cylinder(d = buje_d, h = buje_l + 4);
            // Cartelas de refuerzo
            for (sy = [-1, 1])
                translate([x_pared, sy * 20 - 2.5, 0])
                    rotate([90, 0, 0]) mirror([1, 0, 0])
                        linear_extrude(height = 5)
                            polygon([[0, 0], [26, 0], [0, z_apice - 4]]);
        }
        // Cajera vertical: el rodamiento entra por arriba y apoya en el labio
        cajera_608(buje_l);
        // Cajera horizontal: el rodamiento entra por el lado exterior (+X)
        translate([x_pared - 4, 0, z_apice]) rotate([0, 90, 0])
            cajera_608(buje_l + 4);
        // Taladros M4 a la corona de la carcasa (Modulo 1)
        for (a = [45 : 90 : 359])
            rotate([0, 0, a]) translate([pcd_brida / 2, 0, -1])
                cylinder(d = m4_paso, h = base_esp + 2);
    }
}

// =====================================================================
//  ACOPLE RIGIDO 8-8 (alternativa al par conico, ejes alineados)
// =====================================================================
module acople() {
    largo = 36;
    difference() {
        cylinder(d = 20, h = largo);
        // alojamientos de eje con tope central
        translate([0, 0, -0.01]) agujero_eje(largo / 2 - 1);
        translate([0, 0, largo]) mirror([0, 0, 1]) agujero_eje(largo / 2 - 1);
        // prisioneros M3 (dos por lado, a 90 grados)
        for (z = [7, largo - 7], a = [0, 90])
            translate([0, 0, z]) rotate([0, 90, a]) cylinder(d = m3_broca, h = 20);
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "soporte") soporte();
else if (pieza == "alojamiento") alojamiento();
else if (pieza == "engranaje") engranaje();
else if (pieza == "acople") acople();
else {
    color("LightGray") soporte();
    color("Orange") translate([0, 0, z_apice]) engranaje_montado();
    color("Coral") translate([0, 0, z_apice]) rotate([0, 90, 0])
        rotate([0, 0, 180 / z_g]) engranaje_montado();
}
