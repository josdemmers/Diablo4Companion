El menú Configuración te permite cambiar entre 'presets de sistema' que coincide con tu resolución.
Sigue los pasos en esta página para crear los suyos.

<img src="https://github.com/josdemmers/Diablo4Companion/blob/master/readme/readme-004.png" height="300">

## Convención de nombres

Para mantener todos los preajustes del sistema organizados, el nombre de la carpeta debe contener la resolución y la configuración relevante en el juego.
Por ejemplo:
```
1080p_HSF : HDR Fuente pequeña
1440p_SMF : SDR Fuente Media
2160p_HLF : HDR Fuente Grande
```
Para los siguientes idiomas se puede usar la misma carpeta: 
* Portugues de Brasil
* Inglés
* Frances
* Alemán
* Italiano
* Polaco
* Ruso
* Español de Hispanoamerica
* Español de España
* Turco

Los idiomas restantes difieren demasiado y requieren su propia carpeta:
* Chino (Simplificado)
* Chino (Tradicional)
* Japones
* Coreano

Para esos idiomas, agrega el código de idioma a la carpeta preestablecida como esta:
```
1440p_SMF_zhCN
1440p_SMF_zhTW
1440p_SMF_jaJP
1440p_SMF_koKR
```

### Idiomas

| Language | code |
| --- | --- |
| English | enUS  |
| Brazilian Portuguese | ptBR |
| Chinese (Simplified) | zhCN |
| Chinese (Traditional) | zhTW |
| French | frFR  |
| German | deDE  |
| Italian | itIT  |
| Japanese | jaJP  |
| Korean | koKR  |
| Polish | plPL  |
| Russian | ruRU  |
| Spanish (EU) | esES |
| Spanish (LA) | esUS |
| Turkish | trTR  |

## Estructura de carpetas

