// =====================================================================
//  VISTA DE CONJUNTO GENERAL (documentacion, no imprimible)
//  Ensambla los STL ya exportados en coordenadas de obra.
//
//  USO:   ./render_stl.sh          (genera ./stl)
//         openscad 07_conjunto_general.scad
//
//  Origen del sistema: base del tubo de la carcasa (boca del casquillo
//  del embudo), con el conjunto ya inclinado a su angulo de trabajo.
//  Los parametros de abajo DEBEN coincidir con los de cada modulo; el
//  archivo recalcula con ellos toda la cadena de cotas y la imprime por
//  consola, asi que sirve de comprobacion del montaje.
// =====================================================================

$fn = 60;
dir = "stl";

/* [Deben coincidir con los modulos] */
inclinacion   = 40;    // Modulo 1
carcasa_largo = 200;   // Modulo 1
boquilla_l    = 55;    // Modulo 1
embudo_h      = 45;    // Modulo 1
z_apice       = 42;    // Modulo 3
ap_axial      = 9.3262;// Modulo 3: r_prim / tan(delta)
cubo_g_h      = 14;    // Modulo 3
z_g           = 20;    // Modulo 3
z_hombro      = 38.5;  // Modulo 4: cam_int_d/2 + 4
x_manguito    = 47.5;  // Modulo 4: encaje + camara/2
canal_largo   = 600;   // longitud del tramo de PVC entre tapones
altura_mastil = 130;   // del apice a la base del rotor

// ---------------------------------------------------------------------
tilt    = 90 - inclinacion;
z_desc  = (carcasa_largo - 20) * sin(inclinacion);
x_desc  = (carcasa_largo - 20) * sin(tilt);
z_boca  = z_desc - boquilla_l;
canal_z = z_boca - z_hombro;
apice   = [(carcasa_largo + z_apice) * sin(tilt), 0,
           (carcasa_largo + z_apice) * sin(inclinacion)];
z_flip  = ap_axial + cubo_g_h;

echo(str("Descarga a z=", z_desc, " | boca de la boquilla a z=", z_boca));
echo(str("EJE DEL CANAL a z=", canal_z, " mm y x=", x_desc));
echo(str("Apice del par conico en ", apice));
// La lamina util del canal la fija el rebose del Tapon B (25 mm sobre la
// generatriz interior del tubo). El deposito debe quedar por debajo para
// que el retorno baje por gravedad, y por encima de las ventanas del
// embudo para que el tornillo tenga agua que coger.
z_rebose = canal_z - 69 / 2 + 25;
echo(str("Lamina de agua en el canal a z=", z_rebose,
         " | el deposito debe quedar por debajo de esa cota"));

module p(f) { import(str(dir, "/", f, ".stl")); }

// --- bomba inclinada -------------------------------------------------
rotate([0, tilt, 0]) {
    color("Gold")      p("01_tornillo_arquimedes_carcasa");
    color("Silver")    translate([0, 0, -embudo_h]) p("01_tornillo_arquimedes_embudo");
    color("SteelBlue") translate([0, 0, 8]) p("01_tornillo_arquimedes_tornillo");
    color("Gainsboro") translate([0, 0, carcasa_largo]) p("03_soporte_rodamientos_soporte");
    // engranaje del tornillo (apice arriba, cuerpo hacia la carcasa)
    color("Orange") translate([0, 0, carcasa_largo + z_apice]) rotate([180, 0, 0])
        translate([0, 0, z_flip]) mirror([0, 0, 1]) p("03_soporte_rodamientos_engranaje");
}
// --- engranaje y mastil verticales ------------------------------------
translate(apice) {
    color("Coral") rotate([0, 0, 180 / z_g])
        translate([0, 0, z_flip]) mirror([0, 0, 1]) p("03_soporte_rodamientos_engranaje");
    color("DimGray") translate([0, 0, 25]) cylinder(d = 8, h = altura_mastil - 25);
}
// --- turbina ----------------------------------------------------------
translate(apice + [0, 0, altura_mastil]) {
    color("RoyalBlue") p("02_turbina_savonius_rotor");
    color("CornflowerBlue") translate([0, 0, 147]) p("02_turbina_savonius_disco_sup");
    color("SteelBlue") translate([0, 0, 75]) p("02_turbina_savonius_disco_medio");
}
// --- canal horizontal --------------------------------------------------
translate([x_desc, 0, canal_z]) {
    // Tapon A girado 180 grados: el canal sale hacia +X, lejos de la bomba
    color("SeaGreen") rotate([0, 0, 180]) translate([-x_manguito, 0, 0])
        p("04_tapones_pvc75_tapon_a");
    color("Gainsboro", 0.4)
        difference() {
            translate([x_manguito, 0, 0]) rotate([0, 90, 0]) cylinder(d = 75, h = canal_largo);
            translate([x_manguito - 1, 0, 0]) rotate([0, 90, 0]) cylinder(d = 69, h = canal_largo + 2);
            for (i = [1 : 5]) translate([x_manguito + 60 + i * 100, 0, 0])
                cylinder(d = 50.6, h = 60);
        }
    color("Teal") translate([x_manguito + canal_largo, 0, 0])
        p("04_tapones_pvc75_tapon_b");
    // el vaso cuelga del taladro: la pestaña apoya en la generatriz del tubo
    for (i = [1 : 5]) translate([x_manguito + 60 + i * 100, 0, 37.5 - 47])
        color("White") p("05_net_cup_50_vaso");
}
