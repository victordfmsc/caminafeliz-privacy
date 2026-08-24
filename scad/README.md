# Bomba de Arquímedes eólica para canal hidropónico — modelos OpenSCAD

Cinco módulos paramétricos listos para renderizar **sin librerías externas**.
Todo el conjunto se ha verificado exportando STL con OpenSCAD 2021.01: las 14
piezas salen como sólidos cerrados y sin auto-intersecciones (`Simple: yes`).

![Módulo 1](img/01_tornillo_arquimedes.png)

## Configuración global aplicada

| Regla | Valor | Dónde se aplica |
|---|---|---|
| Suavizado | `$fn = 100` | todas las superficies curvas |
| Pared mínima | 3.0 mm | carcasa, embudo, tapones, cubos, alojamientos |
| Tolerancia | 0.3 mm | uniones mecánicas |

La tolerancia se aplica con dos criterios, declarados en cada archivo:

* **Unión deslizante o rotativa** → `Ø + 2·tol` (0.3 mm radial). Ej.: hélice
  Ø40 dentro de carcasa Ø40.6; boquilla Ø32 dentro de manguito Ø32.6.
* **Encaje a presión** → `Ø + 1·tol` (0.3 mm diametral). Ej.: agujero Ø8.3
  sobre varilla de Ø8.

## Archivos

```
01_tornillo_arquimedes.scad   tornillo · carcasa · embudo
02_turbina_savonius.scad      rotor · disco_sup · disco_medio
03_soporte_rodamientos.scad   soporte · alojamiento · engranaje · acople
04_tapones_pvc75.scad         tapon_a · tapon_b
05_net_cup_50.scad            vaso · plantilla
render_stl.sh                 exporta los 14 STL a ./stl
```

Cada archivo tiene una variable `pieza` al principio. Desde la GUI se cambia
esa variable; desde la consola:

```bash
openscad -o tornillo.stl -D 'pieza="tornillo"' 01_tornillo_arquimedes.scad
./render_stl.sh          # todas las piezas de una vez
```

Con `pieza = "conjunto"` cada archivo muestra la vista de montaje (solo para
comprobar, no exportar a STL).

---

## Módulo 1 · Tornillo de Arquímedes y carcasa

| Pieza | Uds | Cotas |
|---|---|---|
| `tornillo` | 1 | helicoide Ø40, paso 30, largo 180, núcleo Ø14.3, agujero Ø8.3 |
| `carcasa` | 1 | tubo Ø40.6 int / Ø46.6 ext, largo 200, boquilla Ø32, corona M4 |
| `embudo` | 1 | boca Ø90, patas, buje liso Ø8.6 para el extremo inferior del eje |

Decisiones de diseño:

* **Núcleo de Ø14.3 y no de Ø8.** El enunciado pide eje central de Ø8 *para
  varilla metálica*: si el núcleo impreso midiera Ø8 no quedaría material
  alrededor del agujero. El núcleo se calcula como `Ø8.3 + 2·3 mm` para
  cumplir la pared mínima; la varilla sigue siendo de Ø8.
* **Helicoide por barrido con `hull()`** en lugar de `linear_extrude(twist)`.
  Con *twist* el espesor del filete se adelgaza al crecer el radio (a Ø40
  quedaría en ~0.5 mm, no imprimible). El barrido de sectores encadenados da
  un espesor axial constante de 2.2 mm en todo el radio.
* **Carcasa partida en tubo (200 mm) + embudo (77 mm)**: ambas caben de pie en
  camas de 220–250 mm sin cortar el conducto por un plano de fuga. El embudo
  encaja a presión sobre el tubo con casquillo Ø47.2.
* La boquilla termina en **bajante vertical**, no en un chorro inclinado, para
  que entre a plomo en el manguito del Tapón A.

Impresión: tornillo y tubo de pie; el codo de la boquilla pide soporte solo
bajo su vuelo. Embudo de pie sobre las patas (nervios y buje autoportantes a 45°).

## Módulo 2 · Turbina Savonius de 2 álabes

