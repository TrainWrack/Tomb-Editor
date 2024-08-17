﻿using System;
using System.Collections.Generic;
using TombLib.LevelData.SectorEnums;
using TombLib.LevelData.SectorEnums.Extensions;

namespace TombLib.LevelData.SectorGeometry;

/// <summary>
/// Represents the wall of a single sector.
/// </summary>
public readonly struct SectorWall
{
	/// <summary>
	/// The direction the wall is facing.
	/// </summary>
	public readonly Direction Direction;

	/// <summary>
	/// The (X, Z) position and the minimum and maximum Y coordinate of the start corner of the wall.
	/// </summary>
	public readonly WallEnd Start;

	/// <summary>
	/// The (X, Z) position and the minimum and maximum Y coordinate of the end corner of the wall.
	/// </summary>
	public readonly WallEnd End;

	/// <summary>
	/// Main floor split of the wall.
	/// </summary>
	public readonly WallSplit QA;

	/// <summary>
	/// Main ceiling split of the wall.
	/// </summary>
	public readonly WallSplit WS;

	/// <summary>
	/// Extra floor splits of the wall.
	/// </summary>
	public readonly IReadOnlyList<WallSplit> ExtraFloorSplits;

	/// <summary>
	/// Extra ceiling splits of the wall.
	/// </summary>
	public readonly IReadOnlyList<WallSplit> ExtraCeilingSplits;

	public SectorWall(Direction direction, WallEnd start, WallEnd end, WallSplit qa, WallSplit ws,
		IReadOnlyList<WallSplit> extraFloorSplits, IReadOnlyList<WallSplit> extraCeilingSplits)
	{
		Direction = direction;
		Start = start;
		End = end;
		QA = qa;
		WS = ws;
		ExtraFloorSplits = extraFloorSplits;
		ExtraCeilingSplits = extraCeilingSplits;
	}

	/// <summary>
	/// Returns true if the QA part of the wall is fully above the maximum Y coordinate of the wall.
	/// </summary>
	public readonly bool IsQaFullyAboveMaxY => QA.StartY >= Start.MaxY && QA.EndY >= End.MaxY;

	/// <summary>
	/// Returns true if the WS part of the wall is fully below the minimum Y coordinate of the wall.
	/// </summary>
	public readonly bool IsWsFullyBelowMinY => WS.StartY <= Start.MinY && WS.EndY <= End.MinY;

	/// <summary>
	/// Returns true if the diagonal wall direction can have a non-diagonal (square from top-down) floor part.
	/// </summary>
	public readonly bool CanHaveNonDiagonalFloorPart(DiagonalSplit diagonalFloorSplitOfSector)
	{
		return
			(diagonalFloorSplitOfSector is DiagonalSplit.XnZp && Direction is Direction.NegativeZ or Direction.PositiveX) ||
			(diagonalFloorSplitOfSector is DiagonalSplit.XpZn && Direction is Direction.NegativeX or Direction.PositiveZ) ||
			(diagonalFloorSplitOfSector is DiagonalSplit.XpZp && Direction is Direction.NegativeZ or Direction.NegativeX) ||
			(diagonalFloorSplitOfSector is DiagonalSplit.XnZn && Direction is Direction.PositiveZ or Direction.PositiveX);
	}

	/// <summary>
	/// Returns true if the diagonal wall direction can have a non-diagonal (square from top-down) ceiling part.
	/// </summary>
	public readonly bool CanHaveNonDiagonalCeilingPart(DiagonalSplit diagonalCeilingSplitOfSector)
	{
		return
			(diagonalCeilingSplitOfSector is DiagonalSplit.XnZp && Direction is Direction.NegativeZ or Direction.PositiveX) ||
			(diagonalCeilingSplitOfSector is DiagonalSplit.XpZn && Direction is Direction.NegativeX or Direction.PositiveZ) ||
			(diagonalCeilingSplitOfSector is DiagonalSplit.XpZp && Direction is Direction.NegativeZ or Direction.NegativeX) ||
			(diagonalCeilingSplitOfSector is DiagonalSplit.XnZn && Direction is Direction.PositiveZ or Direction.PositiveX);
	}

	/// <summary>
	/// Returns the vertical floor part of the wall (dark green), if it can be rendered.
	/// </summary>
	public readonly IReadOnlyList<SectorFace> GetVerticalFloorPartFaces(DiagonalSplit diagonalFloorSplit, bool isAnyWall)
	{
		bool isQaFullyAboveMaxY = IsQaFullyAboveMaxY; // Technically should be classified as a wall if true
		bool canHaveDiagonalWallFloorPart = CanHaveNonDiagonalFloorPart(diagonalFloorSplit); // The wall bit under the flat floor triangle of a diagonal wall

		var faces = new List<SectorFace>();
		SectorFace? faceData;

		for (int extraSplitIndex = -1; extraSplitIndex < ExtraFloorSplits.Count; extraSplitIndex++)
		{
			bool isQA = extraSplitIndex == -1;

			(int yStartA, int yStartB) = isQA
				? (QA.StartY, QA.EndY) // Render QA face
				: (ExtraFloorSplits[extraSplitIndex].StartY, ExtraFloorSplits[extraSplitIndex].EndY); // Render extra split face

			SectorFaceIdentifier sectorFace = isQA
				? SectorFaceExtensions.GetQaFace(Direction) // QA face
				: SectorFaceExtensions.GetExtraFloorSplitFace(Direction, extraSplitIndex); // Extra split face

			// Start with the floor as a baseline for the bottom end of the face
			(int yEndA, int yEndB) = (Start.MinY, End.MinY);

			if (extraSplitIndex + 1 < ExtraFloorSplits.Count) // If a next floor split exists
			{
				int yNextSplitStart = ExtraFloorSplits[extraSplitIndex + 1].StartY,
					yNextSplitEnd = ExtraFloorSplits[extraSplitIndex + 1].EndY;

				if ((canHaveDiagonalWallFloorPart || isQaFullyAboveMaxY) && (yNextSplitStart > QA.StartY || yNextSplitEnd > QA.EndY))
					continue; // Skip it, since it's above the flat, walkable triangle of a diagonal wall

				if (yNextSplitStart >= Start.MinY && yNextSplitEnd >= End.MinY) // If next split is NOT in void below floor
				{
					// Make the next split the bottom end of the face
					yEndA = yNextSplitStart;
					yEndB = yNextSplitEnd;
				}
			}

			if (yStartA <= yEndA && yStartB <= yEndB)
				continue; // 0 or negative height face, don't render it

			faceData = SectorFace.CreateVerticalFloorFaceData(sectorFace, (Start.X, Start.Z), (End.X, End.Z), new(yStartA, yStartB), new(yEndA, yEndB));

			if (!faceData.HasValue)
			{
				// Try overdraw

				bool isEvenWithQA = yStartA == QA.StartY && yStartB == QA.EndY; // Whether the split has the same Y coordinates as QA
																				// if so, then it covers the whole floor part of the wall
				bool isValidOverdraw = isEvenWithQA && (!isAnyWall || canHaveDiagonalWallFloorPart);

				if (!isValidOverdraw)
					continue;

				// Find lowest point between split and baseline, then try and create an overdraw face out of it
				int lowest = Math.Min(Math.Min(yStartA, yStartB), Math.Min(yEndA, yEndB));
				faceData = SectorFace.CreateVerticalFloorFaceData(sectorFace, (Start.X, Start.Z), (End.X, End.Z), new(yStartA, yStartB), new(lowest, lowest));
			}

			if (faceData.HasValue)
				faces.Add(faceData.Value);
		}

		return faces;
	}

	/// <summary>
	/// Returns the vertical ceiling part of the wall (light green), if it can be rendered.
	/// </summary>
	public readonly IReadOnlyList<SectorFace> GetVerticalCeilingPartFaces(DiagonalSplit diagonalCeilingSplit, bool isAnyWall)
	{
		bool isWsFullyBelowMinY = IsWsFullyBelowMinY; // Technically should be classified as a wall if true
		bool canHaveNonDiagonalCeilingPart = CanHaveNonDiagonalCeilingPart(diagonalCeilingSplit); // The wall bit over the flat ceiling triangle of a diagonal wall

		var faces = new List<SectorFace>();
		SectorFace? faceData;

		for (int extraSplitIndex = -1; extraSplitIndex < ExtraCeilingSplits.Count; extraSplitIndex++)
		{
			bool isWS = extraSplitIndex == -1;

			(int yStartA, int yStartB) = isWS
				? (WS.StartY, WS.EndY) // Render WS face
				: (ExtraCeilingSplits[extraSplitIndex].StartY, ExtraCeilingSplits[extraSplitIndex].EndY); // Render split face

			SectorFaceIdentifier sectorFace = isWS
				? SectorFaceExtensions.GetWsFace(Direction)
				: SectorFaceExtensions.GetExtraCeilingSplitFace(Direction, extraSplitIndex);

			// Start with the ceiling as a baseline for the top end of the face
			(int yEndA, int yEndB) = (Start.MaxY, End.MaxY);

			if (extraSplitIndex + 1 < ExtraCeilingSplits.Count) // If a next ceiling split exists
			{
				int yNextSubdivA = ExtraCeilingSplits[extraSplitIndex + 1].StartY,
					yNextSubdivB = ExtraCeilingSplits[extraSplitIndex + 1].EndY;

				if ((canHaveNonDiagonalCeilingPart || isWsFullyBelowMinY) && (yNextSubdivA < WS.StartY || yNextSubdivB < WS.EndY))
					continue; // Skip it, since it's below the flat ceiling triangle

				if (yNextSubdivA <= Start.MaxY && yNextSubdivB <= End.MaxY) // If next split is NOT in void above ceiling
				{
					// Make the next split the top end of the face
					yEndA = yNextSubdivA;
					yEndB = yNextSubdivB;
				}
			}

			if (yStartA >= yEndA && yStartB >= yEndB)
				continue; // 0 or negative height face, don't render it

			faceData = SectorFace.CreateVerticalCeilingFaceData(sectorFace, (Start.X, Start.Z), (End.X, End.Z), new(yStartA, yStartB), new(yEndA, yEndB));

			if (!faceData.HasValue)
			{
				// Try overdraw

				bool isEvenWithWS = yStartA == WS.StartY && yStartB == WS.EndY; // Whether the split has the same Y coordinates as WS
																				// if so, then it covers the whole ceiling part of the wall
				bool isValidOverdraw = isEvenWithWS && (!isAnyWall || canHaveNonDiagonalCeilingPart);

				if (!isValidOverdraw)
					continue;

				// Find highest point between split and baseline, then try and create an overdraw face out of it
				int highest = Math.Max(Math.Max(yStartA, yStartB), Math.Max(yEndA, yEndB));
				faceData = SectorFace.CreateVerticalCeilingFaceData(sectorFace, (Start.X, Start.Z), (End.X, End.Z), new(highest, highest), new(yStartA, yStartB));
			}

			if (faceData.HasValue)
				faces.Add(faceData.Value);
		}

		return faces;
	}

	/// <summary>
	/// Returns the vertical middle face of the wall, if it can be rendered.
	/// </summary>
	public readonly SectorFace? GetVerticalMiddleFace()
	{
		int qaStartY = QA.StartY, qaEndY = QA.EndY,
			wsStartY = WS.StartY, wsEndY = WS.EndY;

		if (qaStartY < Start.MinY || qaEndY < End.MinY)
		{
			qaStartY = Start.MinY;
			qaEndY = End.MinY;
		}

		if (wsStartY > Start.MaxY || wsEndY > End.MaxY)
		{
			wsStartY = Start.MaxY;
			wsEndY = End.MaxY;
		}

		int yStartA = qaStartY <= Start.MinY ? Start.MinY : qaStartY,
			yStartB = qaEndY <= End.MinY ? End.MinY : qaEndY,
			yEndA = wsStartY >= Start.MaxY ? Start.MaxY : wsStartY,
			yEndB = wsEndY >= End.MaxY ? End.MaxY : wsEndY;

		SectorFaceIdentifier sectorFace = SectorFaceExtensions.GetMiddleFace(Direction);
		return SectorFace.CreateVerticalMiddleFaceData(sectorFace, (Start.X, Start.Z), (End.X, End.Z), new(yStartA, yStartB), new(yEndA, yEndB));
	}

	/// <summary>
	/// Normalizes the wall splits to prevent overdraw and to make sure the wall is rendered correctly, then returns the normalized wall.
	/// </summary>
	public readonly SectorWall Normalize(DiagonalSplit diagonalFloorSplit, DiagonalSplit diagonalCeilingSplit, bool isAnyWall)
	{
		WallSplit
			qa = NormalizeFloorSplit(QA, diagonalFloorSplit, isAnyWall, out bool isFloorSplitInFloorVoid),
			ws = NormalizeCeilingSplit(WS, diagonalCeilingSplit, isAnyWall, out bool isCeilingSplitInCeilingVoid);

		List<WallSplit>
			extraFloorSplits = new(ExtraFloorSplits),
			extraCeilingSplits = new(ExtraCeilingSplits);

		if (!isFloorSplitInFloorVoid)
		{
			for (int i = 0; i < extraFloorSplits.Count; i++)
			{
				WallSplit normalizedSplit = NormalizeFloorSplit(extraFloorSplits[i], diagonalFloorSplit, isAnyWall, out isFloorSplitInFloorVoid);

				if (isFloorSplitInFloorVoid)
				{
					// Remove the rest as it will also be in the void, therefore not rendered
					extraFloorSplits.RemoveRange(i, extraFloorSplits.Count - i);
					break;
				}

				extraFloorSplits[i] = normalizedSplit;
			}
		}

		if (!isCeilingSplitInCeilingVoid)
		{
			for (int i = 0; i < extraCeilingSplits.Count; i++)
			{
				WallSplit normalizedSplit = NormalizeCeilingSplit(extraCeilingSplits[i], diagonalCeilingSplit, isAnyWall, out isCeilingSplitInCeilingVoid);

				if (isCeilingSplitInCeilingVoid)
				{
					// Remove the rest as it will also be in the void, therefore not rendered
					extraCeilingSplits.RemoveRange(i, extraCeilingSplits.Count - i);
					break;
				}

				extraCeilingSplits[i] = normalizedSplit;
			}
		}

		return new SectorWall(Direction, Start, End, qa, ws, extraFloorSplits, extraCeilingSplits);
	}

	/// <summary>
	/// Normalizes the floor split to prevent overdraw and to make sure the wall is rendered correctly, then returns the normalized floor split.
	/// </summary>
	private readonly WallSplit NormalizeFloorSplit(WallSplit split, DiagonalSplit diagonalFloorSplit, bool isAnyWall, out bool isInFloorVoid)
	{
		isInFloorVoid = true;

		bool canHaveNonDiagonalFloorPart = CanHaveNonDiagonalFloorPart(diagonalFloorSplit); // The wall bit under the flat floor triangle of a diagonal wall
		bool isFaceInFloorVoid = split.StartY < Start.MinY || split.EndY < End.MinY || (split.StartY == Start.MinY && split.EndY == End.MinY);

		if (isFaceInFloorVoid && isAnyWall && !canHaveNonDiagonalFloorPart) // Part of overdraw prevention
			return split; // Stop the loop, since the rest of the splits will also be in the void

		isInFloorVoid = false;

		bool isQaFullyAboveMaxY = IsQaFullyAboveMaxY; // Technically should be classified as a wall if true

		bool isEitherStartPointAboveCeiling = split.StartY > Start.MaxY || split.EndY > End.MaxY; // If either start point A or B is in the void above ceiling
		bool areBothStartPointsAboveCeiling = split.StartY >= Start.MaxY && split.EndY >= End.MaxY; // Are both start points A and B in the void above ceiling

		(int startY, int endY) = (split.StartY, split.EndY);

		// Full walls can't have overdraw, so if either point is in void, then snap it to ceiling
		// Diagonal walls are an exception, since, even though they are walls, they have a flat floor bit, so we can allow overdraw
		if ((isEitherStartPointAboveCeiling && (isAnyWall || isQaFullyAboveMaxY) && !canHaveNonDiagonalFloorPart) || areBothStartPointsAboveCeiling)
		{
			// Snap points to ceiling
			startY = Start.MaxY;
			endY = End.MaxY;
		}
		else if (startY > QA.StartY || endY > QA.EndY) // If either split point is above QA
		{
			// Snap points to the heights of QA
			startY = QA.StartY;
			endY = QA.EndY;
		}

		return new WallSplit(startY, endY);
	}

	/// <summary>
	/// Normalizes the ceiling split to prevent overdraw and to make sure the wall is rendered correctly, then returns the normalized ceiling split.
	/// </summary>
	private readonly WallSplit NormalizeCeilingSplit(WallSplit split, DiagonalSplit diagonalCeilingSplit, bool isAnyWall, out bool isInCeilingVoid)
	{
		isInCeilingVoid = true;

		bool canHaveNonDiagonalCeilingPart = CanHaveNonDiagonalCeilingPart(diagonalCeilingSplit); // The wall bit over the flat ceiling triangle of a diagonal wall
		bool isFaceInCeilingVoid = split.StartY > Start.MaxY || split.EndY > End.MaxY || (split.StartY == Start.MaxY && split.EndY == End.MaxY);

		if (isFaceInCeilingVoid && isAnyWall && !canHaveNonDiagonalCeilingPart) // Part of overdraw prevention
			return split; // Stop the loop, since the rest of the splits will also be in the void

		isInCeilingVoid = false;

		bool isWsFullyBelowMinY = IsWsFullyBelowMinY; // Technically should be classified as a wall if true

		bool isEitherStartPointBelowFloor = split.StartY < Start.MinY || split.EndY < End.MinY; // If either start point A or B is in the void below floor
		bool areBothStartPointsBelowFloor = split.StartY <= Start.MinY && split.EndY <= End.MinY; // Are both start points A and B in the void below floor

		(int startY, int endY) = (split.StartY, split.EndY);

		// Full walls can't have overdraw, so if either point is in void, then snap it to floor
		// Diagonal walls are an exception, since, even though they are walls, they have a flat ceiling bit, so we can allow overdraw
		if ((isEitherStartPointBelowFloor && (isAnyWall || isWsFullyBelowMinY) && !canHaveNonDiagonalCeilingPart) || areBothStartPointsBelowFloor)
		{
			// Snap points to floor
			startY = Start.MinY;
			endY = End.MinY;
		}
		else if (startY < WS.StartY || endY < WS.EndY) // If either split point is below WS
		{
			// Snap points to the heights of WS
			startY = WS.StartY;
			endY = WS.EndY;
		}

		return new WallSplit(startY, endY);
	}
}
