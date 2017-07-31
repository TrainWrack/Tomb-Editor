﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using System.Windows.Forms;
using System.IO;
using TombLib.Graphics;
using TombEditor.Geometry;
using TombEditor.Controls;
using SharpDX.Direct3D;
using System.Runtime.InteropServices;

namespace TombEditor
{
    public enum EditorMode
    {
        None, Geometry, Map2D, FaceEdit, Lighting
    }

    public enum EditorAction
    {
        None, PlaceItem, PlaceLight, PlaceCamera, PlaceFlyByCamera, PlaceSound, PlaceSink, PlaceNoCollision
    }

    public enum EditorSubAction
    {
        None, GeometryBeginSelection, GeometrySelecting, GeometrySelected
    }

    public enum EditorItemType
    {
        Moveable, StaticMesh
    }

    public class Editor
    {
        public static readonly System.Drawing.Color ColorFloor = System.Drawing.Color.FromArgb(0, 190, 190);
        public static readonly System.Drawing.Color ColorWall = System.Drawing.Color.FromArgb(0, 160, 0);
        public static readonly System.Drawing.Color ColorTrigger = System.Drawing.Color.FromArgb(200, 0, 200);
        public static readonly System.Drawing.Color ColorMonkey = System.Drawing.Color.FromArgb(255, 100, 100);
        public static readonly System.Drawing.Color ColorBox = System.Drawing.Color.FromArgb(100, 100, 100);
        public static readonly System.Drawing.Color ColorDeath = System.Drawing.Color.FromArgb(20, 240, 20);
        public static readonly System.Drawing.Color ColorClimb = System.Drawing.Color.FromArgb(0, 100, 0);
        public static readonly System.Drawing.Color ColorNoCollision = System.Drawing.Color.FromArgb(255, 128, 0, 0);
        public static readonly System.Drawing.Color ColorNotWalkable = System.Drawing.Color.FromArgb(0, 0, 100);

        // istanza dell'editor
        private static Editor _instance;

        public Dictionary<string, Texture2D> Textures { get; } = new Dictionary<string, Texture2D>();
        public Dictionary<string, Effect> Effects { get; } = new Dictionary<string, Effect>();
        public Level Level { get; set; }
        public LightType LightType { get; set; }
        public Control RenderControl { get; set; }
        public GraphicsDevice GraphicsDevice { get; set; }
        public SpriteFont Font { get; set; }
        public SpriteBatch DebugSprites { get; set; }
        public EditorAction Action { get; set; }
        public EditorMode Mode { get; set; }
        public EditorSubAction SubAction { get; set; }
        public EditorItemType ItemType { get; set; }
        public PickingResult PickingResult { get; set; }
        public System.Drawing.Point BlockSelectionStart { get; set; } = new System.Drawing.Point(-1, -1);
        public System.Drawing.Point BlockSelectionEnd { get; set; } = new System.Drawing.Point(-1, -1);
        public int BlockEditingType { get; set; }
        public Dictionary<int, string> MoveableNames { get; } = new Dictionary<int, string>();
        public Dictionary<int, string> StaticNames { get; } = new Dictionary<int, string>();
        public int SelectedItem { get; set; } = -1;
        public Room SelectedRoom { get; set; }
        public bool IsFlipMap { get; set; }
        public int FlipMap { get; set; } = -1;
        public bool DrawPortals { get; set; }
        public int SelectedTexture { get; set; } = -1;
        public Vector2[] UV { get; } = new Vector2[4];
        public TextureTileType TextureTriangle { get; set; }
        public int LightIndex { get; set; } = -1;
        public List<System.Drawing.Color> Palette { get; } = new List<System.Drawing.Color>();
        public bool NoCollision { get; set; }
        public bool InvisiblePolygon { get; set; }
        public bool DoubleSided { get; set; }
        public bool Transparent { get; set; }
        public bool DrawRoomNames { get; set; }
        public bool DrawHorizon { get; set; }
        public float FPS { get; set; }
        public short SoundID { get; set; }
        public bool RelocateCameraActive { get; set; }

        private Panel2DGrid _panelGrid;
        private PanelRendering3D _panel3D;
        private FormMain _formEditor;

        // le griglie XYZ e la griglia free
        private Buffer<VertexPositionColor>[] _grids = new Buffer<VertexPositionColor>[4];
        private VertexInputLayout _gridLayout;

        private Editor()
        {
            _instance = this;

            for (int i = 0; i < 100; i++)
                StaticNames.Add(i, "Static Mesh #" + i);

        }

