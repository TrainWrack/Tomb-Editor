﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using NLog;
using TombLib.Graphics;
using TombEditor.Geometry;
using Rectangle = System.Drawing.Rectangle;
using System.Drawing.Drawing2D;

namespace TombEditor.Controls
{
    public class Panel2DGrid : Panel
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private bool _doSectorSelection = false;
        private Editor _editor;

        private static readonly float _outlineHighlightWidth = 4;
        private static readonly Pen _gridPen = Pens.Black;
        private static readonly Pen _selectedPortalPen = new Pen(Color.YellowGreen, 2);
        private static readonly Pen _selectedTriggerPen = new Pen(Color.White, 2);
        private static readonly Pen _selectionPen = new Pen(Color.Red, 2);
        private static readonly Brush _floorBrush = new SolidBrush(Utils.ToWinFormsColor(Editor.ColorFloor, true));
        private static readonly Brush _wallBrush = new SolidBrush(Utils.ToWinFormsColor(Editor.ColorWall, true));
        private static readonly Brush _borderWallBrush = new SolidBrush(Color.Gray);
        private static readonly Brush _forceFloorSolidBrush = new HatchBrush(HatchStyle.WideUpwardDiagonal, Color.Transparent, Utils.ToWinFormsColor(Editor.ColorFloor, true).MixWith(Color.Black, 0.1));
        private static readonly Brush _climbBrush = new SolidBrush(Utils.ToWinFormsColor(Editor.ColorClimb, true));

        private float _gridSize => Math.Min(ClientSize.Width, ClientSize.Height);
        private float _gridStep => _gridSize / Room.MaxRoomDimensions;

        public string Message;

        public Panel2DGrid()
        {
            DoubleBuffered = true;

            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                _editor = Editor.Instance;
                _editor.EditorEventRaised += EditorEventRaised;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _editor.EditorEventRaised -= EditorEventRaised;
            base.Dispose(disposing);
        }

        private void EditorEventRaised(IEditorEvent obj)
        {
            // Update drawing
            if ((obj is Editor.ChangeHighlightEvent) ||
                (obj is Editor.SelectedRoomChangedEvent) ||
                (obj is Editor.SelectedSectorsChangedEvent) ||
                (obj is Editor.RoomSectorPropertiesChangedEvent) ||
                ((obj is Editor.SelectedObjectChangedEvent) && IsObjectChangeRelevant((Editor.SelectedObjectChangedEvent)obj)))
            {
                Invalidate();
            }

            // Update curser
            if (obj is Editor.ActionChangedEvent)
            {
                EditorAction currentAction = ((Editor.ActionChangedEvent)obj).Current;
                Cursor = currentAction.RelocateCameraActive ? Cursors.Cross : Cursors.Arrow;
            }
        }

        private static bool IsObjectChangeRelevant(Editor.SelectedObjectChangedEvent e)
        {
            return (e.Previous is SectorBasedObjectInstance) || (e.Current is SectorBasedObjectInstance);
        }

        private RectangleF getVisualAreaTotal()
        {
            return new RectangleF(
                (ClientSize.Width - _gridSize) * 0.5f,
                (ClientSize.Height - _gridSize) * 0.5f,
                _gridSize,
                _gridSize);
        }

        private RectangleF getVisualAreaRoom()
        {
            Room currentRoom = _editor.SelectedRoom;
            RectangleF totalArea = getVisualAreaTotal();

            return new RectangleF(
                totalArea.X + _gridStep * ((Room.MaxRoomDimensions - currentRoom.NumXSectors) / 2),
                totalArea.Y + _gridStep * ((Room.MaxRoomDimensions - currentRoom.NumZSectors) / 2),
                _gridStep * currentRoom.NumXSectors,
                _gridStep * currentRoom.NumZSectors);
        }

        public PointF toVisualCoord(SharpDX.DrawingPoint sectorCoord)
        {
            RectangleF roomArea = getVisualAreaRoom();
            return new PointF(sectorCoord.X * _gridStep + roomArea.X, roomArea.Bottom - (sectorCoord.Y + 1) * _gridStep);
        }

        public RectangleF ToVisualCoord(SharpDX.Rectangle sectorArea)
        {
            PointF convertedPoint0 = toVisualCoord(new SharpDX.DrawingPoint(sectorArea.Left, sectorArea.Top));
            PointF convertedPoint1 = toVisualCoord(new SharpDX.DrawingPoint(sectorArea.Right, sectorArea.Bottom));
            return RectangleF.FromLTRB(
                Math.Min(convertedPoint0.X, convertedPoint1.X), Math.Min(convertedPoint0.Y, convertedPoint1.Y),
                Math.Max(convertedPoint0.X, convertedPoint1.X) + _gridStep, Math.Max(convertedPoint0.Y, convertedPoint1.Y) + _gridStep);
        }

