# FAQ: Input/Output Linking - How It Works

## Common Questions About Input Linking

### Q: How does the game know which value to use - the linked input or the manual argument?

**A: Linked inputs take PRECEDENCE over manual arguments. The system follows this priority:**

1. **First**: Check if input is linked → Use linked value
2. **Fallback**: If not linked → Use manual argument value

This allows you to:
- Define default values via arguments (always visible in UI)
- Override them by linking to another node's output
- See which inputs are linked vs using manual values

---

## Example: Understanding the Priority System

### The Node Definition

```lua
-- !Name "Set moveable transform"
-- !Inputs "newPosition, Vector3, Position to set (can be linked)" "newRotation, Vector3, Rotation to set (can be linked)"
-- !Arguments "NewLine, Vector3, [ -1000000 | 1000000 | 0 | 1 | 32 ], 50, Position value"
-- !Arguments "Vector3, [ -360 | 360 | 0 | 1 | 1 ], 50, Rotation value"
-- !Arguments "NewLine, Moveables, 100, Target moveable"

LevelFuncs.Engine.Node.SetMoveableTransform = function(positionValue, rotationValue, moveableName)
    local moveable = TEN.Objects.GetMoveableByName(moveableName)
    moveable:SetPosition(positionValue)
    moveable:SetRotation(rotationValue)
end
```

### What This Means

**When Input "newPosition" IS Linked:**
- ✅ `positionValue` receives data from the linked node's output
- ❌ Manual argument value is IGNORED (but still visible in UI)
- The linked output value flows directly to the parameter

**When Input "newPosition" is NOT Linked:**
- ❌ No linked value available
- ✅ `positionValue` receives the manual argument from UI
- User can adjust the Vector3 value in the node's UI controls

### Visual Representation

```
Scenario 1: INPUT IS LINKED
┌────────────────────┐
│ Source Node        │
│ Output: position   │──┐
│   Value: (10,20,30)│  │
└────────────────────┘  │
                        │  Link carries this value
                        ↓
┌────────────────────────────────┐
│ Target Node                    │
│ Input: newPosition (LINKED)    │
│ Argument: (0,0,0) [IGNORED]    │
│                                │
│ positionValue = (10,20,30) ✓   │
└────────────────────────────────┘


Scenario 2: INPUT NOT LINKED
┌────────────────────────────────┐
│ Target Node                    │
│ Input: newPosition (NOT LINKED)│
│ Argument: (0,0,0) [USED]       │
│                                │
│ positionValue = (0,0,0) ✓      │
└────────────────────────────────┘
```

---

## Runtime Behavior (How the Engine Works)

### The Node Execution Process

When the engine executes a node:

1. **Parse Metadata**: Load `!Inputs` and `!Arguments` definitions
2. **Check Linking State**: For each input, check if it has a linked connection
3. **Resolve Values**:
   ```
   For each function parameter:
     If corresponding input exists AND is linked:
       → Retrieve value from linked node's output
     Else:
       → Use value from corresponding argument UI control
   ```
4. **Call Function**: Pass resolved values to the Lua function

### Pseudo-code Implementation

```lua
-- Conceptual runtime behavior (not actual TEN code)
function ExecuteNode(node)
    local resolvedParams = {}
    
    -- For each function parameter
    for i, param in ipairs(node.FunctionParams) do
        -- Check if there's a linked input for this parameter
        local input = node.Inputs[param.name]
        
        if input and input.IsLinked then
            -- Priority 1: Get value from linked output
            local sourceNode = FindNodeById(input.LinkedOutputNodeId)
            resolvedParams[i] = sourceNode.Outputs[input.LinkedOutputName].Value
        else
            -- Priority 2: Fall back to manual argument
            resolvedParams[i] = node.Arguments[i].Value
        end
    end
    
    -- Call the actual function with resolved parameters
    node.Function(unpack(resolvedParams))
end
```

---

## UI Behavior

### What You See in the Editor

**Node with Unlinked Inputs:**
```
┌────────────────────────────────┐
│ Set Moveable Transform         │
├────────────────────────────────┤
│ Position: [___][___][___]  📝  │ ← Editable
│ Rotation: [___][___][___]  📝  │ ← Editable
│ Moveable: [Dropdown      ] 📝  │ ← Editable
├────────────────────────────────┤
│ Inputs:                        │
│ ○ newPosition (not linked)     │
│ ○ newRotation (not linked)     │
└────────────────────────────────┘
```

**Node with Linked Inputs:**
```
┌────────────────────────────────┐
│ Set Moveable Transform         │
├────────────────────────────────┤
│ Position: [___][___][___]  🔗  │ ← Visible but overridden
│ Rotation: [___][___][___]  🔗  │ ← Visible but overridden
│ Moveable: [Dropdown      ] 📝  │ ← Still editable
├────────────────────────────────┤
│ Inputs:                        │
│ ● newPosition ← [GetTransform] │ ← Linked!
│ ● newRotation ← [GetTransform] │ ← Linked!
└────────────────────────────────┘
```

**Key Points:**
- Argument controls remain visible even when input is linked
- Visual indicator (e.g., 🔗 icon) shows input is linked
- Linked input values override argument values at runtime
- You can always see the manual fallback values

---

## Why This Design?

### Benefits of Input + Argument Pattern

