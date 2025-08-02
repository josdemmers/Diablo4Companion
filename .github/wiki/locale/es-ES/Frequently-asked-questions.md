# Cómo activar la compatibilidad con Mando

Ve a ajustes y activa el `Modo Mando`.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/385a61ab-fb31-485b-83cf-86d02d352350" height="300">

Pulse el icono de configuración y añada el icono o iconos correspondientes a su Mando.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/9bc013f7-d385-4f2c-a235-08eaa644901b" height="300">

# Cómo actualizar de v2 a v3

La aplicación se actualizará automáticamente de v2 a v3. Después se recomienda borrar la carpeta de preajustes del sistema.
Por ejemplo: `.\Diablo4Companion\Images\1440p_SMF_en`.

Ahora inicia la aplicación, ve a ajustes y descarga el último preajuste del sistema que coincida con tu resolución. Esto descargará el preajuste de sistema compatible con v3.

Si ha creado su propio preajuste del sistema, consulta la actualización [wiki](https://github.com/josdemmers/Diablo4Companion/wiki/How-to-create-a-new-System-Preset) sobre cómo crear uno para v3.

# Cómo actualizar de v3 a v4

La aplicación se actualizará automáticamente de v3 a v4. Después se recomienda borrar la carpeta Builds.
También hay algunas dlls obsoletas de la v3 que necesitas borrar, mira la lista de abajo:

![image](https://github.com/user-attachments/assets/23542b15-1d4e-46eb-9962-b9b1d0e3f59f)


# Micro tirones con PC de gama alta y g-sync

Si experimentas tirones en un PC de gama alta, comprueba si tienes g-sync activado.
La aplicación Diablo IV Companion no es compatible con g-sync. Hasta ahora, la única solución es desactivar esta función.

# Missing dll exception from Emgu.CV.CvInvoke

`System.TypeInitializationException: The type initializer for 'Emgu.CV.CvInvoke' threw an exception.
 ---> System.DllNotFoundException: Dll was not found.`

Emgu.CV es una dependencia de `cvextern`, que requiere múltiples dlls de Windows `Media Feature Pack`.
Este no está instalado por defecto para la versión de Windows `Windows Pro N`.
La excepción puede solucionarse instalando este `Media Feature Pack`.

# Faltan puntos verdes/rojos incluso cuando el icono Overlay de la aplicación está visible.

### Problemas de configuración

Esto suele deberse a un problema de configuración, ya sea en el juego o en los ajustes de la aplicación. 
También es posible que el preajuste del sistema no esté actualizado. Pulsa el botón de actualización/descarga para asegurarte de que tienes la última versión.

Para resolver posibles problemas de configuración, asegúrate de que el preajuste del sistema que has seleccionado coincide con tu configuración dentro del juego.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/63be95e9-a95c-4b60-bda8-919f74ced8bd" height="200">

- Resolución
- Tamaño de la fuente
- SDR o HDR
- Idioma

Los ajustes de Diablo4Companion para la anchura de la información sobre herramientas y el umbral de brillo se pueden cambiar en la sección de Debug.

### Brillo

Comprueba si tus ajustes de brillo dentro del juego coinciden con los predeterminados. Debería ser parecido al ejemplo de abajo. Si no es así, pulsa el botón `Reiniciar por defecto`.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/032e9499-7909-415e-ba32-362dbf053196" height="200">

### Poder de Objeto

Si todo lo demás parece estar bien, y las imágenes de depuración muestran correctamente todos los afijos, entonces comprueba si has activado el límite de potencia de objeto. Esta característica ignora todos los objetos por debajo de un cierto poder de objeto.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/bf010a39-5a35-45dc-a9a6-0e918c93faae" height="300">

# Falta el icono de la aplicación(Overlay) en la esquina superior izquierda

Cuando el Overlay está activado y Diablo IV se está ejecutando pero aún no ves el icono del Overlay en la esquina superior izquierda, es probable que esté bloqueado por software de seguridad. El siguiente software es conocido por causar problemas:
- Riot Vanguard

# ¿Qué significan las diferentes formas de los iconos?

Circulo
* Icono normal, sin significado especial.

Cuadrado
* El valor actual del afijo está por debajo del valor mínimo del filtro.
* Afijo correcto pero sin tipo de elemento específico. (Cuando el afijo está configurado como «cualquier tipo»)

Triangulo
* Afijos que desea tener como afijo mayor.
