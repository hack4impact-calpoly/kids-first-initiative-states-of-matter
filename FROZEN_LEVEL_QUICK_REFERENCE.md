# Quick Reference Guide - Frozen Level

## 🎮 Core Components

### FreezeOnClick.cs
**What it does**: Makes pipes freezable on click
**Add to**: Every pipe GameObject
**Inspector Properties**:
- `frozenSprite` (optional): Sprite to show when frozen

### FrozenLevelHandler.cs
**What it does**: Checks win/lose conditions
**Add to**: GameManager or similar GameObject
**Inspector Properties**:
- `ui`: PipeUIController reference
- `startButton`: Start button GameObject

### FrozenPipeVisuals.cs (Optional)
**What it does**: Enhanced visual effects
**Add to**: Pipes for extra polish
**Inspector Properties**:
- `frozenParticles`: Particle system for ice effect
- `frozenOverlay`: Ice overlay sprite
- `frozenScale`: Size when frozen (default: 1.05)

### FrozenLevelDebugger.cs (Optional)
**What it does**: Testing and debugging tools
**Add to**: GameManager
**Keyboard Shortcuts**:
- `F`: Freeze all pipes
- `U`: Unfreeze all pipes
- `R`: Recalculate water
- `P`: Print status

---

## 🔧 Quick Setup (5 Steps)

### Step 1: Prepare Pipes
For each pipe GameObject:
```
1. Add: StraightConnection/CornerConnection/etc.
2. Add: FreezeOnClick
3. Set: xPos, yPos
4. Set: drySprite, waterSprite
5. Ensure: Has Collider2D
```

### Step 2: Set Source & End
- Source: Pipe at position (1, 4)
- End: Pipe at position (8, 1)

### Step 3: Create Game Manager
```
1. Create empty GameObject → name it "GameManager"
2. Add FrozenLevelHandler component
3. Assign ui reference
4. Assign startButton reference
```

### Step 4: Setup UI Button
```
1. Select Start Button
2. Inspector → Button component → OnClick()
3. Add FrozenLevelHandler.OnStartPressed()
```

### Step 5: Test
- Play scene
- Click pipes to freeze/unfreeze
- Click Start to check win/lose

---

## 🎯 How Freeze Behavior Works

### Normal State (Unfrozen)
- Water flows through pipe
- Connections: Based on pipe type
- Visual: Normal sprite or water sprite
- Click: Freezes the pipe

### Frozen State
- Water blocked (no flow through)
- Connections: All set to false
- Visual: Blue tint or frozen sprite
- Click: Unfreezes the pipe

### Protected Pipes
- Source pipe: Cannot freeze
- End pipe: Cannot freeze

---

## ✅ Testing Checklist

- [ ] Pipes are stationary (don't rotate)
- [ ] Clicking pipe freezes it (turns blue)
- [ ] Clicking again unfreezes it
- [ ] Source pipe cannot freeze
- [ ] End pipe cannot freeze
- [ ] Water recalculates after freeze/unfreeze
- [ ] Start button checks win condition
- [ ] Success UI shows when water reaches end
- [ ] Failure UI shows when water doesn't reach end

---

## 🐛 Common Issues & Fixes

### Issue: Pipes won't freeze when clicked
**Fix**: Add Collider2D component to pipe GameObject

### Issue: Pipes rotate instead of freeze
**Fix**: Remove or disable `RotateOnClick` component, ensure `FreezeOnClick` is attached

### Issue: Water doesn't flow
**Fix**: 
- Check source is at (1, 4)
- Verify sprites are assigned
- Call ValidateLevelSetup() from debugger

### Issue: Start button doesn't work
**Fix**:
- Verify FrozenLevelHandler is in scene
- Check ui and startButton are assigned
- Ensure OnClick event is connected

### Issue: No visual feedback when freezing
**Fix**:
- Assign frozenSprite in FreezeOnClick
- OR allow default blue tint
- OR add FrozenPipeVisuals for effects

---

## 🎨 Visual Customization

### Basic (Blue Tint)
No setup needed - automatic when frozenSprite not assigned

### Custom Sprite
1. Create ice/frozen version of pipe sprite
2. Assign to `FreezeOnClick.frozenSprite`

### Advanced (Particles)
1. Create Particle System
2. Add FrozenPipeVisuals component
3. Assign particle system to `frozenParticles`

---

## 💡 Level Design Tips

### Easy Puzzle
- Clear main path
- 1-2 obvious pipes to freeze
- Multiple solutions OK

### Medium Puzzle
- Branching paths
- 3-4 pipes to freeze
- Some trial and error

### Hard Puzzle
- Complex pipe layout
- 5+ pipes to freeze
- Specific order required
- Only one solution

---

## 📝 Code Reference

### Check if pipe is frozen
```csharp
FreezeOnClick freezer = pipe.GetComponent<FreezeOnClick>();
bool isFrozen = freezer.IsFrozen();
```

### Freeze a pipe programmatically
```csharp
freezer.SendMessage("OnMouseDown"); // Toggle freeze state
```

### Check win condition
```csharp
FrozenLevelHandler handler = FindObjectOfType<FrozenLevelHandler>();
handler.OnStartPressed();
```

### Reset level
```csharp
handler.ResetLevel();
```

---

## 🚀 Next Steps

1. **Open** `Pipes-Frozen-Level.unity`
2. **Follow** Quick Setup (5 steps above)
3. **Test** in Play mode
4. **Iterate** on puzzle design
5. **Polish** with visual effects

---

## 📚 Additional Resources

- Full documentation: `FROZEN_LEVEL_README.md`
- Implementation details: `FROZEN_LEVEL_IMPLEMENTATION.md`
- Debug tools: Add `FrozenLevelDebugger` component

---

**Need Help?**
- Check console for debug messages
- Use FrozenLevelDebugger.ValidateLevelSetup()
- Review inline code comments