| Pieza | Uds | Cotas |
|---|---|---|
| `rotor` | 1 | Ø120 × 150 total, álabes de 2.5 mm, disco inferior Ø132 |
| `disco_sup` | 1 | Ø132 × 3 con ranura de encaje y cubo |
| `disco_medio` | 1 | Ø126 × 3, ranuras pasantes (opcional) |

* Geometría S clásica: dos semicilindros de Ø66 desplazados 12 mm de solape
  → `D = 2·66 − 12 = 120`. Cantos libres engrosados con perla de refuerzo.
* Altura 150 mm **incluidos** los dos discos (álabes de 144 mm).
* Cubo `D-shaft` (cota D = 7.0 mm) o cilíndrico con prisionero M3, a elegir
  con `cubo_tipo`.
* Los discos superior e intermedio se imprimen aparte: un disco de Ø132
  impreso en el aire sobre los álabes saldría descolgado. El intermedio lleva
  ranura pasante y se desliza desde arriba hasta media altura.

## Módulo 3 · Soporte, rodamientos y reenvío cónico

| Pieza | Uds | Cotas |
|---|---|---|
| `soporte` | 1 | los **dos** alojamientos 608 a 90°, base con 4×M4 a PCD 60 |
| `alojamiento` | 1 | chumacera suelta 608 para el extremo libre de la turbina |
| `engranaje` | 2 | cónico recto 1:1, m=2, z=20, Ø primitivo 40, Ø ext 44 |
| `acople` | 0–1 | acople rígido 8–8 con 4 prisioneros M3 (alternativa) |

* Cajera de Ø22.2 × 7.4 mm con labio de retención de Ø19.5 que apoya sobre la
  pista exterior del 608.
* **El perfil de evolvente se genera en el propio archivo** (`inv_deg`,
  `diente_2d`), sin librerías. El dentado cónico se obtiene extruyendo el
  perfil con `scale = (A₀−F)/A₀` hacia el ápice, lo que sitúa los dientes
  sobre el cono primitivo de 45°: reducción 1:1 con ejes a 90°.
* Se imprimen con el extremo menor sobre la cama: el cono crece a 45°, que es
  el límite autoportante, así que salen sin soportes.
* Los dos conos comparten ápice a `z = 42` sobre la base de la torreta.
* Si prefieres montar los ejes alineados en vez de a 90°, usa `acople` y
  prescinde del par cónico.

## Módulo 4 · Tapones del canal de PVC Ø75

| Pieza | Uds | Cotas |
|---|---|---|
| `tapon_a` | 1 | casquillo Ø75.6/Ø81.6, cámara, manguito de entrada Ø32.6 |
| `tapon_b` | 1 | ídem + racor de rebose Ø12 con espigas |

* `modo_encaje = "exterior"` (casquillo sobre el tubo) o `"interior"` (tapón
  macho dentro del tubo): de ahí lo de *ajustables*. El escalón entre el
  encaje y la cámara hace de tope del tubo, sin piezas añadidas.
* **Comprueba la pared real de tu tubo** (`tubo_pared`, 1.8 o 3.0 mm en PVC de
  evacuación Ø75): de ella depende el Ø interior y, con él, la cota del rebose.
* La lámina de agua se fija con `nivel_agua = 25`: el programa coloca el eje
  del racor a `−Ø_int/2 + 25 + Ø_racor/2 = −3.5 mm` respecto al eje del tubo,
  de modo que la **generatriz inferior** del paso quede justo a 25 mm del
  fondo interior.
* Impresión: girar 90° (eje del tubo vertical, fondo sobre la cama). El
  manguito del Tapón A queda horizontal y pide soporte bajo su vuelo.

## Módulo 5 · Vasos de cultivo Ø50

| Pieza | Uds | Cotas |
|---|---|---|
| `vaso` | n | Ø50 arriba → Ø35 abajo, alto 50, pestaña Ø60 × 3 mm |
| `plantilla` | 1 | galga curva para marcar dos taladros a 100 mm |