        public void Initialize(PanelRendering3D renderControl, Panel2DGrid grid, FormMain formEditor)
        {
            // Load configuration
            Configuration.LoadConfiguration();

#if DEBUG

#else
            // Hide the console in release mode
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
#endif
            using (BinaryReader readerPalette = new BinaryReader(File.OpenRead("Editor\\Palette.bin")))
                while (readerPalette.BaseStream.Position < readerPalette.BaseStream.Length)
                    Palette.Add(System.Drawing.Color.FromArgb(255, readerPalette.ReadByte(), readerPalette.ReadByte(), readerPalette.ReadByte()));

            _panel3D = renderControl;
            _panelGrid = grid;
            _formEditor = formEditor;

            GraphicsDevice = GraphicsDevice.New(DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.None,
                                                FeatureLevel.Level_10_0);

            PresentationParameters pp = new PresentationParameters();
            pp.BackBufferFormat = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
            pp.BackBufferWidth = _panel3D.Width;
            pp.BackBufferHeight = _panel3D.Height;
            pp.DepthStencilFormat = DepthFormat.Depth24Stencil8;
            pp.DeviceWindowHandle = _panel3D;
            pp.IsFullScreen = false;
            pp.MultiSampleCount = MSAALevel.None;
            pp.PresentationInterval = PresentInterval.Immediate;
            pp.RenderTargetUsage = SharpDX.DXGI.Usage.RenderTargetOutput | SharpDX.DXGI.Usage.BackBuffer;
            pp.Flags = SharpDX.DXGI.SwapChainFlags.None;
            
            SwapChainGraphicsPresenter presenter = new SwapChainGraphicsPresenter(GraphicsDevice, pp);
            GraphicsDevice.Presenter = presenter;
           
            // compilo gli effetti
            IEnumerable<string> effectFiles = Directory.EnumerateFiles(EditorPath + "\\Editor", "*.fx");
            foreach (string fileName in effectFiles)
            {
                string effectName = Path.GetFileNameWithoutExtension(fileName);
                Effects.Add(effectName, LoadEffect(fileName));
            }

            // aggiungo il BasicEffect
            BasicEffect bEffect = new BasicEffect(GraphicsDevice);
            Effects.Add("Toolkit.BasicEffect", bEffect);

            // carico le texture dell'editor
            IEnumerable<string> textureFiles = Directory.EnumerateFiles(EditorPath + "\\Editor", "*.png");
            foreach (string fileName in textureFiles)
            {
                string textureName = Path.GetFileNameWithoutExtension(fileName);
                Textures.Add(textureName, TombLib.Graphics.TextureLoad.LoadToTexture(GraphicsDevice, fileName));
            }

            // carico il Editor
            SpriteFontData fontData = SpriteFontData.Load("Editor\\Font.bin");
            Font = SpriteFont.New(GraphicsDevice, fontData);

            DebugSprites = new SpriteBatch(GraphicsDevice);

            // carico gli id degli oggetti
            using (StreamReader reader = new StreamReader("Editor\\Objects.txt"))
                while (reader.EndOfStream == false)
                {
                    string line = reader.ReadLine();
                    string[] tokens = line.Split(';');
                    MoveableNames.Add(Int32.Parse(tokens[0]), tokens[1]);
                }
            
            Debug.Initialize();
        }

        public void ChangeLightColorFromPalette()
        {
            _formEditor.ChangeLightColorFromPalette();
        }

        public void ResetPanel3DCursor()
        {
            _formEditor.ResetPanel3DCursor();
        }

        private Effect LoadEffect(string fileName)
        {
            EffectCompilerResult result = EffectCompiler.CompileFromFile(fileName);

            if (result.HasErrors)
            {
                string errors = "";
                foreach (var err in result.Logger.Messages)
                    errors += err + Environment.NewLine;

                // Hide all forms because we otherwise crash when trying to render the form for showing the message box.
                foreach (Form form in Application.OpenForms)
                    form.Hide();

                MessageBox.Show("Could not compile effect '" + fileName + "'" + Environment.NewLine + errors, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            Effect effect = new SharpDX.Toolkit.Graphics.Effect(GraphicsDevice, result.EffectData);
            return effect;
        }

        public void MoveCameraToSector(int sectorX, int sectorZ)
        {
            _panel3D.MoveCameraToSector(sectorX, sectorZ);
        }
        public void LoadTriggersInUI()
        {
            _formEditor.LoadTriggersInUI();
        }

        public static string EditorPath
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            }
        }

