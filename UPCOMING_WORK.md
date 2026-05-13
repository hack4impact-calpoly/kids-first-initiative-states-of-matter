# Upcoming Work

Local planning note. Do not commit unless the team explicitly decides this should become project documentation.

## State Change Cutscene Refactor

`StateChangeCutsceneAnimation.cs` is still flexible enough for the current work, but it is becoming too broad. It now owns view construction, copy, colors, particles, bonds, container shapes, stage timing, and all per-cutscene animation behavior. That is manageable for the current branch, but future cutscene variants will keep adding switch cases and special-case helpers.

Recommended direction:

- Keep `CutsceneManager` as the generic playback layer. It should continue handling overlay setup, camera movement, lifecycle, cleanup, and completion callbacks.
- Keep `StateChangeCutsceneAnimation` as a host/orchestrator, but move per-kind animation behavior into smaller strategy classes.
- Extract behavior classes such as:
  - `ChocolateMeltingCutsceneBehavior`
  - `LiquidFlowCutsceneBehavior`
  - `LiquidFreezingCutsceneBehavior`
  - `PipeFlowCutsceneBehavior`
  - `CircuitCandleMeltingCutsceneBehavior`
  - `CircuitPlasmaIonizingCutsceneBehavior`
- Each behavior should own its title/stage text, particle colors, optional view extras, and stage animation logic.
- Shared primitives should stay common: particle creation, bond rendering, simple containers, tube/container geometry helpers, bounce/clamp math, fade/timing helpers, and `CutsceneView` data.

Important behavior requirement:

- Some animations should interpolate particles into intentional positions, such as freezing into a solid lattice.
- Other animations should preserve current motion and continue naturally, such as melted wax flowing after bonds break.
- The refactor should make that distinction explicit per behavior instead of hiding it inside shared stage transitions.

Suggested migration plan:

1. Introduce an internal behavior interface for state-change cutscenes.
2. Move one low-risk existing kind first, likely `LiquidFreezing`, to prove the shape.
3. Move the wire-specific candle and plasma behaviors next, since they currently have the newest special cases.
4. Leave old switch branches in place while migrating one behavior at a time.
5. Delete migrated switch branches only after each kind is visually verified.

Avoid doing this in the middle of visual tuning unless the file starts blocking work. This is a good follow-up once the wire overhaul is approved.

## Dialogue System

Build a shared dialogue/prompt system instead of keeping prompt behavior scene-specific.

Known upcoming needs:

- Support the wire game prompt moving to the top of the screen.
- Support persistent prompts versus temporary messages.
- Support future Figma-driven styling and behavior.
- Keep scene/game logic focused on state changes, not text layout or display timing.

Details are intentionally thin for now. The next step is to review the Figma mocks and expected behavior, then define the component API and migration plan.
