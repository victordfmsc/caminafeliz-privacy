// =====================================================================
//  VISTAS DE MONTAJE PASO A PASO (documentacion, no imprimible)
//  Compone los STL ya exportados en su posicion de obra y va añadiendo
//  piezas segun 'paso'. La pieza que entra en cada paso va resaltada.
//
//  USO:  ./render_stl.sh && openscad -D paso=3 08_montaje.scad
// =====================================================================

$fn = 40;
paso = 10;                 // 1..10
dir  = "stl";

/* [Cotas de obra: deben coincidir con los modulos] */
inclinacion   = 40;
carcasa_largo = 200;
boquilla_l    = 55;
embudo_h      = 45;
z_apice       = 42;
ap_axial      = 9.3262;
cubo_g_h      = 14;
z_g           = 20;
z_hombro      = 38.5;
x_manguito    = 47.5;
canal_largo   = 600;
altura_mastil = 130;
paso_vasos    = 150;

tilt    = 90 - inclinacion;
z_desc  = (carcasa_largo - 20) * sin(inclinacion);
x_desc  = (carcasa_largo - 20) * sin(tilt);
canal_z = z_desc - boquilla_l - z_hombro;
apice   = [(carcasa_largo + z_apice) * sin(tilt), 0,
           (carcasa_largo + z_apice) * sin(inclinacion)];
z_flip  = ap_axial + cubo_g_h;
lamina  = canal_z - 69 / 2 + 25;

NUEVO = "#e8622a";         // pieza que entra en este paso
VIEJO = "#c9ccd1";         // ya montado
AGUA  = "#4aa3df";

module p(f) { import(str(dir, "/", f, ".stl")); }
module cuando(n) { if (paso >= n) color(paso == n ? NUEVO : VIEJO) children(); }

// --- 1 varilla + tornillo -------------------------------------------
cuando(1) rotate([0, tilt, 0]) translate([0, 0, 8]) p("01_tornillo_arquimedes_tornillo");
if (paso >= 1) color(paso == 1 ? "#555" : "#777")
    rotate([0, tilt, 0]) translate([0, 0, -25]) cylinder(d = 8, h = 262);
// --- 2 carcasa -------------------------------------------------------
cuando(2) rotate([0, tilt, 0]) p("01_tornillo_arquimedes_carcasa");
// --- 3 embudo --------------------------------------------------------
cuando(3) rotate([0, tilt, 0]) translate([0, 0, -embudo_h]) p("01_tornillo_arquimedes_embudo");
// --- 4 torreta -------------------------------------------------------
cuando(4) rotate([0, tilt, 0]) translate([0, 0, carcasa_largo])
    p("03_soporte_rodamientos_soporte");
// --- 5 engranaje del tornillo ---------------------------------------
cuando(5) rotate([0, tilt, 0]) translate([0, 0, carcasa_largo + z_apice])
    rotate([180, 0, 0]) translate([0, 0, z_flip]) mirror([0, 0, 1])
        p("03_soporte_rodamientos_engranaje");
// --- 6 engranaje y varilla del mastil --------------------------------
cuando(6) translate(apice) rotate([0, 0, 180 / z_g])
    translate([0, 0, z_flip]) mirror([0, 0, 1]) p("03_soporte_rodamientos_engranaje");
if (paso >= 6) color(paso == 6 ? "#555" : "#777")
    translate(apice + [0, 0, 25]) cylinder(d = 8, h = altura_mastil + 160);
// --- 7 rotor y discos -------------------------------------------------
cuando(7) translate(apice + [0, 0, altura_mastil]) {
    p("02_turbina_savonius_rotor");
    translate([0, 0, 147]) p("02_turbina_savonius_disco_sup");
    translate([0, 0, 75]) p("02_turbina_savonius_disco_medio");
}
// --- 8 canal y tapones ------------------------------------------------
if (paso >= 8) translate([x_desc, 0, canal_z]) {
    color(paso == 8 ? NUEVO : VIEJO) rotate([0, 0, 180])
        translate([-x_manguito, 0, 0]) p("04_tapones_pvc75_tapon_a");
    color("#dfe3e6", 0.55) difference() {
        translate([x_manguito, 0, 0]) rotate([0, 90, 0]) cylinder(d = 75, h = canal_largo);
        translate([x_manguito - 1, 0, 0]) rotate([0, 90, 0]) cylinder(d = 69, h = canal_largo + 2);
        for (i = [1 : 3]) translate([x_manguito + 90 + i * paso_vasos, 0, 0])
            cylinder(d = 50.6, h = 60);
    }
    color(paso == 8 ? NUEVO : VIEJO) {
        translate([x_manguito + canal_largo, 0, 0]) p("04_tapones_pvc75_tapon_b");
        translate([x_manguito + canal_largo + 30, 0, -3.5]) rotate([0, -90, 0])
            p("04_tapones_pvc75_racor");
    }
}
// --- 9 vasos ----------------------------------------------------------
cuando(9) translate([x_desc, 0, canal_z])
    for (i = [1 : 3]) translate([x_manguito + 90 + i * paso_vasos, 0, 37.5 - 47])
        p("05_net_cup_50_vaso");
// --- 10 niveles de agua -----------------------------------------------
if (paso >= 10) {
    // lamina del canal (25 mm sobre el fondo interior)
    color(AGUA, 0.6) translate([x_desc + x_manguito, -30, canal_z - 34.5])
        cube([canal_largo, 60, 25]);
    // deposito: la lamina queda a la altura de la base de la carcasa
    color(AGUA, 0.30) translate([-60, -130, -120]) cube([260, 260, 120]);
}