* 16 ranuras verticales de 2 mm en la pared y 8 radiales en el fondo, más
  drenaje central Ø6. Las ranuras del fondo no llegan al canto: el anillo
  exterior queda continuo y la base no se abre.
* Pared de 2.5 mm en lugar de 3 mm: la regla de 3 mm cubre las piezas
  estancas; la canastilla es una celosía sin presión y con 3 mm las ranuras
  quedarían demasiado profundas para el paso de raíz. El parámetro `pared`
  está expuesto si prefieres subirla.
* Taladro en el canal: **Ø50.6** (Ø50 + 2·0.3), separación sugerida 100 mm.
* Chaflán de 45° bajo la pestaña para que el vuelo de 5 mm salga sin soporte.

---

## Interfaces entre módulos

| Unión | Cota que manda | Valor |
|---|---|---|
| Hélice ↔ carcasa | juego radial | Ø40 / Ø40.6 |
| Varilla ↔ tornillo, cubos, engranajes | encaje a presión | Ø8 / Ø8.3 |
| Varilla ↔ buje del embudo | giro libre | Ø8 / Ø8.6 |
| Carcasa ↔ soporte | corona Ø74, 4×M4 | PCD 60 |
| Boquilla ↔ Tapón A | encaje deslizante | Ø32 / Ø32.6, 20 mm de inserción |
| Vaso ↔ canal | taladro | Ø50.6 |
| Rodamiento 608 ↔ cajera | ajuste a presión | Ø22.2 × 7.4 |

Cotas de montaje que se deducen de lo anterior (con los parámetros por defecto):

* Eje del canal de PVC: **85 mm** por encima de la base del tubo de la carcasa
  (142 mm sobre el suelo en el que apoyan las patas del embudo).
* Vertical del manguito del Tapón A: **55 mm** del eje del tornillo.
* Eje horizontal de la turbina: **242 mm** sobre la base del tubo de la carcasa.

Si cambias `boquilla_vuelo`, `boquilla_ang`, `bajante_h` o `manguito_h`,
recalcula estas tres cotas antes de cortar el bastidor.

## Materiales y herrajes

* Varilla de acero inoxidable Ø8: ~250 mm (tornillo) + ~200 mm (turbina).
* 3 × rodamiento **608ZZ** (2 en la torreta, 1 en la chumacera).
* Prisioneros M3×6 (6 uds) y tornillería M4×20 con tuerca (6 uds).
* Tubo PVC Ø75 de evacuación, a la longitud del canal.
* Manguito flexible Ø16 para el retorno del rebose al depósito.

## Laminado

* **Material: PETG o ASA.** Nada de PLA: se degrada por hidrólisis con el agua
  del nutriente y por UV a la intemperie.
* **4 perímetros como mínimo** y **relleno 100 %** en carcasa, embudo y
  tapones: así son estancos sin sellador.
* Resto de piezas: 4 perímetros, 40–60 % de relleno.
* Altura de capa 0.2 mm; para la hélice, 0.15 mm mejora el acabado del filete.
* Sin soportes salvo: codo de la boquilla (Módulo 1) y manguito del Tapón A
  (Módulo 4).

## Orden de montaje

1. Buje del embudo y rodamiento inferior; embudo a presión sobre la carcasa.
2. Tornillo sobre la varilla, prisioneros M3; introducir el conjunto en la carcasa.
3. Torreta atornillada a la corona (4×M4), rodamiento 608 vertical y engranaje
   cónico sobre la varilla del tornillo, con el ápice a 42 mm de la base.
4. Eje de la turbina: 608 en la torreta + chumacera en el extremo libre,
   segundo engranaje enfrentado a 90°. Verificar juego de engrane antes de
   apretar prisioneros.
5. Rotor Savonius: pegar el disco superior en su ranura y deslizar el disco
   intermedio hasta media altura.
6. Canal: taladrar Ø50.6 con la plantilla, montar Tapón A bajo la bajante de
   la boquilla y Tapón B con el racor hacia el depósito.
