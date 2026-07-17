# Repository Guide for Agents

This is a Unity states-of-matter learning game. Read this file before changing scenes, gameplay flow, dialogue, or old branches. Several important behaviors are not obvious from filenames alone.

## Repository and Worktree Safety

- Always run `git worktree list` before switching branches or moving refs.
- The primary Unity Editor is normally connected to:
  `/Users/matthewlin/Desktop/workspace/kids-first-initiative-states-of-matter`.
- That checkout may contain active audio, dialogue, scene, package, tooling, and generated-file work. Do not clean, reset, stash, or commit those changes as incidental work.
- Prefer a separate clean worktree from `origin/main` for branch integration, commits, and pull requests.
- `/Users/matthewlin/Desktop/workspace/kids-first-water-display` is another worktree of this repository, not an independent repository or remote.
- Do not commit `Library`, `Library.bak-*`, generated screenshots, or unrelated local tools.
- Unity asset identity depends on `.meta` GUIDs. Move assets with Unity-aware operations and preserve matching `.meta` files.
- Before fixing a serialized `m_Script` reference, compare its GUID with the target script's `.meta` file. A matching class name in `m_EditorClassIdentifier` does not repair a stale GUID.

## Current Baseline

- Unity version: `6000.3.1f1`.
- The pipe overhaul was merged into `main` by PR #26 at merge commit `56026f9`.
- The intentional build configuration disables `Assets/Scenes/Pipes game.unity` and enables `Assets/Scenes/Pipes-Frozen-Level.unity`.
- `Docs/game-experience-redesign-proposal.md` is a proposal only. The redesign and centralized progression described there are not implemented yet.
- There are currently no meaningful automated Unity tests. EditMode and PlayMode can report success while each contains zero concrete test cases.

## Current Scene Routing

The routing is more limited than the scene list suggests.

- `States of Matter Menu` opens `GameSelector`.
- The Matter Kitchen card in `GameSelector` directly opens `Kitchen Game - Freezing Pour`.
- The Pipes card directly opens `Pipes-Frozen-Level`.
- The State Lab card opens `Wires`.
- `Kitchen Game - Solid` is enabled in build settings but is not the selector's Matter Kitchen destination.
- `Pipes game` is disabled and is not the selector's Pipes destination.
- Selector routing is currently stored as serialized button calls with scene-name strings, not in a shared progress service.

When testing the shipped child path, begin at `GameSelector`. Loading every scene directly tests content that may not be reachable through the current UI.

## Matter Kitchen: Current Mechanics

The intended educational arc spans three scenes even though the selector starts at the second one.

### Solid Scene

Scene: `Assets/Scenes/Kitchen Game - Solid.unity`

- Drag the chocolate UI item into the pot.
- `IngredientBarDragToWorld2D` creates the world ingredient.
- `MockPotController` registers an `IngredientInstance` through 2D collision or trigger callbacks.
- Raise the `HeatSlider` to maximum.
- `KitchenGameManager` wins when the required ingredient is present and maximum heat has been reached.
- Heating before adding the ingredient can fail the stage.
- The scene does not automatically continue to the pouring scene.

### Freezing Pour Scene

Scene: `Assets/Scenes/Kitchen Game - Freezing Pour.unity`

- Drag and tilt the juice bottle over the tray.
- `JuicePouring` rotates after pointer down and emits `JuiceDroplet` objects while dragging.
- The bottle's spawn point, not its center, must be above the tray.
- `IceTray` fills in droplet increments.
- `JuiceFreezingManager` plays a liquid-flow result and then loads `Kitchen Game - Freezing Station` after dialogue is idle.

### Freezing Station Scene

Scene: `Assets/Scenes/Kitchen Game - Freezing Station.unity`

- Drag the juice mold into the freezer/snap area.
- `MockPotController` detects the placed ingredient.
- Lower the temperature slider to its minimum/cold region.
- `JuicePouringGameManager` completes when an ingredient is present and `JuiceCoolingController.IsColdEnough` is true.
- Completion returns to `States of Matter Menu` after result dialogue is idle.

## Pipes: Current Mechanics and Risks

### Disabled Basic Pipe Scene

Scene: `Assets/Scenes/Pipes game.unity`

Do not assume this is a working first level merely because it exists.

- Runtime inspection found no configured source pipe or endpoint pipe.
- The current pipe instances do not have `RotateOnClick` components.
- The Start action therefore cannot validate a normal source-to-end path.
- The scene is intentionally disabled in build settings.

### Active Frozen Pipe Scene

Scene: `Assets/Scenes/Pipes-Frozen-Level.unity`

- Water begins at `(1, 4)` and the visual endpoint is `(8, 4)`.
- Clicking a pipe invokes `FreezeOnClick`, toggles `PipeObject.isFrozen`, recalculates water, and calls `FrozenFlowValidator.Validate()`.
- The obvious dead-end sink coordinates are `(2, 3)`, `(4, 4)`, `(4, 2)`, and `(8, 2)`.
- The Start button uses `StartButtonHandler`, which only checks whether an `isEnd` pipe has water.
- `FrozenFlowValidator` separately checks endpoint water and leak counts.
- These two success definitions are currently inconsistent. The four visually obvious frozen blockers can deliver water to the endpoint while the leak validator still rejects the T-junction at `(3, 2)`.
- Pressing Start on a failed arrangement hides the Start button, leaving no normal retry of that action.
- The Start-button success path plays dialogue but does not itself record progress or leave the scene.

Treat the frozen level as functionally incomplete until it has one validator, repeatable attempts, one completion event, and explicit progression. The proposed replacement design is in `Docs/game-experience-redesign-proposal.md`.

## State Lab / Wires: Current Mechanics

