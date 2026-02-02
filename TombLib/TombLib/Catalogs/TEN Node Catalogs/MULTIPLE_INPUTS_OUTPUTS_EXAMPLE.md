# Example: Linking Multiple Outputs to Multiple Inputs

This example demonstrates how to create nodes that have **multiple outputs** and **multiple inputs**, and how to link them together.

## Overview

Sometimes you need to transfer multiple related values between nodes. For example:
- Position AND rotation together (transform)
- RGB color values
- Min/max range values
- Multiple calculation results

Instead of creating separate nodes for each value, you can define a single node with multiple outputs and another node with multiple inputs.

## Complete Example: Transform Operations

### 1. Node with Multiple Outputs

This node has **TWO outputs** - position and rotation:

```lua
-- !Name "Get moveable transform"
-- !Section "Moveable parameters"
-- !Description "Gets both position AND rotation of a moveable.\nBoth values can be linked to other nodes that need transform data."
-- !Outputs "position, Vector3, Current XYZ position" "rotation, Vector3, Current XYZ rotation in degrees"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Moveables, 100, Moveable to get transform from"

LevelFuncs.Engine.Node.GetMoveableTransform = function(moveableName)
    local moveable = TEN.Objects.GetMoveableByName(moveableName)
    local position = moveable:GetPosition()
    local rotation = moveable:GetRotation()
    
    -- Return BOTH values - they become separate output slots
    return position, rotation
end
```

**Key Points:**
- Define multiple outputs: `!Outputs "name1, Type1, Desc1" "name2, Type2, Desc2"`
- Return multiple values: `return value1, value2`
- Each output can be linked independently

### 2. Node with Multiple Inputs

This node has **TWO inputs** - new position and new rotation:

```lua
-- !Name "Set moveable transform"
-- !Section "Moveable parameters"
-- !Description "Sets both position AND rotation of a moveable.\nBoth values can be linked from other nodes (e.g., Get Transform)."
-- !Inputs "newPosition, Vector3, Position to set (can be linked)" "newRotation, Vector3, Rotation to set (can be linked)"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Vector3, [ -1000000 | 1000000 | 0 | 1 | 32 ], 50, Position value"
-- !Arguments "Vector3, [ -360 | 360 | 0 | 1 | 1 ], 50, Rotation value in degrees"
-- !Arguments "NewLine, Moveables, 100, Target moveable"

LevelFuncs.Engine.Node.SetMoveableTransform = function(positionValue, rotationValue, moveableName)
    local moveable = TEN.Objects.GetMoveableByName(moveableName)
    
    -- These values come from:
    -- 1. Linked inputs (if connected to another node's outputs)
    -- 2. Manual argument values (if not linked)
    
    moveable:SetPosition(positionValue)
    moveable:SetRotation(rotationValue)
end
```

**Key Points:**
- Define multiple inputs: `!Inputs "name1, Type1, Desc1" "name2, Type2, Desc2"`
- Each input can receive from a linked output
- Falls back to manual argument if not linked

### 3. How to Link in Tomb Editor

**Step-by-step:**

1. **Create source node**: Add "Get moveable transform" node to your OnLoop event
   - Select source moveable (e.g., "enemy_01")
   
2. **Create target node**: Add "Set moveable transform" node
   - Select target moveable (e.g., "camera_dummy")
   
3. **Link first output→input**: 
   - Connect `position` output from GetTransform 
   - To `newPosition` input on SetTransform
   
4. **Link second output→input**:
   - Connect `rotation` output from GetTransform
   - To `newRotation` input on SetTransform

5. **Result**: The target moveable will now follow the source moveable's position AND rotation every frame!

### Visual Representation

```
┌─────────────────────────────┐
│  Get Moveable Transform     │
│  (Moveable: enemy_01)       │
│                             │
│  Outputs:                   │
│  ○ position (Vector3) ──────┼──┐
│  ○ rotation (Vector3) ──────┼──┼──┐
└─────────────────────────────┘  │  │
                                 │  │
                                 │  │
┌─────────────────────────────┐  │  │
│  Set Moveable Transform     │  │  │
│  (Moveable: camera_dummy)   │  │  │
│                             │  │  │
│  Inputs:                    │  │  │
│  ● newPosition (Vector3) ◄──┼──┘  │
│  ● newRotation (Vector3) ◄──┼─────┘
└─────────────────────────────┘
```

## More Examples

### Example: Pass-Through Node

A node that both receives inputs AND provides outputs:

```lua
-- !Name "Copy moveable transform"
-- !Inputs "sourcePosition, Vector3, Position from source" "sourceRotation, Vector3, Rotation from source"
-- !Outputs "position, Vector3, Output position" "rotation, Vector3, Output rotation"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Moveables, 100, Target moveable"

LevelFuncs.Engine.Node.CopyMoveableTransform = function(moveableName)
    local moveable = TEN.Objects.GetMoveableByName(moveableName)
    
    -- Get values from linked inputs
    local position = -- from linked input
    local rotation = -- from linked input
    
    -- Apply to target
    moveable:SetPosition(position)
    moveable:SetRotation(rotation)
    
    -- Also output them for further linking
    return position, rotation
end
```

This allows you to create transform chains:
- Node A outputs transform
- Node B receives from A, applies to moveable B, outputs again
- Node C receives from B, applies to moveable C
- And so on...

### Example: RGB Color Node

```lua
-- !Name "Get light color"
-- !Outputs "red, Numerical, Red component" "green, Numerical, Green component" "blue, Numerical, Blue component"
-- !Arguments "NewLine, Moveables, 100, Light source"

LevelFuncs.Engine.Node.GetLightColor = function(lightName)
    local light = TEN.Objects.GetMoveableByName(lightName)
    local color = light:GetColor()
    return color.r, color.g, color.b
end

-- !Name "Set light color"
-- !Inputs "red, Numerical, Red component" "green, Numerical, Green component" "blue, Numerical, Blue component"
-- !Arguments "NewLine, Moveables, 100, Target light"

LevelFuncs.Engine.Node.SetLightColor = function(r, g, b, lightName)
    local light = TEN.Objects.GetMoveableByName(lightName)
    light:SetColor(TEN.Color(r, g, b))
end
```

## Best Practices

1. **Group related values**: Position+rotation, min+max, RGB, etc.
2. **Use consistent naming**: If output is "position", input should be "newPosition"
3. **Document relationships**: Clearly state which outputs link to which inputs
4. **Type matching**: Ensure output type matches input type (Vector3→Vector3, etc.)
5. **Consider atomicity**: Multiple outputs ensure related values transfer in same frame

## Benefits of Multiple Outputs/Inputs

✅ **Efficiency**: One node instead of multiple separate nodes
✅ **Synchronization**: All values transfer in same frame
✅ **Cleaner graphs**: Fewer nodes, easier to read
✅ **Related data**: Keeps logically related values together
✅ **Flexible**: Each output can link independently to different targets

## Summary

To create nodes with multiple outputs/inputs:

1. **Define multiple outputs**: `!Outputs "out1, Type1, Desc1" "out2, Type2, Desc2"`
2. **Define multiple inputs**: `!Inputs "in1, Type1, Desc1" "in2, Type2, Desc2"`
3. **Return multiple values**: `return value1, value2, value3`
4. **Link in editor**: Connect each output to its corresponding input
5. **Test**: Verify all values transfer correctly

See `Sample Input-Output Nodes.lua` for complete working examples!
