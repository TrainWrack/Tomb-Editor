using DarkUI.Controls;
using DarkUI.Docking;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using TombLib.LevelData;
using TombLib.LevelData.Properties;

namespace TombEditor.ToolWindows
{
    public partial class PropertyWindow : DarkToolWindow
    {
        private readonly Editor _editor;
        private ItemInstance _currentInstance;

        public PropertyWindow()
        {
            InitializeComponent();
            
            _editor = Editor.Instance;
            _editor.EditorEventRaised += EditorEventRaised;

            // Initialize property manager
            InitializePropertyManager();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _editor.EditorEventRaised -= EditorEventRaised;
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializePropertyManager()
        {
            // Set the properties directory path
            string propertiesPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.ExecutablePath),
                "Resources", "Properties");

            if (System.IO.Directory.Exists(propertiesPath))
            {
                PropertyManager.Instance.SetPropertiesDirectory(propertiesPath);
            }
        }

        private void EditorEventRaised(IEditorEvent obj)
        {
            // Update when selection changes
            if (obj is Editor.SelectedObjectChangedEvent selectionEvent)
            {
                UpdateProperties(selectionEvent.Current);
            }

            // Update when object changes
            if (obj is Editor.ObjectChangedEvent objectEvent)
            {
                if (objectEvent.Object == _currentInstance)
                {
                    UpdateProperties(_currentInstance);
                }
            }

            // Clear when level changes
            if (obj is Editor.LevelChangedEvent)
            {
                UpdateProperties(null);
            }
        }

