using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TombLib.LevelData;
using TombLib.LevelData.Properties;

namespace TombEditor.Windows
{
    /// <summary>
    /// Context where PropertyEditorWindow is being used
    /// </summary>
    public enum PropertyEditorContext
    {
        /// <summary>
        /// Editing properties in TombEditor - Reset button restores WAD2 values
        /// </summary>
        TombEditor,

        /// <summary>
        /// Editing properties in Wadtool - Reset button restores XML defaults
        /// </summary>
        Wadtool
    }

    /// <summary>
    /// WPF window for editing object properties dynamically (supports both single and batch editing)
    /// </summary>
    public partial class PropertyEditorWindow : Window
    {
        private readonly ItemInstance _instance;
        private readonly List<ItemInstance> _instances;
        private readonly bool _isBatchMode;
        private readonly List<PropertyDefinition> _propertyDefinitions;
        private readonly Dictionary<string, FrameworkElement> _propertyControls;
        private readonly bool _isTombEngine;
        private readonly PropertyEditorContext _context;
        private readonly PropertyCollection _savedWad2Properties; // Stores original WAD2 properties for TombEditor context
        private readonly TRVersion.Game _gameVersion;

        public bool PropertiesChanged { get; private set; }

        // Constructor for single object editing
        public PropertyEditorWindow(ItemInstance instance, bool isTombEngine, PropertyEditorContext context = PropertyEditorContext.TombEditor, TRVersion.Game? gameVersion = null)
        {
            InitializeComponent();
            _instance = instance;
            _context = context;
            _gameVersion = gameVersion ?? TRVersion.Game.TombEngine;
            _instances = new List<ItemInstance> { instance };
            _isBatchMode = false;
            _isTombEngine = isTombEngine;
            _propertyControls = new Dictionary<string, FrameworkElement>();
            PropertiesChanged = false;

            // In TombEditor context, save current CustomProperties for "Reset to Saved" functionality
            if (_context == PropertyEditorContext.TombEditor)
            {
                _savedWad2Properties = new PropertyCollection();
                
                // Access CustomProperties from the appropriate derived type
                if (instance is MoveableInstance moveableInst)
                {
                    foreach (var kvp in moveableInst.CustomProperties.GetAll())
                    {
                        _savedWad2Properties.SetProperty(kvp.Key, kvp.Value);
                    }
                }
                else if (instance is StaticInstance staticInst)
                {
                    foreach (var kvp in staticInst.CustomProperties.GetAll())
                    {
                        _savedWad2Properties.SetProperty(kvp.Key, kvp.Value);
                    }
                }
            }

            // Load property definitions based on instance type
            if (instance is MoveableInstance moveable)
            {
                TitleText.Text = "Moveable Properties";
                TitleText.Style = (Style)TryFindResource("PropertyNameLabel");
                SubtitleText.Text = $"Object: {moveable.WadObjectId.ToString(_gameVersion)}";
                SubtitleText.Style = (Style)TryFindResource("PropertyNameLabel");
                
                var propertySet = PropertyManager.Instance.GetMoveableProperties(moveable.WadObjectId.ToString(_gameVersion));
                _propertyDefinitions = propertySet.Properties;
            }
            else if (instance is StaticInstance staticMesh)
            {
                TitleText.Text = "Static Properties";
                TitleText.Style = (Style)TryFindResource("PropertyNameLabel");
                SubtitleText.Text = $"Object: {staticMesh.WadObjectId.ToString(_gameVersion)}";
                SubtitleText.Style = (Style)TryFindResource("PropertyNameLabel");
                
                var propertySet = PropertyManager.Instance.GetStaticProperties();
                _propertyDefinitions = propertySet.Properties;
            }
            else
            {
                _propertyDefinitions = new List<PropertyDefinition>();
            }

            // Build the UI
            BuildPropertyControls();
        }

