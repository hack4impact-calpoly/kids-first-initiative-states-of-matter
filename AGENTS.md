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
- The shared flow layer from `Docs/game-experience-redesign-proposal.md` is implemented on top of the current scenes. The larger one-scene Kitchen and three-board Pipe Rescue rebuilds remain proposals.
- `StageProgressServiceTests` covers stage state, progress-aware routing, statuses, replay completion events, and one-shot full-game completion.

## Current Scene Routing

Runtime routing is owned by the flow layer in `Assets/Scripts/Flow` and `ActivityFlowCatalog`, not by the selector's old serialized scene-name listeners.

- `States of Matter Menu` opens `GameSelector` without an opening dialogue interruption.
- Matter Kitchen opens its first incomplete phase: Solid, Pour, then Freezing Station.
- Pipes opens `Pipes-Frozen-Level`; `Pipes game` remains disabled.
- State Lab opens `Wires` and tracks its two supported experiments separately.
- Complete cards open a Replay or Keep Exploring choice.
- Stable activity/stage IDs are persisted through `StageProgressService`; scene names are routing details only.

The runtime controllers are created by `ActivityFlowBootstrap` and guarded by the persistent `ActivityFlowRuntimeHost`. Test the shipped child path from the title or selector, then also load each activity scene directly to verify bootstrap coverage.

`ActivityFlowController` also creates a runtime `ChildVisualGuide`. It projects both UI `RectTransform` targets and world renderer/collider bounds into one overlay, then shows persistent rings, pointers, action labels, and animated drag paths. Update the guide target whenever a gameplay event changes the child's next action; do not rely on the four-second dialogue caption as the only instruction.

`SceneVisualPolishController` is also created at runtime before the scene-specific flow controller. It:

- expands orthographic gameplay cameras on narrow screens so world interactions remain visible;
- scales world-sprite and `Canvas/Image` backdrops independently to cover the viewport;
- gives the baked selector artwork a contained 4:3 presentation with a matching warm surround;
- normalizes ingredient trays, heat controls, the Lab power dial, clear colors, and legacy controls;
- removes the active Pipe scene's coordinate labels without changing its board geometry.

Do not serialize these runtime overlays into scenes. The selector's three illustrated cards are one baked background image; the button objects are only interaction overlays. The Pour backdrop is a canvas `Image`, while Solid, Freezing Station, Pipes, and Wires use world sprite renderers.

## Matter Kitchen: Current Mechanics

The educational arc still spans three scenes, presented as sequential phases under one Matter Kitchen card.

### Solid Scene

Scene: `Assets/Scenes/Kitchen Game - Solid.unity`

- Drag the chocolate UI item into the pot.
- `IngredientBarDragToWorld2D` creates the world ingredient.
- `MockPotController` registers an `IngredientInstance` through 2D collision or trigger callbacks.
- Raise the `HeatSlider` to maximum.
- `KitchenGameManager` wins when the required ingredient is present and maximum heat has been reached.
- Heating before adding the ingredient can fail the stage.
- Completion shows an experiment-specific recap with Continue to the pouring phase or Activities.

### Freezing Pour Scene

Scene: `Assets/Scenes/Kitchen Game - Freezing Pour.unity`

- Drag and tilt the juice bottle over the tray.
- `JuicePouring` rotates after pointer down and emits `JuiceDroplet` objects while dragging.
- The bottle's spawn point, not its center, must be above the tray.
- `IceTray` fills in droplet increments.
- `JuiceFreezingManager` records completion and publishes its presentation event, but waits for the shared Continue action before loading the station.

### Freezing Station Scene

Scene: `Assets/Scenes/Kitchen Game - Freezing Station.unity`

