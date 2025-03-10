# RDModifications

## a bepinex 6 (mono) mod !! sorry bepinex 5 users

some random modifications that some people could want and such so why not

## warning: some of the modifications could cause issues (accidentally and purposefully) in levels
#### (e.g. 2x+ speed seems to cause some ***visual*** issues sometimes)<br>

Check the config file or use that Config manager that bepinex have said about

List of modifications available with a simple summary:

- AnimatedSleeves -> Allows for animated sleeves [(see the section for how to do it)](#animatedsleeves-setup)
- APNGPreviewImage -> Allows for animated preview images using the APNG file format
- CustomDifficulty -> Allows for a custom hit margin
- CustomDiscordRichPresence -> Allows for customising the discord rich presence, including the presence id
- CustomIceChiliSpeeds -> Allows for having a custom speed on ice/chili speeds instead of 0.75x/1.5x
- CustomSamuraiMode -> Allows for setting the `Samurai.` from Samurai. mode to any string
- DoctorMode -> Removes the `Rhythm` from `Rhythm Doctor` (destroys the rhythm engine)
- EditorPatches -> Some patches to extend editor features or to improve editor [(see the section for more info)](#editorpatches)
- ExtraLevelEndDetails -> Provides extra details at the end of a level, e.g. previous best
- ForceGameSpeed -> Forces the game speed to a certain value (may cause a few issues due to how forceful it is)
- LevelPRStatus -> Colors the syringe body of a custom level depending on the peer-review status of it on Rhythm Café
- PretendFOnMistake -> Allows for flashing a fake rank screen and a sound to play on each miss, with being able to choose the Rank shown/said
- MassUnwrapLevels -> Allows for unwrapped all currently wrapped levels with a simple key combination
- WindowTransparency (WINDOWS ONLY) -> Allows for setting how transparent the game window is
- ZeroOffsetSign -> Makes the Numerical Hit Judgement sign colored based on the hit's ""zero-offsetness""

## AnimatedSleeves setup

### there are three methods for having animated sleeves, either each frame is a seperate image or the sleeve image is an apng file or one big spritesheet

#### each `frame` referred to here is and must be 524px x 40px (width x height) in size !!!

#### the sleeve images are located at where rhythm doctor's save files are to e.g. `C:\Program Files (x86)\Steam\steamapps\common\Rhythm Doctor\User\`

### if you are doing the apng, do note that:

- this is experimental so idk if it works please alert me if not
- apngs are a bit special and idk where you make them so

the apng should be placed where the sleeve images are and should be suffixed with `_animated`, e.g. `scribbleP1_0_animated.png`
<br>

### if you are doing the spritesheet, do note that:

- there cannot be empty spots in the spritesheet
- the program will read from left to right, top to bottom, so make sure to export correctly (e.g. in aseprite horizontal strip or by rows i think)
- the game will refuse to handle an image that's too big in width or height so keep that in mind

the spritesheet should be placed where the sleeve images are and should be suffixed with `_animated`, e.g. `scribbleP1_0_animated.png`
<br>

### if you are doing it with individual images

each image should have the suffix of `_frameX` where X is a number starting from 1 e.g. like:

`scribbleP1_0_frame1.png`, `scribbleP1_0_frame2.png`, `scribbleP1_0_frame3.png`, ...
<br>

### *please do ask for more details if there's some parts you don't understand*

## EditorPatches

- DisableSliderLimits -> numerical slider limits are disabled (albeit you must use the input field)
- DuplicateDecorationButton -> adds a button to duplicate a deco, above the delete button for the deco
- EditorBorderTintOpacity -> (will only work with DisableSliderLimits enabled) adds a slider for border/tint opacity, meant for under/overtinting (hence why slider limits need to be disabled)
- InsertDeleteBars -> Alt + left click inserts a bar, alt + right click deletes the bar clicked (hard to explain shortly)

# Create a github issue for suggestions/bugs or just ping me if you're in a server i'm in
