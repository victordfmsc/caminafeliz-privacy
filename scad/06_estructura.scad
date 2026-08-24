// =====================================================================
//  MODULO 6 - Estructura: conectores de bastidor y abrazaderas
//  Sostiene el canal, la carcasa inclinada del tornillo y el mastil
//  vertical de la turbina sobre un bastidor de tubo de Ø25.
// ---------------------------------------------------------------------
//  REGLAS GLOBALES: $fn = 100 | pared min. 3 mm | tolerancia 0.3 mm
//  Sin librerias externas.
// ---------------------------------------------------------------------
//  PIEZAS
//    "conector_3v" -> nudo de esquina de 3 vias (imprimir en diagonal o
//                     con las dos bocas horizontales sobre soporte)
//    "abraz_canal" -> abrazadera del canal Ø75 sobre travesaño Ø25
//    "abraz_carcasa"-> abrazadera de la carcasa del tornillo, con el nido
//                     ya girado a la inclinacion de trabajo
//    "sop_mastil"  -> abrazadera con alojamiento 608 para el mastil
//    "conjunto"    -> vista comparada (NO exportar a STL)
// =====================================================================

$fn = 100;

/* [Pieza a renderizar] */
pieza = "conector_3v";  // ["conector_3v","abraz_canal","abraz_carcasa","sop_mastil","conjunto"]

/* [Reglas globales] */
tol   = 0.3;
pared = 3;

/* [Bastidor] */
tubo_b   = 25;          // tubo del bastidor
casq_l   = 34;          // longitud de cada boca del conector
anillo_h = 20;          // altura del anillo de las abrazaderas
anillo_p = 5;           // pared del anillo (rigidez de apriete)
ranura   = 3;           // apertura de la ranura de apriete

/* [Cargas] */
canal_d     = 75;       // canal de PVC (Modulo 4)
carcasa_d   = 46.3;     // exterior de la carcasa del tornillo (Modulo 1):
                        // helice 40 + 2*holgura 0.15 + 2*pared 3
inclinacion = 40;       // grados de la carcasa sobre la horizontal
nido_abrazo = 200;      // grados de abrazo del nido (>180 -> retiene a presion)

/* [Rodamiento 608] */
rod_ext_d = 22.2;
rod_esp   = 7;
rod_labio = 19.5;
buje_d    = 34;
buje_l    = 14;

// ---------------------------------------------------------------------
//  COTAS DERIVADAS
// ---------------------------------------------------------------------
casq_int  = tubo_b + 2 * tol;              // 25.6
casq_ext  = casq_int + 2 * pared;          // 31.6
anillo_int= casq_int;
anillo_ext= anillo_int + 2 * anillo_p;     // 35.6
canal_int = canal_d + 2 * tol;             // 75.6
carc_int  = carcasa_d + 2 * tol;           // 47.2
m4_paso   = 4.4;
m4_nucleo = 3.6;                           // roscado directo en PETG
m4_tuerca = 7.9;                           // entrecaras de tuerca M4

echo(str("Bastidor Ø", tubo_b, " -> casquillo Ø", casq_int, " ext Ø", casq_ext));
echo(str("Nido canal Ø", canal_int, " | nido carcasa Ø", carc_int));

// =====================================================================
//  UTILIDADES
// =====================================================================

// Nido cilindrico de 'abrazo' grados. Se genera en dos mitades (macizo y
// hueco) para poder unir primero toda la pieza y vaciar al final: asi la
// columna de union puede subir hasta el eje del nido sin invadir la pieza
// abrazada, y la union nunca queda a tope.
module nido_solido(d_int, largo, abrazo) {
    intersection() {
        cylinder(d = d_int + 2 * pared, h = largo);
        rotate([0, 0, 90 - abrazo / 2])
            linear_extrude(height = largo)
                polygon(concat([[0, 0]],
                    [for (a = [0 : abrazo / 24 : abrazo])
                        (d_int + 2 * pared) * [cos(a), sin(a)]]));
    }
}

module nido_hueco(d_int, largo) {
    translate([0, 0, -0.5]) cylinder(d = d_int, h = largo + 1);
    // pasadores para brida de nylon
    for (z = [largo * 0.25, largo * 0.75])
        translate([0, 0, z]) rotate([90, 0, 0])
            translate([0, 0, -d_int]) cylinder(d = 4, h = 2 * d_int);
}

