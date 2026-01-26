using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TombLib.Utils;

namespace TombLib.LevelData.Properties
{
    /// <summary>
    /// Manages loading and caching of property definitions from XML files
    /// </summary>
    public class PropertyManager
    {
        private static PropertyManager _instance;
        private Dictionary<string, MoveablePropertySet> _moveableProperties;
        private StaticPropertySet _staticProperties;
        private string _propertiesDirectory;

        public static PropertyManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PropertyManager();
                return _instance;
            }
        }

        private PropertyManager()
        {
            _moveableProperties = new Dictionary<string, MoveablePropertySet>();
        }

        /// <summary>
        /// Sets the directory where property XML files are located
        /// </summary>
        public void SetPropertiesDirectory(string directory)
        {
            _propertiesDirectory = directory;
            LoadProperties();
        }

        /// <summary>
        /// Loads all property definitions from the properties directory
        /// </summary>
        private void LoadProperties()
        {
            if (string.IsNullOrEmpty(_propertiesDirectory) || !Directory.Exists(_propertiesDirectory))
                return;

            // Load static properties from shared file
            string staticPropertiesPath = Path.Combine(_propertiesDirectory, "StaticProperties.xml");
            if (File.Exists(staticPropertiesPath))
            {
                try
                {
                    _staticProperties = XmlUtils.ReadXmlFile<StaticPropertySet>(staticPropertiesPath);
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    System.Diagnostics.Debug.WriteLine($"Error loading static properties: {ex.Message}");
                }
            }

            // Load moveable properties from individual files
            string moveablePropertiesPath = Path.Combine(_propertiesDirectory, "Moveables");
            if (Directory.Exists(moveablePropertiesPath))
            {
                foreach (string file in Directory.GetFiles(moveablePropertiesPath, "*.xml"))
                {
                    try
                    {
                        string moveableName = Path.GetFileNameWithoutExtension(file);
                        var propertySet = XmlUtils.ReadXmlFile<MoveablePropertySet>(file);
                        _moveableProperties[moveableName] = propertySet;
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue
                        System.Diagnostics.Debug.WriteLine($"Error loading moveable properties from {file}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets property definitions for a specific moveable
        /// </summary>
        public MoveablePropertySet GetMoveableProperties(string moveableName)
        {
            if (_moveableProperties.ContainsKey(moveableName))
                return _moveableProperties[moveableName];

            // Return default moveable properties (HP and OCB)
            return GetDefaultMoveableProperties();
        }

        /// <summary>
        /// Gets the shared static property definitions
        /// </summary>
        public StaticPropertySet GetStaticProperties()
        {
            if (_staticProperties != null)
                return _staticProperties;

            // Return default static properties
            return GetDefaultStaticProperties();
        }

        /// <summary>
        /// Returns default mandatory properties for moveables
        /// </summary>
        public static MoveablePropertySet GetDefaultMoveableProperties()
        {
            var propertySet = new MoveablePropertySet();
            
            propertySet.Properties.Add(new PropertyDefinition
            {
                Name = "HP",
                Type = PropertyType.Integer,
                Default = "100",
                Description = "Hit Points",
                Min = "0",
                Max = "32767"
            });

            propertySet.Properties.Add(new PropertyDefinition
            {
                Name = "OCB",
                Type = PropertyType.Integer,
                Default = "0",
                Description = "Object Combination Block identifier",
                Min = "-32768",
                Max = "32767"
            });

            return propertySet;
        }

        /// <summary>
        /// Returns default mandatory properties for statics
        /// </summary>
        public static StaticPropertySet GetDefaultStaticProperties()
        {
            var propertySet = new StaticPropertySet();

            propertySet.Properties.Add(new PropertyDefinition
            {
                Name = "ShatterSound",
                Type = PropertyType.Dropdown,
                Default = "None",
                Description = "Sound played when object shatters",
                Options = new List<string> { "None", "Stone", "Wood", "Glass", "Custom" }
            });

            propertySet.Properties.Add(new PropertyDefinition
            {
                Name = "HP",
                Type = PropertyType.Integer,
                Default = "150",
                Description = "Hit Points",
                Min = "0",
                Max = "32767"
            });

            propertySet.Properties.Add(new PropertyDefinition
            {
                Name = "Shatter",
                Type = PropertyType.Boolean,
                Default = "false",
                Description = "Whether the object can shatter"
            });

            return propertySet;
        }

        /// <summary>
        /// Creates default property values based on definitions
        /// </summary>
        public static PropertyCollection CreateDefaultProperties(List<PropertyDefinition> definitions)
        {
            var collection = new PropertyCollection();

            foreach (var def in definitions)
            {
                object defaultValue = ParseDefaultValue(def);
                collection.SetProperty(def.Name, defaultValue);
            }

            return collection;
        }

        /// <summary>
        /// Parses the default value string based on property type
        /// </summary>
        private static object ParseDefaultValue(PropertyDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.Default))
                return GetTypeDefaultValue(definition.Type);

            try
            {
                switch (definition.Type)
                {
                    case PropertyType.Integer:
                        return int.Parse(definition.Default);

                    case PropertyType.Float:
                        return float.Parse(definition.Default);

                    case PropertyType.Boolean:
                        return bool.Parse(definition.Default);

                    case PropertyType.Dropdown:
                        return definition.Default;

                    case PropertyType.Checkbox:
                        // For checkbox lists, default can be comma-separated values
                        return definition.Default.Split(',').Select(s => s.Trim()).ToList();

                    default:
                        return definition.Default;
                }
            }
            catch
            {
                return GetTypeDefaultValue(definition.Type);
            }
        }

        /// <summary>
        /// Gets the default value for a property type
        /// </summary>
        private static object GetTypeDefaultValue(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.Integer:
                    return 0;
                case PropertyType.Float:
                    return 0.0f;
                case PropertyType.Boolean:
                    return false;
                case PropertyType.Dropdown:
                    return "";
                case PropertyType.Checkbox:
                    return new List<string>();
                default:
                    return null;
            }
        }
    }
}
