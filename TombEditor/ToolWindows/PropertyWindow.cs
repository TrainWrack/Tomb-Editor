using DarkUI.Controls;
using DarkUI.Docking;
using System;
using System.Collections.Generic;
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
        private List<ItemInstance> _currentInstances;

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
                var selectedObject = selectionEvent.Current;
                
                // Check if it's an ObjectGroup (multiple selection)
                if (selectedObject is ObjectGroup group)
                {
                    var items = group.OfType<ItemInstance>().ToList();
                    UpdateProperties(items);
                }
                else if (selectedObject is ItemInstance item)
                {
                    UpdateProperties(new List<ItemInstance> { item });
                }
                else
                {
                    UpdateProperties(null);
                }
            }

            // Update when object changes
            if (obj is Editor.ObjectChangedEvent objectEvent)
            {
                if (_currentInstances != null && _currentInstances.Contains(objectEvent.Object))
                {
                    // Refresh the property grid without changing selection
                    propertyGrid.Refresh();
                }
            }

            // Clear when level changes
            if (obj is Editor.LevelChangedEvent)
            {
                UpdateProperties(null);
            }
        }

        private void UpdateProperties(List<ItemInstance> instances)
        {
            // Only handle ItemInstance objects for TombEngine
            if (instances != null && instances.Any() && _editor.Level.IsTombEngine)
            {
                _currentInstances = instances;
                
                // Create a custom object that wraps the instances with PropertyDescriptors
                var wrapper = new ItemPropertiesWrapper(instances);
                propertyGrid.SelectedObject = wrapper;
            }
            else
            {
                _currentInstances = null;
                propertyGrid.SelectedObject = null;
            }
        }

        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_currentInstances != null && _currentInstances.Any())
            {
                // Notify editor that objects have changed
                foreach (var instance in _currentInstances)
                {
                    _editor.ObjectChange(instance, ObjectChangeType.Change);
                }
            }
        }
    }

    /// <summary>
    /// Wrapper class that provides PropertyDescriptor-based access to ItemInstance properties
    /// Supports both single and multiple instance editing
    /// </summary>
    internal class ItemPropertiesWrapper : ICustomTypeDescriptor
    {
        private readonly List<ItemInstance> _instances;
        private readonly PropertyDescriptorCollection _properties;
        private readonly bool _isSameType;
        private readonly bool _isMoveable;

        public ItemPropertiesWrapper(List<ItemInstance> instances)
        {
            _instances = instances;
            
            // Determine if all instances are of the same type
            if (_instances.Count == 1)
            {
                _isSameType = true;
                _isMoveable = _instances[0] is MoveableInstance;
            }
            else
            {
                var firstType = _instances[0].GetType();
                _isSameType = _instances.All(i => i.GetType() == firstType);
                _isMoveable = _instances[0] is MoveableInstance;
                
                // For same type moveables, also check if they have the same WadObjectId
                if (_isSameType && _isMoveable)
                {
                    var firstId = (_instances[0] as MoveableInstance).WadObjectId;
                    _isSameType = _instances.OfType<MoveableInstance>().All(m => m.WadObjectId == firstId);
                }
                // For same type statics (all statics share the same properties)
                else if (_isSameType && !_isMoveable)
                {
                    _isSameType = true;
                }
            }
            
            // Build property descriptors
            var descriptors = new System.Collections.Generic.List<PropertyDescriptor>();

            if (_isSameType)
            {
                // Show all properties for same type
                if (_isMoveable)
                {
                    var moveable = _instances[0] as MoveableInstance;
                    var propertySet = PropertyManager.Instance.GetMoveableProperties(
                        moveable.WadObjectId.ToString(TRVersion.Game.TombEngine));
                    
                    foreach (var propDef in propertySet.Properties)
                    {
                        descriptors.Add(new CustomPropertyDescriptor(_instances, propDef));
                    }
                }
                else
                {
                    var propertySet = PropertyManager.Instance.GetStaticProperties();
                    
                    foreach (var propDef in propertySet.Properties)
                    {
                        descriptors.Add(new CustomPropertyDescriptor(_instances, propDef));
                    }
                }
            }
            else
            {
                // Show only default properties for mixed types
                if (_isMoveable)
                {
                    var defaultProps = PropertyManager.GetDefaultMoveableProperties();
                    foreach (var propDef in defaultProps.Properties)
                    {
                        descriptors.Add(new CustomPropertyDescriptor(_instances, propDef));
                    }
                }
                else
                {
                    var defaultProps = PropertyManager.GetDefaultStaticProperties();
                    foreach (var propDef in defaultProps.Properties)
                    {
                        descriptors.Add(new CustomPropertyDescriptor(_instances, propDef));
                    }
                }
            }

            _properties = new PropertyDescriptorCollection(descriptors.ToArray());
        }

        public AttributeCollection GetAttributes() => AttributeCollection.Empty;
        public string GetClassName()
        {
            if (_instances.Count == 1)
                return _instances[0].GetType().Name;
            else
                return $"{_instances.Count} objects selected";
        }
        public string GetComponentName()
        {
            if (_instances.Count == 1)
                return _instances[0].ToString();
            else
                return $"{_instances.Count} objects";
        }
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
    /// Custom PropertyDescriptor that wraps a PropertyDefinition and provides access to ItemInstance(s)
    /// Supports batch editing of multiple instances
    /// </summary>
    internal class CustomPropertyDescriptor : PropertyDescriptor
    {
        private readonly List<ItemInstance> _instances;
        private readonly PropertyDefinition _definition;

        public CustomPropertyDescriptor(List<ItemInstance> instances, PropertyDefinition definition)
            : base(definition.Name, GetAttributes(definition))
        {
            _instances = instances;
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
            
            // Add UITypeEditor for checkbox (multiple selection)
            if (definition.Type == PropertyType.Checkbox && definition.Options != null && definition.Options.Count > 0)
            {
                attrs.Add(new EditorAttribute(typeof(CheckboxEditor), typeof(System.Drawing.Design.UITypeEditor)));
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
                        return typeof(string);
                    case PropertyType.Checkbox:
                        // For multiple selections, we'll use a comma-separated string
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
            // Get value from first instance
            var firstInstance = _instances[0];
            
            // Special handling for OCB
            if (_definition.Name == "OCB")
            {
                return (int)firstInstance.Ocb;
            }

            // Get from CustomProperties
            var customProps = GetCustomProperties(firstInstance);

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
            // Apply value to all instances
            foreach (var instance in _instances)
            {
                // Special handling for OCB
                if (_definition.Name == "OCB")
                {
                    instance.Ocb = (short)(int)value;
                    continue;
                }

                // Set in CustomProperties
                var customProps = GetCustomProperties(instance);

                if (customProps == null)
                    continue;

                // Convert color to hex string
                if (_definition.Type == PropertyType.Color && value is System.Drawing.Color color)
                {
                    value = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }

                customProps.SetProperty(_definition.Name, value);
            }
        }

        public override void ResetValue(object component)
        {
            SetValue(component, GetDefaultValue());
        }

        public override bool ShouldSerializeValue(object component) => false;

        // Helper method to get CustomProperties from an instance
        private PropertyCollection GetCustomProperties(ItemInstance instance)
        {
            return instance is MoveableInstance m ? m.CustomProperties : 
                   instance is StaticInstance s ? s.CustomProperties : null;
        }

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
            return new StandardValuesCollection(Array.Empty<string>());
        }
    }

    /// <summary>
    /// UITypeEditor for checkbox list (multiple selection) properties
    /// </summary>
    internal class CheckboxEditor : System.Drawing.Design.UITypeEditor
    {
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return System.Drawing.Design.UITypeEditorEditStyle.DropDown;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null && context?.PropertyDescriptor is CustomPropertyDescriptor customDesc)
            {
                var editorService = provider.GetService(typeof(System.Windows.Forms.Design.IWindowsFormsEditorService)) 
                    as System.Windows.Forms.Design.IWindowsFormsEditorService;

                if (editorService != null)
                {
                    // Create a CheckedListBox with the options
                    var listBox = new System.Windows.Forms.CheckedListBox();
                    listBox.CheckOnClick = true;
                    listBox.BorderStyle = System.Windows.Forms.BorderStyle.None;

                    var options = customDesc.PropertyDefinition.Options;
                    if (options != null && options.Count > 0)
                    {
                        // Parse currently selected values
                        var currentValues = (value as string ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => v.Trim()).ToList();

                        // Add options and check the selected ones
                        foreach (var option in options)
                        {
                            int index = listBox.Items.Add(option);
                            if (currentValues.Contains(option))
                            {
                                listBox.SetItemChecked(index, true);
                            }
                        }

                        // Size the control
                        listBox.Height = Math.Min(listBox.Items.Count * listBox.ItemHeight + 2, 200);

                        // Show the dropdown
                        editorService.DropDownControl(listBox);

                        // Collect checked items
                        var selected = new System.Collections.Generic.List<string>();
                        foreach (int index in listBox.CheckedIndices)
                        {
                            selected.Add(listBox.Items[index].ToString());
                        }

                        // Return comma-separated list
                        return string.Join(", ", selected);
                    }
                }
            }

            return value;
        }
    }
}
