SOUND EFFECTS
=============

Every sound effect is generated in code when the game starts (Assets/Scripts/Sfx.cs), so the game
has full sound with no audio files.

To replace a sound with your own:
  1. Put an audio file (.wav, .ogg or .mp3) in  Assets/Audio/Resources/SFX/
  2. Name it exactly after the sound, e.g.  jump.wav  or  shatter.ogg
  It is used automatically next time you press Play. Delete the file to go back to the generated one.

To hear the built-in sounds, or to edit one as a starting point:
  Tools > Deadweight > Export Generated Sounds  ->  Assets/Audio/SFX_Generated/

Sound names:
  jump, walljump, land, swing, launch, ricochet, shatter, crumble_crack, crumble_fall,
  death, respawn, checkpoint, relic, skill, complete, pulse,
  jet_warn, jet_fire, beam_warn, beam_fire, wallslide (loops while sliding down a wall),
  menu_move, menu_confirm, menu_back, menu_error, pause, hint, text (dialogue typing blip)

Volume: the pause menu has SOUND and MUSIC sliders (left / right). They are saved between sessions.