- Drag the juice mold into the freezer/snap area.
- `MockPotController` detects the placed ingredient.
- Lower the temperature slider to its minimum/cold region.
- `JuicePouringGameManager` completes when an ingredient is present and `JuiceCoolingController.IsColdEnough` is true.
- Completion shows `Juice Frozen!` with Next Activity or Activities. It does not leave automatically.

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
- Clicking a pipe invokes `FreezeOnClick`, toggles `PipeObject.isFrozen`, and recalculates water. It does not validate success on every click.
- The four marked sink coordinates are `(2, 3)`, `(4, 4)`, `(4, 2)`, and `(8, 2)`. Runtime guidance advances to the next unfrozen sink.
- `StartButtonHandler` labels the action `TEST ROUTE` and delegates the active level to `FrozenFlowValidator`.
- Validation accepts a wet path to `(8, 4)`, external board terminals, and openings sealed by adjacent frozen plugs while rejecting unmatched internal wet openings.
- Failed tests keep Test Route available. Success records progress, emits one completion event, and shows `Water Delivered!` with explicit progression.

The current large board is now completable, but the proposed replacement with three smaller teaching boards is still pending.

## State Lab / Wires: Current Mechanics

Scene: `Assets/Scenes/Wires.unity`

This is currently the most complete end-to-end activity.

1. Drag either `HotPlate` or `Plasma Tube` into `SnapZone`.
2. `SnapZone.Snap()` calls `Main.DeviceConnected()` and unlocks wire interaction.
3. Match four wire pairs. Matching is determined by the parent object name, such as `Wire Green`, not by sampled color values.
4. `Main.LightOn(1)` increments the connected count.
5. The power slider is gated until an output is placed and all required wires are connected.
6. Raising power to the threshold triggers the selected device effect and win flow.

The power gate correctly resets premature power attempts and provides guidance. The shared header provides Activities, Hint, Restart, progress dots, and Undo. Completion shows an experiment-specific recap and either Continue to another unfinished experiment or Activities.

`PowerDialController` may parent its generated `Power Dial` under the first overlay canvas it finds, including the child-guide canvas. Find the active dial by object name or component, not by the old `Canvas/Power Dial` path.

Dialogue references more possible experiments than are visibly polished in the current scene. Inspect actual scene objects before assuming HotPlate, IceFlask, Candle, and Plasma are all separately available.

## Dialogue and Cutscene Behavior

- Gameplay adapters derive from `DialogueFlowAdapterBase`.
- Runtime adapters can create or locate a shared `DialogueFlowController` and register default lines.
- Hint replay must use `ReplayFlowNow()`. Default prompt flows are `playOnce`, so calling `TryPlayFlowNow()` directly after the first play silently rejects the replay.
- Default gameplay prompts use `promptAutoAdvanceDelay = 4` seconds.
- Voice lines may keep a line active until audio completes.
- `DialogueWaitUtility.WaitUntilIdle()` waits for both active and queued dialogue.
- Shared completion panels wait for dialogue to become idle before appearing. Kitchen phase managers default to explicit Continue and do not auto-load the next scene.
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
- When Unity is unfocused, set `Application.runInBackground = true` for multi-scene screenshot runs; otherwise captures can repeatedly return the last rendered frame even though runtime objects have changed.
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

`manage_camera(action="screenshot")` can write into `Temp/FlowCaptures` without creating tracked assets. If captures are written under `Assets`, delete both PNG and `.meta` files after diagnostics.

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

## Implemented Flow and Pending Redesign

Implemented on the current scene architecture:

- open card choice with sequential progress inside each activity;
- progress-aware selector cards and replay choices;
- a compact shared objective/control shell;
- persistent child-facing tap, press, drag, matching, and slider guidance;
- repeatable current-step Hint playback that also re-emphasizes the visual target;
- experiment-specific result recaps and child-controlled progression;
- explicit kitchen transitions, repeatable pipe testing, and one active pipe validator.

Still pending from the larger proposal:

- combining Matter Kitchen into one continuous scene;
- replacing the large frozen pipe board with three smaller boards;
- adding socket shape symbols and further State Lab content polish;
- molecule cutaways and deeper per-phase visual transformations.

Do not describe the structural redesign as complete merely because the shared flow layer is present. See the implementation-status note at the top of `Docs/game-experience-redesign-proposal.md`.
