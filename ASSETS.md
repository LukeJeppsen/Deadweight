# DEADWEIGHT — Asset List

Every asset that makes up the game. **Part 1** covers the files in this repo's [`assets/`](assets/) folder (art, audio and level files you can open directly). **Part 2** covers the Unity-only assets (tiles, animation clips, scenes, code) in the [`Unity/`](Unity/) project folder.

The **Prompt** column points to the numbered prompts in the [Prompt & Generation Log](docs/DEADWEIGHT_Prompt_Generation_Log.pdf). Assets marked *later sessions* were made after that log (Oct 12 – Nov 22, 2026).

Palette: `#272744` `#494d7e` `#8b6d9c` `#c69fa5` `#f2d3ab` `#fbf5ef` · Screen 320×180 · Tiles 8×8 · Character 16×16

## Totals

- **158 PNG images**, 32 SVG sources and 20 preview GIFs
- **1 music track** and **28 sound effects**
- **6 level files**, 6 area palettes

---

# Part 1: files in `assets/`

## Player

Drawn as two layers so the huge sword can swing outside the 16×16 body. Each animation folder holds the body strip, the sword-layer strip, both layers combined, a preview GIF, and SVG sources. All strips are 8 frames. The character always faces right; Unity flips him.

| Folder | Animation | Prompt | Contents | Preview |
|---|---|---|---|---|
| [`idle/`](assets/Player/idle/) | Base idle | #15 | 6 files, 91.1 KB | [preview](assets/Player/idle/idle_preview.gif) |
| [`idle_rare_flourish/`](assets/Player/idle_rare_flourish/) | Rare idle: sword flourish | #15 | 6 files, 117.5 KB | [preview](assets/Player/idle_rare_flourish/idle_rare_flourish_preview.gif) |
| [`idle_rare_shake/`](assets/Player/idle_rare_shake/) | Rare idle: shake | #15 | 6 files, 92.1 KB | [preview](assets/Player/idle_rare_shake/idle_rare_shake_preview.gif) |
| [`idle_rare_shoulder_tap/`](assets/Player/idle_rare_shoulder_tap/) | Rare idle: shoulder tap | #15 | 6 files, 107.0 KB | [preview](assets/Player/idle_rare_shoulder_tap/idle_rare_shoulder_tap_preview.gif) |
| [`idle_rare_stretch/`](assets/Player/idle_rare_stretch/) | Rare idle: stretch | #15 | 6 files, 119.1 KB | [preview](assets/Player/idle_rare_stretch/idle_rare_stretch_preview.gif) |
| [`walk/`](assets/Player/walk/) | Walk | #3, #5 | 6 files, 91.6 KB | [preview](assets/Player/walk/walk_preview.gif) |
| [`run/`](assets/Player/run/) | Run | #3, #5 | 6 files, 104.3 KB | [preview](assets/Player/run/run_preview.gif) |
| [`jump/`](assets/Player/jump/) | Jump | #3, #5 | 6 files, 107.8 KB | [preview](assets/Player/jump/jump_preview.gif) |
| [`slash_forward/`](assets/Player/slash_forward/) | Slash forward | #7–#10 | 6 files, 115.8 KB | [preview](assets/Player/slash_forward/slash_forward_preview.gif) |
| [`slash_forward_up/`](assets/Player/slash_forward_up/) | Slash forward-up | #9, #10 | 6 files, 126.4 KB | [preview](assets/Player/slash_forward_up/slash_forward_up_preview.gif) |
| [`slash_up/`](assets/Player/slash_up/) | Slash up | #9, #10 | 6 files, 130.8 KB | [preview](assets/Player/slash_up/slash_up_preview.gif) |
| [`slash_forward_down/`](assets/Player/slash_forward_down/) | Slash forward-down | #9, #10 | 6 files, 122.1 KB | [preview](assets/Player/slash_forward_down/slash_forward_down_preview.gif) |
| [`slash_down/`](assets/Player/slash_down/) | Slash down | #9, #10 | 6 files, 134.6 KB | [preview](assets/Player/slash_down/slash_down_preview.gif) |

| File | Size | What it is | Prompt |
|---|---|---|---|
| [`player_idle.png`](assets/Player/player_idle.png) | 350 B | First character sprite (single 16×16 frame) | #1, #2 |
| [`player_idle.svg`](assets/Player/player_idle.svg) | 4.8 KB | SVG source of the first character | #1, #2 |
| [`sword.png`](assets/Player/sword.png) | 262 B | First sword sprite | #1, #2 |
| [`sword.svg`](assets/Player/sword.svg) | 9.6 KB | SVG source of the first sword | #1, #2 |
| [`_archive/slash_old_backswing/`](assets/Player/_archive/slash_old_backswing/) | 6 files | First slash, swung behind the player (replaced) | #7, #8 |
| [`_archive/slash_forward_8f_long/`](assets/Player/_archive/slash_forward_8f_long/) | 6 files | Longer-windup forward slash (replaced by the snappier one) | #7, #8 |

## Gameplay background (first area)