Scene: `Assets/Scenes/Wires.unity`

This is currently the most complete end-to-end activity.

1. Drag either `HotPlate` or `Plasma Tube` into `SnapZone`.
2. `SnapZone.Snap()` calls `Main.DeviceConnected()` and unlocks wire interaction.
3. Match four wire pairs. Matching is determined by the parent object name, such as `Wire Green`, not by sampled color values.
4. `Main.LightOn(1)` increments the connected count.
5. The power slider is gated until an output is placed and all required wires are connected.
6. Raising power to the threshold triggers the selected device effect and win flow.

The power gate correctly resets premature power attempts and provides guidance. Completion remains in the Wires scene with Menu and Retry available.

Dialogue references more possible experiments than are visibly polished in the current scene. Inspect actual scene objects before assuming HotPlate, IceFlask, Candle, and Plasma are all separately available.

## Dialogue and Cutscene Behavior

- Gameplay adapters derive from `DialogueFlowAdapterBase`.
- Runtime adapters can create or locate a shared `DialogueFlowController` and register default lines.
- Default gameplay prompts use `promptAutoAdvanceDelay = 4` seconds.
- Voice lines may keep a line active until audio completes.
- `DialogueWaitUtility.WaitUntilIdle()` waits for both active and queued dialogue.
- Kitchen scene-transition coroutines already wait for dialogue to become idle before loading the next scene.
- A tool-driven play-mode transition can take several seconds. By the time a screenshot is requested, a four-second intro may already have completed. Inspect `DialogueRunner.IsPlaying`, `QueuedCount`, and `CurrentLine` before concluding that an opening prompt is missing.
- State-change cutscenes use a runtime Screen Space Overlay canvas at a high sorting order and temporarily disable world mouse handlers.
- Do not add another independent completion loader to a dialogue adapter. Gameplay should emit outcomes; only one owner should decide progression.

## Unity MCP Workflow

Before Unity work:

1. Read `mcpforunity://instances`.
2. Read `mcpforunity://custom-tools`.
3. Pin the exact instance with `set_active_instance` when needed.
4. Read `mcpforunity://editor/state` before changing play mode or scenes.

The expected instance has been named:
`kids-first-initiative-states-of-matter@5969b000f278fd5b`.

Important control details:

- `manage_editor(action="pause")` toggles pause. It reports either `Game paused` or `Game resumed`.
- If gameplay time appears frozen, read editor state. MCP screenshots can leave play mode paused.
- Use `manage_scene(action="load")` for scenes and restore the user's starting scene after diagnostics.
- Use `read_console` after scene loads, script refreshes, and gameplay completion.
- `execute_code` can inspect runtime state and invoke public gameplay APIs. Prefer actual interaction methods such as `SnapZone.Snap`, slider value changes, and pointer handlers over editing private fields.
- The execute-code environment may fall back to CodeDom. Keep diagnostic snippets compatible with older C# syntax unless Roslyn is known to be installed.
- The Unity MCP Editor uses the primary project checkout, not another clean Git worktree. Confirm `mcpforunity://project/info` before assuming which source state is running.

### Reliable Screenshot Procedure

Capturing while a render transition is in progress can produce black rectangular tiles and memoryless depth-surface warnings that are not present in a stable Game View.

For reliable evidence:

1. Let the scene or feedback animation settle.
2. Explicitly pause with `manage_editor(action="pause")`.
3. Capture the Game View.
4. Resume by calling the same pause action.

Do not change gameplay rendering solely because of one moving-frame MCP capture. Reproduce on a stable paused frame first.

`manage_camera(action="screenshot")` writes PNG and `.meta` files. Use a temporary folder such as `Assets/Screenshots`, then delete that folder after diagnostics and verify it is absent from `git status`.

## Verification Practices

- After external file edits, call `refresh_unity` and wait until editor state says tools are ready.
- Check compilation and console output before entering play mode.
- Validate affected scenes, but remember scene validation does not replace an actual playthrough.
- Scan scenes and prefabs for missing scripts with `UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` through Unity editor code.
- A clean scan should cover both scene hierarchies and prefab assets.
- For gameplay fixes, test the failure path, retry path, success path, result presentation, and final scene/progress behavior.
- Before finishing, stop play mode, restore a neutral scene such as `GameSelector`, remove generated captures, and inspect `git status`.

## Dated Branch Archaeology

These conclusions were established on 2026-07-12 and should be rechecked against the latest `main` before acting:

- `pipes-overhaul` is merged into `main` and is no longer additive.
- `codex/pipe-asset-alignment-handoff`, `art-assets`, and the parallel water-display work are covered by the merged pipe overhaul intent.
- `cutscene-stack/05-pipe-api-cleanup` has five unique historical commits, but its shared cutscene, kitchen, pipe-success, and Unity object-lookup intent is represented by newer implementations on `main`. Do not merge it wholesale.
- `Steam-PIpe` conflicts with the newer pipe overlay and frozen-decoration implementation. Recreate any useful steam behavior on current `main` rather than merging the old branch.
- `feature/kitchen-game-solid` is based on a stale tree and must not be merged wholesale.
- Old cutscene branches may show unique commits while their behavior is already present through independently rewritten commits. Compare feature intent and current code, not only ancestry or ahead counts.

## Proposed Direction, Not Yet Implemented

The review proposal recommends:

- one continuous three-phase Matter Kitchen experience;
- three small replacement Pipe Rescue boards with one graph validator;
- two polished State Lab experiments first;
- open card choice with sequential progress inside each card;
- a central progress/stage controller;
- explicit child-controlled Continue actions instead of automatic result transitions.

Do not begin this redesign until the proposal decisions are approved. Small correctness fixes and validation work can proceed independently.
