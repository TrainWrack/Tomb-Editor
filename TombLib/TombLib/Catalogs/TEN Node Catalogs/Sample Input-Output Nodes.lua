-- Sample node catalog demonstrating input/output variable linking and event mode restrictions
-- These nodes showcase the new !Inputs, !Outputs, and !EventModes metadata tags

-- !Name "Get moveable position"
-- !Section "Moveable parameters"
-- !Description "Gets the current position of a moveable.\nThis position can be linked to other nodes as input."
-- !Outputs "position, Vector3, Current XYZ position of the moveable"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Moveables, 100, Moveable to get position from"

LevelFuncs.Engine.Node.GetMoveablePosition = function(moveableName)
	local moveable = TEN.Objects.GetMoveableByName(moveableName)
	local position = moveable:GetPosition()
	
	-- In a real implementation, this would store the position value
	-- so it can be retrieved by linked nodes via the output slot
	return position
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

	-- Check if position is linked from another node
	-- In a real implementation, this would check if the input is linked
	-- and retrieve the value from the linked node's output
	
	if (operation == 0) then
		-- Change mode: add/subtract from current position
		local position = moveable:GetPosition()
		position.x = position.x + value.x
		position.y = position.y + value.y
		position.z = position.z + value.z
		moveable:SetPosition(position)
	else
		-- Set mode: force position
		moveable:SetPosition(value)
	end
end

-- !Name "Get moveable rotation"
-- !Section "Moveable parameters"
-- !Description "Gets the current rotation of a moveable.\nThis rotation can be linked to other nodes as input."
-- !Outputs "rotation, Vector3, Current XYZ rotation of the moveable in degrees"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Moveables, 100, Moveable to get rotation from"

LevelFuncs.Engine.Node.GetMoveableRotation = function(moveableName)
	local moveable = TEN.Objects.GetMoveableByName(moveableName)
	local rotation = moveable:GetRotation()
	
	return rotation
end

-- !Name "Modify rotation of a moveable"
-- !Section "Moveable parameters"
-- !Description "Set or modify given moveable rotation.\nRotation can be linked from another node's output or manually set."
-- !Inputs "newRotation, Vector3, New rotation value (can be linked from Get Rotation node)"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Enumeration, [ Change | Set ], 25, Change adds/subtracts given value while Set forces it."
-- !Arguments "Vector3, [ -360 | 360 | 0 | 1 | 1 ], 75, Rotation value in degrees"
-- !Arguments "NewLine, Moveables, 100, Moveable to modify"

LevelFuncs.Engine.Node.SetMoveableRotation = function(operation, value, moveableName)
	local moveable = TEN.Objects.GetMoveableByName(moveableName)

	if (operation == 0) then
		-- Change mode: add/subtract from current rotation
		local rotation = moveable:GetRotation()
		rotation.x = rotation.x + value.x
		rotation.y = rotation.y + value.y
		rotation.z = rotation.z + value.z
		moveable:SetRotation(rotation)
	else
		-- Set mode: force rotation
		moveable:SetRotation(value)
	end
end

-- !Name "Calculate distance between moveables"
-- !Section "Moveable parameters"
-- !Description "Calculates the distance between two moveables.\nThis can be useful for proximity checks or distance-based logic."
-- !Outputs "distance, Numerical, Distance between the two moveables"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Moveables, 50, First moveable"
-- !Arguments "Moveables, 50, Second moveable"

LevelFuncs.Engine.Node.CalculateMoveableDistance = function(moveableName1, moveableName2)
	local moveable1 = TEN.Objects.GetMoveableByName(moveableName1)
	local moveable2 = TEN.Objects.GetMoveableByName(moveableName2)
	
	local pos1 = moveable1:GetPosition()
	local pos2 = moveable2:GetPosition()
	
	local dx = pos2.x - pos1.x
	local dy = pos2.y - pos1.y
	local dz = pos2.z - pos1.z
	
	local distance = math.sqrt(dx*dx + dy*dy + dz*dz)
	
	return distance
end

-- !Name "If distance is..."
-- !Section "Moveable parameters"
-- !Description "Compares distance between two moveables with a threshold.\nDistance can be linked from Calculate Distance node."
-- !Conditional "True"
-- !Inputs "distance, Numerical, Distance value to check (can be linked from Calculate Distance)"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, CompareOperator, 30, Comparison operator"
-- !Arguments "Numerical, 70, [ 0 | 100000 | 0 ], Threshold distance"

LevelFuncs.Engine.Node.TestDistance = function(operator, threshold)
	-- In a real implementation, this would get the distance from the linked input
	-- For now, we'll assume it's passed as a parameter
	local distance = 0  -- This would come from the linked input
	
	return LevelFuncs.Engine.Node.CompareValue(distance, threshold, operator)
end

-- ============================================================================
-- ADVANCED EXAMPLE: Multiple Outputs Linked to Multiple Inputs
-- ============================================================================
-- This demonstrates a node with TWO outputs linking to another node with TWO inputs
-- This is useful for operations that need to transfer multiple related values at once

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
	
	-- Both position and rotation are available as outputs
	-- that can be independently linked to other nodes' inputs
	return position, rotation
end

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
	
	-- In a real implementation, these would check if inputs are linked
	-- and retrieve values from the linked outputs
	-- If not linked, they fall back to the manual argument values
	
	moveable:SetPosition(positionValue)
	moveable:SetRotation(rotationValue)
end

-- !Name "Copy moveable transform"
-- !Section "Moveable parameters"
-- !Description "Copies position and rotation from source to target.\nThis demonstrates linking 2 outputs to 2 inputs in a single operation."
-- !Inputs "sourcePosition, Vector3, Position from source" "sourceRotation, Vector3, Rotation from source"
-- !Outputs "position, Vector3, Output position" "rotation, Vector3, Output rotation"
-- !EventModes "OnLoop"
-- !Arguments "NewLine, Moveables, 100, Target moveable to apply transform to"

LevelFuncs.Engine.Node.CopyMoveableTransform = function(moveableName)
	-- This node acts as a pass-through, receiving 2 inputs and providing 2 outputs
	-- Useful for transform chains or applying the same transform to multiple targets
	
	-- In a real implementation:
	-- 1. Gets position and rotation from linked inputs
	-- 2. Applies them to the target moveable
	-- 3. Also outputs them so they can be linked to other nodes
	
	local moveable = TEN.Objects.GetMoveableByName(moveableName)
	-- Would get values from linked inputs here
	local position = TEN.Vec3(0, 0, 0)  -- From linked input
	local rotation = TEN.Vec3(0, 0, 0)  -- From linked input
	
	moveable:SetPosition(position)
	moveable:SetRotation(rotation)
	
	return position, rotation
end

-- ============================================================================
-- USAGE EXAMPLE for Multiple Input/Output Linking:
-- ============================================================================
-- 1. Create "Get moveable transform" node for source moveable
-- 2. Create "Set moveable transform" node for target moveable
-- 3. Link "position" output → "newPosition" input
-- 4. Link "rotation" output → "newRotation" input
-- 5. Now position and rotation flow from source to target automatically!
--
-- This is more efficient than creating separate nodes for position and rotation,
-- and ensures both values are transferred atomically in the same frame.
-- ============================================================================
