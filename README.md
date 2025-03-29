# Dream Games - Blast Game

This repository contains my implementation of the Dream Games Software Engineering Study, a level-based mobile puzzle game built in Unity with C#. The game features match-3 mechanics with special rockets, various obstacle types, and 10 unique levels.

## Gameplay 


<p align="center">
  <img src="https://github.com/user-attachments/assets/7f4bbb59-a917-4d40-9cfb-b1dbc5602469" width="250" />
  <img src="https://github.com/user-attachments/assets/538c9c9c-9e8e-4721-867d-d138fd5331a0" width="250" />
  <img src="https://github.com/user-attachments/assets/9a878967-9481-4af3-9f55-72c3734bc7d0" width="250" />
</p>

[Gameplay Video](https://www.youtube.com/watch?v=U9H3MfrRxQw)





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

### Animation Settings

Animation behavior can be customized through the AnimationManager object:

1. In the Hierarchy panel, find and select the **AnimationManager** object
2. In the Inspector panel, you'll find several animation settings:
   - **Debug Mode**: Toggle for detailed animation logging
   - **Global Animation Settings**: Overall animation speed and toggle
   - **Animation Type Toggles**: Enable/disable specific animation types
   - **Specific Animation Settings**: Duration and intensity for various animations

These settings allow for fine-tuning the visual experience or disabling animations entirely for testing purposes.

### Special Keys to Press

In order to facilitate testing and debugging, the following keyboard shortcuts are available:

- **1-9**: Instantly set the game level to the corresponding number (e.g., pressing '3' will load level 3).
- **R**: Restart the current level, allowing for quick iteration during testing.
- **Q**: Return to the main menu, providing a fast way to exit a level without completing it.
- **P**: Display the win popup screen to simulate level completion.
- **Return (Enter)**: Restart the current level from the beginning without requiring manual navigation.