        public static Editor Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                else
                    return new Editor();
            }
        }

        public void PrepareXGrid(int rows, int columns, float pass)
        {
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();

            for (int x = -rows / 2; x < rows / 2; x++)
            {
                VertexPositionColor vertex = new VertexPositionColor(new Vector3(x * pass, 0, -columns / 2 * pass), Color.White);
                vertices.Add(vertex);
                vertex = new VertexPositionColor(new Vector3(x * pass, 0, columns / 2 * pass), Color.White);
                vertices.Add(vertex);
            }

            for (int z = -columns / 2; z < columns / 2; z++)
            {
                VertexPositionColor vertex = new VertexPositionColor(new Vector3(-rows / 2 * pass, 0, z * pass), Color.White);
                vertices.Add(vertex);
                vertex = new VertexPositionColor(new Vector3(rows / 2 * pass, 0, z * pass), Color.White);
                vertices.Add(vertex);
            }

            // creo il vertex buffer
            if (_grids[0] == null)
                _grids[0] = Buffer<VertexPositionColor>.New<VertexPositionColor>(GraphicsDevice, vertices.ToArray(), BufferFlags.VertexBuffer);

            // creo il vertex input layout
            if (_gridLayout == null)
                _gridLayout = VertexInputLayout.FromBuffer<VertexPositionColor>(0, _grids[0]);
        }

        public void LoadStaticMeshColorInUI()
        {
            _formEditor.LoadStaticMeshColorInUI();
        }

        public void DrawPanelGrid()
        {
            _panelGrid.Invalidate();
        }

        public void DrawPanel3D()
        {
            _panel3D.Draw();
        }

        public void DrawPanelMap2D()
        {
            _formEditor.DrawPanelMap2D();
        }

        public void ResetCamera()
        {
            _panel3D.ResetCamera();
        }

        public Camera Camera
        {
            get
            {
                return _panel3D.Camera;
            }
        }

        public void LoadTextureMapInEditor(Level level)
        {
            _formEditor.LoadTextureMapInEditor(level);
        }

        public string UpdateStatistics()
        {
            if (SelectedRoom == null || SelectedRoom == null)
                return "";

            string stats = "Room X: " + SelectedRoom.Position.X.ToString();
            stats += " Y floor: " + (SelectedRoom.Position.Y + SelectedRoom.GetLowestCorner());
            stats += " Y ceiling: " + (SelectedRoom.Position.Y + SelectedRoom.GetHighestCorner());
            stats += " Z: " + SelectedRoom.Position.Z.ToString();
            stats += "    Size: " + (SelectedRoom.NumXSectors - 2).ToString();
            stats += "x";
            stats += (SelectedRoom.NumZSectors - 2).ToString();
            stats += "    ";
            stats += "Portals: " + Level.Portals.Count;
            stats += "    ";
            stats += "Triggers: " + Level.Triggers.Count;

            _formEditor.UpdateStatistics();

            return stats;
        }

        public static PickingResult PickingResultEmpty;

        public void ResetSelection()
        {
            _formEditor.ResetSelection();
        }

        public void SelectRoom(Room room)
        {
            LightIndex = -1;
            BlockSelectionReset();
            LoadTriggersInUI();
    
            _formEditor.SelectRoom(room);
        }
        
        public void UpdateRoomName()
        {
            if (SelectedRoom == null)
                return;
            //_formEditor.UpdateLabelRoom(Level.Rooms[SelectedRoom].Name);
        }
        
        public void CenterCamera()
        {
            _formEditor.CenterCamera();
        }

        public void LoadWadInInterface()
        {
            _formEditor.LoadWadInInterface();
        }

        public void EditLight()
        {
            _formEditor.EditLight();
        }

        // The rectangle is (-1, -1, -1, 1) when nothing is selected.
        // The "Right" and "Bottom" point of the rectangle is inclusive.
        public Rectangle BlockSelection
        {
            get
            {
                return new SharpDX.Rectangle(
                    Math.Min(BlockSelectionStart.X, BlockSelectionEnd.X), Math.Min(BlockSelectionStart.Y, BlockSelectionEnd.Y),
                    Math.Max(BlockSelectionStart.X, BlockSelectionEnd.X), Math.Max(BlockSelectionStart.Y, BlockSelectionEnd.Y));
            }
            set
            {
                BlockSelectionStart = new System.Drawing.Point(value.X, value.Y);
                BlockSelectionEnd = new System.Drawing.Point(value.Right, value.Bottom);
            }
        }

        public void BlockSelectionReset()
        {
            BlockSelectionStart = new System.Drawing.Point(-1, -1);
            BlockSelectionEnd = new System.Drawing.Point(-1, -1);
        }

        public bool BlockSelectionAvailable
        {
           get { return (BlockSelectionStart.X != -1) && (BlockSelectionStart.Y != -1) && (BlockSelectionEnd.X != -1) && (BlockSelectionEnd.Y != -1); }
        }
    }
}