| File | Size | What it is | Prompt |
|---|---|---|---|
| [`background_composite.png`](assets/Background/background_composite.png) | 4.7 KB | All five layers combined | #11, #39 |
| [`background_parallax_preview.gif`](assets/Background/background_parallax_preview.gif) | 1017.4 KB | Animated parallax pan | #11, #39 |
| [`clouds.png`](assets/Background/clouds.png) | 855 B | Clouds | #11, #39 |
| [`forest.png`](assets/Background/forest.png) | 1.2 KB | Forest | #11, #39 |
| [`mountains_far.png`](assets/Background/mountains_far.png) | 1.6 KB | Far mountains | #11, #39 |
| [`mountains_mid.png`](assets/Background/mountains_mid.png) | 1.5 KB | Mid mountains | #11, #39 |
| [`sky.png`](assets/Background/sky.png) | 2.4 KB | Sky | #11, #39 |
| [`test_scene_with_player_and_tiles.png`](assets/Background/test_scene_with_player_and_tiles.png) | 5.7 KB | Test scene: player + tiles on the background | #11, #39 |

## Tileset

| File | Size | What it is | Prompt |
|---|---|---|---|
| [`demo_level_preview.png`](assets/Tileset/demo_level_preview.png) | 9.3 KB | Demo level built from the tileset | #12 |
| [`tileset.png`](assets/Tileset/tileset.png) | 2.5 KB | Base 8×8 tileset (89 tiles: terrain, back walls, hanging pieces, decor) | #12 |
| [`tileset_decor_preview.gif`](assets/Tileset/tileset_decor_preview.gif) | 5.8 KB | Decor sway preview | #12 |
| [`tileset_layout.json`](assets/Tileset/tileset_layout.json) | 12.5 KB | Where each tile sits on the sheet | #12 |
| [`decor_flower_sway.png`](assets/Tileset/Decor/decor_flower_sway.png) | 122 B | 4-frame sway strip: flowe | #13, #14 |
| [`decor_grass_a_sway.png`](assets/Tileset/Decor/decor_grass_a_sway.png) | 179 B | 4-frame sway strip: grass  | #13, #14 |
| [`decor_grass_b_sway.png`](assets/Tileset/Decor/decor_grass_b_sway.png) | 205 B | 4-frame sway strip: grass  | #13, #14 |
| [`decor_grass_c_sway.png`](assets/Tileset/Decor/decor_grass_c_sway.png) | 185 B | 4-frame sway strip: grass  | #13, #14 |
| [`decor_sway_preview.gif`](assets/Tileset/Decor/decor_sway_preview.gif) | 5.8 KB | Sway animation preview | #13, #14 |
| [`decor_sway_sheet.png`](assets/Tileset/Decor/decor_sway_sheet.png) | 463 B | All sway strips on one sheet | #13, #14 |
| [`decor_tallgrass_sway.png`](assets/Tileset/Decor/decor_tallgrass_sway.png) | 215 B | 4-frame sway strip: tallgras | #13, #14 |

## Main menu

Dithered with a 4×4 Bayer pattern using only the six palette colours.

