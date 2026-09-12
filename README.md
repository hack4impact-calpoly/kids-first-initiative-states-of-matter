# States of Matter

A Kids First Initiative Unity game about heating, cooling, and changes of state. Children explore
Matter Kitchen, Pipe Rescue, and State Lab. The website supplies classroom access, quizzes, and results.

## Open and run

Install Git, Git LFS, and Unity Hub with **Unity 6000.3.1f1** and its WebGL Build Support module.
The exact editor version is in `ProjectSettings/ProjectVersion.txt`.

```sh
git lfs install
git clone https://github.com/hack4impact-calpoly/kids-first-initiative-states-of-matter.git
cd kids-first-initiative-states-of-matter
git lfs pull
```

Add this folder in Unity Hub, open it, and let assets finish importing. Open
`Assets/Scenes/States of Matter Menu.unity` and press Play; verify navigation through `GameSelector`.
Work from `main`. Preserve `.meta` files and read [AGENTS.md](AGENTS.md) before changing scenes or flow.

## Where to work

| Area                                        | Entry point                                                  |
| ------------------------------------------- | ------------------------------------------------------------ |
| Runtime guidance and activity flow          | `Assets/Scripts/Flow/`                                       |
| Completion IDs and website bridge           | [Stage progress](Docs/stage-progress.md)                     |
| Current behavior / scene pitfalls           | [Repository guide](AGENTS.md)                                |
| Future redesign, with implementation status | [Design proposal](Docs/game-experience-redesign-proposal.md) |

`Pipes-Frozen-Level` is the active pipe scene; `Pipes game` is intentionally disabled. Keep persisted
stage IDs stable even when labels/scenes change. The flow layer is implemented; the proposed
single-scene Kitchen and three-board Pipe Rescue are not.

## Verify and release

Run the EditMode tests in Unity's Test Runner (`Assets/Tests/EditMode/`). Then play the title-to-quiz
path, failure/retry paths, and each activity opened directly. Check the Console and visible guidance.
Compilation alone does not verify scene wiring, audio, or touch input.

To publish, run the **website repository's** `build-unity-webgl` workflow for `states-of-matter`
at the reviewed source commit. Review and merge its website artifact PR, then verify the actual
WebGL game on the target device. Source merges alone do not update the website.

Platform guides: [partner use](https://github.com/hack4impact-calpoly/kids-first-initiative-site/blob/develop/docs/partner-guide.md),
[developer setup](https://github.com/hack4impact-calpoly/kids-first-initiative-site/blob/develop/docs/handbook.md),
[releases](https://github.com/hack4impact-calpoly/kids-first-initiative-site/blob/develop/docs/releases.md),
[ownership and sign-off](https://github.com/hack4impact-calpoly/kids-first-initiative-site/blob/develop/docs/handoff.md).
