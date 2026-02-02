# How to Define Nodes with Input/Output and Event Mode Features

This guide shows you how to create nodes with the new input/output linking system and event mode restrictions.

## Quick Example: Position Nodes

Here's exactly how to define the **GetPosition** and **ModifyPosition** nodes you requested, restricted to **OnLoop** events only:

### 1. Get Position Node (with Output)

```lua
-- !Name "Get moveable position"
-- !Section "Moveable parameters"
-- !Description "Gets the current position of a moveable.\nThis position can be linked to other nodes as input."
-- !Outputs "position, Vector3, Current XYZ position of the moveable"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Moveables, 100, Moveable to get position from"

LevelFuncs.Engine.Node.GetMoveablePosition = function(moveableName)
    local moveable = TEN.Objects.GetMoveableByName(moveableName)
    local position = moveable:GetPosition()
    
    -- Return the position so it can be used by linked nodes
    return position
end
```

**Key points:**
- `!Outputs "position, Vector3, Current XYZ position of the moveable"` defines an output slot
  - Format: `"outputName, dataType, description"`
- `!EventModes "OnLoop"` restricts this node to OnLoop global events only
- The function returns the position value that can be linked to other nodes

### 2. Modify Position Node (with Input)

```lua
-- !Name "Modify position of a moveable"
-- !Section "Moveable parameters"
-- !Description "Set or modify given moveable position.\nPosition can be linked from another node's output or manually set."
-- !Inputs "newPosition, Vector3, New position value (can be linked from Get Position node)"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Enumeration, [ Change | Set ], 25, Change adds/subtracts given value while Set forces it."
-- !Arguments "Vector3, [ -1000000 | 1000000 | 0 | 1 | 32 ], 75, Position value to define"
-- !Arguments "NewLine, Moveables, 100, Moveable to modify"

LevelFuncs.Engine.Node.SetMoveablePosition = function(operation, value, moveableName)
    local moveable = TEN.Objects.GetMoveableByName(moveableName)

    if (operation == 0) then
        -- Change mode: add/subtract from current position
        local position = moveable:GetPosition()
        position.x = position.x + value.x
        position.y = position.y + value.y
        position.z = position.z + value.z
        moveable:SetPosition(position)
    else
        -- Set mode: force position to the value
        moveable:SetPosition(value)
    end
end
```

**Key points:**
- `!Inputs "newPosition, Vector3, New position value..."` defines an input slot
  - Format: `"inputName, dataType, description"`
- `!EventModes "OnLoop"` restricts this node to OnLoop global events only
- The input can receive data from the output of another node (like GetPosition)
- If no input is linked, the node falls back to the manual `value` argument

## Metadata Reference

### !Outputs Syntax
```lua
-- !Outputs "outputName1, Type1, Description1" "outputName2, Type2, Description2"
```

Valid types: `Vector3`, `Vector2`, `Numerical`, `String`, `Boolean`, `Color`

### !Inputs Syntax
```lua
-- !Inputs "inputName1, Type1, Description1" "inputName2, Type2, Description2"
```

Valid types: Same as outputs

### !EventModes Syntax
```lua
-- !EventModes "Mode1, Mode2, Mode3"
```

Valid modes:
- `OnStart` - Runs once when level starts
- `OnEnd` - Runs once when level ends  
- `OnLoad` - Runs when level loads
- `OnSave` - Runs when level saves
- `OnControlPhase` - Runs during control phase
- `OnLoop` - Runs every frame
- `OnUseItem` - Runs when item is used
- `OnFreeze` - Runs when game freezes

If `!EventModes` is omitted, the node can be used in any event.

## How Linking Works

1. **Create both nodes** in an OnLoop global event
2. **In the node editor**, you can link the output of GetPosition to the input of SetPosition
3. **At runtime**, when GetPosition executes, its return value is passed to SetPosition's input
4. **Fallback behavior**: If no input is linked, SetPosition uses its manual argument value

## Event Mode Validation

When you try to use these nodes in a non-OnLoop event (like OnStart), Tomb Editor will:
- Show a warning: "Node not explicitly designed for 'OnStart' events. Proceed with caution."
- Still allow you to place the node (not a hard error)
- Help you catch potential logic errors

## Complete Working Example

Place this in any `.lua` file in the `TEN Node Catalogs` folder:

```lua
-- !Name "Get moveable position"
-- !Section "Moveable parameters"
-- !Description "Gets the current position of a moveable.\nThis position can be linked to other nodes as input."
-- !Outputs "position, Vector3, Current XYZ position of the moveable"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Moveables, 100, Moveable to get position from"

LevelFuncs.Engine.Node.GetMoveablePosition = function(moveableName)
    local moveable = TEN.Objects.GetMoveableByName(moveableName)
    return moveable:GetPosition()
end

-- !Name "Modify position of a moveable"
-- !Section "Moveable parameters"
-- !Description "Set or modify given moveable position.\nPosition can be linked from another node's output or manually set."
-- !Inputs "newPosition, Vector3, New position value (can be linked from Get Position node)"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Enumeration, [ Change | Set ], 25, Change adds/subtracts given value while Set forces it."
-- !Arguments "Vector3, [ -1000000 | 1000000 | 0 | 1 | 32 ], 75, Position value to define"
-- !Arguments "NewLine, Moveables, 100, Moveable to modify"

LevelFuncs.Engine.Node.SetMoveablePosition = function(operation, value, moveableName)
    local moveable = TEN.Objects.GetMoveableByName(moveableName)

    if (operation == 0) then
        local position = moveable:GetPosition()
        position.x = position.x + value.x
        position.y = position.y + value.y
        position.z = position.z + value.z
        moveable:SetPosition(position)
    else
        moveable:SetPosition(value)
    end
end
```

After adding this file, restart Tomb Editor and you'll see both nodes available in the "Moveable parameters" section, and they will only be usable in OnLoop global events!

## Additional Examples

See `Sample Input-Output Nodes.lua` for more examples including:
- Rotation nodes (input/output)
- Distance calculation nodes
- Conditional nodes with inputs
- Multiple inputs/outputs on the same node
