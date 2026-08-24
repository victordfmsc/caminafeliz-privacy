# Bomba de Arquímedes eólica para canal hidropónico — modelos OpenSCAD

Seis módulos paramétricos, sin librerías externas, más una vista de conjunto.
Las 18 piezas se han exportado a STL con OpenSCAD 2021.01: todas salen como
sólido cerrado y sin auto-intersecciones (`Simple: yes`).

![Conjunto general](img/07_conjunto_general.png)

## Configuración global aplicada

| Regla | Valor | Dónde |
|---|---|---|
| Suavizado | `$fn = 100` | todas las superficies curvas |
| Pared mínima | 3.0 mm | carcasa, embudo, tapones, cubos, alojamientos |
| Tolerancia | 0.3 mm | uniones mecánicas |

Dos criterios, declarados en cada archivo:

* **Deslizante o rotativa** → `Ø + 2·tol`. Hélice Ø40 en carcasa Ø40.6;
  boquilla Ø32 en manguito Ø32.6; casquillo de PVC Ø75 → Ø75.6.
* **A presión** → `Ø + 1·tol`. Agujero Ø8.3 sobre varilla de Ø8.

## Las dos decisiones que gobiernan el resto

**1. El tornillo trabaja inclinado 40°, no vertical.** Un Arquímedes vertical no
bombea: el agua no forma cangilones cerrados. El criterio de Rorres (2000) exige
que el paso adimensional

```
Λ = S · tan(α) / (2 π Ro)
```

sea menor que 1. Con el paso de 30 mm y Ø40 pedidos, **α = 40° da Λ = 0.200**,
que además cae en el entorno del óptimo de caudal por vuelta. El archivo calcula
Λ y aborta con `assert` si la combinación paso/ángulo deja de cerrar cangilones.
Elevación útil resultante: **116 mm** con los 180 mm de hélice del enunciado.

**2. Con turbina de eje vertical y tornillo a 40°, el par cónico no es de 90°.**
Los dos ejes se cortan en el ápice de los conos primitivos y salen de él hacia
sus respectivos engranajes: el mástil hacia arriba y el eje del tornillo hacia
abajo-adelante. El ángulo entre esas direcciones es

```
Σ = 90 + inclinación = 130°   →   cono primitivo = Σ/2 = 65°  (relación 1:1)
```

Con `inclinacion = 0` el par degenera en el cónico clásico de 90°/45°, que era
lo especificado en el enunciado original.

## Archivos

```
01_tornillo_arquimedes.scad   tornillo · carcasa · embudo
02_turbina_savonius.scad      rotor · disco_sup · disco_medio
03_soporte_rodamientos.scad   soporte · alojamiento · engranaje · acople
04_tapones_pvc75.scad         tapon_a · tapon_b
05_net_cup_50.scad            vaso · plantilla
06_estructura.scad            conector_3v · abraz_canal · abraz_carcasa · sop_mastil
07_conjunto_general.scad      vista de obra (importa los STL, no imprimible)
render_stl.sh                 exporta los 18 STL a ./stl
```

```bash
openscad -o tornillo.stl -D 'pieza="tornillo"' 01_tornillo_arquimedes.scad
./render_stl.sh                       # todas las piezas
openscad 07_conjunto_general.scad     # vista de obra, tras render_stl.sh
```

`pieza = "conjunto"` en cualquier módulo muestra su vista de montaje.

---

## Módulo 1 · Tornillo de Arquímedes y carcasa

![Módulo 1](img/01_tornillo_arquimedes.png)

| Pieza | Uds | Cotas |
|---|---|---|
| `tornillo` | 1 | helicoide Ø40, paso 30, largo 180, núcleo Ø14.3, agujero Ø8.3 |
| `carcasa` | 1 | tubo Ø40.6/Ø46.6, largo 200, boquilla Ø32, corona 4×M4 |
| `embudo` | 1 | boca Ø90 con ventanas de admisión y buje liso Ø8.6 |

* **Núcleo Ø14.3 y no Ø8.** La varilla es de Ø8; con núcleo de Ø8 no quedaría
  material alrededor del agujero. Se calcula como `Ø8.3 + 2·3`.
