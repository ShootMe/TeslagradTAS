# TeslagradTAS
Simple TAS Tools for the game Teslagrad

## Installation
- Go to [Releases](https://github.com/ShootMe/TeslagradTAS/releases)
- Download Assembly-Boo.dll, Assembly-Boo-Addons.dll, J2i.Net.XInputWrapper.dll, and XInputInterface.dll
- Place those in your Teslagrad game data directory (usually C:\Program Files (x86)\Steam\steamapps\common\Teslagrad\Teslagrad_Data\Managed\)
- Make sure to back up the original Assembly-Boo.dll before copying. (Can rename to .bak or something)

## Input File
Input file is called Teslagrad.tas and needs to be in the main Teslagrad directory (usually C:\Program Files (x86)\Steam\steamapps\common\Teslagrad\Teslagrad.tas)

Format for the input file is (Frames),(Actions)

ie) 123,R,J (For 123 frames, hold Right and Jump)

## Actions Available
- R = Right
- L = Left
- U = Up
- D = Down
- J = Jump
- N = Negative Charge (Blue)
- P = Positive Charge (Red)
- B = Blink
- [ = Negative Cloak (Blue)
- ] = Positive Cloak (Red)
- S = Start/Menu
- M = Map
- C = Toggle Cloak Action

## Playback / Recording of Input File
### Controller
While in game
- When NOT Playing Back
  - Save Game - DPad Up (Saves abilities, checkpoint)
  - Load Game - DPad Down (Loads the saved state and brings you back to the main menu so you can reload the game with that state)
  - Kill Player - DPad Right
- When Playing Back
  - Playback: Right Stick
  - Stop: Right Stick
  - Record: Left Stick
  - Faster/Slower Playback: Right Stick X+/X-
  - Frame Step: DPad Up
  - While Frame Stepping:
    - One more frame: DPad Up
    - Continue at normal speed: DPad Down
    - Frame step continuously: Right Stick X+

### Keyboard
While in game
- Playback: Control + [
- Stop: Control + ]
- Record: Control + Backspace
- Faster/Slower Playback: RightShift / LeftShift
- Frame Step: [
- While Frame Stepping:
  - One more frame: [
  - Continue at normal speed: ]
  - Frame step continuously: RightShift
  
## Teslagrad Studio
Can be used instead of notepad or similar for easier editing of the TAS file. Is located in [Releases](https://github.com/ShootMe/TeslagradTAS/releases) as well.

If Teslagrad.exe is running it will automatically open Teslagrad.tas if it exists. You can hit Ctrl+O to open a different file, which will automatically save it to Teslagrad.tas as well. Ctrl+Shift+S will open a Save As dialog as well.
