# Wire Scene - Device Connection Implementation Plan

## Overview
Add a snap-to-connect system where players drag a hot plate or plasma tube into a snap zone on the right side of the wire board. The wire puzzle is locked until a device is snapped in. Devices can be swapped freely.

## Layout
```
                    [Wire Board]---( Snap Zone )

              [Hot Plate]    [Plasma Tube]
```

## New Scripts

### 1. `Assets/Scripts/DraggableDevice.cs`
- Uses `OnMouseDrag` / `OnMouseUp` (consistent with Wire.cs pattern)
- Requires a `Collider2D` and `SpriteRenderer` on the GameObject
- On drag: follow mouse position (world coords)
- On release: check if overlapping a `SnapZone` collider (using `Physics2D.OverlapCircleAll`)
  - If yes → snap to zone's transform position, notify Main
  - If no → return to original start position
- Tracks whether currently snapped (so it can be pulled out)
- When pulled out of snap zone → notify Main to re-lock wires

### 2. `Assets/Scripts/SnapZone.cs`
- Attached to the snap area `()` GameObject
- Has a `Collider2D` trigger and a tag or layer to identify it
- Tracks the currently snapped device (reference)
- Public methods: `Snap(DraggableDevice)`, `Unsnap()`
- Visual feedback: could change sprite/color when a device is snapped

### 3. Modify `Assets/Scripts/Main.cs`
- Add `bool isLocked = true` — controls whether wires are interactive
- Add `public SnapZone snapZone` reference
- Add `public void DeviceConnected()` — called when device snaps in, sets `isLocked = false`
- Add `public void DeviceDisconnected()` — called when device removed, sets `isLocked = true`, resets wire connections if any were made
- Existing `LightOn()` method unchanged, but win condition now triggers the state transition

### 4. Modify `Assets/Scripts/Wire.cs`
- At the top of `OnMouseDrag()`: check `Main.Instance.isLocked` — if true, return early (don't allow dragging)

## New Scene Objects (added to Wire.unity)

### ConnectionWire (static sprite)
- Position: right of the wire board, connecting board to snap zone
- Just a SpriteRenderer, no script — purely visual `---`

### SnapZone
- Position: right end of the connection wire
- Components: `SpriteRenderer` (placeholder visual), `BoxCollider2D` (trigger), `SnapZone.cs`

### HotPlate
- Position: below the wire board (left)
- Components: `SpriteRenderer`, `BoxCollider2D`, `DraggableDevice.cs`

### PlasmaTube
- Position: below the wire board (right)
- Components: `SpriteRenderer`, `BoxCollider2D`, `DraggableDevice.cs`

## Implementation Order
1. Create `SnapZone.cs`
2. Create `DraggableDevice.cs`
3. Modify `Main.cs` — add lock state + device connection methods
4. Modify `Wire.cs` — add lock check
5. Add placeholder GameObjects to `Wire.unity` scene (user will replace sprites with real assets later)

## State Transition (on win)
When all 4 wires are connected with a device snapped in, trigger a visual state transition (details TBD — could be animation, color change, or enabling a new set of objects). For now, we'll fire an event/callback that can be hooked up later.
