using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TombLib.LevelData;
using TombLib.LevelData.Properties;
using TombLib.Wad;
using TombEditor.Windows;

namespace WadTool.Controls.ContextMenus
{
    class MoveableContextMenu : BaseContextMenu
    {
        public MoveableContextMenu(WadToolClass tool, WadMoveableId moveableId)
            : base(tool)
        {
            Items.Add(new ToolStripMenuItem("Edit skeleton", Properties.Resources.edit_16, (o, e) =>
            {
                //using (var form = new FormSkeletonEditor(tool,WadToolClass.)
                //     _tool.SelectedObjectEdited();
            }));

            // Add "Edit Properties" menu item for TombEngine moveables
            var wad = tool.DestinationWad ?? tool.SourceWad;
            if (wad != null && wad.GameVersion == TRVersion.Game.TombEngine)
            {
                Items.Add(new ToolStripSeparator());
                Items.Add(new ToolStripMenuItem("Edit Properties", Properties.Resources.properties_16, (o, e) =>
                {
                    EditMoveableProperties(tool, moveableId);
                }));
            }
        }

        private void EditMoveableProperties(WadToolClass tool, WadMoveableId moveableId)
        {
            // Get the moveable from the appropriate WAD
            Wad2 wad = tool.DestinationWad ?? tool.SourceWad;
            if (wad == null || !wad.Moveables.ContainsKey(moveableId))
                return;

            var moveable = wad.Moveables[moveableId];

            // Load property definitions from XML
            var propertySet = PropertyManager.GetMoveableProperties(moveableId.ToString(wad.GameVersion));
            if (propertySet == null || propertySet.Properties.Count == 0)
            {
                MessageBox.Show(
                    "No property definitions found for this moveable.\n\n" +
                    "To define properties, create an XML file in:\n" +
                    "Resources/Properties/Moveables/" + moveableId.ShortName(wad.GameVersion) + ".xml",
                    "No Properties Defined",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Create a WadMoveable wrapper that acts like an ItemInstance
            var wadMoveableWrapper = new WadMoveableWrapper(moveable, moveableId, wad.GameVersion);

            // Open property editor window in Wadtool context (resets to XML defaults)
            var window = new PropertyEditorWindow(wadMoveableWrapper, propertySet, true, PropertyEditorContext.Wadtool);
            if (window.ShowDialog() == true)
            {
                // Properties were updated in the moveable.CustomProperties
                // Mark WAD as modified (if there's such a method)
                // tool.WadChanged(WadArea.Destination); // or similar
            }
        }
    }

    // Wrapper class to make WadMoveable compatible with PropertyEditorWindow
    internal class WadMoveableWrapper : MoveableInstance
    {
        private WadMoveableId _id;
        private TRVersion.Game _gameVersion;

        public WadMoveableWrapper(WadMoveable moveable, WadMoveableId id, TRVersion.Game gameVersion)
            : base()
        {
            _id = id;
            _gameVersion = gameVersion;
            WadObjectId = id;
            CustomProperties = moveable.CustomProperties;
        }

        public override ItemType ItemType => new ItemType(_id);

        public override string ToString()
        {
            return _id.ToString(_gameVersion);
        }
    }
}
