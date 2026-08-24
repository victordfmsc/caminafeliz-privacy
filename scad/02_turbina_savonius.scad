// =====================================================================
//  MODULO 2 - Turbina Savonius de 2 alabes (alto par de arranque)
//  Acciona el tornillo de Arquimedes del Modulo 1 a traves del
//  reenvio conico del Modulo 3.
// ---------------------------------------------------------------------
//  REGLAS GLOBALES: $fn = 100 | pared min. 3 mm | tolerancia 0.3 mm
//  Sin librerias externas.
// ---------------------------------------------------------------------
//  ALABES HELICOIDALES
//    La torsion reparte el par a lo largo de la vuelta: el rotor arranca
//    solo desde CUALQUIER posicion y el rizado del par cae mucho, que es
//    lo que se busca para mover un tornillo de Arquimedes a bajo regimen.
//    Limite de impresion: el canto del alabe se inclina
//        atan(R * torsion_rad / altura)  respecto a la vertical,
//    y debe quedar por debajo de 45 grados para salir sin soportes.
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
torsion      = 120;      // grados de torsion del alabe en toda su altura
                         // (0 = Savonius recto clasico)
encaje_disco = 2;        // profundidad de la ranura de los discos

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
alabe_h   = altura_total - 2 * disco_esp + encaje_disco;  // el alabe entra
                                         // 2 mm dentro del disco superior
z_medio   = altura_total / 2;            // cota del disco intermedio
eje_ag_d  = eje_d + tol;                 // 8.3 encaje a presion
plano_d   = d_shaft - eje_d / 2 + tol / 2;   // distancia del centro al plano
manguito_d= eje_ag_d + 2 * pared;        // 14.3
m3_broca  = 2.8;
m4_paso   = 4.4;
tw_slices = max(20, ceil(torsion / 2));
lean      = atan(rotor_d / 2 * torsion * PI / 180 / alabe_h);  // inclinacion del canto
function ang_alabe(h) = -torsion * h / alabe_h;   // giro de la seccion a la altura h

assert(pala_esp >= 2.4, "Pared curva demasiado fina para PETG/ASA.");
assert(alabe_off + alabe_d/2 == rotor_d/2, "Geometria Savonius incoherente.");
assert(lean <= 45,
       "Torsion excesiva: el canto del alabe pasa de 45 grados y pide soportes.");

echo(str("Alabe D=", alabe_d, " offset=", alabe_off, " altura alabe=", alabe_h));
echo(str("Torsion=", torsion, " deg | inclinacion del canto=", lean,
         " deg respecto a la vertical (limite 45)"));

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

// Tramo de alabe entre las alturas h0 y h0+esp, con su giro helicoidal.
// Se usa tanto para las ranuras ciegas del disco superior como para las
// ranuras pasantes del disco intermedio: asi la ranura copia exactamente
// la seccion del alabe a la cota en la que se monta cada disco.
module seccion_alabe(h0, esp, extra = 0) {
    rotate([0, 0, ang_alabe(h0)])
        linear_extrude(height = esp, twist = torsion * esp / alabe_h,
                       slices = max(2, ceil(esp)))
            alabes_2d(extra);
}

// Rotor: disco inferior + alabes (+ manguito central opcional)
module rotor() {
    difference() {
        union() {
            cylinder(d = disco_d, h = disco_esp);              // disco inferior
            translate([0, 0, disco_esp])                       // alabes
                linear_extrude(height = alabe_h, twist = torsion,
                               slices = tw_slices) alabes_2d();
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

// Disco superior: ranura ciega que abraza los ultimos 2 mm del alabe
module disco_sup() {
    difference() {
        union() {
            cylinder(d = disco_d, h = disco_esp);
            translate([0, 0, disco_esp]) cylinder(d = cubo_d, h = cubo_h_sup);
        }
        translate([0, 0, -0.01])
            seccion_alabe(alabe_h - encaje_disco, encaje_disco + 0.01, tol);
        translate([0, 0, -0.01]) agujero_eje(disco_esp + cubo_h_sup + 0.02);
        translate([0, 0, disco_esp + cubo_h_sup / 2]) rotate([0, 90, 0])
            cylinder(d = m3_broca, h = cubo_d);
    }
}

// Disco intermedio: ranuras pasantes con la seccion de su cota de montaje.
// Con alabe torsionado el disco no se desliza: se ENROSCA hasta su sitio.
module disco_medio_p() {
    difference() {
        cylinder(d = disco_d - 6, h = disco_esp);
        translate([0, 0, -0.01])
            seccion_alabe(z_medio - disco_esp, disco_esp + 0.02, tol);
        translate([0, 0, -0.01]) cylinder(d = eje_d + 2 * tol, h = disco_esp + 0.02);
    }
}

// =====================================================================
//  SELECCION DE PIEZA
// =====================================================================
if (pieza == "rotor") rotor();
else if (pieza == "disco_sup") disco_sup();
else if (pieza == "disco_medio") disco_medio_p();
else if (pieza == "conjunto") {
    color("Khaki") rotor();
    color("Tan") translate([0, 0, altura_total - disco_esp]) disco_sup();
    if (disco_medio)
        color("Sienna") translate([0, 0, z_medio]) disco_medio_p();
}
// cualquier otro valor de 'pieza' no genera geometria (util para pruebas)