        public SharpDX.DrawingPoint FromVisualCoord(PointF point)
        {
            RectangleF roomArea = getVisualAreaRoom();
            return new SharpDX.DrawingPoint(
                (int)Math.Max(0, Math.Min(_editor.SelectedRoom.NumXSectors - 1, (point.X - roomArea.X) / _gridStep)),
                (int)Math.Max(0, Math.Min(_editor.SelectedRoom.NumZSectors - 1, (roomArea.Bottom - point.Y) / _gridStep)));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if ((_editor == null) || (_editor.SelectedRoom == null))
                return;

            // Move camera to selected sector
            if (_editor.Action.RelocateCameraActive)
            {
                _editor.MoveCameraToSector(FromVisualCoord(e.Location));
                return;
            }
            SharpDX.DrawingPoint sectorPos = FromVisualCoord(e.Location);

            // Find existing sector based object (eg portal or trigger)
            SectorBasedObjectInstance selectedSectorObject = _editor.SelectedObject as SectorBasedObjectInstance;

            // Choose action
            if (e.Button == MouseButtons.Left)
            {
                if ((selectedSectorObject != null) &&
                    (selectedSectorObject.Room == _editor.SelectedRoom) &&
                    selectedSectorObject.Area.Contains(sectorPos))
                {
                    if (selectedSectorObject is PortalInstance)
                    {
                        Room room = _editor.SelectedRoom;
                        _editor.SelectRoomAndResetCamera(((PortalInstance)selectedSectorObject).AdjoiningRoom);
                        _editor.SelectedObject = ((PortalInstance)selectedSectorObject).FindOppositePortal(room);
                    }
                    else if (selectedSectorObject is TriggerInstance)
                    { // Open trigger options
                        EditorActions.EditObject(selectedSectorObject, this.Parent);
                    }
                }
                else
                { // Do block selection
                    _editor.SelectedSectors = new SectorSelection { Start = sectorPos, End = sectorPos };

                    if (selectedSectorObject != null) // Unselect previous object
                        _editor.SelectedObject = null;
                    _doSectorSelection = true;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Find next object
                var portalsInRoom = _editor.SelectedRoom.Portals.Cast<SectorBasedObjectInstance>();
                var triggersInRoom = _editor.SelectedRoom.Triggers.Cast<SectorBasedObjectInstance>();
                var relevantTriggersAndPortals = portalsInRoom.Concat(triggersInRoom)
                    .Where((obj) => obj.Area.Contains(sectorPos));

                SectorBasedObjectInstance nextPortalOrTrigger = relevantTriggersAndPortals.
                    FindFirstAfterWithWrapAround((obj) => obj == selectedSectorObject, (obj) => true);
                if (nextPortalOrTrigger != null)
                {
                    _editor.SelectedObject = nextPortalOrTrigger;
                    Message = nextPortalOrTrigger.ToString();
                }
                else
                    Message = null;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if ((_editor?.SelectedRoom == null) || _editor.Action.RelocateCameraActive)
                return;

            if ((e.Button == MouseButtons.Left) && _doSectorSelection)
                _editor.SelectedSectors = new SectorSelection { Start = _editor.SelectedSectors.Start, End = FromVisualCoord(e.Location) };
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            _doSectorSelection = false;
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                if ((_editor == null) || (_editor.SelectedRoom == null))
                    return;

                Room currentRoom = _editor.SelectedRoom;
                RectangleF totalArea = getVisualAreaTotal();
                RectangleF roomArea = getVisualAreaRoom();
                bool probePortals = _editor.Configuration.Editor_ProbeAttributesThroughPortals;

                e.Graphics.FillRectangle(Brushes.White, totalArea);

                // Draw tiles
                for (int x = 0; x < currentRoom.NumXSectors; x++)
                    for (int z = 0; z < currentRoom.NumZSectors; z++)
                    {
                        RectangleF rectangle = new RectangleF(roomArea.X + x * _gridStep, roomArea.Y + (currentRoom.NumZSectors - 1 - z) * _gridStep, _gridStep, _gridStep);
                        Block block = currentRoom.Blocks[x, z];
                        Block bottomBlock = currentRoom.ProbeLowestBlock(x, z, probePortals).Block;
                        
                        e.Graphics.FillRectangle(_floorBrush, rectangle);

                        // Draw border wall
                        if (block.Type == BlockType.BorderWall)
                            e.Graphics.FillRectangle(_borderWallBrush, rectangle);

                        // Draw solid sector attributes
                        var currentHighlight = _editor.HighlightManager.GetColor(currentRoom, x, z, probePortals);
                        if(currentHighlight.HasValue)
                            using (var b = new SolidBrush(Utils.ToWinFormsColor(currentHighlight.Value, true)))
                                e.Graphics.FillRectangle(b, rectangle);

                        // Always overlay any solid sector attributes with ForceFloorSolid semi-transparent brush
                        if (block.ForceFloorSolid)
                            e.Graphics.FillRectangle(_forceFloorSolidBrush, rectangle);

                        // Draw framed sector attributes
                        currentHighlight = _editor.HighlightManager.GetColor(currentRoom, x, z, probePortals, true);
                        if (currentHighlight.HasValue)
                            using (var b = new Pen(Utils.ToWinFormsColor(currentHighlight.Value, true), _outlineHighlightWidth))
                                e.Graphics.DrawRectangle(b, rectangle);

                        // Always draw climb above any other attributes
                        if (bottomBlock.Flags.HasFlag(BlockFlags.ClimbPositiveZ))
                            e.Graphics.FillRectangle(_climbBrush, rectangle.X, rectangle.Y, rectangle.Width, _outlineHighlightWidth);
                        if (bottomBlock.Flags.HasFlag(BlockFlags.ClimbPositiveX))
                            e.Graphics.FillRectangle(_climbBrush, rectangle.Right - _outlineHighlightWidth, rectangle.Y, _outlineHighlightWidth, rectangle.Height);
                        if (bottomBlock.Flags.HasFlag(BlockFlags.ClimbNegativeZ))
                            e.Graphics.FillRectangle(_climbBrush, rectangle.X, rectangle.Bottom - _outlineHighlightWidth, rectangle.Width, _outlineHighlightWidth);
                        if (bottomBlock.Flags.HasFlag(BlockFlags.ClimbNegativeX))
                            e.Graphics.FillRectangle(_climbBrush, rectangle.X, rectangle.Y, _outlineHighlightWidth, rectangle.Height);

                        // Draw walls
                        if (block.Type == BlockType.Wall)
                        {
                            if (block.FloorDiagonalSplit == DiagonalSplit.None)
                                e.Graphics.FillRectangle(_wallBrush, rectangle);
                            else
                            {
                                PointF[] points = new PointF[3];

                                switch (block.FloorDiagonalSplit)
                                {
                                    case DiagonalSplit.XnZn:
                                        points[0] = new PointF(rectangle.Left, rectangle.Top);
                                        points[1] = new PointF(rectangle.Left, rectangle.Bottom);
                                        points[2] = new PointF(rectangle.Right, rectangle.Bottom);
                                        break;
                                    case DiagonalSplit.XnZp:
                                        points[0] = new PointF(rectangle.Left, rectangle.Bottom);
                                        points[1] = new PointF(rectangle.Left, rectangle.Top);
                                        points[2] = new PointF(rectangle.Right, rectangle.Top);
                                        break;
                                    case DiagonalSplit.XpZn:
                                        points[0] = new PointF(rectangle.Left, rectangle.Bottom);
                                        points[1] = new PointF(rectangle.Right, rectangle.Top);
                                        points[2] = new PointF(rectangle.Right, rectangle.Bottom);
                                        break;
                                    case DiagonalSplit.XpZp:
                                        points[0] = new PointF(rectangle.Left, rectangle.Top);
                                        points[1] = new PointF(rectangle.Right, rectangle.Top);
                                        points[2] = new PointF(rectangle.Right, rectangle.Bottom);
                                        break;

                                }
                                e.Graphics.FillPolygon(_wallBrush, points);
                            }
                        }
                    }

                // Draw black grid lines
                for (int x = 0; x <= Room.MaxRoomDimensions; ++x)
                    e.Graphics.DrawLine(_gridPen, totalArea.X + x * _gridStep, totalArea.Y, totalArea.X + x * _gridStep, totalArea.Y + _gridSize);

                for (int y = 0; y <= Room.MaxRoomDimensions; ++y)
                    e.Graphics.DrawLine(_gridPen, totalArea.X, totalArea.Y + y * _gridStep, totalArea.X + _gridSize, totalArea.Y + y * _gridStep);

                // Draw selection
                if (_editor.SelectedSectors.Valid)
                    e.Graphics.DrawRectangle(_selectionPen, ToVisualCoord(_editor.SelectedSectors.Area));

                var instance = _editor.SelectedObject as SectorBasedObjectInstance;
                if ((instance != null) && (instance.Room == _editor.SelectedRoom))
                {
                    Pen pen = instance is PortalInstance ? _selectedPortalPen : _selectedTriggerPen;
                    RectangleF visualArea = ToVisualCoord(instance.Area);
                    e.Graphics.DrawRectangle(pen, visualArea);
                }
            }
            catch (Exception exc)
            {
                logger.Error(exc, "An exception occured while drawing the 2D grid.");
            }
        }
    }
}