* **Helicoide por barrido con `hull()`**, no `linear_extrude(twist)`: con *twist*
  el filete se adelgaza al crecer el radio (~0.5 mm a Ø40, no imprimible). El
  barrido da 2.2 mm de espesor axial constante.
* **La boquilla es un tramo recto girado exactamente la inclinación**, así que
  cae **a plomo** con el conjunto montado y su boca queda cortada horizontal.
  No hace falta codo.
* El embudo va **suelto de patas**: cuelga sumergido y la carcasa la sujeta la
  abrazadera del Módulo 6. Cuatro ventanas dejan entrar agua aunque la boca
  quede cerca del fondo. Lleva la araña con el buje inferior del eje.
* Carcasa partida en tubo (200 mm) + embudo (77 mm) para que ambas quepan de pie.

## Módulo 2 · Turbina Savonius helicoidal

![Módulo 2](img/02_turbina_savonius.png)

| Pieza | Uds | Cotas |
|---|---|---|
| `rotor` | 1 | Ø120 × 150 total, álabes de 2.5 mm, torsión 120° |
| `disco_sup` | 1 | Ø132 × 3 con ranura helicoidal de encaje |
| `disco_medio` | 1 | Ø126 × 3, ranuras pasantes |

* Geometría S clásica: dos semicilindros de Ø66 con 12 mm de solape
  → `D = 2·66 − 12 = 120`.
* **Álabe helicoidal (120°).** Reparte el par en la vuelta: arranca solo desde
  cualquier posición y baja el rizado, que es lo que necesita un tornillo de
  Arquímedes a bajo régimen. El límite lo pone la impresión: el canto se inclina
  `atan(R·torsión/altura)` y el archivo aborta si pasa de 45°. Con 120° sale
  **40.7°**, autoportante; con 180° serían 52.6° y pediría soportes.
* Las ranuras de los discos **no son rectas**: copian la sección helicoidal a la
  cota exacta de montaje. Comprobado por booleana — restar la ranura al tramo de
  álabe correspondiente da geometría vacía, mientras que con la ranura sin
  corregir (control negativo) queda material. El disco intermedio no se desliza:
  **se enrosca** hasta media altura.
* Los discos superior e intermedio se imprimen aparte: un disco de Ø132 impreso
  en el aire sobre los álabes se descolgaría.

## Módulo 3 · Soporte, rodamientos y reenvío cónico

![Módulo 3](img/03_soporte_rodamientos.png)

| Pieza | Uds | Cotas |
|---|---|---|
| `soporte` | 1 | los **dos** alojamientos 608 (uno vertical al tornillo, otro a 50° para el mástil), base 4×M4 a PCD 60 |
| `engranaje` | 2 | cónico 1:1, m=2, z=20, Ø primitivo 40, Ø cabeza exterior 41.7 |
| `alojamiento` | 0–1 | chumacera 608 suelta |
| `acople` | 0–1 | acople rígido 8–8, alternativa si los ejes se alinean |

* **El perfil de evolvente se genera en el propio archivo** (`inv_deg`,
  `diente_2d`), sin librerías.
* El dentado se extruye escalado hacia el ápice (Tredgold) y después se
  **recorta con los dos conos reales**:
  * *cono de cabeza* (ápice en el ápice primitivo, semiángulo `δ + atan(m/A₀)`).
    Sin él la punta del diente sobra `m·(1−cos δ)` = 1.15 mm y se clava en el
    fondo del conjugado.
  * *cono trasero*, perpendicular al primitivo en el punto primitivo exterior.

  Con ambos, el radio de cabeza exterior sale **20.845 mm = r + m·cos δ**, la
  cota de norma. La interferencia medida entre los dos engranajes engranados
  cayó de **0.016 cm³ a 0.002 cm³**; lo que queda está repartido en cinco
  dientes alrededor del punto primitivo, donde los conos son tangentes por
  definición, y es del orden del error de teselado.
* **Juego entre flancos de 0.3 mm** (`juego_g`), restado del semiespesor
  angular. Sin él el par se agarrota.