Mira [1440p_SMF](https://github.com/josdemmers/Diablo4Companion/tree/master/downloads/systempresets/images/1440p_SMF) por ejemplo:

```
\---D4Companion:.
    \---Tooltips
        |    tooltip_gc_square.png (optional)
        |    tooltip_gc_x.png (optional)
        |    tooltip_kb_all.png
    |   dot-affixes_greater.png
    |   dot-affixes_normal.png
    |   dot-affixes_reroll.png
    |   dot-affixes_rune_invocation.png
    |   dot-affixes_rune_ritual.png
    |   dot-affixes_temper_defensive.png
    |   dot-affixes_temper_mobility.png
    |   dot-affixes_temper_offensive.png
    |   dot-affixes_temper_resource.png
    |   dot-affixes_temper_utility.png
    |   dot-affixes_temper_weapons.png
    |   dot-aspects_legendary.png
    |   dot-aspects_unique.png
    |   dot-socket_1.png
    |   dot-socket_1_mask.png
    |   dot-socket_invocation.png
    |   dot-socket_invocation_mask.png
    |   dot-socket_ritual.png
    |   dot-socket_ritual_mask.png
    |   dot-splitter_1.png
    |   dot-splitter_top_1.png
```
### Servicio de herramientas

Esta carpeta contiene imágenes para reconocer la información sobre herramientas del elemento.
Asegúrate de usar los nombres de archivo como se muestra en el ejemplo.
- Para Ratón/Teclado: `tooltip_kb_all.png`
- Para Mandos: `tooltip_gc_*`. e.j. `tooltip_gc_square.png` o `tooltip_gc_x.png`

La imagen "herramienta" predeterminada es `tooltip_kb_all.png`, cuando un idioma requiere una imagen diferente, puede agregar el código de idioma al nombre. La aplicación usará automáticamente la imagen correcta. Por ejemplo:
* tooltip_kb_all_esES.png
* tooltip_kb_all_esMX.png
* tooltip_kb_all_frFR.png
* tooltip_kb_all_itIT.png

## Capturar imágenes

Puedes capturar fácilmente las imágenes requeridas presionando la tecla `Shift + Windows + S`. Luego usa la herramienta de recorte para dibujar un rectángulo alrededor de la parte que desea capturar.

<img src="https://github.com/josdemmers/Diablo4Companion/blob/master/readme/wiki-systempreset-001.png" height="400">

1. Guardar la ubicación de Afijo como `dot-affixes_normal.png`.
2. Guardar la ubicación de afijo encantada como `dot-affixes_reroll.png`.
3. Guardar la ubicación del aspecto como `dot-aspects_legendary.png` and for unique items as `dot-aspects_unique.png`.
4. Guarde la ubicación del hueco como `dot-socket_1.png` and `dot-socket_1_mask.png`.
5. Guarde la ubicación del divisor superior como `dot-splitter_top_1.png`.
6. Guarde la otra ubicación de divisor como `dot-splitter_1.png`.
7. Guardar la imagen 'shift' como `tooltip_kb_all.png` En la carpeta 'ToolTips`.
8. Desde un tipo de elemento de runa, crea un `dot-affixes_rune_ritual.png`
![dot-affixes_rune_ritual](https://github.com/user-attachments/assets/11cc9197-c44a-4dd4-abc0-c3f304c48241) y `dot-affixes_rune_invocation.png`
![dot-affixes_rune_invocation](https://github.com/user-attachments/assets/681cccff-0675-4fef-a0f2-55804600b623) imagen.

### Imágenes de huecos

Para poder detectar huecos se requiere un par de imágenes. El primero de un zócalo vacío (`dot-socket_1.png`). El segundo es una máscara (`dot-socket_1_mask.png`). Se usa la máscara para que podamos ignorar las gemas engarzadas.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/5b91efb1-f685-4ff2-afd7-7c167c7cf17e" height="75">
<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/65389375-f31f-4fd5-92ff-0c20b98ba178" height="75">
<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/94b14389-fedb-4a40-bdbb-c97fbef1dffb" height="75">

1. Esta es la imagen de hueco vacía. Nombra esta `dot-socket_1.png`.
2. Este es un ejemplo sobre cómo crear la imagen de máscara. Abre `dot-socket_1.png`y editalo por ejemplo con [Gimp](https://www.gimp.org/). Selecciona un rectángulo como se muestra en el ejemplo para que solo las esquinas estén afuera. Ahora haz todo dentro del rectángulo blanco y negro. Guarda esta nueva imagen como `dot-socket_1_mask.png`.
3. Esta es la imagen `dot-socket_1_mask.png` resultante.

El segundo par de imágenes que necesitamos es detectar runas engarzadas.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/bd5d4ba0-e923-410f-af86-10a170aba26f" height="75">
<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/403f5da8-912c-4182-a8a8-a9a42373a750" height="75">
<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/f7360c2e-861a-4c1f-86e9-5d51ff019706" height="75">

1. Esta es la imagen de runa engarzada. Nombra este `dot-socket_ritual.png`.
2. Este es un ejemplo sobre cómo crear la imagen de máscara. Abra el `dot-socket_ritual.png` y edítelo por ejemplo: [Gimp](https://www.gimp.org/). Ahora haz todo dentro de la runa blanca y el negro exterior. Guarde esta nueva imagen como `dot-socket_ritual_mask.png`.
3. Este es el resultado `dot-socket_ritual_mask.png` imagen.
4. Repita el paso 1-3 para la runa de invocación y nombrarlos `dot-socket_invocation.png` y `dot-socket_invocation_mask.png`.

## Pruebas

Comienza por crear la imagen de información sobre herramientas `tooltip_kb_all.png` y guárdalo en tu carpeta `Tooltips`.
* Now start the app, go to `Settings`, and choose your `System preset`.
* Vete a 'Affijos` y activa ' Overlay de Afijo.
* Vete a 'Debug' y verifica si se detecta la información sobre herramientas. Debería ver un rectángulo rojo como en la imagen a continuación.

<img src="https://github.com/josdemmers/Diablo4Companion/blob/master/readme/wiki-systempreset-002.png" height="400">

Si el ancho no coincide con la información sobre herramientas, puede ajustarlo utilizando la configuración de 'WoolTip Width'.

* El siguiente paso es recopilar todas las otras imágenes requeridas. Puedes usar la pestaña de Debug para probar esas imágenes también.

## Compartir

Puedes compartir tu preajuste creado con otros y enviándome un archivo zip que contiene todas las imágenes.
* Cree un nuevo problema de GitHub y agregue su archivo zip como archivo adjunto.
* O publique el archivo zip en Discord.

Otra opción es crear una solicitud de extracción. Esto requiere los siguientes pasos:
1. Añade tus imágenes en: [tree/master/downloads/systempresets/images](https://github.com/josdemmers/Diablo4Companion/tree/master/downloads/systempresets/images)
2. Añade tu archivo zip aquí: [tree/master/downloads/systempresets](https://github.com/josdemmers/Diablo4Companion/tree/master/downloads/systempresets)
3. Actualiza [blob/master/downloads/systempresets/systempresets.json](https://github.com/josdemmers/Diablo4Companion/blob/master/downloads/systempresets/systempresets.json)

### systempresets.json

* FileName: Nombre del archivo zip.
* Resolution: Configuración en el juego.
* Config: Configuración en el juego.
* TooltipWidth: Configuración de aplicaciones. (Ver Debug)
* BrightnessThreshold: Configuración de aplicacion. (Ver Debug)
* Affix Area Height Offset (Top): Configuración de aplicación. (Ver Debug)
* Affix Area Height Offset (Bottom): Configuración de aplicación. (Ver Debug)
* Affix/Aspect Area Width Offset: Configuración de aplicación (Ver Debug)
* Aspect Area Height Offset (Top): Configuración de aplicación (Ver Debug)