| File | Size | What it is | Prompt |
|---|---|---|---|
| [`menu_preview.gif`](assets/Menu/menu_preview.gif) | 2.2 MB | Animated main menu loop | #27 |
| [`menu_preview.png`](assets/Menu/menu_preview.png) | 7.9 KB | Full main menu (still) | #27 |
| [`bg_clouds.png`](assets/Menu/Background_Layers/bg_clouds.png) | 819 B | Drifting clouds | #27 |
| [`bg_foreground.png`](assets/Menu/Background_Layers/bg_foreground.png) | 2.0 KB | Foreground cliff with the giant sword planted in it | #27 |
| [`bg_mist.png`](assets/Menu/Background_Layers/bg_mist.png) | 611 B | Drifting mist | #27 |
| [`bg_ridge_far.png`](assets/Menu/Background_Layers/bg_ridge_far.png) | 1.2 KB | Far ridge | #27 |
| [`bg_ridge_mid.png`](assets/Menu/Background_Layers/bg_ridge_mid.png) | 1.3 KB | Mid ridge | #27 |
| [`bg_sky.png`](assets/Menu/Background_Layers/bg_sky.png) | 2.8 KB | Sky with distant sun | #27 |
| [`bg_spires.png`](assets/Menu/Background_Layers/bg_spires.png) | 650 B | Rock spires | #27 |
| [`bg_stars_a.png`](assets/Menu/Background_Layers/bg_stars_a.png) | 496 B | Stars (twinkle set A) | #27 |
| [`bg_stars_b.png`](assets/Menu/Background_Layers/bg_stars_b.png) | 439 B | Stars (twinkle set B) | #27 |
| [`btn_back_hover.png`](assets/Menu/Buttons/btn_back_hover.png) | 396 B | BACK button, hover | #27 |
| [`btn_back_normal.png`](assets/Menu/Buttons/btn_back_normal.png) | 372 B | BACK button, normal | #27 |
| [`btn_back_pressed.png`](assets/Menu/Buttons/btn_back_pressed.png) | 265 B | BACK button, pressed | #27 |
| [`btn_blank_hover.png`](assets/Menu/Buttons/btn_blank_hover.png) | 151 B | BLANK button, hover (9-slice, no text) | #27 |
| [`btn_blank_normal.png`](assets/Menu/Buttons/btn_blank_normal.png) | 163 B | BLANK button, normal (9-slice, no text) | #27 |
| [`btn_blank_pressed.png`](assets/Menu/Buttons/btn_blank_pressed.png) | 135 B | BLANK button, pressed (9-slice, no text) | #27 |
| [`btn_options_hover.png`](assets/Menu/Buttons/btn_options_hover.png) | 447 B | OPTIONS button, hover | #27 |
| [`btn_options_normal.png`](assets/Menu/Buttons/btn_options_normal.png) | 431 B | OPTIONS button, normal | #27 |
| [`btn_options_pressed.png`](assets/Menu/Buttons/btn_options_pressed.png) | 284 B | OPTIONS button, pressed | #27 |
| [`btn_play_hover.png`](assets/Menu/Buttons/btn_play_hover.png) | 460 B | PLAY button, hover | #27 |
| [`btn_play_normal.png`](assets/Menu/Buttons/btn_play_normal.png) | 440 B | PLAY button, normal | #27 |
| [`btn_play_pressed.png`](assets/Menu/Buttons/btn_play_pressed.png) | 304 B | PLAY button, pressed | #27 |
| [`btn_quit_hover.png`](assets/Menu/Buttons/btn_quit_hover.png) | 464 B | QUIT button, hover | #27 |
| [`btn_quit_normal.png`](assets/Menu/Buttons/btn_quit_normal.png) | 446 B | QUIT button, normal | #27 |
| [`btn_quit_pressed.png`](assets/Menu/Buttons/btn_quit_pressed.png) | 299 B | QUIT button, pressed | #27 |
| [`ember.png`](assets/Menu/UI/ember.png) | 78 B | Rising ember particle | #27 |
| [`label_coming_soon.png`](assets/Menu/UI/label_coming_soon.png) | 439 B | COMING SOON label | #27 |
| [`label_options.png`](assets/Menu/UI/label_options.png) | 247 B | OPTIONS label | #27 |
| [`panel.png`](assets/Menu/UI/panel.png) | 244 B | 9-slice menu panel | #27 |
| [`title.png`](assets/Menu/UI/title.png) | 1.9 KB | DEADWEIGHT / SWING TO RISE logo (the I is a sword) | #27 |
| [`vignette.png`](assets/Menu/UI/vignette.png) | 2.2 KB | Screen-edge vignette | #27 |

## Music

| File | Size | What it is | Prompt |
|---|---|---|---|
| [`Beneath_the_Dead_Sun.mp3`](assets/Menu/Music/Beneath_the_Dead_Sun.mp3) | 4.2 MB | Main menu theme. Slow, no lyrics. Generated in **Gemini** from a prompt with the palette, menu screenshot and story (full prompt in the log). | #36 |

## Sound effects

*Later sessions.* All 28 are **synthesised in code** at startup (square and noise waves in `Sfx.cs`), so the game ships with no SFX files. These `.wav` files are exports from *Tools > Deadweight > Export Generated Sounds*.

