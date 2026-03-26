# tap_miner

Tap Miner is a Unity 6 mobile prototype built in URP. The project is being developed as a playable vertical slice with an AI-assisted task workflow: define a task, implement it, validate it, and iterate.

## Current Project State
- Status: playable prototype / vertical slice in active post-pipeline development
- Main scene: `Assets/Scenes/SampleScene.unity`
- Primary target: Android portrait devices (current testing focus: Pixel 9)
- Rendering: URP with mobile renderer path enabled
- Input: swipe for lane change, tap for segment processing

## Current Gameplay Loop
The player launches into a menu-first shell, starts a run, changes lanes with swipe, taps to process the current segment, collects run coins, avoids failure, and returns to a post-run menu/results state.

### Currently working
- Main menu with `PLAY` and `UPGRADES` entry points
- Stub upgrades panel with `COMING SOON`
- In-run HUD
  - depth
  - coins
  - collapse bar
- Post-run results / restart flow
- Swipe left/right lane transitions
- Tap to process the current segment
- Responsive world-width layout for portrait mobile aspect ratios
- Mobile renderer selection for the main camera
- Android development APK output pipeline

### Current limitations / known issues
- Presentation and Android feel tuning are still in progress
- World scroll / descent presentation is not yet convincing; the run can still feel visually static
- Tap/break reward feedback is still lightweight
- Hazards/obstacles need further player-facing polish and validation on device
- Some temporary Android/build investigation logs may still exist in runtime code and should be cleaned up in a follow-up pass
- `UPGRADES` is a UI stub, not a real economy screen yet

## Controls
- `PLAY`: start run from menu or restart after death
- `UPGRADES`: open placeholder panel
- Swipe left / right: move between lanes
- Tap during run: process the current segment

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
- `Assets/Scenes/`
  - playable Unity scenes (`SampleScene.unity` is the active gameplay scene)
- `Assets/_Project/Scripts/Core/`
  - gameplay runtime, state, progression, and presentation code
- `Assets/_Project/Editor/`
  - validation and smoke-test tooling
- `Assets/_Project/Data/`
  - execution reports, task documents, and chat reports from the original pipeline
- `Assets/_Project/Art/`
  - runtime materials and UI-facing art assets
- `Builds/Android/`
  - generated Android development APKs

## Validation / Tooling
The project is set up for MCP-assisted inspection and validation.

Current workflow expectations:
- inspect scene/runtime state through Unity MCP when possible
- validate compile status and logs after each task
- keep scope minimal and rollbackable
- avoid changing ProjectSettings, packages, or build config unless explicitly approved

Useful repo tooling:
- `Tools/Tap Miner/Run Core Loop Smoke`
- `_Project/Editor/CoreLoopSmokeRunner.cs`
- `_Project/Editor/T018PlaytestInstrumentationRunner.cs`

## Build Notes
- Current Android dev build output:
  - `Builds/Android/TapMiner_dev.apk`
- Android player settings and rendering path have already been adjusted for mobile validation
- Device-side testing remains the source of truth for touch feel and rendering correctness

## Tech Stack
- Unity 6
- Universal Render Pipeline (URP)
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
The original locked execution pipeline `T001-T019` is complete. The project is now being iterated as a post-pipeline vertical slice, with current work focused on Android presentation, input validation, and player-facing polish.