        // Constructor for batch editing multiple objects
        public PropertyEditorWindow(List<ItemInstance> instances, bool isTombEngine, PropertyEditorContext context = PropertyEditorContext.TombEditor, TRVersion.Game? gameVersion = null)
        {
            if (instances == null || instances.Count == 0)
                throw new ArgumentException("No instances provided for batch editing");

            // Verify all instances are of the same type
            var firstType = instances[0].GetType();
            if (!instances.All(i => i.GetType() == firstType))
                throw new ArgumentException("All instances must be of the same type for batch editing");

            InitializeComponent();
            _instance = instances[0]; // Keep reference to first for property definitions
            _instances = instances;
            _isBatchMode = true;
            _isTombEngine = isTombEngine;
            _context = context;
            _gameVersion = gameVersion ?? TRVersion.Game.TombEngine;
            _propertyControls = new Dictionary<string, FrameworkElement>();
            PropertiesChanged = false;

            // Load property definitions based on first instance type
            if (instances[0] is MoveableInstance moveable)
            {
                TitleText.Text = $"Batch Edit {instances.Count} Moveables";
                TitleText.Style = (Style)TryFindResource("PropertyNameLabel");
                TitleText.Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground") ?? System.Windows.Media.Brushes.LightGray;
                SubtitleText.Text = "Changes will be applied to all selected moveables";
                SubtitleText.Style = (Style)TryFindResource("PropertyNameLabel");
                SubtitleText.Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground") ?? System.Windows.Media.Brushes.LightGray;

                var propertySet = PropertyManager.Instance.GetMoveableProperties(moveable.WadObjectId.ToString(_gameVersion));
                _propertyDefinitions = propertySet.Properties;
            }
            else if (instances[0] is StaticInstance)
            {
                TitleText.Text = $"Batch Edit {instances.Count} Statics";
                TitleText.Style = (Style)TryFindResource("PropertyNameLabel");
                TitleText.Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground") ?? System.Windows.Media.Brushes.LightGray;
                SubtitleText.Text = "Changes will be applied to all selected statics";
                SubtitleText.Style = (Style)TryFindResource("PropertyNameLabel");
                SubtitleText.Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground") ?? System.Windows.Media.Brushes.LightGray;

                var propertySet = PropertyManager.Instance.GetStaticProperties();
                _propertyDefinitions = propertySet.Properties;
            }
            else
            {
                _propertyDefinitions = new List<PropertyDefinition>();
            }

            // Build the UI
            BuildPropertyControls();
        }

        /// <summary>
        /// Dynamically creates controls for each property definition in a grid layout
        /// </summary>
        private void BuildPropertyControls()
        {
            PropertiesPanel.Children.Clear();
            _propertyControls.Clear();

            // Add info message for batch mode
            if (_isBatchMode)
            {
                var infoBlock = new TextBlock
                {
                    Text = "Only properties that you modify will be updated across all selected objects. Leave unchanged to keep individual values.",
                    Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground_Weak") ?? System.Windows.Media.Brushes.LightGray,
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                PropertiesPanel.Children.Add(infoBlock);
            }

            foreach (var propDef in _propertyDefinitions)
            {
                // Create a border for the property row
                var rowBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(62, 62, 66)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(10, 8, 10, 8)
                };

                // Create a grid with two columns for property name and value
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Property name label
                var nameLabel = new TextBlock
                {
                    Text = propDef.Name,
                    Style = (Style)TryFindResource("PropertyNameLabel")
                };
                Grid.SetColumn(nameLabel, 0);

                // Add tooltip with description if available
                if (!string.IsNullOrEmpty(propDef.Description))
                {
                    nameLabel.ToolTip = propDef.Description;
                }

                // Create control based on property type
                FrameworkElement control = CreateControlForProperty(propDef);
                if (control != null)
                {
                    Grid.SetColumn(control, 1);
                    _propertyControls[propDef.Name] = control;

                    // Also add tooltip to the control
                    if (!string.IsNullOrEmpty(propDef.Description))
                    {
                        control.ToolTip = propDef.Description;
                    }

                    // Add focus handlers to update description panel
                    control.GotFocus += (s, e) => UpdateDescriptionPanel(propDef);
                    control.MouseEnter += (s, e) => UpdateDescriptionPanel(propDef);

                    grid.Children.Add(nameLabel);
                    grid.Children.Add(control);
                }

                rowBorder.Child = grid;
                PropertiesPanel.Children.Add(rowBorder);
            }
        }

