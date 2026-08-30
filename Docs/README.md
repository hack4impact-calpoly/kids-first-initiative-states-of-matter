# States of Matter — documentation

Unity WebGL learning game. Children move matter between solid, liquid, and gas across three
activities: Matter Kitchen, Pipe Rescue, and State Lab.

| Document | Covers |
| --- | --- |
| [stage-progress.md](./stage-progress.md) | How stage completion is tracked and reported to the website. Read this before changing any stage ID. |
| [game-experience-redesign-proposal.md](./game-experience-redesign-proposal.md) | The design direction for the three activities, with an implementation status showing what is built and what is still proposed. |

Repository conventions for agents and contributors are in [`AGENTS.md`](../AGENTS.md) at the root,
including worktree safety notes that matter before switching branches.

## Related

The website that embeds this game documents the other side of the contract:

- `docs/handbook.md` in `kids-first-initiative-site` — how the whole platform fits together
- `docs/game-progress-bridge.md` there — the progress payload this game sends

The sibling game, Penguin Run, follows the same visual guidance approach described in the redesign
proposal. Its `AttentionHighlight` component is a direct port of this repository's, so a child moving
between the two games meets one consistent idea: a thing that glows is a thing you can use.
