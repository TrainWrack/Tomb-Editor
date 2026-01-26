using DarkUI.Docking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TombLib.LevelData;
using TombLib.Rendering;
using TombLib.Wad;
using TombLib.Wad.Catalog;

namespace TombEditor.ToolWindows
{
    public partial class ItemBrowser : DarkToolWindow
    {
        private readonly Editor _editor;
        private HashSet<string> _selectedCategories = new HashSet<string>();
        private bool _staticsOnlyFilter = false;

        public ItemBrowser()
        {
            InitializeComponent();
            CommandHandler.AssignCommandsToControls(Editor.Instance, this, toolTip);

            _editor = Editor.Instance;
            _editor.EditorEventRaised += EditorEventRaised;

            lblFromWad.ForeColor = DarkUI.Config.Colors.DisabledText;
        }

        public void InitializeRendering(RenderingDevice device)
        {
            panelItem.InitializeRendering(device, _editor.Configuration.RenderingItem_Antialias);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _editor.EditorEventRaised -= EditorEventRaised;
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void EditorEventRaised(IEditorEvent obj)
        {
            // Update available items combo box
            if (obj is Editor.LoadedWadsChangedEvent ||
                obj is Editor.GameVersionChangedEvent ||
                obj is Editor.ConfigurationChangedEvent)
            {
                PopulateItemsList();

                if (comboItems.Items.Count > 0)
                {
                    // Check if any reloaded wads still have current selected item present. If they do, re-select it
                    // to preserve item list position. If item is not present, just reset selection to first item in the list.

                    if (_editor.ChosenItem.HasValue &&
                        _editor.Level.Settings.Wads.Any(w => w.Wad != null && ((!_editor.ChosenItem.Value.IsStatic && w.Wad.Moveables.Any(w2 => w2.Key == _editor.ChosenItem.Value.MoveableId)) ||
                                                                               ( _editor.ChosenItem.Value.IsStatic && w.Wad.Statics.Any  (w2 => w2.Key == _editor.ChosenItem.Value.StaticId)))))
                    {
                        ChoseItem(_editor.ChosenItem.Value);
                    }
                    else
                    {
                        comboItems.SelectedIndex = 0;

                        var allMoveables = _editor.Level.Settings.WadGetAllMoveables();
                        var allStatics = _editor.Level.Settings.WadGetAllStatics();

                        // Update visible conflicting item, otherwise it's not updated in 3D control.
                        if (comboItems.SelectedItem is WadMoveable)
                        {
                            var currentObject = (WadMoveableId)panelItem.CurrentObject.Id;
                            if (allMoveables.ContainsKey(currentObject))
                                panelItem.CurrentObject = allMoveables[currentObject];
                        }
                        else if (comboItems.SelectedItem is WadStatic)
                        {
                            var currentObject = (WadStaticId)panelItem.CurrentObject.Id;
                            if (allStatics.ContainsKey(currentObject))
                                panelItem.CurrentObject = allStatics[currentObject];
                        }
                    }
                }
            }

            // Update selection of items combo box
            if (obj is Editor.ChosenItemChangedEvent)
            {
                var e = (Editor.ChosenItemChangedEvent)obj;
                if (!e.Current.HasValue)
                    comboItems.SelectedItem = panelItem.CurrentObject = null;
                else
                    ChoseItem(e.Current.Value);
            }

            if (obj is Editor.ChosenItemChangedEvent ||
                obj is Editor.GameVersionChangedEvent ||
                obj is Editor.LevelChangedEvent ||
                obj is Editor.LoadedWadsChangedEvent ||
                obj is Editor.ConfigurationChangedEvent)
                FindLaraSkin();

            // Update tooltip texts
            if (obj is Editor.ConfigurationChangedEvent)
            {
                if (((Editor.ConfigurationChangedEvent)obj).UpdateKeyboardShortcuts)
                    CommandHandler.AssignCommandsToControls(_editor, this, toolTip, true);
            }

            // Activate default control
            if (obj is Editor.DefaultControlActivationEvent)
            {
                if (DockPanel != null && ((Editor.DefaultControlActivationEvent)obj).ContainerName == GetType().Name)
                {
                    MakeActive();
                    comboItems.Search();
                }
            }

            // Update UI
            if (obj is Editor.ConfigurationChangedEvent ||
                obj is Editor.InitEvent)
            {
                panelItem.AnimatePreview = _editor.Configuration.RenderingItem_Animate;
                lblFromWad.Visible = _editor.Configuration.RenderingItem_ShowMultipleWadsPrompt;
            }

        }

        private void ChoseItem(ItemType item)
        {
            if (item == null)
                return;

            if (item.IsStatic)
            {
                comboItems.SelectedItem = panelItem.CurrentObject = _editor.Level.Settings.WadTryGetStatic(item.StaticId);
            }
            else
            {
                if (!_editor.Configuration.RenderingItem_HideInternalObjects ||
                    !TrCatalog.IsHidden(_editor.Level.Settings.GameVersion, item.MoveableId.TypeId))
                {
                    comboItems.SelectedItem = panelItem.CurrentObject = _editor.Level.Settings.WadTryGetMoveable(item.MoveableId);
                }
            }

            MakeActive();
            panelItem.ResetCamera();
        }

        private void FindLaraSkin()
        {
            if (comboItems.Items.Count == 0 || comboItems.SelectedIndex < 0 || !(comboItems.SelectedItem is WadMoveable))
                return;

            var item = comboItems.SelectedItem as WadMoveable;
            var skinId = new WadMoveableId(TrCatalog.GetMoveableSkin(_editor.Level.Settings.GameVersion, item.Id.TypeId));
            var skin = _editor.Level.Settings.WadTryGetMoveable(skinId);

            if (skin != null && skin != item)
                panelItem.CurrentObject = item.ReplaceDummyMeshes(skin);
            else
                panelItem.CurrentObject = item;

            panelItem.ResetCamera();
        }

        private void comboItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboItems.SelectedItem == null)
                _editor.ChosenItem = null;
            else if (comboItems.SelectedItem is WadMoveable)
                _editor.ChosenItem = new ItemType(((WadMoveable)comboItems.SelectedItem).Id, _editor?.Level?.Settings);
            else if (comboItems.SelectedItem is WadStatic)
                _editor.ChosenItem = new ItemType(((WadStatic)comboItems.SelectedItem).Id, _editor?.Level?.Settings);