        /// <summary>
        /// Updates the description panel with the given property's description
        /// </summary>
        private void UpdateDescriptionPanel(PropertyDefinition propDef)
        {
            if (!string.IsNullOrEmpty(propDef.Description))
            {
                DescriptionText.Text = propDef.Description;
                DescriptionText.FontStyle = FontStyles.Normal;
                DescriptionText.Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground") ?? System.Windows.Media.Brushes.LightGray;
            }
            else
            {
                DescriptionText.Text = "No description available for this property.";
                DescriptionText.FontStyle = FontStyles.Italic;
                DescriptionText.Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground_Weak") ?? System.Windows.Media.Brushes.Gray;
            }
        }

        /// <summary>
        /// Creates the appropriate WPF control for a property
        /// </summary>
        private FrameworkElement CreateControlForProperty(PropertyDefinition propDef)
        {
            // Get current value from instance (or detect mixed values in batch mode)
            object currentValue = null;
            bool hasMixedValues = _isBatchMode && HasMixedValues(propDef.Name);
            
            if (!hasMixedValues)
            {
                currentValue = GetPropertyValue(propDef.Name);
            }

            switch (propDef.Type)
            {
                case PropertyType.Integer:
                    return CreateIntegerControl(propDef, currentValue, hasMixedValues);

                case PropertyType.Float:
                    return CreateFloatControl(propDef, currentValue, hasMixedValues);

                case PropertyType.Boolean:
                    return CreateBooleanControl(propDef, currentValue, hasMixedValues);

                case PropertyType.Dropdown:
                    return CreateDropdownControl(propDef, currentValue, hasMixedValues);

                case PropertyType.Checkbox:
                    return CreateCheckboxListControl(propDef, currentValue, hasMixedValues);

                case PropertyType.Color:
                    return CreateColorControl(propDef, currentValue, hasMixedValues);

                default:
                    return CreateTextControl(propDef, currentValue, hasMixedValues);
            }
        }

        private FrameworkElement CreateIntegerControl(PropertyDefinition propDef, object currentValue, bool hasMixedValues)
        {
            var textBox = new TextBox
            {
                Text = hasMixedValues ? "<Mixed>" : (currentValue?.ToString() ?? propDef.Default ?? "0")
            };

            // Add validation for integers
            textBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !IsValidInteger(((TextBox)s).Text + e.Text);
            };

            return textBox;
        }

        private FrameworkElement CreateFloatControl(PropertyDefinition propDef, object currentValue, bool hasMixedValues)
        {
            var textBox = new TextBox
            {
                Text = hasMixedValues ? "<Mixed>" : (currentValue?.ToString() ?? propDef.Default ?? "0.0")
            };

            // Add validation for floats during typing
            textBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !IsValidFloatPartial(((TextBox)s).Text + e.Text);
            };

            // Ensure valid float when focus is lost
            textBox.LostFocus += (s, e) =>
            {
                var tb = s as TextBox;
                if (tb.Text == "<Mixed>")
                    return; // Don't validate placeholder
                    
                if (!float.TryParse(tb.Text, out float val))
                {
                    // Reset to default if invalid
                    tb.Text = propDef.Default ?? "0.0";
                }
            };