| File | Size | What it is |
|---|---|---|
| [`beam_fire.wav`](assets/Audio/SFX/beam_fire.wav) | 19.4 KB | Sunbeam firing |
| [`beam_warn.wav`](assets/Audio/SFX/beam_warn.wav) | 15.1 KB | Sunbeam warning |
| [`checkpoint.wav`](assets/Audio/SFX/checkpoint.wav) | 21.1 KB | Checkpoint lit |
| [`complete.wav`](assets/Audio/SFX/complete.wav) | 45.7 KB | Level complete |
| [`crumble_crack.wav`](assets/Audio/SFX/crumble_crack.wav) | 7.4 KB | Crumbling platform cracking |
| [`crumble_fall.wav`](assets/Audio/SFX/crumble_fall.wav) | 16.4 KB | Crumbling platform falling |
| [`death.wav`](assets/Audio/SFX/death.wav) | 21.6 KB | Death |
| [`hint.wav`](assets/Audio/SFX/hint.wav) | 7.8 KB | Hint appears |
| [`jet_fire.wav`](assets/Audio/SFX/jet_fire.wav) | 21.6 KB | Fire jet firing |
| [`jet_warn.wav`](assets/Audio/SFX/jet_warn.wav) | 15.1 KB | Fire jet warning |
| [`jump.wav`](assets/Audio/SFX/jump.wav) | 5.6 KB | Jump |
| [`land.wav`](assets/Audio/SFX/land.wav) | 3.9 KB | Landing |
| [`launch.wav`](assets/Audio/SFX/launch.wav) | 10.4 KB | The sword throwing you (swing launch) |
| [`menu_back.wav`](assets/Audio/SFX/menu_back.wav) | 6.1 KB | Menu back |
| [`menu_confirm.wav`](assets/Audio/SFX/menu_confirm.wav) | 6.1 KB | Menu confirm |
| [`menu_error.wav`](assets/Audio/SFX/menu_error.wav) | 6.9 KB | Not allowed (e.g. locked level) |
| [`menu_move.wav`](assets/Audio/SFX/menu_move.wav) | 1.8 KB | Menu cursor move |
| [`pause.wav`](assets/Audio/SFX/pause.wav) | 6.9 KB | Pause |
| [`pulse.wav`](assets/Audio/SFX/pulse.wav) | 21.6 KB | Shard Pulse |
| [`relic.wav`](assets/Audio/SFX/relic.wav) | 30.2 KB | Relic pickup |
| [`respawn.wav`](assets/Audio/SFX/respawn.wav) | 10.8 KB | Respawn |
| [`ricochet.wav`](assets/Audio/SFX/ricochet.wav) | 13.8 KB | Bouncing off a surface (Ricochet) |
| [`shatter.wav`](assets/Audio/SFX/shatter.wav) | 19.4 KB | Cracked stone breaking (Breaker) |
| [`skill.wav`](assets/Audio/SFX/skill.wav) | 43.1 KB | New skill unlocked |
| [`swing.wav`](assets/Audio/SFX/swing.wav) | 6.5 KB | Sword swing |
| [`text.wav`](assets/Audio/SFX/text.wav) | 1.1 KB | Dialogue typing blip |
| [`walljump.wav`](assets/Audio/SFX/walljump.wav) | 5.2 KB | Leaping off a wall (Wall Grip) |
| [`wallslide.wav`](assets/Audio/SFX/wallslide.wav) | 21.6 KB | Sliding down a wall (loops) |

## Story and dialogue

| File | Size | What it is | Prompt |
|---|---|---|---|
| [`dialogue_preview.gif`](assets/Story/dialogue_preview.gif) | 1.1 MB | Dialogue scene with typewriter text (animated) | #31, #39 |
| [`dialogue_preview.png`](assets/Story/dialogue_preview.png) | 3.8 KB | Dialogue scene with typewriter text | #31, #39 |
| [`font_5x7.png`](assets/Story/font_5x7.png) | 674 B | Custom 5×7 pixel font atlas. All in-game text uses it. | #31, #39 |
| [`portrait_frame.png`](assets/Story/portrait_frame.png) | 196 B | Portrait frame | #31, #39 |
| [`portrait_marrow.png`](assets/Story/portrait_marrow.png) | 515 B | Marrow (hood, glowing eyes), 32×32 | #31, #39 |
| [`portrait_wren.png`](assets/Story/portrait_wren.png) | 569 B | Wren (headband and scarf), 32×32 | #31, #39 |

## The six areas

*Later sessions.* Each area has its own six-colour palette, a recoloured tileset, a props sheet (altar, checkpoints, hazards, decor) and five parallax layers.

| Folder | Area | Skill earned | Files (7 each) |
|---|---|---|---|
| [`cinder_foot/`](assets/Areas/cinder_foot/) | The Cinder Foot | Wall Grip | [atmosphere layer](assets/Areas/cinder_foot/bg_aux.png), [far layer](assets/Areas/cinder_foot/bg_far.png), [mid layer](assets/Areas/cinder_foot/bg_mid.png), [near layer](assets/Areas/cinder_foot/bg_near.png), [sky](assets/Areas/cinder_foot/bg_sky.png), [props sheet](assets/Areas/cinder_foot/props.png), [recoloured tileset](assets/Areas/cinder_foot/tileset.png) |
| [`drowned_cistern/`](assets/Areas/drowned_cistern/) | The Drowned Cistern | Breaker | [atmosphere layer](assets/Areas/drowned_cistern/bg_aux.png), [far layer](assets/Areas/drowned_cistern/bg_far.png), [mid layer](assets/Areas/drowned_cistern/bg_mid.png), [near layer](assets/Areas/drowned_cistern/bg_near.png), [sky](assets/Areas/drowned_cistern/bg_sky.png), [props sheet](assets/Areas/drowned_cistern/props.png), [recoloured tileset](assets/Areas/drowned_cistern/tileset.png) |
| [`hollow_garden/`](assets/Areas/hollow_garden/) | The Hollow Garden | Twin Edge | [atmosphere layer](assets/Areas/hollow_garden/bg_aux.png), [far layer](assets/Areas/hollow_garden/bg_far.png), [mid layer](assets/Areas/hollow_garden/bg_mid.png), [near layer](assets/Areas/hollow_garden/bg_near.png), [sky](assets/Areas/hollow_garden/bg_sky.png), [props sheet](assets/Areas/hollow_garden/props.png), [recoloured tileset](assets/Areas/hollow_garden/tileset.png) |
| [`pale_ledges/`](assets/Areas/pale_ledges/) | The Pale Ledges | Shard Pulse | [atmosphere layer](assets/Areas/pale_ledges/bg_aux.png), [far layer](assets/Areas/pale_ledges/bg_far.png), [mid layer](assets/Areas/pale_ledges/bg_mid.png), [near layer](assets/Areas/pale_ledges/bg_near.png), [sky](assets/Areas/pale_ledges/bg_sky.png), [props sheet](assets/Areas/pale_ledges/props.png), [recoloured tileset](assets/Areas/pale_ledges/tileset.png) |
| [`rust_foundry/`](assets/Areas/rust_foundry/) | The Rust Foundry | Ricochet | [atmosphere layer](assets/Areas/rust_foundry/bg_aux.png), [far layer](assets/Areas/rust_foundry/bg_far.png), [mid layer](assets/Areas/rust_foundry/bg_mid.png), [near layer](assets/Areas/rust_foundry/bg_near.png), [sky](assets/Areas/rust_foundry/bg_sky.png), [props sheet](assets/Areas/rust_foundry/props.png), [recoloured tileset](assets/Areas/rust_foundry/tileset.png) |
| [`zenith/`](assets/Areas/zenith/) | The Zenith | — (finale) | [atmosphere layer](assets/Areas/zenith/bg_aux.png), [far layer](assets/Areas/zenith/bg_far.png), [mid layer](assets/Areas/zenith/bg_mid.png), [near layer](assets/Areas/zenith/bg_near.png), [sky](assets/Areas/zenith/bg_sky.png), [props sheet](assets/Areas/zenith/props.png), [recoloured tileset](assets/Areas/zenith/tileset.png) |