1. **Flexibility**: Node works standalone OR linked
2. **Default Values**: Arguments provide sensible defaults
3. **Discoverability**: Users see what data is needed
4. **Debugging**: Can temporarily unlink and test with manual values
5. **Backward Compatibility**: Existing projects without links still work

### Example Use Cases

**Use Case 1: Standalone Operation**
- Create "Set Transform" node
- Set position manually to (100, 200, 300)
- Works immediately without any linking

**Use Case 2: Dynamic Operation**
- Create "Get Transform" node for source
- Create "Set Transform" node for target
- Link position output → position input
- Position now dynamically follows source
- Manual argument ignored but provides fallback

---

## Technical Implementation Details

### How TombEditor Manages This

The `NodeEditor` class provides methods:

```csharp
// Check if input is linked
public bool IsInputLinked(TriggerNode node, string inputName)
{
    var input = node.Inputs.FirstOrDefault(i => i.Name == inputName);
    return input != null && input.IsLinked;
}

// Get effective value (linked or argument)
public string GetEffectiveInputValue(TriggerNode node, string inputName)
{
    var input = node.Inputs.FirstOrDefault(i => i.Name == inputName);
    
    if (input != null && input.IsLinked)
    {
        // Find linked source node and get output value
        var sourceNode = Nodes.FirstOrDefault(n => n.Id == input.LinkedOutputNodeId);
        if (sourceNode != null)
            return $"[Linked from {sourceNode.Name}.{input.LinkedOutputName}]";
    }
    
    // Fallback to argument value
    if (node.DynamicArguments.UserDefinedArguments.TryGetValue(inputName, out string value))
        return value;
    
    return string.Empty;
}
```

---

## Best Practices

### When Designing Nodes

**✅ DO:**
- Define both `!Inputs` AND `!Arguments` for the same data
- Provide sensible defaults in arguments
- Document that inputs override arguments
- Name inputs clearly (e.g., "newPosition" vs just "position")

**❌ DON'T:**
- Assume users will always link inputs
- Hide argument controls when input is linked (keep them visible)
- Make nodes that REQUIRE linking to function
- Create ambiguous parameter names

### Example: Well-Designed Node

```lua
-- GOOD: Works standalone AND with linking
-- !Inputs "targetPosition, Vector3, Position to move to"
-- !Arguments "NewLine, Vector3, [0|1000|0], 100, Target position (fallback if not linked)"

LevelFuncs.Engine.Node.MoveToPosition = function(position, moveable)
    -- Works with linked position OR manual position
    moveable:SetPosition(position)
end
```

```lua
-- BAD: Requires linking, no fallback
-- !Inputs "targetPosition, Vector3, Position to move to"
-- (No arguments - what if not linked?)

LevelFuncs.Engine.Node.MoveToPosition = function(position, moveable)
    -- If position input not linked, this breaks!
    moveable:SetPosition(position)
end
```

---

## Summary: Quick Reference

| Scenario | Input Linked? | Value Source | UI Behavior |
|----------|---------------|--------------|-------------|
| **Linked** | ✅ Yes | Linked output value | Arguments visible but overridden |
| **Not Linked** | ❌ No | Manual argument value | Arguments editable and used |
| **Partial** | Mixed | Per-input basis | Some linked, some manual |

**Priority Rule:** `Linked Input Value > Manual Argument Value`

**Fallback Pattern:** Always define arguments so node can work standalone

**UI Philosophy:** Keep arguments visible to show defaults and allow debugging

---

## Additional Examples

### Example 1: Color Node

```lua
-- !Name "Set color"
-- !Inputs "red, Numerical, Red component" "green, Numerical, Green component" "blue, Numerical, Blue component"
-- !Arguments "NewLine, Numerical, [0|255|0], 33, Red" "Numerical, [0|255|0], 33, Green" "Numerical, [0|255|0], 34, Blue"

LevelFuncs.Engine.Node.SetColor = function(r, g, b, target)
    -- Each component: linked value OR manual value
    -- Can link all 3, or just 1, or none
    target:SetColor(TEN.Color(r, g, b))
end
```

**Scenario A:** All inputs linked → All values from linked outputs
**Scenario B:** Only red linked → Red from link, green/blue from manual
**Scenario C:** Nothing linked → All values from manual arguments

### Example 2: Conditional Node

```lua
-- !Name "If distance is..."
-- !Conditional "True"
-- !Inputs "distance, Numerical, Distance value to check"
-- !Arguments "NewLine, Numerical, [0|10000|0], 50, Distance threshold"
-- !Arguments "CompareOperator, 50, Comparison"

LevelFuncs.Engine.Node.TestDistance = function(threshold, operator)
    -- Note: 'threshold' from argument
    -- But distance comes from INPUT (if linked) or would need another argument
    local distance = 0  -- This should come from linked input or argument
    return LevelFuncs.Engine.Node.CompareValue(distance, threshold, operator)
end
```

---

## Need More Help?

See also:
- `HOW_TO_USE_INPUTS_OUTPUTS.md` - Basic tutorial
- `MULTIPLE_INPUTS_OUTPUTS_EXAMPLE.md` - Advanced multi-IO examples
- `Sample Input-Output Nodes.lua` - Working code examples
- `Readme.md` - Complete metadata reference