            return textBox;
        }

        private FrameworkElement CreateBooleanControl(PropertyDefinition propDef, object currentValue, bool hasMixedValues)
        {
            bool? value = null;
            
            if (hasMixedValues)
            {
                value = null; // Indeterminate state for mixed values
            }
            else
            {
                value = currentValue != null ? Convert.ToBoolean(currentValue) : 
                        bool.TryParse(propDef.Default, out bool defVal) ? defVal : false;
            }

            var checkBox = new CheckBox
            {
                Content = propDef.Name,
                IsChecked = value,
                IsThreeState = hasMixedValues // Allow indeterminate in batch mode
            };

            return checkBox;
        }

        private FrameworkElement CreateDropdownControl(PropertyDefinition propDef, object currentValue, bool hasMixedValues)
        {
            var comboBox = new ComboBox();

            if (propDef.Options != null && propDef.Options.Count > 0)
            {
                foreach (var option in propDef.Options)
                {
                    comboBox.Items.Add(option);
                }
            }

            if (hasMixedValues)
            {
                comboBox.Items.Insert(0, "<Mixed>");
                comboBox.SelectedIndex = 0;
            }
            else
            {
                string currentStr = currentValue?.ToString() ?? propDef.Default ?? "";
                if (comboBox.Items.Contains(currentStr))
                    comboBox.SelectedItem = currentStr;
                else if (comboBox.Items.Count > 0)
                    comboBox.SelectedIndex = 0;
            }

            return comboBox;
        }

        private FrameworkElement CreateCheckboxListControl(PropertyDefinition propDef, object currentValue, bool hasMixedValues)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            if (hasMixedValues)
            {
                // In batch mode with mixed values, show a label
                var mixedLabel = new TextBlock
                {
                    Text = "<Mixed values>",
                    FontStyle = FontStyles.Italic,
                    Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground_Weak") ?? System.Windows.Media.Brushes.Gray
                };
                stackPanel.Children.Add(mixedLabel);
            }
            
            var selectedValues = currentValue?.ToString()?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();

            if (propDef.Options != null && propDef.Options.Count > 0)
            {
                foreach (var option in propDef.Options)
                {
                    var checkBox = new CheckBox
                    {
                        Content = option,
                        IsChecked = !hasMixedValues && selectedValues.Contains(option),
                        Tag = option,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    stackPanel.Children.Add(checkBox);
                }
            }

            return stackPanel;
        }

        private FrameworkElement CreateColorControl(PropertyDefinition propDef, object currentValue, bool hasMixedValues)
        {
            // Parse current color
            string colorStr = hasMixedValues ? "#808080" : (currentValue?.ToString() ?? propDef.Default ?? "#FFFFFF");
            var color = ParseColor(colorStr);

            // Create a button that shows the current color and opens the color picker
            var colorButton = new Button
            {
                Height = 30,
                MinWidth = 80,
                Padding = new Thickness(5),
                Tag = new { PropDef = propDef, CurrentColor = color, HasMixedValues = hasMixedValues }
            };

            // Create a grid inside the button to show color preview and text
            var buttonGrid = new Grid();
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Color preview rectangle
            var colorPreview = new System.Windows.Shapes.Rectangle
            {
                Fill = new SolidColorBrush(color),
                Stroke = (System.Windows.Media.Brush)TryFindResource("Brush_Border_Low") ?? System.Windows.Media.Brushes.Gray,
                StrokeThickness = 1,
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(colorPreview, 0);

            // Hex text
            var hexText = new TextBlock
            {
                Text = hasMixedValues ? "<Mixed>" : ColorToHex(color),
                Foreground = (System.Windows.Media.Brush)TryFindResource("Brush_Foreground") ?? System.Windows.Media.Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = new System.Windows.Media.FontFamily("Consolas")
            };
            Grid.SetColumn(hexText, 1);

            buttonGrid.Children.Add(colorPreview);
            buttonGrid.Children.Add(hexText);
            colorButton.Content = buttonGrid;

            // Click event to open color picker dialog
            colorButton.Click += (s, e) =>
            {
                var btn = s as Button;
                if (btn?.Tag != null)
                {
                    dynamic tag = btn.Tag;
                    var currentColor = tag.CurrentColor as System.Windows.Media.Color?;
                    
                    // Open color picker window
                    var colorPickerWindow = new ColorPickerWindow(currentColor ?? System.Windows.Media.Colors.White);
                    colorPickerWindow.Owner = this;
                    
                    if (colorPickerWindow.ShowDialog() == true)
                    {
                        var selectedColor = colorPickerWindow.SelectedColor;
                        
                        // Update button display
                        if (btn.Content is Grid btnGrid)
                        {
                            foreach (var child in btnGrid.Children)
                            {
                                if (child is System.Windows.Shapes.Rectangle rect)
                                    rect.Fill = new SolidColorBrush(selectedColor);
                                else if (child is TextBlock tb)
                                    tb.Text = ColorToHex(selectedColor);
                            }
                        }
                        
                        // Update tag
                        btn.Tag = new { PropDef = tag.PropDef, CurrentColor = selectedColor, HasMixedValues = false };
                    }
                }
            };

            return colorButton;
        }

        /// <summary>
        /// Parses a color string in various formats (#RRGGBB, R,G,B, etc.)
        /// </summary>
        private System.Windows.Media.Color ParseColor(string colorStr)
        {
            if (string.IsNullOrEmpty(colorStr))
                return System.Windows.Media.Colors.White;

            colorStr = colorStr.Trim();

            // Hex format: #RRGGBB or RRGGBB
            if (colorStr.StartsWith("#"))
            {
                colorStr = colorStr.Substring(1);
            }

            if (colorStr.Length == 6)
            {
                try
                {
                    byte r = Convert.ToByte(colorStr.Substring(0, 2), 16);
                    byte g = Convert.ToByte(colorStr.Substring(2, 2), 16);
                    byte b = Convert.ToByte(colorStr.Substring(4, 2), 16);
                    return System.Windows.Media.Color.FromRgb(r, g, b);
                }
                catch
                {
                    return System.Windows.Media.Colors.White;
                }
            }

            // RGB format: "R,G,B"
            if (colorStr.Contains(","))
            {
                var parts = colorStr.Split(',');
                if (parts.Length == 3)
                {
                    try
                    {
                        byte r = byte.Parse(parts[0].Trim());
                        byte g = byte.Parse(parts[1].Trim());
                        byte b = byte.Parse(parts[2].Trim());
                        return System.Windows.Media.Color.FromRgb(r, g, b);
                    }
                    catch
                    {
                        return System.Windows.Media.Colors.White;
                    }
                }
            }

            return System.Windows.Media.Colors.White;
        }

        /// <summary>
        /// Converts a WPF color to hex string format
        /// </summary>
        private string ColorToHex(System.Windows.Media.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private FrameworkElement CreateTextControl(PropertyDefinition propDef, object currentValue, bool hasMixedValues)
        {
            return new TextBox
            {
                Text = hasMixedValues ? "<Mixed>" : (currentValue?.ToString() ?? propDef.Default ?? "")
            };
        }

        /// <summary>
        /// Gets the current property value from the instance
        /// </summary>
        private object GetPropertyValue(string propertyName)
        {
            // For standard properties (HP, OCB for moveables)
            if (_instance is MoveableInstance moveable)
            {
                if (propertyName == "HP")
                    return moveable.CustomProperties.GetProperty("HP", 100);
                if (propertyName == "OCB")
                    return moveable.Ocb;
                
                return moveable.CustomProperties.GetProperty(propertyName);
            }
            else if (_instance is StaticInstance staticMesh)
            {
                if (propertyName == "HP")
                    return staticMesh.CustomProperties.GetProperty("HP", 150);
                if (propertyName == "OCB")
                    return staticMesh.Ocb;
                
                return staticMesh.CustomProperties.GetProperty(propertyName);
            }

            return null;
        }

        /// <summary>
        /// Saves all property values back to the instance(s)
        /// </summary>
        private void SaveProperties()
        {
            if (_isBatchMode)
            {
                // For batch mode, only update properties that have been modified
                foreach (var propDef in _propertyDefinitions)
                {
                    if (!_propertyControls.ContainsKey(propDef.Name))
                        continue;

                    var control = _propertyControls[propDef.Name];
                    
                    // Check if value was modified (not in indeterminate state for batch)
                    if (!IsValueModified(control, propDef))
                        continue;

                    object value = ExtractValueFromControl(control, propDef.Type);

                    // Apply to all instances
                    foreach (var instance in _instances)
                    {
                        SetPropertyValueOnInstance(instance, propDef.Name, value);
                    }
                }
            }
            else
            {
                // For single mode, update all properties
                foreach (var propDef in _propertyDefinitions)
                {
                    if (!_propertyControls.ContainsKey(propDef.Name))
                        continue;

                    var control = _propertyControls[propDef.Name];
                    object value = ExtractValueFromControl(control, propDef.Type);

                    SetPropertyValue(propDef.Name, value);
                }
            }

            PropertiesChanged = true;
        }

        /// <summary>
        /// Checks if a control's value has been modified in batch mode
        /// </summary>
        private bool IsValueModified(FrameworkElement control, PropertyDefinition propDef)
        {
            // For checkboxes in batch mode, null (indeterminate) means not modified
            if (propDef.Type == PropertyType.Boolean && control is CheckBox cb)
            {
                return cb.IsChecked != null;
            }

            // For other controls, we assume modification if they have a value
            // In the future, we could track which controls were actually interacted with
            return true;
        }

        /// <summary>
        /// Extracts the value from a WPF control
        /// </summary>
        private object ExtractValueFromControl(FrameworkElement control, PropertyType type)
        {
            switch (type)
            {
                case PropertyType.Integer:
                    if (control is TextBox tb1 && int.TryParse(tb1.Text, out int intVal))
                        return intVal;
                    return 0;

                case PropertyType.Float:
                    if (control is TextBox tb2 && float.TryParse(tb2.Text, out float floatVal))
                        return floatVal;
                    return 0.0f;

                case PropertyType.Boolean:
                    if (control is CheckBox cb)
                    {
                        return cb.IsChecked == true;
                    }
                    return false;

                case PropertyType.Dropdown:
                    if (control is ComboBox dropdown)
                        return dropdown.SelectedItem?.ToString() ?? "";
                    return "";

                case PropertyType.Checkbox:
                    if (control is StackPanel sp2)
                    {
                        var selected = new List<string>();
                        foreach (var child in sp2.Children)
                        {
                            if (child is CheckBox chk && chk.IsChecked == true)
                                selected.Add(chk.Tag?.ToString() ?? chk.Content?.ToString() ?? "");
                        }
                        return selected;
                    }
                    return new List<string>();

                case PropertyType.Color:
                    if (control is Button colorBtn && colorBtn.Tag != null)
                    {
                        dynamic tag = colorBtn.Tag;
                        var color = tag.CurrentColor as System.Windows.Media.Color?;
                        if (color.HasValue)
                            return ColorToHex(color.Value);
                    }
                    return "#FFFFFF";

                default:
                    if (control is TextBox tb)
                        return tb.Text;
                    return "";
            }
        }

        /// <summary>
        /// Sets a property value on a specific instance
        /// </summary>
        private void SetPropertyValueOnInstance(ItemInstance instance, string propertyName, object value)
        {
            if (instance is MoveableInstance moveable)
            {
                // Store all properties in CustomProperties for consistency
                // OCB and HP are now treated the same as other properties
                moveable.CustomProperties.SetProperty(propertyName, value);
            }
            else if (instance is StaticInstance staticMesh)
            {
                // Store all properties in CustomProperties for consistency
                // OCB and HP are now treated the same as other properties
                staticMesh.CustomProperties.SetProperty(propertyName, value);
            }
        }

        /// <summary>
        /// Sets a property value on the instance (single mode)
        /// </summary>
        private void SetPropertyValue(string propertyName, object value)
        {
            SetPropertyValueOnInstance(_instance, propertyName, value);
        }

        /// <summary>
        /// Checks if all instances have the same value for a property
        /// </summary>
        private bool HasMixedValues(string propertyName)
        {
            if (!_isBatchMode || _instances.Count <= 1)
                return false;

            var firstValue = GetPropertyValueFromInstance(_instances[0], propertyName);
            
            for (int i = 1; i < _instances.Count; i++)
            {
                var currentValue = GetPropertyValueFromInstance(_instances[i], propertyName);
                
                // Compare values (handle null cases)
                if (firstValue == null && currentValue == null)
                    continue;
                if (firstValue == null || currentValue == null)
                    return true;
                if (!firstValue.Equals(currentValue))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a property value from a specific instance
        /// </summary>
        private object GetPropertyValueFromInstance(ItemInstance instance, string propertyName)
        {
            PropertyCollection properties = null;
            
            if (instance is MoveableInstance moveable)
                properties = moveable.CustomProperties;
            else if (instance is StaticInstance staticMesh)
                properties = staticMesh.CustomProperties;
            
            if (properties == null)
                return null;
            
            // Get the raw value from CustomProperties
            var value = properties.GetProperty<object>(propertyName, null);
            
            // If it's already the correct type (List<string>, int, etc.), return it
            if (value is List<string>)
                return value;
            
            // For HP and OCB, try to convert to int
            if (propertyName == "HP" || propertyName == "OCB")
            {
                if (value == null)
                    return propertyName == "HP" ? 100 : 0;
                
                if (value is int)
                    return value;
                
                // Try to parse from string
                if (int.TryParse(value.ToString(), out int intValue))
                    return intValue;
                
                return propertyName == "HP" ? 100 : 0;
            }
            
            // For other types, return as is or convert to string
            return value ?? "";
        }

        /// <summary>
        /// Resets all properties based on context:
        /// - Wadtool: Reset to XML defaults
        /// - TombEditor: Reset to WAD2 saved values (or XML defaults if none)
        /// </summary>
        private void ResetToDefaults()
        {
            if (_context == PropertyEditorContext.Wadtool)
            {
                // Wadtool context: Reset to XML defaults
                foreach (var propDef in _propertyDefinitions)
                {
                    if (!_propertyControls.ContainsKey(propDef.Name))
                        continue;

                    var control = _propertyControls[propDef.Name];
                    SetControlToDefault(control, propDef);
                }
                
                MessageBox.Show("Properties reset to XML defaults.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else // PropertyEditorContext.TombEditor
            {
                // TombEditor context: Try to restore WAD2 saved values
                if (_savedWad2Properties != null && _savedWad2Properties.GetAll().Count > 0)
                {
                    // Restore from WAD2 saved values
                    foreach (var propDef in _propertyDefinitions)
                    {
                        if (!_propertyControls.ContainsKey(propDef.Name))
                            continue;

                        var control = _propertyControls[propDef.Name];
                        
                        // Check if WAD2 had a value for this property
                        // Get as object (actual stored type) then convert to string
                        var savedValueObj = _savedWad2Properties.GetProperty<object>(propDef.Name, null);
                        string savedValue = savedValueObj?.ToString();
                        if (savedValue != null)
                        {
                            SetControlToValue(control, propDef, savedValue);
                        }
                        else
                        {
                            // No WAD2 value, use XML default
                            SetControlToDefault(control, propDef);
                        }
                    }
                    
                    MessageBox.Show("Properties reset to WAD2 saved values.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // No WAD2 properties exist, use XML defaults
                    foreach (var propDef in _propertyDefinitions)
                    {
                        if (!_propertyControls.ContainsKey(propDef.Name))
                            continue;

                        var control = _propertyControls[propDef.Name];
                        SetControlToDefault(control, propDef);
                    }
                    
                    MessageBox.Show("Properties reset to XML defaults (no WAD2 values exist).", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void SetControlToDefault(FrameworkElement control, PropertyDefinition propDef)
        {
            switch (propDef.Type)
            {
                case PropertyType.Integer:
                case PropertyType.Float:
                    if (control is TextBox tbNumeric)
                        tbNumeric.Text = propDef.Default ?? "0";
                    break;

                case PropertyType.Boolean:
                    if (control is CheckBox cb)
                    {
                        bool defVal = bool.TryParse(propDef.Default, out bool b) && b;
                        cb.IsChecked = defVal;
                    }
                    break;

                case PropertyType.Dropdown:
                    if (control is ComboBox dropdownBox && !string.IsNullOrEmpty(propDef.Default))
                    {
                        if (dropdownBox.Items.Contains(propDef.Default))
                            dropdownBox.SelectedItem = propDef.Default;
                    }
                    break;

                case PropertyType.Checkbox:
                    if (control is StackPanel sp2)
                    {
                        var defaults = propDef.Default?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
                        foreach (var child in sp2.Children)
                        {
                            if (child is CheckBox chk)
                                chk.IsChecked = defaults.Contains(chk.Tag?.ToString() ?? chk.Content?.ToString() ?? "");
                        }
                    }
                    break;

                case PropertyType.Color:
                    if (control is Button colorBtn)
                    {
                        string defaultColor = propDef.Default ?? "#FFFFFF";
                        var color = ParseColor(defaultColor);
                        
                        // Update button display
                        if (colorBtn.Content is Grid btnGrid)
                        {
                            foreach (var child in btnGrid.Children)
                            {
                                if (child is System.Windows.Shapes.Rectangle rect)
                                    rect.Fill = new SolidColorBrush(color);
                                else if (child is TextBlock tbColor)
                                    tbColor.Text = ColorToHex(color);
                            }
                        }
                        
                        // Update tag
                        if (colorBtn.Tag != null)
                        {
                            dynamic tag = colorBtn.Tag;
                            colorBtn.Tag = new { PropDef = tag.PropDef, CurrentColor = color };
                        }
                    }
                    break;

                default:
                    if (control is TextBox tbDefault)
                        tbDefault.Text = propDef.Default ?? "";
                    break;
            }
        }

        /// <summary>
        /// Sets a control to a specific saved value (used for WAD2 restore)
        /// </summary>
        private void SetControlToValue(FrameworkElement control, PropertyDefinition propDef, string value)
        {
            switch (propDef.Type)
            {
                case PropertyType.Integer:
                case PropertyType.Float:
                    if (control is TextBox tb)
                        tb.Text = value;
                    break;

                case PropertyType.Boolean:
                    if (control is CheckBox cb)
                    {
                        bool boolVal = bool.TryParse(value, out bool b) && b;
                        cb.IsChecked = boolVal;
                    }
                    break;

                case PropertyType.Dropdown:
                    if (control is ComboBox combo && !string.IsNullOrEmpty(value))
                    {
                        if (combo.Items.Contains(value))
                            combo.SelectedItem = value;
                    }
                    break;

                case PropertyType.Checkbox:
                    if (control is StackPanel sp)
                    {
                        var values = value?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
                        foreach (var child in sp.Children)
                        {
                            if (child is CheckBox chk)
                                chk.IsChecked = values.Contains(chk.Tag?.ToString() ?? chk.Content?.ToString() ?? "");
                        }
                    }
                    break;

                case PropertyType.Color:
                    if (control is Button colorBtn)
                    {
                        var color = ParseColor(value);
                        
                        // Update button display
                        if (colorBtn.Content is Grid btnGrid)
                        {
                            ((System.Windows.Shapes.Rectangle)btnGrid.Children[0]).Fill = new SolidColorBrush(color);
                            ((TextBlock)btnGrid.Children[1]).Text = value;
                        }
                        
                        // Update tag
                        if (colorBtn.Tag != null)
                        {
                            dynamic tag = colorBtn.Tag;
                            colorBtn.Tag = new { PropDef = tag.PropDef, CurrentColor = color };
                        }
                    }
                    break;

                default:
                    if (control is TextBox tbDefault)
                        tbDefault.Text = value;
                    break;
            }
        }

        // Validation helpers
        private bool IsValidInteger(string text)
        {
            return int.TryParse(text, out _) || text == "-" || string.IsNullOrEmpty(text);
        }

        private bool IsValidFloatPartial(string text)
        {
            // Allow partial input during typing
            return float.TryParse(text, out _) || text == "-" || text.EndsWith(".") || string.IsNullOrEmpty(text);
        }

        private bool IsValidFloat(string text)
        {
            return float.TryParse(text, out _);
        }

        // Event handlers
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all properties to their default values?",
                "Reset Properties",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ResetToDefaults();
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SaveProperties();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