| File | Size | What it is |
|---|---|---|
| [`props_layout.json`](assets/Areas/props_layout.json) | 7.6 KB | Where each prop sits on the props sheets |

## Level select and HUD

*Later sessions.*

| File | Size | What it is |
|---|---|---|
| [`cursor_0.png`](assets/LevelSelect/cursor_0.png) | 191 B | Cursor frame 1 |
| [`cursor_1.png`](assets/LevelSelect/cursor_1.png) | 234 B | Cursor frame 2 |
| [`glyph_check.png`](assets/LevelSelect/glyph_check.png) | 137 B | Check glyph |
| [`glyph_down.png`](assets/LevelSelect/glyph_down.png) | 132 B | Down arrow |
| [`glyph_lock.png`](assets/LevelSelect/glyph_lock.png) | 160 B | Lock glyph |
| [`glyph_up.png`](assets/LevelSelect/glyph_up.png) | 137 B | Up arrow |
| [`icon_blade.png`](assets/LevelSelect/icon_blade.png) | 243 B | HUD icon: blade |
| [`icon_breaker.png`](assets/LevelSelect/icon_breaker.png) | 262 B | Skill icon: Breaker |
| [`icon_pulse.png`](assets/LevelSelect/icon_pulse.png) | 283 B | Skill icon: Shard Pulse |
| [`icon_ricochet.png`](assets/LevelSelect/icon_ricochet.png) | 280 B | Skill icon: Ricochet |
| [`icon_swing.png`](assets/LevelSelect/icon_swing.png) | 234 B | HUD icon: swing |
| [`icon_twinedge.png`](assets/LevelSelect/icon_twinedge.png) | 250 B | Skill icon: Twin Edge |
| [`icon_wallgrip.png`](assets/LevelSelect/icon_wallgrip.png) | 247 B | Skill icon: Wall Grip |
| [`levelselect_bg.png`](assets/LevelSelect/levelselect_bg.png) | 6.6 KB | The Spire from outside, one band per area |
| [`levelselect_nodes.json`](assets/LevelSelect/levelselect_nodes.json) | 326 B | Node positions on the Spire |
| [`node_done.png`](assets/LevelSelect/node_done.png) | 265 B | Node: completed |
| [`node_locked.png`](assets/LevelSelect/node_locked.png) | 279 B | Node: locked |
| [`node_open.png`](assets/LevelSelect/node_open.png) | 247 B | Node: open |

## Game icon

| File | Size | What it is |
|---|---|---|
| [`deadweight_icon.png`](assets/Icon/deadweight_icon.png) | 2.6 KB | 256×256 icon (itch.io page, browser tab) |

## Level files

*Later sessions.* Each level is a text grid (one character = one 8×8 tile) plus placed objects (hints, wind, fire jets, sunbeams). The Unity level builder turns each into a scene. See the README for the glyph legend.

| File | Size | Area | Tiles (w×h) | Objects |
|---|---|---|---|---|
| [`cinder_foot.json`](assets/Levels/cinder_foot.json) | 491.8 KB | The Cinder Foot | 2200×228 | 2 hint |
| [`drowned_cistern.json`](assets/Levels/drowned_cistern.json) | 700.2 KB | The Drowned Cistern | 2632×270 | 2 hint, 24 jet |
| [`hollow_garden.json`](assets/Levels/hollow_garden.json) | 652.4 KB | The Hollow Garden | 2675×249 | 2 hint, 1 wind |
| [`pale_ledges.json`](assets/Levels/pale_ledges.json) | 564.0 KB | The Pale Ledges | 2628×219 | 2 hint, 3 wind |
| [`rust_foundry.json`](assets/Levels/rust_foundry.json) | 778.8 KB | The Rust Foundry | 3208×246 | 2 hint, 40 jet, 1 wind |
| [`zenith.json`](assets/Levels/zenith.json) | 945.5 KB | The Zenith | 3561×270 | 1 hint, 29 beam, 2 wind |