            if (_editor.ChosenItem != null)
            {
                bool multiple;
                var wad = _editor.Level.Settings.WadTryGetWad(_editor.ChosenItem.Value, out multiple);

                if (wad != null && multiple)
                {
                    lblFromWad.Text = "From " + Path.GetFileName(wad.Path);
                    toolTip.SetToolTip(lblFromWad, "This object exists in several wads." + "\n" + "Used one is: " + _editor.Level.Settings.MakeAbsolute(wad.Path, VariableType.LevelDirectory));
                    return;
                }
            }

            lblFromWad.Text = string.Empty;
            toolTip.SetToolTip(lblFromWad, string.Empty);
        }

        private void comboItems_Format(object sender, ListControlConvertEventArgs e)
        {
            TRVersion.Game? gameVersion = _editor?.Level?.Settings?.GameVersion;
            IWadObject listItem = e.ListItem as IWadObject;
            if (gameVersion != null && listItem != null)
                e.Value = listItem.ToString(gameVersion.Value);
        }

        private void butItemUp_Click(object sender, EventArgs e)
        {
            if (comboItems.Items.Count == 0)
                return;

            if (comboItems.SelectedIndex > 0)
                comboItems.SelectedIndex--;
            else
                comboItems.SelectedIndex = comboItems.Items.Count - 1;
        }

        private void butItemDown_Click(object sender, EventArgs e)
        {
            if (comboItems.Items.Count == 0)
                return;

            if (comboItems.SelectedIndex < comboItems.Items.Count - 1)
                comboItems.SelectedIndex++;
            else
                comboItems.SelectedIndex = 0;
        }

