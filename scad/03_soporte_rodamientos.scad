// =====================================================================
//  MODULO 3 - Soporte mecanico, alojamientos 608 y reenvio conico 1:1
//  Une el eje VERTICAL de la turbina (Modulo 2) con el eje INCLINADO
//  del tornillo de Arquimedes (Modulo 1).
// ---------------------------------------------------------------------
//  REGLAS GLOBALES: $fn = 100 | pared min. 3 mm | tolerancia 0.3 mm
//  Sin librerias externas: el perfil de evolvente se genera aqui mismo.
// ---------------------------------------------------------------------
//  ANGULO DEL PAR CONICO
//    Los dos ejes se cortan en el APICE comun de los conos primitivos.
//    Cada eje sale del apice hacia su propio engranaje:
//        - mastil de la turbina : hacia ARRIBA          (vertical)
//        - eje del tornillo     : hacia ABAJO-adelante  (contrapendiente)
//    El angulo entre esas dos direcciones es
//        SIGMA = 90 + inclinacion        (130 grados con inclinacion 40)
//    y en una reduccion 1:1 cada cono primitivo vale SIGMA/2 = 65 grados.
//    Con inclinacion = 0 el par degenera en el conico clasico de 90/45.
// ---------------------------------------------------------------------
//  PIEZAS
//    "soporte"     -> torreta con los DOS alojamientos a presion 608:
//                     uno para el eje del tornillo y otro, inclinado,
//                     para el mastil vertical de la turbina
//    "alojamiento" -> chumacera suelta 608 (segundo apoyo del mastil)
//    "engranaje"   -> engranaje conico 1:1 (imprimir DOS unidades)
//    "acople"      -> acople rigido 8-8 mm para ejes alineados
//    "conjunto"    -> vista de montaje (NO exportar a STL)
// =====================================================================

$fn = 100;

/* [Pieza a renderizar] */
pieza = "soporte";       // ["soporte","alojamiento","engranaje","acople","conjunto"]

/* [Reglas globales] */
tol   = 0.3;
pared = 3;

/* [Cinematica] */
inclinacion = 40;        // grados del tornillo sobre la horizontal (= Modulo 1)

/* [Rodamiento 608] */
rod_ext_d = 22.2;        // alojamiento a presion (Ø22 nominal + ajuste)
rod_int_d = 8;
rod_esp   = 7;
rod_labio = 19.5;        // labio de retencion: toca solo la pista exterior

/* [Eje] */
eje_d     = 8;
cubo_tipo = "D";         // ["D","redondo"]
d_shaft   = 7.0;
cubo_d    = 18;

/* [Engranaje conico 1:1] */
mod_g     = 2;           // modulo
z_g       = 20;          // numero de dientes
ang_pres  = 20;          // angulo de presion
ancho_g   = 7;           // ancho de diente sobre la generatriz (<= A0/3)
cubo_g_h  = 14;          // longitud del cubo
juego_g   = 0.3;         // juego entre flancos medido en la circunferencia
                         // primitiva (backlash). Sin el, los dientes se
                         // tocan sin holgura y el par se agarrota.

/* [Torreta] */
pcd_brida = 60;          // taladros M4 (coincide con la corona del Modulo 1)
base_esp  = 6;
base_d    = 80;
z_apice   = 42;          // apice de los conos sobre la base de la torreta
buje_d    = 34;          // bujes que alojan los rodamientos
buje_l    = 14;
d_mastil  = 48;          // distancia del apice al centro del rodamiento
                         // del mastil, medida sobre su eje
brazo_esp = 9;           // espesor del brazo del mastil

// ---------------------------------------------------------------------
//  COTAS DERIVADAS
// ---------------------------------------------------------------------
eje_ag_d = eje_d + tol;
plano_d  = d_shaft - eje_d / 2 + tol / 2;
m3_broca = 2.8;
m4_paso  = 4.4;

ang_ejes = 90 + inclinacion;         // angulo entre ejes
delta    = ang_ejes / 2;             // semiangulo del cono primitivo (1:1)
ang_mast = 90 - inclinacion;         // el mastil respecto al eje del tornillo

r_prim   = mod_g * z_g / 2;          // 20
cono_A   = r_prim / sin(delta);      // distancia de cono exterior
esc_g    = (cono_A - ancho_g) / cono_A;
alt_g    = ancho_g * cos(delta);     // altura axial del dentado
ap_axial = r_prim / tan(delta);      // plano trasero medido desde el apice
z_flip   = ap_axial + cubo_g_h;      // para pasar de impresion a montaje
ang_cab  = delta + atan(mod_g / cono_A);      // semiangulo del cono de cabeza
z_cono_t = ap_axial + r_prim * tan(delta);    // apice del cono trasero
r_cab_ext= r_prim + mod_g * cos(delta);       // radio de cabeza en el cono trasero

