The settings menu allows you to switch between `System presets` matching your resolution.
Follow the steps on this page to create your own.

<img src="https://github.com/josdemmers/Diablo4Companion/blob/master/readme/readme-004.png" height="300">

## Naming convention

To keep all system presets organized the folder name should contain the resolution and relevant in-game settings.
For example:
```
1080p_HSF : HDR Small Font
1440p_SMF : SDR Medium Font
2160p_HLF : HDR Large Font
```
For the following languages the same folder can be used: 
* Brazilian Portuguese
* English
* French
* German
* Italian
* Polish
* Russian
* Spanish (AL)
* Spanish (EU)
* Turkish

The remaining languages differ too much and require their own folder:
* Chinese (Simplified)
* Chinese (Traditional)
* Japanese
* Korean

For those languages add the language code to the preset folder like this:
```
1440p_SMF_zhCN
1440p_SMF_zhTW
1440p_SMF_jaJP
1440p_SMF_koKR
```

### Languages

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

## Folder structure

See [1440p_SMF](https://github.com/josdemmers/Diablo4Companion/tree/master/downloads/systempresets/images/1440p_SMF) for an example:

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
### Tooltips

This folder contains images to recognize the item tooltip.
Make sure to use the file names as shown in the example.
- For keyboard/mouse: `tooltip_kb_all.png`
- For game controllers: `tooltip_gc_*`. e.g. `tooltip_gc_square.png` or `tooltip_gc_x.png`

The default tooltip image is `tooltip_kb_all.png`, when a language requires a different image you can add the language code to the name. The app will then automatically use the correct image. For example:
* tooltip_kb_all_esES.png
* tooltip_kb_all_esMX.png
* tooltip_kb_all_frFR.png
* tooltip_kb_all_itIT.png

## Capture images

You can easily capture the required images by pressing `Shift + Windows Key + S`. Then use the snipping tool to draw a rectangle around the part you want to capture.

<img src="https://github.com/josdemmers/Diablo4Companion/blob/master/readme/wiki-systempreset-001.png" height="400">

1. Save the affix location as `dot-affixes_normal.png`.
2. Save the enchanted affix location as `dot-affixes_reroll.png`.
3. Save the aspect location as `dot-aspects_legendary.png` and for unique items as `dot-aspects_unique.png`.
4. Save the socket location as `dot-socket_1.png` and `dot-socket_1_mask.png`.
5. Save the top splitter location as `dot-splitter_top_1.png`.
6. Save the other splitter location as `dot-splitter_1.png`.
7. Save the `Shift` image as `tooltip_kb_all.png` in the `Tooltips` folder.
8. From a rune item type create a `dot-affixes_rune_ritual.png`
![dot-affixes_rune_ritual](https://github.com/user-attachments/assets/11cc9197-c44a-4dd4-abc0-c3f304c48241) and `dot-affixes_rune_invocation.png`
![dot-affixes_rune_invocation](https://github.com/user-attachments/assets/681cccff-0675-4fef-a0f2-55804600b623) image.

### Socket images

To be able to detect sockets a pair of images is required. The first one from an empty socket (`dot-socket_1.png`). The second one is a mask (`dot-socket_1_mask.png`). The mask is used so we can ignore socketed gems.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/5b91efb1-f685-4ff2-afd7-7c167c7cf17e" height="75">
<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/65389375-f31f-4fd5-92ff-0c20b98ba178" height="75">
<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/94b14389-fedb-4a40-bdbb-c97fbef1dffb" height="75">

1. This is the empty socket image. Name this one `dot-socket_1.png`.
2. This is an example on how to create the mask image. Open the `dot-socket_1.png`and edit it in for example [Gimp](https://www.gimp.org/). Select a rectangle as shown in the example so that only the corners are outside. Now make everything inside the rectangle white and the outside black. Save this new image as `dot-socket_1_mask.png`.
3. This is the resulting `dot-socket_1_mask.png` image.

The second pair of images we need is to detect socketed runes.

<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/bd5d4ba0-e923-410f-af86-10a170aba26f" height="75">
<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/403f5da8-912c-4182-a8a8-a9a42373a750" height="75">
<img src="https://github.com/josdemmers/Diablo4Companion/assets/8627157/f7360c2e-861a-4c1f-86e9-5d51ff019706" height="75">

1. This is the socketed rune image. Name this one `dot-socket_ritual.png`.
2. This is an example on how to create the mask image. Open the `dot-socket_ritual.png`and edit it in for example [Gimp](https://www.gimp.org/). Now make everything inside the rune white and the outside black. Save this new image as `dot-socket_ritual_mask.png`.
3. This is the resulting `dot-socket_ritual_mask.png` image.
4. Repeat step 1-3 for the Invocation rune and name those `dot-socket_invocation.png` and `dot-socket_invocation_mask.png`.

## Testing

Start by creating the tooltip image `tooltip_kb_all.png` and save it in your `Tooltips` folder.
* Now start the app, go to `Settings`, and choose your `System preset`.
* Go to `Affixes` and enable the `Affix Overlay`.
* Go to `Debug` and check if the tooltip is detected. You should see a red rectangle like in the image below.

<img src="https://github.com/josdemmers/Diablo4Companion/blob/master/readme/wiki-systempreset-002.png" height="400">

If the width does not match the tooltip you can adjust it using the `Tooltip width` setting.

* Next step is to collect all the other required images. You can use the debug tab to test those images as well.

## Sharing

You can share your created preset with others by sending me zip file containing all the images.
* Create a new GitHub issue and add your zip file as attachment.
* Or post the zip file on Discord.

Another option is to create a pull request. This requires the following steps:
1. Add your images in: [tree/master/downloads/systempresets/images](https://github.com/josdemmers/Diablo4Companion/tree/master/downloads/systempresets/images)
2. Add your zip file in: [tree/master/downloads/systempresets](https://github.com/josdemmers/Diablo4Companion/tree/master/downloads/systempresets)
3. Update [blob/master/downloads/systempresets/systempresets.json](https://github.com/josdemmers/Diablo4Companion/blob/master/downloads/systempresets/systempresets.json)

### systempresets.json

* FileName: Name of the zip file.
* Resolution: In-game setting.
* Config: In-game setting.
* TooltipWidth: App setting. (See debug)
* BrightnessThreshold: App setting. (See debug)
* Affix Area Height Offset (Top): App setting. (See debug)
* Affix Area Height Offset (Bottom): App setting. (See debug)
* Affix/Aspect Area Width Offset: App setting. (See debug)
* Aspect Area Height Offset (Top): App setting. (See debug)