---

# Part 2: Unity-only assets

These live in the Unity project in [`Unity/`](Unity/) (Unity 6, 6000.3.10f1); paths below are relative to `Unity/Assets/`. Most are generated or wired up by the editor tools. The six level scenes are not in the repo (they're over GitHub's 100 MB limit); *Tools > Deadweight > Rebuild Level Scenes Only* recreates them from the level files.

## Tiles and animation

| Path | Count | What it is |
|---|---|---|
| `Art/Tileset/Tiles/*.asset` | 48 | Base tile assets, including a Rule Tile that auto-picks edges, and animated decor tiles |
| `Tiles/<area>/*.asset` | 33 × 6 | Per-area copies of the tiles, pointing at that area's recoloured tileset |
| `Sprites/Animations/Player/*.anim` | 32 | Body and sword clips (16 states × 2 layers) |
| `Sprites/Animations/*.controller` | 3 | Body, sword and sword-particle animator controllers |
| `Prefabs/Player.prefab` | 1 | The player, shared by every level |
| `Art/Background/ParallaxBackground.prefab`, `Art/Tileset/Level Tiles.prefab` | 2 | Parallax background and tile palette |

## Scenes

| File | Size | What it is |
|---|---|---|
| `Scenes/Ending.unity` | 7.0 KB | Ending and credits |
| `Scenes/LevelSelect.unity` | 8.4 KB | Level select |
| `Scenes/Levels/Level_cinder_foot.unity` | 75.6 MB | Level scene, generated from its level file |
| `Scenes/Levels/Level_drowned_cistern.unity` | 94.4 MB | Level scene, generated from its level file |
| `Scenes/Levels/Level_hollow_garden.unity` | 101.6 MB | Level scene, generated from its level file |
| `Scenes/Levels/Level_pale_ledges.unity` | 102.1 MB | Level scene, generated from its level file |
| `Scenes/Levels/Level_rust_foundry.unity` | 120.0 MB | Level scene, generated from its level file |
| `Scenes/Levels/Level_zenith.unity` | 136.2 MB | Level scene, generated from its level file |
| `Scenes/MainMenu.unity` | 66.9 KB | Title screen |

## Early prototype art (Aseprite, hand-drawn)

From the first prototype (May–July 2026), before the generated art.

| File | Size | What it is |
|---|---|---|
| `Sprites/LabBackground.ase` | 20.4 KB | Sandbox background |
| `Sprites/LabTileset.ase` | 4.4 KB | Sandbox tileset |
| `Sprites/basicTilest 1.ase` | 5.7 KB | First test tileset (copy) |
| `Sprites/basicTilest.ase` | 5.7 KB | First test tileset |
| `Sprites/mainCharacterNew.ase` | 3.1 KB | First hand-drawn character |
| `Sprites/mainCharacterSword.ase` | 5.2 KB | First hand-drawn sword |
| `Sprites/Tiles/TempTile.ase` | 842 B | Placeholder tile |

## Game scripts

