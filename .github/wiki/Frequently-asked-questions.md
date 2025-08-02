# How to enable controller support

Go to settings and turn on `Controller Mode`.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/385a61ab-fb31-485b-83cf-86d02d352350" height="300">

Press the configuration icon add the icon(s) matching your controller.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/9bc013f7-d385-4f2c-a235-08eaa644901b" height="300">

# How to update from v2 to v3

The app will automatic update from v2 to v3. Afterwards it's recommended to delete you system preset folder.
For example `.\Diablo4Companion\Images\1440p_SMF_en`.

Now start the app, go to settings, and download the latest system preset matching your resolution. This will download the v3 compatible system preset.

If you had created your own system preset see the updated [wiki](https://github.com/josdemmers/Diablo4Companion/wiki/How-to-create-a-new-System-Preset) on how to create one for v3.

# How to update from v3 to v4

The app will automatic update from v3 to v4. Afterwards it's recommended to delete your Builds folder.
There are also some obsolete dlls left from v3 that you need to delete, see the list below:

![image](https://github.com/user-attachments/assets/23542b15-1d4e-46eb-9962-b9b1d0e3f59f)


# Micro stutters with High-End PC and g-sync

If you experience micro stutters on a high-end PC check if you have g-sync enabled.
The Diablo IV Companion App is not compatible with g-sync. So far the only workaround is to turn this feature off.

# Missing dll exception from Emgu.CV.CvInvoke

`System.TypeInitializationException: The type initializer for 'Emgu.CV.CvInvoke' threw an exception.
 ---> System.DllNotFoundException: Dll was not found.`

Emgu.CV has a dependency on `cvextern`, which requires multiple dlls from the Windows `Media Feature Pack`.
This is not installed by default for Windows version `Windows Pro N`.
The exception can be solved by installing this `Media Feature Pack`.

# Missing green/red dots even when overlay app icon is visible

### Configuration issue

This is often caused by a configuration issue, either ingame or your app settings. 
It's also possible the system preset is out-of-date. Press the update/download button to make sure you have the latest.

To solve possible configuration issues, make sure the system preset you selected matches your ingame settings.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/63be95e9-a95c-4b60-bda8-919f74ced8bd" height="200">

- Resolution
- Font size
- SDR or HDR
- Language

The Diablo4Companion settings for the tooltip width and the brightness threshold can be changed at the debug window.

### Brightness

Check if your ingame brightness settings match the defaults. Should be close to the example below. If not, preset the `Reset to Defaults` button.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/032e9499-7909-415e-ba32-362dbf053196" height="200">

### Item Power

If everything else seems OK, and the debug images correctly show all affixes then check if you have enabled the Item Power limit. This feature ignores all items below a certain item power.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/bf010a39-5a35-45dc-a9a6-0e918c93faae" height="300">

# Missing overlay app icon in the top left corner

When the overlay is turned on and Diablo IV is running but you still don't see the overlay icon in the top left corner it is probably blocked by security software. The following software is known to cause problems:
- Riot Vanguard

# What do the different icon shapes mean?

Circle
* Normal icon, no special meaning.

Square
* Current affix value is below the minimal filter value.
* Correct affix but with no specific item type. (When affix is set to `any type`)

Triangle
* Affixes that you want to have as a greater affix.
