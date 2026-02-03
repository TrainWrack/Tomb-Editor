# Parameter Mapping: Linking Inputs to Specific Arguments

## The Question

**"I have a node with 4 arguments, but I only want to allow one input. How do I define which argument/parameter is linked to the input?"**

## The Answer

### Input-to-Parameter Mapping by Name

The system maps inputs to function parameters **by matching the input name to the parameter name** in your function signature.

**Key Rule:** The input's `Name` field should match the function parameter name you want it to replace.

### Example: Selective Parameter Linking

Let's say you have a function with 4 parameters but only want to make one of them linkable:

```lua
-- !Name "Complex operation"
-- !Section "Example"
-- !Description "Demonstrates selective parameter linking"
-- 
-- ONLY the 'targetPosition' parameter is linkable via input
-- !Inputs "targetPosition, Vector3, Position can be linked from other nodes"
-- 
-- All 4 parameters have manual argument UI controls
-- !Arguments "NewLine, Vector3, [ -1000 | 1000 ], 25, Target position"
-- !Arguments "Numerical, [ 0 | 100 ], 25, Speed value"
-- !Arguments "Boolean, 25, Enable flag"
-- !Arguments "Moveables, 25, Target object"

LevelFuncs.Engine.Node.ComplexOperation = function(targetPosition, speed, enabled, targetObject)
    -- targetPosition: Can be linked OR manual (has input + argument)
    -- speed: Manual only (argument only, no input)
    -- enabled: Manual only (argument only, no input)
    -- targetObject: Manual only (argument only, no input)
    
    local moveable = TEN.Objects.GetMoveableByName(targetObject)
    if enabled then
        moveable:MoveTo(targetPosition, speed)
    end
end
```

### How It Works

**Parameter Resolution:**

1. **targetPosition** (1st parameter):
   - Has input: `"targetPosition, Vector3, ..."`
   - Has argument: `Vector3` argument
   - **Resolution**: If input is linked → uses linked value; if not → uses argument value

2. **speed** (2nd parameter):
   - NO input defined
   - Has argument: `Numerical` argument
   - **Resolution**: Always uses argument value (not linkable)

3. **enabled** (3rd parameter):
   - NO input defined
   - Has argument: `Boolean` argument
   - **Resolution**: Always uses argument value (not linkable)

4. **targetObject** (4th parameter):
   - NO input defined
   - Has argument: `Moveables` argument
   - **Resolution**: Always uses argument value (not linkable)

### Visual Representation

```
Function Signature:
function(targetPosition, speed, enabled, targetObject)
           ^              ^      ^        ^
           |              |      |        |
           |              |      |        +-- Argument only (not linkable)
           |              |      +----------- Argument only (not linkable)
           |              +------------------ Argument only (not linkable)
           +--------------------------------- Input OR Argument (linkable!)
```

### Important Naming Rule

**The input name MUST match the function parameter name:**

```lua
-- ✅ CORRECT - Names match
-- !Inputs "targetPosition, Vector3, Description"
function(targetPosition, speed, enabled, targetObject)
         ^^^^^^^^^^^^^^^
         Names match! ✓

-- ❌ WRONG - Names don't match
-- !Inputs "newPosition, Vector3, Description"
function(targetPosition, speed, enabled, targetObject)
         ^^^^^^^^^^^^^^^
         Input name doesn't match parameter name!
         System won't know which parameter to link
```

## More Examples

### Example 1: Link Only the Value, Not the Object

```lua
-- !Name "Modify health"
-- !Inputs "healthAmount, Numerical, Health value to set"
-- !Arguments "NewLine, Numerical, [ 0 | 1000 ], 50, Health value"
-- !Arguments "Moveables, 50, Target object"

LevelFuncs.Engine.Node.ModifyHealth = function(healthAmount, targetObject)
    -- healthAmount: Linkable (can receive from other nodes)
    -- targetObject: Manual selection only (not linkable)
    local moveable = TEN.Objects.GetMoveableByName(targetObject)
    moveable:SetHP(healthAmount)
end
```

**Use Case:** You want to link a calculated health value from another node, but always manually select which object to apply it to.

### Example 2: Link the Target, Not the Value

```lua
-- !Name "Apply damage"
-- !Inputs "targetObject, Moveables, Target to damage"
-- !Arguments "NewLine, Numerical, [ 0 | 1000 ], 50, Damage amount"
-- !Arguments "Moveables, 50, Target (can be linked)"

LevelFuncs.Engine.Node.ApplyDamage = function(damageAmount, targetObject)
    -- damageAmount: Manual value only (not linkable)
    -- targetObject: Linkable (can receive from node that outputs moveable name)
    local moveable = TEN.Objects.GetMoveableByName(targetObject)
    moveable:DealDamage(damageAmount)
end
```