* El soporte **resta el volumen de giro de los dos engranajes**, así que ni el
  brazo ni las cartelas pueden rozar aunque se cambien las cotas. Antes de
  añadirlo, el brazo rozaba 1 mm³ con el engranaje del mástil.
* Impresión sin soportes: cubo abajo, alma cónica a 45°, dentado arriba. El
  dentado siempre reduce su radio al subir, sea δ 45° o 65°.

## Módulo 4 · Tapones del canal de PVC Ø75

![Módulo 4](img/04_tapones_pvc75.png)

| Pieza | Uds | Cotas |
|---|---|---|
| `tapon_a` | 1 | casquillo Ø75.6/Ø81.6, cámara, manguito de entrada Ø32.6 |
| `tapon_b` | 1 | ídem + racor de rebose Ø12 con espigas |

* `modo_encaje = "exterior"` (casquillo sobre el tubo) o `"interior"` (macho
  dentro del tubo): de ahí lo de *ajustables*. El escalón entre encaje y cámara
  hace de tope, sin piezas añadidas.
* **Comprueba la pared real de tu tubo** (`tubo_pared`, 1.8 o 3.0 mm): de ella
  dependen el Ø interior y la cota del rebose.
* La lámina de agua la fija `nivel_agua = 25`: el eje del racor se coloca en
  `−Ø_int/2 + 25 + Ø_racor/2 = −3.5 mm` respecto al eje del tubo, para que la
  **generatriz inferior** del paso quede a 25 mm del fondo interior.
* La profundidad de encaje del manguito **se deriva** del hombro de tope
  (4 mm sobre la generatriz del conducto) en vez de fijarse a mano: fijarla
  hacía caer el hombro justo en la tangente de la cámara y generaba un sólido
  degenerado.

## Módulo 5 · Vasos de cultivo Ø50

![Módulo 5](img/05_net_cup_50.png)

| Pieza | Uds | Cotas |
|---|---|---|
| `vaso` | n | Ø50 → Ø35, alto 50, pestaña Ø60 × 3 mm |
| `plantilla` | 1 | galga curva para marcar dos taladros a 100 mm |

* 16 ranuras verticales de 2 mm y 8 radiales en el fondo, más drenaje Ø6. Las
  del fondo no llegan al canto: el anillo exterior queda continuo.
* Pared de 2.5 mm en vez de 3: la regla de 3 mm cubre las piezas estancas; la
  canastilla es celosía sin presión. El parámetro está expuesto.
* Taladro en el canal **Ø50.6**, separación sugerida 100 mm.
* Chaflán de 45° bajo la pestaña: el vuelo de 5 mm sale sin soporte.

## Módulo 6 · Estructura

![Módulo 6](img/06_estructura.png)

| Pieza | Uds | Función |
|---|---|---|
| `conector_3v` | 4–8 | nudo de esquina de 3 vías para bastidor de tubo Ø25 |
| `abraz_canal` | 2–3 | cuna de 200° que retiene el canal Ø75 a presión |
| `abraz_carcasa` | 1–2 | ídem para la carcasa, con la cuna ya girada a 40° |
| `sop_mastil` | 1 | abrazadera con alojamiento 608 para el mástil |

* Anillo partido con oreja y tornillo M4 con alojamiento hexagonal.
* Las cunas abrazan más de 180°, así que sujetan a presión; además llevan
  pasadores para brida de nylon.
* Las cunas se posicionan por `children()` y el vaciado se hace al final, de
  forma que la columna de unión sube hasta el eje y la junta nunca queda a tope.

---

## Cadena de cotas del montaje

Origen: base del tubo de la carcasa (boca del casquillo del embudo), con el
conjunto ya inclinado. `07_conjunto_general.scad` recalcula e imprime todo esto.

| Cota | Valor |
|---|---|
| Elevación útil del tornillo (180 mm a 40°) | 115.7 mm |
| Eje de la boquilla (descarga) | z = 115.7 mm |
| Boca de la boquilla | z = 60.7 mm |
| Vuelo libre de la boquilla fuera de la carcasa | 24.6 mm (14 encajan en el manguito) |
| **Eje del canal de PVC** | **z = 22.2 mm, x = 137.9 mm** |
| Lámina de agua en el canal (la fija el rebose) | z = 12.7 mm |
| Ápice del par cónico | (185.4, 0, 155.6) |