// Anillo de apriete sobre el tubo del bastidor. Eje = Y (el travesaño).
// La ranura mira hacia -Z y las orejas llevan el tornillo M4 de apriete.
module abrazadera_tubo() {
    difference() {
        union() {
            rotate([-90, 0, 0]) cylinder(d = anillo_ext, h = anillo_h);
            // orejas de apriete
            translate([-9, 0, -anillo_ext / 2 - 5])
                cube([18, anillo_h, anillo_ext / 2 + 5]);
        }
        rotate([-90, 0, 0]) translate([0, 0, -0.5])
            cylinder(d = anillo_int, h = anillo_h + 1);
        // ranura de apriete
        translate([-ranura / 2, -0.5, -anillo_ext])
            cube([ranura, anillo_h + 1, anillo_ext]);
        // tornillo M4 transversal + alojamiento de tuerca
        translate([-15, anillo_h / 2, -anillo_ext / 2 - 2.5]) rotate([0, 90, 0])
            cylinder(d = m4_paso, h = 30);
        translate([-14, anillo_h / 2, -anillo_ext / 2 - 2.5]) rotate([0, 90, 0])
            rotate([0, 0, 30]) cylinder(d = m4_tuerca / cos(30), h = 4, $fn = 6);
    }
}

// =====================================================================
//  PIEZAS
// =====================================================================

// Nudo de 3 vias: los tubos topan entre si dentro del nudo
module conector_3v() {
    difference() {
        union() {
            sphere(d = casq_ext);
            cylinder(d = casq_ext, h = casq_l);
            rotate([0, 90, 0]) cylinder(d = casq_ext, h = casq_l);
            rotate([-90, 0, 0]) cylinder(d = casq_ext, h = casq_l);
        }
        cylinder(d = casq_int, h = casq_l + 0.1);
        rotate([0, 90, 0]) cylinder(d = casq_int, h = casq_l + 0.1);
        rotate([-90, 0, 0]) cylinder(d = casq_int, h = casq_l + 0.1);
        // prisioneros M4 (roscado directo)
        translate([0, 0, casq_l - 9]) rotate([0, 90, 0])
            cylinder(d = m4_nucleo, h = casq_ext);
        rotate([0, 90, 0]) translate([0, 0, casq_l - 9]) rotate([-90, 0, 0])
            cylinder(d = m4_nucleo, h = casq_ext);
        rotate([-90, 0, 0]) translate([0, 0, casq_l - 9]) rotate([0, 90, 0])
            cylinder(d = m4_nucleo, h = casq_ext);
    }
}

// Abrazadera del canal: cuna horizontal (eje Y) sobre el travesaño.
// La cuna abraza mas de 180 grados, asi que el canal entra a presion.
module abraz_canal() {
    h_nido = anillo_ext / 2 + canal_int / 2 + pared;
    module colocar() {
        translate([0, 0, h_nido]) rotate([-90, 0, 0]) children();
    }
    difference() {
        union() {
            abrazadera_tubo();
            translate([-anillo_h / 2, 0, 0]) cube([anillo_h, anillo_h, h_nido]);
            colocar() nido_solido(canal_int, anillo_h, nido_abrazo);
        }
        colocar() nido_hueco(canal_int, anillo_h);
    }
}

// Abrazadera de la carcasa: la cuna nace girada a la inclinacion de
// trabajo (eje en el plano XZ), de modo que el travesaño queda horizontal.
// El giro previo de -90 grados sobre el eje del nido lleva su bisectriz
// bajo la carcasa: la pieza sujeta desde abajo y se cierra por arriba.
module abraz_carcasa() {
    h_nido = anillo_ext / 2 + carc_int / 2 + pared + 12;
    module colocar() {
        translate([0, anillo_h / 2, h_nido])
            rotate([0, 90 - inclinacion, 0]) rotate([0, 0, -90])
                translate([0, 0, -anillo_h / 2]) children();
    }
    difference() {
        union() {
            abrazadera_tubo();
            translate([-anillo_h / 2, 0, 0]) cube([anillo_h, anillo_h, h_nido]);
            colocar() nido_solido(carc_int, anillo_h, nido_abrazo);
        }
        colocar() nido_hueco(carc_int, anillo_h);
    }
}

// Soporte del mastil: abrazadera + alojamiento 608 de eje vertical
module sop_mastil() {
    h_buje = anillo_ext / 2 + 16;
    difference() {
        union() {
            abrazadera_tubo();
            hull() {
                translate([0, anillo_h / 2, h_buje]) cylinder(d = buje_d, h = buje_l);
                translate([-anillo_h / 2, 0, 0]) cube([anillo_h, anillo_h, 2]);
            }
        }
        translate([0, anillo_h / 2, h_buje]) {
            translate([0, 0, buje_l - rod_esp - 0.4])
                cylinder(d = rod_ext_d, h = rod_esp + 0.5);
            translate([0, 0, -30]) cylinder(d = rod_labio, h = buje_l + 30);
        }
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "conector_3v") conector_3v();
else if (pieza == "abraz_canal") abraz_canal();
else if (pieza == "abraz_carcasa") abraz_carcasa();
else if (pieza == "sop_mastil") sop_mastil();
else if (pieza == "conjunto") {
    color("Gainsboro") conector_3v();
    color("LightSteelBlue") translate([120, 0, 0]) abraz_canal();
    color("Thistle") translate([260, 0, 0]) abraz_carcasa();
    color("PaleGreen") translate([380, 0, 0]) sop_mastil();
}
