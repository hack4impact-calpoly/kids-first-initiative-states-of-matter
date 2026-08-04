# States of Matter Game Experience Redesign

## Implementation Status - 2026-07-24

The shared experience flow is implemented on the current scene architecture:

- stable activity and stage IDs with progress-aware selector routing;
- New, In Progress, and Complete card states with progress dots;
- Replay or Keep Exploring for completed cards;
- a shared activity header with objective, Activities, Hint, Restart, and contextual Undo;
- persistent visual action guides with target rings, pointers, short action labels, and animated drag paths;
- repeatable Hint playback for the current step, including visual re-emphasis;
- explicit result recaps and child-controlled progression for all active stages;
- sequential Matter Kitchen routing across the existing three scenes;
- repeatable Test Route validation and a completable frozen-pipe board;
- experiment-specific State Lab result titles and progress;
- responsive 16:9 and 4:3 framing without exposed Unity clear-color bars;
- cohesive ingredient trays, heat and power controls, auto-sizing labels, title controls, and selector cards.

The structural redesign remains proposed, not implemented: one-scene Matter Kitchen, three replacement Pipe Rescue boards, molecule cutaways, socket symbols, and remaining content-specific presentation polish. The implementation sequence below should be read as original design guidance plus this status note, not as a claim that all phases are complete.

## Purpose

Turn the current collection of scenes into three short, coherent learning experiences that children can understand without adult explanation:

1. Matter Kitchen: heat and cold change matter between solid and liquid.
2. Pipe Rescue: liquid follows connected paths, while freezing can create a solid plug.
3. State Lab: electrical energy can cause different state changes.

The games should feel related, but each card should still be playable independently. Progress within a card should be sequential; access to the three cards should remain open so a child can choose what interests them.

## Experience Principles

- Show the goal within five seconds of entering a stage.
- Teach one new interaction at a time.
- Keep instructions to one spoken and captioned sentence.
- Use visible cause and effect before introducing scientific vocabulary.
- Never remove the retry action after a failed attempt.
- Treat failure as an experiment: show what happened and let the child change one thing.
- Use shape, symbols, animation, and text in addition to color.
- End every stage with an explicit result and a child-controlled Continue action.

## Recommended Structure

### Card 1: Matter Kitchen

Make Matter Kitchen one continuous scene with three phases instead of three separately loaded scenes.

#### Phase A: Melt Chocolate

Goal: demonstrate solid to liquid.

1. Sam identifies the chocolate as a solid because it keeps its shape.
2. The chocolate pulses once to show that it is draggable.
3. The child drags it into the pot.
4. The heat control unlocks and receives a short highlight.
5. Raising the heat gradually changes the chocolate sprite and viscosity.
6. A short molecule cutaway shows particles loosening and moving faster.

Success statement: "Heat melted solid chocolate into liquid chocolate."

#### Phase B: Pour Juice

Goal: demonstrate that a liquid flows and takes the shape of its container.

1. The pot slides away and the juice bottle and mold enter the same workspace.
2. The child tilts and positions the bottle above the mold.
3. The fill level responds continuously, with forgiving collision bounds.
4. Spilled drops return to the bottle rather than causing failure.

Success statement: "The liquid flowed and took the shape of the mold."

#### Phase C: Freeze the Juice

Goal: demonstrate liquid to solid.

1. The filled mold becomes the draggable object.
2. The child places it into the freezer.
3. The temperature control unlocks.
4. Lowering the temperature changes the juice from moving liquid to fixed ice pops.
5. A short molecule cutaway shows particles slowing and locking together.

Success statement: "Cooling froze the liquid into a solid."

After completion, show `Replay Kitchen` and `Back to Activities`. Do not automatically leave while dialogue or the result is visible.

## Pipe Rescue: Complete Redesign

The current pipe scenes should not be repaired in place as one large board. The basic scene has no configured source, endpoint, or rotation interactions, and the frozen scene combines two incompatible validators. Rebuild the experience around three small boards using the new pipe art and flow effects.

### Core Story

Lucy needs water delivered from a tank on the left to a garden on the right. The pipe route has been mixed up, and later boards introduce leaks that can be sealed by freezing water into solid ice.

### Interaction Model

- Use a 5-by-4 board, not the current 8-by-4 board.
- Mark the source with a water tank and the destination with the garden.
- Clicking a rotatable pipe turns it 90 degrees.
- A persistent valve button runs the water test.
- The valve remains available after every failure.
- During a test, water visibly stops at a disconnected joint or sprays from an open leak.
- The failed location pulses after the test, but the solution is not automatically selected.
- A reset icon restores only the current board.

### Board 1: Connect the Path

Learning goal: liquid water flows through connected openings.

- Four or five movable pieces.
- No branches and no decoy pipes.
- The first rotation demonstrates immediate pipe movement.
- The child runs the valve and watches water reach the garden.

Target duration: 30-45 seconds.

### Board 2: Find the Leak

Learning goal: an open branch lets liquid escape.

- Introduce one T-junction and one visible leaking branch.
- The main route is otherwise connected.
- Running the valve shows water reaching the branch and spraying out.
- The child rotates one downstream piece to remove the open path.

Target duration: 45-60 seconds.

### Board 3: Freeze a Plug

Learning goal: frozen water is solid and can block a liquid path.

