// =====================================================================
//  MODULO 2 - Turbina Savonius de 2 alabes (alto par de arranque)
//  Acciona el tornillo de Arquimedes del Modulo 1 a traves del
//  reenvio conico del Modulo 3.
// ---------------------------------------------------------------------
//  REGLAS GLOBALES: $fn = 100 | pared min. 3 mm | tolerancia 0.3 mm
//  Sin librerias externas.
// ---------------------------------------------------------------------
//  GEOMETRIA SAVONIUS
//    Dos semicilindros de diametro d desplazados sobre su cuerda un
//    solape e:   D_rotor = 2*d - e   ->  d = (D_rotor + e)/2
//    Con D=120 y e=12  ->  d=66, offset de centros = (d-e)/2 = 27 mm
// ---------------------------------------------------------------------
//  PIEZAS
//    "rotor"      -> alabes + disco inferior + cubo   (imprimir de pie)
//    "disco_sup"  -> disco superior con ranura de encaje (ranura hacia
//                    la cama: el hueco de 2 mm se puentea sin soportes)
//    "disco_medio"-> disco intermedio ranurado pasante (opcional; se
//                    desliza desde arriba y se encola a media altura)
//    "conjunto"   -> vista de montaje (NO exportar a STL)
// =====================================================================

$fn = 100;

/* [Pieza a renderizar] */
pieza = "rotor";         // ["rotor","disco_sup","disco_medio","conjunto"]

/* [Reglas globales] */
tol   = 0.3;
pared = 3;

/* [Rotor] */
altura_total = 150;      // altura total incluidos los dos discos
rotor_d      = 120;      // diametro total del rotor
solape       = 12;       // solape entre alabes (0.10-0.20 * d_alabe)
pala_esp     = 2.5;      // espesor de la pared curva
disco_d      = 132;      // discos de refuerzo (1.1 * rotor_d)
disco_esp    = 3;
disco_medio  = true;     // disco intermedio antivibracion

/* [Eje] */
eje_d        = 8;        // varilla metalica
cubo_tipo    = "D";      // ["D","redondo"]  D-shaft o cilindrico con prisionero
d_shaft      = 7.0;      // cota D del eje aplanado (8 mm rebajado a 7 mm)
cubo_d       = 18;
cubo_h_inf   = 22;       // longitud del cubo inferior (lado transmision)
cubo_h_sup   = 15;
eje_pasante  = false;    // true = manguito central macizo de extremo a extremo
                         // (mas rigido, pero ocupa el canal de solape)

// ---------------------------------------------------------------------
//  COTAS DERIVADAS
// ---------------------------------------------------------------------
alabe_d   = (rotor_d + solape) / 2;      // 66
alabe_off = (alabe_d - solape) / 2;      // 27
alabe_h   = altura_total - 2 * disco_esp;// 144
eje_ag_d  = eje_d + tol;                 // 8.3 encaje a presion
plano_d   = d_shaft - eje_d / 2 + tol / 2;   // distancia del centro al plano
manguito_d= eje_ag_d + 2 * pared;        // 14.3
m3_broca  = 2.8;
m4_paso   = 4.4;

assert(pala_esp >= 2.4, "Pared curva demasiado fina para PETG/ASA.");
assert(alabe_off + alabe_d/2 == rotor_d/2, "Geometria Savonius incoherente.");

echo(str("Alabe D=", alabe_d, " offset=", alabe_off, " altura alabe=", alabe_h));

// =====================================================================
//  PERFILES
// =====================================================================

// Un alabe: media corona circular con los bordes libres engrosados.
// 'extra' infla el perfil para generar las ranuras de los discos.
module alabe_2d(extra = 0) {
    offset(r = extra) {
        translate([alabe_off, 0]) {
            intersection() {
                difference() {
                    circle(d = alabe_d);
                    circle(d = alabe_d - 2 * pala_esp);
                }
                translate([-alabe_d, 0]) square([2 * alabe_d, alabe_d]);
            }
            // Perlas de refuerzo en los dos cantos libres
            for (sx = [-1, 1])
                translate([sx * (alabe_d - pala_esp) / 2, 0])
                    circle(d = pala_esp + 1.5);
        }
    }
}

module alabes_2d(extra = 0) {
    alabe_2d(extra);
    rotate([0, 0, 180]) alabe_2d(extra);
}

// Agujero de eje: D-shaft o cilindrico
module agujero_eje(h) {
    if (cubo_tipo == "D")
        intersection() {
            cylinder(d = eje_ag_d, h = h);
            translate([-eje_d, -eje_d, 0]) cube([2 * eje_d, eje_d + plano_d, h]);
        }
    else
        cylinder(d = eje_ag_d, h = h);
}

// Cubo con prisionero M3 radial
module cubo(h) {
    difference() {
        cylinder(d = cubo_d, h = h);
        translate([0, 0, -0.01]) agujero_eje(h + 0.02);
        translate([0, 0, h / 2]) rotate([0, 90, 0])
            cylinder(d = m3_broca, h = cubo_d);
    }
}

// =====================================================================
//  PIEZAS
// =====================================================================

// Rotor: disco inferior + alabes (+ manguito central opcional)
module rotor() {
    difference() {
        union() {
            cylinder(d = disco_d, h = disco_esp);              // disco inferior
            translate([0, 0, disco_esp])
                linear_extrude(height = alabe_h) alabes_2d();  // alabes
            translate([0, 0, -cubo_h_inf]) cylinder(d = cubo_d, h = cubo_h_inf + disco_esp);
            if (eje_pasante)
                cylinder(d = manguito_d, h = altura_total - disco_esp);
        }
        translate([0, 0, -cubo_h_inf - 0.01])
            agujero_eje(altura_total + cubo_h_inf + 0.02);
        translate([0, 0, -cubo_h_inf + 8]) rotate([0, 90, 0])
            cylinder(d = m3_broca, h = cubo_d);
    }
}

// Disco superior: ranura ciega de 2 mm que abraza el canto de los alabes
module disco_sup() {
    ranura = 2;
    difference() {
        union() {
            cylinder(d = disco_d, h = disco_esp);
            translate([0, 0, disco_esp]) cylinder(d = cubo_d, h = cubo_h_sup);
        }
        translate([0, 0, -0.01])
            linear_extrude(height = ranura) alabes_2d(tol);
        translate([0, 0, -0.01]) agujero_eje(disco_esp + cubo_h_sup + 0.02);
        translate([0, 0, disco_esp + cubo_h_sup / 2]) rotate([0, 90, 0])
            cylinder(d = m3_broca, h = cubo_d);
    }
}

// Disco intermedio: ranuras pasantes, se desliza desde arriba
module disco_medio_p() {
    difference() {
        cylinder(d = disco_d - 6, h = disco_esp);
        translate([0, 0, -0.01])
            linear_extrude(height = disco_esp + 0.02) alabes_2d(tol);
        translate([0, 0, -0.01]) cylinder(d = eje_d + 2 * tol, h = disco_esp + 0.02);
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "rotor") rotor();
else if (pieza == "disco_sup") disco_sup();
else if (pieza == "disco_medio") disco_medio_p();
else {
    color("Khaki") rotor();
    color("Tan") translate([0, 0, altura_total - disco_esp]) disco_sup();
    if (disco_medio)
        color("Sienna") translate([0, 0, altura_total / 2]) disco_medio_p();
}
