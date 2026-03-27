# tap_miner

Tap Miner is a Unity 6 mobile prototype built in URP. The project is being developed as a playable vertical slice with an AI-assisted task workflow: define a task, implement it, validate it, and iterate.

## Current Project State
- Status: playable prototype / vertical slice in active post-pipeline development
- Main scene: `Assets/Scenes/SampleScene.unity`
- Primary target: Android portrait devices (current testing focus: Pixel 9)
- Rendering: URP with mobile renderer path enabled
- Input: swipe for lane change, tap to drill blocks

## Core Gameplay Model

The player holds position in a vertical mine shaft. Blocks rise continuously from below. When a block enters the player's lane, it pushes the player upward. The player taps to drill through blocks and fall back to base position. A fixed ceiling sits at the top of the screen — being pushed into it means death.

```
[CEILING — fixed, does not move]         ← touch this = death
─────────────────────────────────
     [PLAYER — rises with blocks]        ← baseY ~35% from bottom
─────────────────────────────────
  ↑↑↑  blocks / coins / hazards rising
```

- **Scroll** runs continuously at constant speed — never stops
- **Block in player's lane** → player rises with it
- **Tap** → drill the block → player returns to baseY
- **Ceiling contact** → death. No UI bar — player sees their position directly
- **Hazard** → HP damage + 2s scroll slowdown, does not block movement
- **Coin/Reward** → auto-collected when player passes through the lane

## Currently Working
- Main menu with `PLAY` and `UPGRADES` entry points
- Swipe left/right lane transitions with 1-slot input buffer
- Continuous world scroll (sliding window, 4 tiles)
- Player at ~35% from bottom of screen
- Block segments rise toward player
- Tap to drill blocks
- Collapse bar (time-driven, to be replaced with position-based pressure)
- Pause menu (Resume / Restart / Sound / Vibration / Menu)
- Restart flow: RunRestarting → RunReady → menu → tap → RunActive
- Coins/Depth reset on restart
- Post-run results overlay

## Known Issues / Next Up
- Ceiling pressure model not yet implemented (player Y is static)
- Coins not auto-collected on scroll-through (tap only)
- HP bar not visible in HUD
- Empty safe cells render as cubes (should be hidden)
- Marker cubes do not despawn above player

## Controls
- `PLAY`: start run from menu or restart after death
- `UPGRADES`: open placeholder panel
- Swipe left / right: move between lanes
- Tap during run: drill block in current lane

## Core Runtime Systems
Main runtime code lives under `Assets/_Project/Scripts/Core/`.

Key systems include:
- `AppBootstrap.cs`: runtime authority and system wiring
- `RunStateMachine.cs`: run state transitions
- `SwipeInputInterpreter.cs`: tap/swipe classification
- `LaneTransitionController.cs`: lane movement
- `RunPresentationController.cs`: runtime-built world and UI presentation
- `SegmentSpawnSystem.cs`: segment generation
- `BreakableBlockResolutionSystem.cs`: break resolution
- `LootDropResolutionSystem.cs`: loot resolution
- `HazardContactResolutionSystem.cs`: hazard resolution
- `RunRewardAggregationSystem.cs`: run reward accumulation
- `UpgradePersistenceSystem.cs`: persistent upgrade progress
- `MissionLayerLiteSystem.cs`: lightweight mission layer
- `PlaytestInstrumentationSystem.cs`: telemetry / validation support

## Repository Layout
- `Assets/Scenes/` — playable Unity scenes
- `Assets/_Project/Scripts/Core/` — gameplay runtime, state, progression, and presentation code
- `Assets/_Project/Editor/` — validation and smoke-test tooling
- `Assets/_Project/Data/` — execution reports, task documents, chat reports
- `Assets/_Project/Art/` — runtime materials and UI-facing art assets
- `Builds/Android/` — generated Android development APKs

## Validation / Tooling
- Inspect scene/runtime state through Unity MCP when possible
- Validate compile status and logs after each task
- Keep scope minimal and rollbackable
- Avoid changing ProjectSettings, packages, or build config unless explicitly approved

## Build Notes
- Android dev build: `Builds/Android/TapMiner_dev.apk`
- Device-side testing is the source of truth for touch feel and rendering correctness

## Tech Stack
- Unity 6 / URP
- Unity Input System
- Unity UI (UGUI)
- Unity MCP tooling
- Git

## Workflow Rules
This repository follows the constraints in `AGENTS.md`:
- work on one task at a time
- do not expand scope beyond the current task
- prefer minimal, rollbackable changes
- ask before destructive actions or protected config changes
- player-facing changes require manual review

## Historical Context
The original locked execution pipeline `T001-T019` is complete. The project is now being iterated as a post-pipeline vertical slice. Core gameplay model was updated after T019: block-pressure / ceiling system replaces the time-based collapse wall.