assert(rod_ext_d - rod_labio >= 2, "El labio no retiene la pista exterior.");
assert(buje_d >= rod_ext_d + 2 * pared, "Pared insuficiente alrededor del rodamiento.");
assert(ancho_g <= cono_A / 3, "Ancho de diente excesivo para la distancia de cono.");
assert(z_apice > buje_l + cubo_g_h + 4, "El cubo del engranaje choca con el buje.");

echo(str("Par conico: SIGMA=", ang_ejes, " deg | cono primitivo=", delta,
         " deg | Dprim=", 2 * r_prim, " Dext=", 2 * (r_prim + mod_g)));
echo(str("Juego entre flancos=", juego_g, " mm | radio de cabeza exterior=",
         r_cab_ext, " mm"));
echo(str("Apice a ", z_apice, " mm sobre la base; mastil a ", ang_mast,
         " deg del eje del tornillo (vertical en obra)."));

// =====================================================================
//  PERFIL DE EVOLVENTE (generado sin librerias)
//    inv(a) = tan(a) - a  [rad]   ->  en grados:
// =====================================================================
function inv_deg(a) = tan(a) * 180 / PI - a;

module diente_2d(m, z, pa, pasos = 12) {
    // El semiespesor angular se recorta medio juego para dejar backlash
    rp = m * z / 2;
    rb = rp * cos(pa);
    ra = rp + m;              // addendum 1.00 m
    rf = rp - 1.25 * m;       // dedendum 1.25 m
    r0 = max(rb, rf);
    fi0 = 90 / z + inv_deg(pa) - (juego_g / 2) / rp * 180 / PI;
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
//  AGUJERO DE EJE
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
//  ENGRANAJE CONICO
//    El perfil se genera como engranaje recto y se extruye escalado hacia
//    el apice (aproximacion de Tredgold), pero el solido se RECORTA con
//    los dos conos que definen un conico de verdad:
//      - cono de cabeza : apice en el apice primitivo, semiangulo
//                         delta + atan(m/A0). Sin el, la punta del diente
//                         sobra radialmente m*(1-cos(delta)) y se clava en
//                         el fondo del engranaje conjugado.
//      - cono trasero   : perpendicular al cono primitivo en el punto
//                         primitivo exterior. Cierra el diente por fuera.
//    Con los dos recortes el radio de cabeza exterior sale exactamente
//    r_primitivo + m*cos(delta), que es la cota de norma.
// =====================================================================
module conos_recorte() {
    h1 = z_flip + 5;
    h2 = z_cono_t + 5;
    intersection() {
        cylinder(r1 = 0, r2 = h1 * tan(ang_cab), h = h1);           // cono de cabeza
        translate([0, 0, z_cono_t]) mirror([0, 0, 1])
            cylinder(r1 = 0, r2 = h2 * tan(90 - delta), h = h2);    // cono trasero
    }
}

// Engranaje con el APICE en el origen y su eje sobre +Z (marco de MONTAJE)
module engranaje_montado() {
    r_ext = r_prim + mod_g;
    h_web = r_ext - cubo_d / 2;          // alma a 45 grados exactos
    difference() {
        union() {
            intersection() {
                translate([0, 0, ap_axial]) mirror([0, 0, 1])
                    linear_extrude(height = alt_g, scale = esc_g, slices = 20)
                        engranaje_2d(mod_g, z_g, ang_pres);
                conos_recorte();
            }
            intersection() {
                translate([0, 0, ap_axial]) cylinder(d1 = 2 * r_ext, d2 = cubo_d, h = h_web);
                conos_recorte();
            }
            translate([0, 0, ap_axial]) cylinder(d = cubo_d, h = cubo_g_h);
        }
        translate([0, 0, ap_axial - alt_g - 1])
            agujero_eje(cubo_g_h + alt_g + 2);
        translate([0, 0, ap_axial + cubo_g_h / 2]) rotate([0, 90, 0])
            cylinder(d = m3_broca, h = cubo_d);
    }
}

// Orientacion de IMPRESION: cubo abajo, alma a 45 grados, dentado arriba.
// El dentado siempre reduce su radio al subir, asi que sale sin soportes
// sea cual sea el semiangulo del cono (45 grados a 90 deg, 65 a 130 deg).
module engranaje() {
    translate([0, 0, z_flip]) mirror([0, 0, 1]) engranaje_montado();
}

// =====================================================================
//  ALOJAMIENTOS DE RODAMIENTO
// =====================================================================
module cajera_608(h_total) {
    translate([0, 0, h_total - rod_esp - 0.4])
        cylinder(d = rod_ext_d, h = rod_esp + 0.5);
    translate([0, 0, -0.01]) cylinder(d = rod_labio, h = h_total + 0.02);
}

// Chumacera suelta: segundo apoyo del mastil de la turbina
module alojamiento() {
    anc = 46; prof = 16; h_eje = 26; base_e = 6;
    difference() {
        hull() {
            translate([0, 0, h_eje]) rotate([-90, 0, 0])
                cylinder(d = buje_d, h = prof);
            translate([-anc / 2, 0, 0]) cube([anc, prof, base_e]);
        }
        translate([0, prof, h_eje]) rotate([90, 0, 0]) cajera_608(prof);
        for (x = [-anc / 2 + 8, anc / 2 - 8])
            translate([x, prof / 2, -1]) cylinder(d = m4_paso, h = base_e + 2);
    }
}

// =====================================================================
//  TORRETA
//    Se atornilla a la corona de la carcasa, de modo que su eje Z es el
//    eje del tornillo. El brazo del mastil sale a 'ang_mast' grados: en
//    obra, con la carcasa inclinada, ese brazo queda VERTICAL.
// =====================================================================
module buje_mastil(l = buje_l + 6) {
    translate([0, 0, z_apice]) rotate([0, -ang_mast, 0])
        translate([0, 0, d_mastil - l / 2]) children();
}

// Volumen de giro de un engranaje montado con su apice en el origen y su
// eje sobre +Z: cualquier pieza fija debe restarlo para no rozar.
module hueco_engranaje() {
    translate([0, 0, -1])
        cylinder(d = 2 * (r_prim + mod_g) + 4, h = cubo_g_h + ap_axial + 4);
}

module soporte() {
    difference() {
        union() {
            // Base atornillada a la corona de la carcasa
            hull() {
                cylinder(d = base_d, h = base_esp);
                translate([-base_d / 2 - 8, -24, 0]) cube([16, 48, base_esp]);
            }
            // Buje del eje del tornillo
            cylinder(d = buje_d, h = buje_l);
            // Brazo inclinado + buje del mastil
            hull() {
                buje_mastil() cylinder(d = buje_d, h = buje_l + 6);
                translate([-base_d / 2 - 6, -20, 0]) cube([14, 40, base_esp]);
            }
            // Cartelas del brazo
            for (sy = [-1, 1])
                translate([0, sy * 20 - brazo_esp / 2, 0]) rotate([90, 0, 0])
                    linear_extrude(height = brazo_esp)
                        polygon([[-base_d / 2 - 6, 0], [-14, 0],
                                 [-base_d / 2 - 6, z_apice * 0.75]]);
        }
        // Cajera vertical (eje del tornillo): el rodamiento entra por arriba
        cajera_608(buje_l);
        // Cajera del mastil: entra por el extremo libre del brazo
        buje_mastil() cajera_608(buje_l + 6);
        // Huecos de giro de los dos engranajes (garantizan que ni el brazo
        // ni las cartelas rocen con el par conico)
        translate([0, 0, z_apice]) rotate([180, 0, 0]) hueco_engranaje();
        translate([0, 0, z_apice]) rotate([0, -ang_mast, 0]) hueco_engranaje();
        // Taladros M4 a la corona de la carcasa
        for (a = [45 : 90 : 359])
            rotate([0, 0, a]) translate([pcd_brida / 2, 0, -1])
                cylinder(d = m4_paso, h = base_esp + 2);
    }
}

// =====================================================================
//  ACOPLE RIGIDO 8-8 (ejes alineados)
// =====================================================================
module acople() {
    largo = 36;
    difference() {
        cylinder(d = 20, h = largo);
        translate([0, 0, -0.01]) agujero_eje(largo / 2 - 1);
        translate([0, 0, largo]) mirror([0, 0, 1]) agujero_eje(largo / 2 - 1);
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
else if (pieza == "conjunto") {
    color("LightGray") soporte();
    // engranaje del tornillo: apice arriba, cuerpo hacia la carcasa
    color("Orange") translate([0, 0, z_apice]) rotate([180, 0, 0]) engranaje_montado();
    // engranaje del mastil: apice abajo, cuerpo hacia la turbina
    color("Coral") translate([0, 0, z_apice]) rotate([0, -ang_mast, 0])
        rotate([0, 0, 180 / z_g]) engranaje_montado();
    // eje del mastil
    color("DimGray") translate([0, 0, z_apice]) rotate([0, -ang_mast, 0])
        translate([0, 0, 20]) cylinder(d = eje_d, h = 90);
}
