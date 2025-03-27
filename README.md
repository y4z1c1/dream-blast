# Dream Games - Match-3 Puzzle Game

This repository contains my implementation of the Dream Games Software Engineering Study, a level-based mobile puzzle game built in Unity with C#. The game features match-3 mechanics with special rockets, various obstacle types, and 10 unique levels.

![Game Screenshot](screenshots/gameplay.png)

## Development Documentation

For a comprehensive overview of the implementation process, project structure, and technical details, please see the [Implementation Documentation](https://github.com/y4z1c1/dream-blast/blob/main/report.pdf).

### Installation

1. Clone this repository:

   ```bash
   git clone https://github.com/y4z1c1/dream-blast.git
   ```

2. Open Unity Hub and click "Add" to add the project folder

3. Make sure you're using Unity version 6000.0.32f1 or later

4. Open the project

5. When the project loads, all dependencies should be resolved automatically with Unity's built-in renderer

## Developer Settings

### Setting Level in Editor

You can manually set the current level from the Unity Editor:

1. Open the Hierarchy panel
2. Find and select the **GameController** object
3. In the Inspector panel, you can adjust the "Debug Level To Set" value
4. Right-click on the GameController component
5. Select **Set Debug Level** from the context menu
6. The level will be set to the specified value

![Setting Level in Editor](screenshots/set_debug_level.png)

### Animation Settings

Animation behavior can be customized through the AnimationManager object:

1. In the Hierarchy panel, find and select the **AnimationManager** object
2. In the Inspector panel, you'll find several animation settings:
   - **Debug Mode**: Toggle for detailed animation logging
   - **Global Animation Settings**: Overall animation speed and toggle
   - **Animation Type Toggles**: Enable/disable specific animation types
   - **Specific Animation Settings**: Duration and intensity for various animations

These settings allow for fine-tuning the visual experience or disabling animations entirely for testing purposes.