| File | Size | What it is |
|---|---|---|
| `Bob.cs` | 636 B | Gentle bob for floating objects |
| `BreakableTilemap.cs` | 2.7 KB | Cracked stone. Any contiguous patch of tiles on this tilemap shatters at once when the Breaker skill hits it (swing at it, or launch into it).  |
| `CameraTarget.cs` | 2.8 KB | What the Cinemachine camera actually follows. It trails the player with a little look-ahead (the way you are moving), leads downward when you fall fast, stays inside the… |
| `Checkpoint.cs` | 1.7 KB | Checkpoint flag: lights up and saves your respawn point |
| `ControlsCard.cs` | 3.2 KB | The CONTROLS card shown from the title screen's Options and from the pause menu. Keep this table in step with Assets/InputSystem_Actions.inputactions. The pixel font has… |
| `CrumblePlatform.cs` | 2.5 KB | Platform that cracks and falls after you land on it |
| `DialogueBox.cs` | 4.8 KB | Dialogue box with portraits and typewriter text |
| `EndingSequence.cs` | 6.4 KB | The ending: the Deadweight comes free and the sun rises. Built from code; the scene only needs this component with the font atlas assigned. Space speeds lines up, Esc… |
| `FadeAndDie.cs` | 743 B | Fades out and removes an effect |
| `GameBootstrap.cs` | 800 B | Runs once at startup (no scene setup needed): turns on vsync so motion doesn't tear or judder, and hides the mouse cursor everywhere except the main menu.  |
| `GameProgress.cs` | 5.3 KB | Saved progress: completed levels, skills, deaths |
| `GameSettings.cs` | 1.5 KB | Player options that aren't volume (volume lives in Sfx): screen shake and fullscreen. Saved in PlayerPrefs, so they carry between sessions (in the browser too).  |
| `GhostTilemap.cs` | 1.1 KB | Ghost stone: faint and non-solid until the Shard Pulses, then solid for a few seconds (it blinks when the pulse is about to run out).  |
| `Hazard.cs` | 2.6 KB | Kills the player on touch (spikes, pits) |
| `HintZone.cs` | 636 B | Shows a hint when you enter an area |
| `IntroCutscene.cs` | 11.3 KB | Opening cutscene: a few narration cards on black, then the player (Wren) falls from the peak, lands, gets up, and talks with Marrow in a dialogue box before control is… |
| `LevelAssets.cs` | 1.8 KB | One place in each level scene that holds the shared art the runtime scripts need (font, UI sprites, skill icons, particle sprites, the area's six colours). Filled in by… |
| `LevelCatalog.cs` | 3.5 KB | Static description of one area / level. Scenes are named Level_<id>. |
| `LevelData.cs` | 1.6 KB | Level file format |
| `LevelDirector.cs` | 13.0 KB | Runs one level: gives the player the right skills, tracks deaths and checkpoints, respawns, handles relic pickups, the level-complete flow, the pause menu and scene… |
| `LevelExit.cs` | 530 B | Level exit trigger |
| `LevelHud.cs` | 13.9 KB | All the on-screen interface in a level, built from code at runtime: skill icons, death count, the area title banner, toasts, the relic pickup card, the level-complete… |
| `LevelSelectController.cs` | 13.0 KB | The level-select screen: the Spire seen from the outside, one node per area from the root up to the summit. Up/down (or the mouse) picks an area, Space/Enter plays it,… |
| `MenuBackground.cs` | 6.9 KB | Gives the main menu its life: slow cloud/mist drift, mouse parallax, a gentle sway, twinkling stars, rising embers and a bobbing title. Everything is snapped to whole… |
| `MenuList.cs` | 4.6 KB | A vertical pixel-text menu (used by the pause menu and the title screen's Options). Rows can be plain choices (confirm runs them) or settings (left / right change them,… |
| `MovingVerticalPlatform.cs` | 3.5 KB | Up/down platform (prototype) |
| `ParallaxLayer.cs` | 1.4 KB | Scrolls a background layer at a fraction of the camera's movement. factor 1 = glued to the camera (sky), 0 = fixed in the world (foreground). Set tileWidth to the… |
| `PixelText.cs` | 6.2 KB | Draws text with the project's 5x7 pixel font (font_5x7.png) into a small Texture2D shown on a RawImage. No font asset needed, always crisp, and supports a typewriter… |
| `PlayerAbilities.cs` | 770 B | Which skills the player has right now. A LevelDirector sets this when a level starts; a scene with no director (like the old sandbox Game scene) keeps everything… |
| `PlayerAnimationDriver.cs` | 5.8 KB | Drives the two layered player animators (body + sword layer). Both animators contain the same state names, so every state change is applied to both at once and they stay… |
| `PlayerController.cs` | 18.2 KB | Input to movement: 8-way aimed swings, coyote time, jump buffering, skills |
| `PlayerEffects.cs` | 3.2 KB | Juice for the player: an afterimage trail while launching, sparks on a ricochet, dust on a wall jump, a ring when the Shard pulses, and the burst when you die.… |
| `PlayerInputHandler.cs` | 2.0 KB | Receives the Player Input messages (Move / Jump / Dash / Attack / Interact) and exposes them to the controller. Set Locked = true (cutscenes, death, pickups, pause) and… |
| `PlayerMovement.cs` | 10.1 KB | Movement physics: run, jump, gravity scaling, swing launch, wall slide/jump, ricochet |
| `PulseHazard.cs` | 2.1 KB | A fire jet / sunbeam that cycles: dormant, a short warning, then deadly. The warning is the cue to move.  |
| `RelicAltar.cs` | 2.4 KB | Altar that grants the area's skill |
| `RingFx.cs` | 1.2 KB | Expanding ring effect |
| `RotatingPlatform.cs` | 1.6 KB | Rotating platform (prototype) |
| `Sfx.cs` | 18.4 KB | Every sound effect in the game. The sounds are synthesised in code when the game starts (retro square / noise waves, no files needed). To replace one with your own… |
| `ShardPulse.cs` | 1.8 KB | The Shard Pulse skill: for a few seconds, ghost stone (GhostTilemap) becomes solid. Lives on the player; the controller calls Trigger() when the Interact button is… |
| `SimpleParticle.cs` | 3.3 KB | Lightweight pixel particles |
| `Skill.cs` | 3.1 KB | The skills Wren can earn on the climb. Each area's relic altar grants exactly one, and the rest of that area (and the areas after it) is built around it. Flags, so a… |
| `SpriteFlipbook.cs` | 754 B | Plays sprites as a simple animation |
| `TitleMenuController.cs` | 11.0 KB | Main menu logic: Play loads the game scene, Options opens the settings menu (sound, music, screen shake, fullscreen, controls, reset progress), Quit exits (hidden in a… |
| `UIKit.cs` | 8.3 KB | Shared menu input and UI helpers |
| `WindZone.cs` | 2.1 KB | Pushes the player (wind) |

## Editor tools

These add the *Tools > Deadweight* and *Tools > Pixel Art* menus that build, check and ship the game.

| File | Size | What it is |
|---|---|---|
| `AreaArt.cs` | 13.3 KB | Imports and slices the art for the six areas (tilesets, props, backgrounds) and the level-select UI, and builds each area's tile assets (copies of the original tiles,… |
| `DeadweightPlayTest.cs` | 4.6 KB | Automated play-through check of every level, in real play mode: dying on a hazard and in a pit, every checkpoint (and that backtracking never moves you back), the relic… |
| `DeadweightProbe.cs` | 2.5 KB | Compares each built level scene's spike tiles with its level JSON (Assets/Levels). Headless: -executeMethod DeadweightProbe.CheckSpikes ; writes Builds/spike_probe.txt  |
| `DeadweightRelease.cs` | 12.0 KB | Release tooling for itch.io. Tools > Deadweight > Apply Release Settings - name, icon, WebGL player settings, scene list, menu wiring Tools > Deadweight > Build WebGL… |
| `DeadweightSetup.cs` | 3.8 KB | Tools > Deadweight: one click builds the whole progression: area art, the Player prefab, every level scene, the level-select and ending scenes, and the Build Settings… |
| `DeadweightVerify.cs` | 3.8 KB | Tools > Deadweight > Verify Levels: opens every level scene (without saving it) and checks that every patch of cracked stone can be shattered by a hit at its edge, that… |
| `FixGameCamera.cs` | 3.8 KB | Tools > Pixel Art > Fix Game Camera (open Game scene first) - Removes the stray extra Camera (and its Pixel Perfect Camera) that was sitting on the CinemachineCamera… |
| `GamePolish.cs` | 2.9 KB | Tools > Pixel Art > Polish Game Scene (open Game.unity first) Smooths the way the game looks and moves: - Pixel Perfect Camera snaps sprites to the pixel grid (stops… |
| `LevelBuilder.cs` | 42.3 KB | Builds a playable scene for each area from its level file (Assets/Levels/<id>.json). Everything is generated, so re-running replaces the scenes cleanly.  |
| `LevelSelectBuilder.cs` | 2.8 KB | Builds the level-select and ending scenes |
| `MainMenuBuilder.cs` | 16.7 KB | Tools > Pixel Art > Build Main Menu Scene Imports the menu art (Assets/Art/Menu), builds Assets/Scenes/MainMenu.unity and puts it first in Build Settings. Safe to… |
| `PlayTestRunner.cs` | 13.7 KB | Runs the automated play-test steps in the editor |
| `PlayerArtBuilder.cs` | 40.0 KB | One-click importer for the generated pixel art: slices every sheet, builds the player animation clips + controllers, wires the player in the open scene, and builds the… |
| `PlayerPrefabTool.cs` | 2.2 KB | Turns the Player in Game.unity into Assets/Prefabs/Player.prefab so every level scene shares one player. Only runs when the prefab does not exist yet (delete the prefab… |
| `SfxExport.cs` | 1.6 KB | Tools > Deadweight > Export Generated Sounds: writes every built-in (synthesised) sound effect to Assets/Audio/SFX_Generated/*.wav so you can listen to them, or edit one… |
| `StoryCutsceneBuilder.cs` | 10.4 KB | Tools > Pixel Art > Add Intro Cutscene To Open Scene Imports Assets/Art/Story and adds a "CutsceneCanvas" (letterbox, narration, dialogue box) plus the IntroCutscene… |

## Unity packages

| Package | Version |
|---|---|
| `com.unity.2d.animation` | 13.0.4 |
| `com.unity.2d.aseprite` | 3.0.1 |
| `com.unity.2d.psdimporter` | 12.0.1 |
| `com.unity.2d.sprite` | 1.0.0 |
| `com.unity.2d.spriteshape` | 13.0.0 |
| `com.unity.2d.tilemap` | 1.0.0 |
| `com.unity.2d.tilemap.extras` | 6.0.1 |
| `com.unity.2d.tooling` | 1.0.2 |
| `com.unity.cinemachine` | 3.1.6 |
| `com.unity.collab-proxy` | 2.11.3 |
| `com.unity.ide.rider` | 3.0.39 |
| `com.unity.ide.visualstudio` | 2.0.26 |
| `com.unity.inputsystem` | 1.18.0 |
| `com.unity.multiplayer.center` | 1.0.1 |
| `com.unity.render-pipelines.universal` | 17.3.0 |
| `com.unity.test-framework` | 1.6.0 |
| `com.unity.timeline` | 1.8.10 |
| `com.unity.ugui` | 2.0.0 |
| `com.unity.visualscripting` | 1.9.9 |