**Use Case:** Fixed damage amount, but target can be determined dynamically by another node.

### Example 3: Multiple Inputs for Some Parameters

```lua
-- !Name "Advanced transform"
-- !Inputs "newPosition, Vector3, Position to set" "newRotation, Vector3, Rotation to set"
-- !Arguments "NewLine, Vector3, 33, Position"
-- !Arguments "Vector3, 33, Rotation"
-- !Arguments "Numerical, 33, Scale"
-- !Arguments "NewLine, Moveables, 100, Target"

LevelFuncs.Engine.Node.AdvancedTransform = function(newPosition, newRotation, scale, target)
    -- newPosition: Linkable (has input)
    -- newRotation: Linkable (has input)
    -- scale: Manual only (no input)
    -- target: Manual only (no input)
    
    local moveable = TEN.Objects.GetMoveableByName(target)
    moveable:SetPosition(newPosition)
    moveable:SetRotation(newRotation)
    moveable:SetScale(scale)
end
```

**Use Case:** Position and rotation can be dynamic from other nodes, but scale and target are always manual.

## Argument Order Matters

**The order of !Arguments must match the function parameter order:**

```lua
-- Function parameters (left to right):
function(param1, param2, param3, param4)

-- Arguments must be in same order:
-- !Arguments "...", "First argument (for param1)"
-- !Arguments "...", "Second argument (for param2)"
-- !Arguments "...", "Third argument (for param3)"
-- !Arguments "...", "Fourth argument (for param4)"
```

**Inputs don't need to be in the same order** - they match by name, not position:

```lua
-- These work the same:
-- !Inputs "param1, Type1, Desc1" "param3, Type3, Desc3"
-- !Inputs "param3, Type3, Desc3" "param1, Type1, Desc1"

-- Both will correctly map:
-- param1 → linkable
-- param2 → argument only
-- param3 → linkable
-- param4 → argument only
```

## Design Patterns

### Pattern 1: Value Input, Target Manual

**When to use:** Value should be dynamic, but user always picks the target.

```lua
-- !Inputs "value, Type, Dynamic value"
-- !Arguments "NewLine, Type, Value" "Target, Target to apply to"
function(value, target)
```

**Example:** Damage from a calculation node, applied to manually selected enemy.

### Pattern 2: Target Input, Value Manual

**When to use:** Target is dynamic, but value is fixed/configured.

```lua
-- !Inputs "target, Type, Dynamic target"
-- !Arguments "NewLine, Type, Fixed value" "Type, Target"
function(value, target)
```

**Example:** Fixed heal amount, applied to player or ally determined by game state.

### Pattern 3: Mixed Linkable Parameters

**When to use:** Some data comes from calculations, some from manual config.

```lua
-- !Inputs "calculatedValue, Type, From other node" "dynamicTarget, Type, From other node"
-- !Arguments "NewLine, Type, Calc value" "Type, Manual setting" "Type, Dynamic target"
function(calculatedValue, manualSetting, dynamicTarget)
```

**Example:** Calculated position + manual speed + dynamic target.

## Summary

### Quick Rules

1. **Match input name to parameter name**: `!Inputs "myParam, ..."` maps to `function(myParam, ...)`
2. **Not all parameters need inputs**: Only define inputs for linkable parameters
3. **All parameters need arguments**: Provide arguments for all parameters (they're the fallback)
4. **Arguments order = parameter order**: Arguments must be in same order as function parameters
5. **Inputs order doesn't matter**: Inputs match by name, not position

### The Binding Table

| What You Want | Input Needed? | Argument Needed? | Behavior |
|---------------|---------------|------------------|----------|
| **Always linkable** | ✅ Yes | ❌ No (but recommended) | Must be linked to work |
| **Optionally linkable** | ✅ Yes | ✅ Yes | Linked if connected, else argument |
| **Never linkable** | ❌ No | ✅ Yes | Always uses argument value |

### Best Practice

**Always provide arguments for ALL parameters, even linkable ones:**
- Arguments show default/fallback values
- Node works standalone without links
- Better UX: users see what's needed
- Easier debugging: can unlink and test manually

## See Also

- `FAQ_INPUT_LINKING.md` - How input vs argument priority works
- `Sample Input-Output Nodes.lua` - Working examples
- `Readme.md` - Complete metadata syntax reference