        private void PopulateItemsList()
        {
            var allMoveables = _editor.Level.Settings.WadGetAllMoveables();
            var allStatics = _editor.Level.Settings.WadGetAllStatics();

            comboItems.GameVersion = _editor.Level.Settings.GameVersion;
            comboItems.Items.Clear();

            // Add moveables with filtering
            if (!_staticsOnlyFilter)
            {
                foreach (var moveable in allMoveables.Values)
                {
                    if (!_editor.Configuration.RenderingItem_HideInternalObjects ||
                        !TrCatalog.IsHidden(_editor.Level.Settings.GameVersion, moveable.Id.TypeId))
                    {
                        // Apply category filter if any categories are selected
                        if (_selectedCategories.Count == 0 || ShouldIncludeMoveable(moveable))
                        {
                            comboItems.Items.Add(moveable);
                        }
                    }
                }
            }

            // Add statics with filtering
            foreach (var staticMesh in allStatics.Values)
            {
                // Apply category filter if any categories are selected
                if (_selectedCategories.Count == 0 || ShouldIncludeStatic(staticMesh))
                {
                    comboItems.Items.Add(staticMesh);
                }
            }
        }

        private bool ShouldIncludeMoveable(WadMoveable moveable)
        {
            var category = TrCatalog.GetMoveableCategory(_editor.Level.Settings.GameVersion, moveable.Id.TypeId);
            return !string.IsNullOrEmpty(category) && _selectedCategories.Contains(category);
        }

        private bool ShouldIncludeStatic(WadStatic staticMesh)
        {
            var category = TrCatalog.GetStaticCategory(_editor.Level.Settings.GameVersion, staticMesh.Id.TypeId);
            return !string.IsNullOrEmpty(category) && _selectedCategories.Contains(category);
        }

        private void butFilter_Click(object sender, EventArgs e)
        {
            // Get all available categories from the current game version
            var moveableCategories = TrCatalog.GetAllMoveableCategories(_editor.Level.Settings.GameVersion).ToList();
            var staticCategories = TrCatalog.GetAllStaticCategories(_editor.Level.Settings.GameVersion).ToList();
            var allCategories = moveableCategories.Union(staticCategories).Distinct().OrderBy(c => c).ToList();

            // Create context menu
            var contextMenu = new DarkUI.Controls.DarkContextMenu();

            // Add "Clear Filter" option
            var clearItem = new ToolStripMenuItem("Clear Filter");
            clearItem.Checked = _selectedCategories.Count == 0 && !_staticsOnlyFilter;
            clearItem.Click += (s, ev) =>
            {
                _selectedCategories.Clear();
                _staticsOnlyFilter = false;
                PopulateItemsList();
            };
            contextMenu.Items.Add(clearItem);

            // Add "Statics Only" option
            var staticsOnlyItem = new ToolStripMenuItem("Statics Only");
            staticsOnlyItem.Checked = _staticsOnlyFilter;
            staticsOnlyItem.Click += (s, ev) =>
            {
                _staticsOnlyFilter = !_staticsOnlyFilter;
                if (_staticsOnlyFilter)
                    _selectedCategories.Clear(); // Clear category filters when enabling statics only
                PopulateItemsList();
            };
            contextMenu.Items.Add(staticsOnlyItem);

            // Add separator
            contextMenu.Items.Add(new ToolStripSeparator());

            // Add category items
            foreach (var category in allCategories)
            {
                var categoryItem = new ToolStripMenuItem(category);
                categoryItem.Checked = _selectedCategories.Contains(category);
                categoryItem.Click += (s, ev) =>
                {
                    if (_selectedCategories.Contains(category))
                        _selectedCategories.Remove(category);
                    else
                    {
                        _selectedCategories.Add(category);
                        _staticsOnlyFilter = false; // Disable statics only when selecting categories
                    }
                    PopulateItemsList();
                };
                contextMenu.Items.Add(categoryItem);
            }

            // Show the menu below the filter button
            contextMenu.Show(butFilter, new System.Drawing.Point(0, butFilter.Height));
        }
    }
}
