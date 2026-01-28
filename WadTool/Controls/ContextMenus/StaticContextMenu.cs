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
    class StaticContextMenu : BaseContextMenu
    {
        public StaticContextMenu(WadToolClass tool, WadStaticId staticId)
            : base(tool)
        {
            // Add "Edit Properties" menu item for TombEngine statics
            var wad = tool.DestinationWad ?? tool.SourceWad;
            if (wad != null && wad.GameVersion == TRVersion.Game.TombEngine)
            {
                Items.Add(new ToolStripMenuItem("Edit Properties", Properties.Resources.general_edit_16, (o, e) =>
                {
                    EditStaticProperties(tool, staticId);
                }));
            }
        }

        private void EditStaticProperties(WadToolClass tool, WadStaticId staticId)
        {
            // Get the static from the appropriate WAD
            Wad2 wad = tool.DestinationWad ?? tool.SourceWad;
            if (wad == null || !wad.Statics.ContainsKey(staticId))
                return;

            var staticMesh = wad.Statics[staticId];

            // Load property definitions from XML (statics use shared XML file)
            var propertySet = TombLib.LevelData.Properties.PropertyManager.Instance.GetStaticProperties();
            if (propertySet == null || propertySet.Properties.Count == 0)
            {
                MessageBox.Show(
                    "No property definitions found for statics.\n\n" +
                    "To define properties, create an XML file at:\n" +
                    "Resources/Properties/StaticProperties.xml",
                    "No Properties Defined",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Create a WadStatic wrapper that acts like an ItemInstance
            var wadStaticWrapper = new WadStaticWrapper(staticMesh, staticId, wad.GameVersion);

            // Open property editor window in Wadtool context (resets to XML defaults)
            var window = new PropertyEditorWindow(wadStaticWrapper, true, PropertyEditorContext.Wadtool);
            if (window.ShowDialog() == true)
            {
                // Properties were updated in the staticMesh.CustomProperties
                // Mark WAD as modified (if there's such a method)
                // tool.WadChanged(WadArea.Destination); // or similar
            }
        }
    }

    // Wrapper class to make WadStatic compatible with PropertyEditorWindow
    internal class WadStaticWrapper : StaticInstance
    {
        private WadStaticId _id;
        private TRVersion.Game _gameVersion;

        public WadStaticWrapper(WadStatic staticMesh, WadStaticId id, TRVersion.Game gameVersion)
            : base()
        {
            _id = id;
            _gameVersion = gameVersion;
            WadObjectId = id;
            CustomProperties = staticMesh.CustomProperties;
        }

        public override ItemType ItemType => new ItemType(_id);

        public override string ToString()
        {
            return _id.ToString(_gameVersion);
        }
    }
}
