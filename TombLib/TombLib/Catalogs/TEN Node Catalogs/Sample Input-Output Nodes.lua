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