- The leaking branch cannot be solved by rotation alone.
- A Freeze tool appears only after the first failed water test.
- Selecting Freeze changes the cursor and highlights freezable wet pipe ends.
- Freezing the leaking end creates a visible ice plug, not an entirely disconnected pipe.
- Running the valve again sends liquid past the sealed branch to the garden.

Success statement: "The liquid flowed through the pipe, and solid ice blocked the leak."

Target duration: 60-90 seconds.

### Pipe Rules and Validation

Use one graph validator for all three boards.

- A pipe opening is valid when it touches a matching neighboring opening.
- The destination must be reachable from the source.
- Every wet opening must connect to another wet pipe, the source, the destination, or a sealed ice plug.
- A frozen plug is a sealed boundary, not a disconnected dry neighbor.
- Only the valve action evaluates success or failure.
- A failed test never hides or disables the valve.
- Success fires one completion event, plays one result sequence, records progress, and waits for Continue.

### Pipe Presentation

- Remove coordinate labels.
- Keep the entire board within the safe area at 16:9 and common tablet ratios.
- Give source, destination, dry, wet, leaking, and frozen states distinct silhouettes.
- Use a short water test rather than running the full board continuously.
- Replace the center text button with a valve icon positioned beside the source.
- Add an audio replay button for the current instruction.

## Card 3: State Lab

Keep the existing interaction sequence because it already works well:

1. Choose an output experiment.
2. Place it in the dashed station.
3. Match the four wire connections.
4. Turn on power.
5. Observe and name the state change.

For the first polished release, ship two clearly differentiated experiments instead of partially supporting four:

- Candle: heat melts solid wax into liquid wax.
- Plasma tube: electrical energy ionizes gas into plasma.

Add a shape symbol to each colored socket so matching is not color-only. Show `0/4`, `1/4`, and so on near the wire board. Rename the final result for the chosen experiment instead of using the generic phrase "wire game."

After completion, offer `Try Another Experiment` and `Back to Activities`.

## Unified Progression Proposal

### Selector Behavior

The selector should no longer contain hard-coded scene names on button events. Each card asks a central progress service for its next stage.

- New: open the first stage.
- In progress: resume the first incomplete stage.
- Complete: open a small choice panel with Replay or Continue Exploring.

All three cards remain selectable. A child is not forced through one global order.

Each card displays:

- activity name;
- one-line learning theme;
- status: New, In Progress, or Complete;
- two or three progress dots for internal phases.

### Shared Stage Contract

Every stage should report the same lifecycle events:

- `StageStarted`
- `ObjectiveChanged`
- `AttemptFailed`
- `StageCompleted`
- `ContinueRequested`

Only the central stage controller may save progress or change scenes. Individual gameplay components report outcomes but do not call `SceneManager.LoadScene` directly.

### Save Model

Store stable activity and stage IDs rather than scene names. A minimal save model needs:

- highest completed Matter Kitchen phase;
- highest completed Pipe Rescue board;
- completed State Lab experiments;
- audio/subtitle preferences.

Provide a teacher/parent reset action outside the primary child workflow.

### Shared UI Shell

Every activity uses the same compact controls:

- back to activities;
- restart current stage;
- replay instruction audio;
- current phase dots;
- captions.

Use icons with tooltips or accessible labels, and keep destructive/reset actions away from the main interaction area.

### Dialogue and Results

- Opening instruction remains visible for at least four seconds and until its voice line finishes.
- Action feedback may queue behind the current line, but repeated hints should replace older hints rather than create a backlog.
- Completion dialogue must finish before Continue becomes active.
- Scene changes happen only after the child selects Continue.
- Scientific vocabulary appears after the visual result: first "the chocolate became runny," then "this change is melting."

## Implementation Sequence

### Phase 0: Stabilize

- Repair missing script references and require a zero-error console.
- Add edit-mode validation for missing prefab scripts and broken scene component references.
- Keep the current selector available while replacement stages are developed.

### Phase 1: Shared Progression

- Add activity/stage definitions with stable IDs.
- Add the central progress service and selector routing.
- Add the shared stage completion panel and explicit Continue action.

### Phase 2: Pipe Rescue

- Build the single graph validator and reusable board definition.
- Implement the three small boards.
- Retire the two current pipe scenes after feature parity is verified.

### Phase 3: Matter Kitchen

- Combine the three existing phases into one scene controller.
- Reuse the current art, drag interactions, temperature control, dialogue, and cutscenes.
- Remove direct scene transitions from the phase managers.

### Phase 4: State Lab Polish

- Add socket symbols and wire progress.
- Clarify the two launch experiments.
- Add experiment-specific completion and Try Another Experiment.

## Acceptance Criteria

- A new player understands each stage goal without external instruction.
- Every stage can be failed and retried without reloading the application.
- No interaction can permanently remove the only completion control.
- Each activity records and resumes progress correctly.
- Dialogue and result screens never disappear because of an automatic scene transition.
- Every supported resolution keeps controls, board, captions, and result text onscreen.
- Unity compilation and console are clean during one complete run of every activity.

## Decisions Requested

1. Keep all three cards open, or enforce a global order? Recommendation: keep them open.
2. Combine Matter Kitchen into one scene, or preserve three scenes behind one controller? Recommendation: one scene.
3. Replace the current pipe scenes with three smaller boards, or salvage the existing large frozen board? Recommendation: replace them.
4. Launch State Lab with two polished experiments or finish all four referenced by dialogue? Recommendation: two polished experiments first.
