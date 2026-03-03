# Echo Shift

A roguelike precision platformer with innovative time-based mechanics.

## 🎮 About the Project

**Echo Shift** is a challenging precision platformer that combines roguelike elements with unique time-manipulation mechanics. Master the art of movement, discover your optimal path through procedurally designed levels, and use echo recordings of your previous attempts to overcome seemingly impossible obstacles.

### Game Concept

Navigate through carefully crafted levels that demand precise timing and execution. Your past actions can become your allies—record your movements and witness your echoes replay your recorded input, creating opportunities to solve puzzles and bypass hazards that would be impossible to overcome alone.

## ✨ Key Features

- **Precision Platforming**: Demanding movement mechanics requiring skill and practice
- **Echo Recording System**: Record your input and spawn echoes that replay your exact movements
- **Dynamic Movement**: Dash, wall jump, crouch mechanics with varied gravity applications
- **Variable Jump Mechanics**: 
  - Coyote time for forgiving jump windows
  - Jump buffering for responsive controls
  - Apex gravity reduction for floaty jump feels
  - Low jump gravity for early jump release
- **Roguelike Elements**: Procedurally varied level design with escalating difficulty
- **State Management**: Complex player state system managing Normal, Crouching, Dashing, Wall Jumping, and Echo Playback states

## 🎮 Gameplay Mechanics

### Movement System
- **Running**: Standard horizontal movement with acceleration and deceleration
- **Crouch**: Reduce movement speed with ceiling detection and collider management
- **Facing Direction**: Automatic character flipping based on movement input

### Jump Mechanics
- **Coyote Time**: Brief window after leaving ground where jump is still available
- **Jump Buffer**: Input buffer that allows jump registration slightly before landing
- **Variable Gravity**: Different gravity multipliers based on jump phase and button hold
- **Apex Gravity**: Lighter gravity at the peak of jumps for better control

### Dash Mechanic
- **High-speed movement** in the input direction
- **Cooldown system** to balance powerful ability
- **Gravity override** during dash for consistent velocity
- **Duration-based** action with clear state transitions

### Wall Jump Mechanic
- **Wall Detection**: Dual-sided wall detection accounting for player flipping
- **Jump Away**: Propels player away from wall with vertical boost
- **Momentum Preservation**: Maintains vertical velocity component

### Echo System
- **Input Recording**: Captures all player actions over a specified duration
- **Echo Spawning**: Create controllable clones that replay recorded input
- **Synchronized Playback**: Echoes replay input with frame-perfect accuracy
- **Buffer Management**: Automatically maintains recording history

## 📁 Project Structure

```
Echo Shift/
├── Assets/
│   ├── Scripts/
│   │   ├── PlayerController.cs         # Main player controller & state machine
│   │   ├── PlayerMovement.cs           # Movement, acceleration, crouch
│   │   ├── PlayerJump.cs               # Jump mechanics & gravity control
│   │   ├── PlayerDash.cs               # Dash ability
│   │   ├── PlayerWallJump.cs           # Wall detection & wall jumping
│   │   └── ...
│   ├── Prefabs/
│   │   └── ...
│   ├── Scenes/
│   │   └── ...
│   ├── Settings/
│   │   └── ...
│   └── ...
├── Packages/
│   └── manifest.json
├── ProjectSettings/
│   └── ...
└── README.md
```

## 🚀 Getting Started

### Prerequisites
- **Unity 2022.3 LTS** or later
- **Input System Package** (included in project)
- Windows, macOS, or Linux

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/echo-shift.git
   cd echo-shift
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Click "Open" and select the `Echo Shift` folder
   - Unity will load the project

3. **Install dependencies**
   - The project uses Unity's Input System
   - Dependencies are configured in `Packages/manifest.json`

### Building & Running

**Play in Editor:**
- Open a scene from `Assets/Scenes/`
- Press the Play button in the Unity Editor

**Build Standalone:**
1. Go to `File → Build Settings`
2. Select your target platform
3. Click "Build and Run"

## 🎮 Controls

| Action | Input |
|--------|-------|
| Move Left/Right | `A` / `D` or Left/Right Arrow |
| Jump | `Space` |
| Crouch | `Ctrl` |
| Dash | `Shift` |
| Spawn Echo | `E` |

*Controls are configured through Unity's Input System and can be remapped via `Assets/InputSystem_Actions.inputactions`*

## 🛠️ Development

### Code Organization

The player controller is organized into logical sections:

- **Nested Types**: Enums, structs, and interfaces
- **Configuration**: Serialized parameters for tuning
- **State Management**: Player state machine and transitions
- **Input Handling**: Input reading from multiple input sources
- **Physics & Ground Detection**: Movement physics and collision
- **Recording System**: Echo recording and playback
- **Event System**: UnityEvent notifications

### Adding New Features

Each subsystem (Movement, Jump, Dash, WallJump) is modular:

1. Create a new script (e.g., `PlayerAbility.cs`)
2. Implement the initialization pattern
3. Add a public method for the main update logic
4. Hook into `PlayerController.FixedUpdate()`
5. Use state machine for state-gated features

### Key Classes

- **PlayerController**: Main controller managing subsystems and state
- **PlayerMovement**: Horizontal movement and crouch mechanics
- **PlayerJump**: Jump physics with variable gravity
- **PlayerDash**: Dash ability with cooldown
- **PlayerWallJump**: Wall detection and wall jumping

## 📊 Feature Status

- ✅ Core movement mechanics
- ✅ Jump system with gravity variants
- ✅ Dash ability
- ✅ Wall jumping
- ✅ Echo recording & playback
- ✅ State machine
- ⚙️ Procedural level generation (in progress)
- ⚙️ Enemy AI (in progress)
- ⚙️ UI/HUD (in progress)
- ⚙️ Audio system (planned)
- ⚙️ Visual effects (planned)

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/AmazingFeature`)
3. **Commit** your changes (`git commit -m 'Add some AmazingFeature'`)
4. **Push** to the branch (`git push origin feature/AmazingFeature`)
5. **Open** a Pull Request

### Code Style
- Use clear, descriptive variable names
- Add XML documentation comments to public methods
- Organize code into logical `#region` sections
- Remove unused variables and imports
- Follow C# naming conventions (PascalCase for classes/methods, camelCase for variables)

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 📧 Contact

For questions or suggestions, please open an issue on GitHub.

---

**Happy platforming!** 🚀

*Echo Shift - Master precision, manipulate time, conquer the impossible.*