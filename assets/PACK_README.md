# DEADWEIGHT - pixel art asset pack

Palette: #fbf5ef  #f2d3ab  #c69fa5  #8b6d9c  #494d7e  #272744  (the player keeps a few extra colours)
Screen 320x180 | Tiles 8x8 | Character 16x16 | Import at 16 PPU, Filter Point, Compression None

Player/       Every animation in its own folder: *_player.png (body strip), *_sword_layer.png (sword layer strip),
              *_composite_sheet.png (both layers together), *_preview.gif, *.svg sources.
              Idle, 4 rare idles, walk, run, jump, 5 aimed slashes (forward, forward-up, up, forward-down, down).
              _archive holds older slash versions.
Background/   Gameplay parallax layers (sky, clouds, far/mid mountains, forest), composite image,
              test scene, background_parallax_preview.gif
Tileset/      tileset.png + tileset_layout.json, demo_level_preview.png, tileset_decor_preview.gif,
              Decor/ (grass, tall grass, flower sway strips + sheet)
Menu/         Background_Layers (9 layers), Buttons (play/options/quit/back, normal/hover/pressed, blank 9-slice),
              UI (title, panel, labels, ember, vignette) Music(main menu music), menu_preview.png, menu_preview.gif
Story/        5x7 pixel font atlas, Wren and Marrow portraits + frame, dialogue_preview.png/.gif

The GIF previews are 3x scale. Background and menu GIFs show motion in a loop; real speeds are set in Unity.