**Consecuencia de diseño que conviene tener presente:** con los 180 mm de hélice
del enunciado el canal queda forzosamente bajo — su eje a 22 mm sobre la base de
la carcasa — porque el agua no puede salir más arriba de donde termina el
tornillo. El depósito tiene que ser bajo o ir embutido, y su lámina debe quedar
por encima de las ventanas del embudo y por debajo de z = 12.7 mm para que el
retorno baje por gravedad. Si necesitas el canal más alto, sube `largo_util` o
`inclinacion` (a 60° la elevación pasa a 156 mm y Λ sigue valiendo 0.41 < 1).

## Interfaces

| Unión | Criterio | Valor |
|---|---|---|
| Hélice ↔ carcasa | deslizante | Ø40 / Ø40.6 |
| Varilla ↔ tornillo, cubos, engranajes | a presión | Ø8 / Ø8.3 |
| Varilla ↔ buje del embudo | giro libre | Ø8 / Ø8.6 |
| Carcasa ↔ soporte | corona Ø74 | 4×M4 a PCD 60 |
| Boquilla ↔ Tapón A | deslizante | Ø32 / Ø32.6, 14 mm de encaje |
| Vaso ↔ canal | taladro | Ø50.6 |
| Rodamiento 608 ↔ cajera | a presión | Ø22.2 × 7.4, labio Ø19.5 |
| Bastidor ↔ abrazaderas | deslizante | Ø25 / Ø25.6 |

## Comprobaciones hechas

* Las 18 piezas exportan a STL como sólido cerrado y simple.
* Encaje álabe/disco: booleana vacía en disco superior e intermedio, con control
  negativo que sí deja material (la prueba no pasa por vacuidad).
* Engranaje del mástil contra el soporte: 1 mm³ de roce → **0** tras añadir el
  volumen de giro.
* Engrane de los dos cónicos: 16 mm³ → **2 mm³** tras recortar por cono de
  cabeza y cono trasero, residuo del orden del teselado.
* El juego entre flancos se verificó midiendo volumen del engranaje con
  `juego_g` = 0 / 0.3 / 1.0 mm: 11.809 / 11.757 / 11.635 cm³.

## Materiales y herrajes

* Varilla inoxidable Ø8: ~250 mm (tornillo) + ~350 mm (mástil de la turbina).
* 3 × rodamiento **608ZZ** (2 en la torreta, 1 en el soporte del mástil).
* Prisioneros M3×6 (8 uds), tornillería M4×20 con tuerca (10 uds).
* Tubo de Ø25 para el bastidor y tubo PVC Ø75 de evacuación para el canal.
* Manguito flexible Ø12 para el retorno del rebose al depósito.

## Laminado

* **PETG o ASA.** Nada de PLA: hidrólisis con el agua del nutriente y UV.
* **4 perímetros mínimo** y **relleno 100 %** en carcasa, embudo y tapones: así
  son estancos sin sellador.
* Resto: 4 perímetros, 40–60 % de relleno.
* Capa 0.2 mm; 0.15 mm mejora el filete de la hélice.
* Sin soportes salvo: manguito del Tapón A (voladizo horizontal) y `conector_3v`
  (dos bocas horizontales).

## Orden de montaje

1. Buje del embudo, embudo a presión sobre la carcasa, tornillo sobre la varilla.
2. Torreta atornillada a la corona (4×M4); rodamiento 608 vertical y engranaje
   cónico del tornillo, con el ápice a 42 mm de la base de la torreta.
3. Mástil: 608 en el brazo inclinado de la torreta + `sop_mastil` en el
   travesaño del bastidor. Comprobar engrane antes de apretar prisioneros.
4. Rotor Savonius: pegar el disco superior en su ranura y **enroscar** el disco
   intermedio hasta media altura.
5. Bastidor de Ø25 con `conector_3v`; fijar la carcasa con `abraz_carcasa` a 40°
   y el canal con `abraz_canal`.
6. Canal: taladrar Ø50.6 con la plantilla, Tapón A bajo la boquilla y Tapón B
   con el racor hacia el depósito.
