# Stage Progress

`StageProgressService` is the runtime owner for local stage progress. It starts
automatically before the first scene, persists to PlayerPrefs, and sends full
progress snapshots to the website through the active WebGL template.

## Stable IDs

Current completion hooks use these persisted keys:

- `matter-kitchen/melt-chocolate`
- `matter-kitchen/pour-juice`
- `matter-kitchen/freeze-juice`
- `pipe-rescue/freeze-a-plug`
- `state-lab/melt-wax`
- `state-lab/ionize-gas`

Do not rename an ID when a scene, card title, or display label changes. Add new
IDs to `StageProgressIds` and explicitly map new State Lab devices before they
can record progress.

## Website Payload

`StageProgressBridge.jslib` calls the WebGL template's
`window.postUnityProgress(payload)` function. The template owns the
same-origin `unity-progress` message envelope.

Every stage start sends the current `completedStageIds` snapshot. A newly
completed stage additionally sends `stageCompleted`, including attempts and an
UTC completion timestamp. Replaying an already completed stage re-sends the
snapshot without producing another completion event.

When every stage in `StageProgressIds.Activities` is complete, the final result
screen offers `TAKE THE QUIZ`. That explicit action sends a full snapshot with
`gameCompleted: true`, after the player has seen the final cutscene, narration,
and recap. The signal is persisted as reported so replays and reloads cannot
emit it twice. Ordinary progress snapshots never finish the game, including
snapshots from completed saves created before this field existed.
After the signal has been reported, replay result screens return to the
activity selector instead of displaying an inactive quiz action.

The website contract remains backward compatible with Penguin Run's numeric
level payload. Its source documentation is `docs/game-progress-bridge.md` in
the website repository.

## Current Limits

- The selector still uses serialized scene names and does not query progress.
- Website saves are outbound-only; a saved website snapshot is not yet loaded
  back into Unity on another browser or device.
- Pipe completion is recorded only by `FrozenFlowValidator`, the stricter of
  the two current pipe success checks.