        private void UpdateProperties(ObjectInstance selectedObject)
        {
            // Only handle ItemInstance objects (MoveableInstance or StaticInstance)
            if (selectedObject is ItemInstance item && _editor.Level.IsTombEngine)
            {
                _currentInstance = item;
                
                // Create a custom object that wraps the instance with PropertyDescriptors
                var wrapper = new ItemPropertiesWrapper(item);
                propertyGrid.SelectedObject = wrapper;
            }
            else
            {
                _currentInstance = null;
                propertyGrid.SelectedObject = null;
            }
        }

        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_currentInstance != null)
            {
                // Notify editor that object has changed
                _editor.ObjectChange(_currentInstance, ObjectChangeType.Change);
            }
        }
    }

    /// <summary>
    /// Wrapper class that provides PropertyDescriptor-based access to ItemInstance properties
    /// </summary>
    internal class ItemPropertiesWrapper : ICustomTypeDescriptor
    {
        private readonly ItemInstance _instance;
        private readonly PropertyDescriptorCollection _properties;

        public ItemPropertiesWrapper(ItemInstance instance)
        {
            _instance = instance;
            
            // Build property descriptors based on the instance type
            var descriptors = new System.Collections.Generic.List<PropertyDescriptor>();

            if (_instance is MoveableInstance moveable)
            {
                var propertySet = PropertyManager.Instance.GetMoveableProperties(
                    moveable.WadObjectId.ToString(TRVersion.Game.TombEngine));
                
                foreach (var propDef in propertySet.Properties)
                {
                    descriptors.Add(new CustomPropertyDescriptor(_instance, propDef));
                }
            }
            else if (_instance is StaticInstance staticMesh)
            {
                var propertySet = PropertyManager.Instance.GetStaticProperties();
                
                foreach (var propDef in propertySet.Properties)
                {
                    descriptors.Add(new CustomPropertyDescriptor(_instance, propDef));
                }
            }

            _properties = new PropertyDescriptorCollection(descriptors.ToArray());
        }

        public AttributeCollection GetAttributes() => AttributeCollection.Empty;
        public string GetClassName() => _instance.GetType().Name;
        public string GetComponentName() => _instance.ToString();
        public TypeConverter GetConverter() => null;
        public EventDescriptor GetDefaultEvent() => null;
        public PropertyDescriptor GetDefaultProperty() => null;
        public object GetEditor(Type editorBaseType) => null;
        public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;
        public EventDescriptorCollection GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;
        public PropertyDescriptorCollection GetProperties() => _properties;
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => _properties;
        public object GetPropertyOwner(PropertyDescriptor pd) => this;
    }

    /// <summary>
    /// Custom PropertyDescriptor that wraps a PropertyDefinition and provides access to the ItemInstance
    /// </summary>
    internal class CustomPropertyDescriptor : PropertyDescriptor
    {
        private readonly ItemInstance _instance;
        private readonly PropertyDefinition _definition;

        public CustomPropertyDescriptor(ItemInstance instance, PropertyDefinition definition)
            : base(definition.Name, GetAttributes(definition))
        {
            _instance = instance;
            _definition = definition;
        }

        private static Attribute[] GetAttributes(PropertyDefinition definition)
        {
            var attrs = new System.Collections.Generic.List<Attribute>();
            
            // Add description
            if (!string.IsNullOrEmpty(definition.Description))
                attrs.Add(new DescriptionAttribute(definition.Description));
            
            // Add TypeConverter for dropdowns
            if (definition.Type == PropertyType.Dropdown && definition.Options != null && definition.Options.Count > 0)
            {
                attrs.Add(new TypeConverterAttribute(typeof(DropdownConverter)));
            }
            
            // Add editor for color picker
            if (definition.Type == PropertyType.Color)
            {
                attrs.Add(new EditorAttribute(typeof(System.Drawing.Design.ColorEditor), typeof(System.Drawing.Design.UITypeEditor)));
            }

            return attrs.ToArray();
        }

        public override Type ComponentType => typeof(ItemPropertiesWrapper);
        public override bool IsReadOnly => false;
        public override Type PropertyType
        {
            get
            {
                switch (_definition.Type)
                {
                    case PropertyType.Integer:
                        return typeof(int);
                    case PropertyType.Float:
                        return typeof(float);
                    case PropertyType.Boolean:
                        return typeof(bool);
                    case PropertyType.Dropdown:
                    case PropertyType.Checkbox:
                        return typeof(string);
                    case PropertyType.Color:
                        return typeof(System.Drawing.Color);
                    default:
                        return typeof(string);
                }
            }
        }

        public override bool CanResetValue(object component) => true;

        // Expose property definition so TypeConverter can access it
        public PropertyDefinition PropertyDefinition => _definition;

        public override object GetValue(object component)
        {
            // Special handling for OCB
            if (_definition.Name == "OCB")
            {
                return (int)_instance.Ocb;
            }

            // Get from CustomProperties
            var customProps = _instance is MoveableInstance m ? m.CustomProperties : 
                             _instance is StaticInstance s ? s.CustomProperties : null;

            if (customProps == null)
                return GetDefaultValue();

            switch (_definition.Type)
            {
                case PropertyType.Integer:
                    return customProps.GetProperty(_definition.Name, ParseInt(_definition.Default));
                case PropertyType.Float:
                    return customProps.GetProperty(_definition.Name, ParseFloat(_definition.Default));
                case PropertyType.Boolean:
                    return customProps.GetProperty(_definition.Name, ParseBool(_definition.Default));
                case PropertyType.Color:
                    var colorStr = customProps.GetProperty(_definition.Name, _definition.Default ?? "#FFFFFF") as string;
                    return ParseColor(colorStr);
                case PropertyType.Dropdown:
                case PropertyType.Checkbox:
                    return customProps.GetProperty(_definition.Name, _definition.Default ?? "");
                default:
                    return customProps.GetProperty(_definition.Name, _definition.Default ?? "");
            }
        }

        public override void SetValue(object component, object value)
        {
            // Special handling for OCB
            if (_definition.Name == "OCB")
            {
                _instance.Ocb = (short)(int)value;
                return;
            }

            // Set in CustomProperties
            var customProps = _instance is MoveableInstance m ? m.CustomProperties : 
                             _instance is StaticInstance s ? s.CustomProperties : null;

            if (customProps == null)
                return;

            // Convert color to hex string
            if (_definition.Type == PropertyType.Color && value is System.Drawing.Color color)
            {
                value = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            customProps.SetProperty(_definition.Name, value);
        }

        public override void ResetValue(object component)
        {
            SetValue(component, GetDefaultValue());
        }

        public override bool ShouldSerializeValue(object component) => false;

        private object GetDefaultValue()
        {
            switch (_definition.Type)
            {
                case PropertyType.Integer:
                    return ParseInt(_definition.Default);
                case PropertyType.Float:
                    return ParseFloat(_definition.Default);
                case PropertyType.Boolean:
                    return ParseBool(_definition.Default);
                case PropertyType.Color:
                    return ParseColor(_definition.Default ?? "#FFFFFF");
                default:
                    return _definition.Default ?? "";
            }
        }

        private int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        private float ParseFloat(string value)
        {
            return float.TryParse(value, out float result) ? result : 0f;
        }

        private bool ParseBool(string value)
        {
            return bool.TryParse(value, out bool result) && result;
        }

        private System.Drawing.Color ParseColor(string value)
        {
            if (string.IsNullOrEmpty(value))
                return System.Drawing.Color.White;

            try
            {
                return System.Drawing.ColorTranslator.FromHtml(value);
            }
            catch
            {
                return System.Drawing.Color.White;
            }
        }
    }

    /// <summary>
    /// TypeConverter for dropdown properties
    /// </summary>
    internal class DropdownConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (context?.PropertyDescriptor is CustomPropertyDescriptor customDesc)
            {
                var options = customDesc.PropertyDefinition.Options;
                if (options != null && options.Count > 0)
                {
                    return new StandardValuesCollection(options);
                }
            }
            return new StandardValuesCollection(new string[0]);
        }
    }
}
