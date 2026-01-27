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
    /// WPF window for editing object properties dynamically
    /// </summary>
    public partial class PropertyEditorWindow : Window
    {
        private readonly ItemInstance _instance;
        private readonly List<PropertyDefinition> _propertyDefinitions;
        private readonly Dictionary<string, FrameworkElement> _propertyControls;
        private readonly bool _isTombEngine;

        public bool PropertiesChanged { get; private set; }

        public PropertyEditorWindow(ItemInstance instance, bool isTombEngine)
        {
            InitializeComponent();
            _instance = instance;
            _isTombEngine = isTombEngine;
            _propertyControls = new Dictionary<string, FrameworkElement>();
            PropertiesChanged = false;

            // Load property definitions based on instance type
            if (instance is MoveableInstance moveable)
            {
                TitleText.Text = "Moveable Properties";
                SubtitleText.Text = $"Object: {moveable.WadObjectId.ToString(TRVersion.Game.TombEngine)}";
                
                var propertySet = PropertyManager.Instance.GetMoveableProperties(moveable.WadObjectId.ToString(TRVersion.Game.TombEngine));
                _propertyDefinitions = propertySet.Properties;
            }
            else if (instance is StaticInstance staticMesh)
            {
                TitleText.Text = "Static Properties";
                SubtitleText.Text = $"Object: {staticMesh.WadObjectId.ToString(TRVersion.Game.TombEngine)}";
                
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
            // Get current value from instance
            object currentValue = GetPropertyValue(propDef.Name);

            switch (propDef.Type)
            {
                case PropertyType.Integer:
                    return CreateIntegerControl(propDef, currentValue);

                case PropertyType.Float:
                    return CreateFloatControl(propDef, currentValue);

                case PropertyType.Boolean:
                    return CreateBooleanControl(propDef, currentValue);

                case PropertyType.Dropdown:
                    return CreateDropdownControl(propDef, currentValue);

                case PropertyType.Checkbox:
                    return CreateCheckboxListControl(propDef, currentValue);

                case PropertyType.Color:
                    return CreateColorControl(propDef, currentValue);

                default:
                    return CreateTextControl(propDef, currentValue);
            }
        }

        private FrameworkElement CreateIntegerControl(PropertyDefinition propDef, object currentValue)
        {
            var textBox = new TextBox
            {
                Text = currentValue?.ToString() ?? propDef.Default ?? "0"
            };

            // Add validation for integers
            textBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !IsValidInteger(((TextBox)s).Text + e.Text);
            };

            return textBox;
        }

        private FrameworkElement CreateFloatControl(PropertyDefinition propDef, object currentValue)
        {
            var textBox = new TextBox
            {
                Text = currentValue?.ToString() ?? propDef.Default ?? "0.0"
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
                if (!float.TryParse(tb.Text, out float val))
                {
                    // Reset to default if invalid
                    tb.Text = propDef.Default ?? "0.0";
                }
            };

            return textBox;
        }

        private FrameworkElement CreateBooleanControl(PropertyDefinition propDef, object currentValue)
        {
            bool value = currentValue != null ? Convert.ToBoolean(currentValue) : 
                         bool.TryParse(propDef.Default, out bool defVal) ? defVal : false;

            var checkBox = new CheckBox
            {
                Content = propDef.Name,
                IsChecked = value
            };

            return checkBox;
        }

        private FrameworkElement CreateDropdownControl(PropertyDefinition propDef, object currentValue)
        {
            var comboBox = new ComboBox();

            if (propDef.Options != null && propDef.Options.Count > 0)
            {
                foreach (var option in propDef.Options)
                {
                    comboBox.Items.Add(option);
                }
            }

            string currentStr = currentValue?.ToString() ?? propDef.Default ?? "";
            if (comboBox.Items.Contains(currentStr))
                comboBox.SelectedItem = currentStr;
            else if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;

            return comboBox;
        }

        private FrameworkElement CreateCheckboxListControl(PropertyDefinition propDef, object currentValue)
        {
            var stackPanel = new StackPanel();

            List<string> selectedValues = new List<string>();
            if (currentValue is List<string> list)
                selectedValues = list;
            else if (currentValue is string str && !string.IsNullOrEmpty(str))
                selectedValues = str.Split(',').Select(s => s.Trim()).ToList();

            if (propDef.Options != null && propDef.Options.Count > 0)
            {
                foreach (var option in propDef.Options)
                {
                    var checkBox = new CheckBox
                    {
                        Content = option,
                        IsChecked = selectedValues.Contains(option),
                        Tag = option
                    };
                    stackPanel.Children.Add(checkBox);
                }
            }

            return stackPanel;
        }

        private FrameworkElement CreateColorControl(PropertyDefinition propDef, object currentValue)
        {
            // Parse current color
            string colorStr = currentValue?.ToString() ?? propDef.Default ?? "#FFFFFF";
            var color = ParseColor(colorStr);

            // Create a button that shows the current color and opens the color picker
            var colorButton = new Button
            {
                Height = 30,
                MinWidth = 80,
                Padding = new Thickness(5),
                Tag = new { PropDef = propDef, CurrentColor = color }
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
                Text = ColorToHex(color),
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
                        btn.Tag = new { PropDef = tag.PropDef, CurrentColor = selectedColor };
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

        private FrameworkElement CreateTextControl(PropertyDefinition propDef, object currentValue)
        {
            return new TextBox
            {
                Text = currentValue?.ToString() ?? propDef.Default ?? ""
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
        /// Saves all property values back to the instance
        /// </summary>
        private void SaveProperties()
        {
            foreach (var propDef in _propertyDefinitions)
            {
                if (!_propertyControls.ContainsKey(propDef.Name))
                    continue;

                var control = _propertyControls[propDef.Name];
                object value = ExtractValueFromControl(control, propDef.Type);

                SetPropertyValue(propDef.Name, value);
            }

            PropertiesChanged = true;
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
                    if (control is ComboBox cb)
                        return cb.SelectedItem?.ToString() ?? "";
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
        /// Sets a property value on the instance
        /// </summary>
        private void SetPropertyValue(string propertyName, object value)
        {
            if (_instance is MoveableInstance moveable)
            {
                if (propertyName == "HP")
                    moveable.CustomProperties.SetProperty("HP", value);
                else if (propertyName == "OCB" && value is int ocb)
                {
                    // Clamp to short range to prevent overflow
                    if (ocb > short.MaxValue)
                        ocb = short.MaxValue;
                    else if (ocb < short.MinValue)
                        ocb = short.MinValue;
                    moveable.Ocb = (short)ocb;
                }
                else
                    moveable.CustomProperties.SetProperty(propertyName, value);
            }
            else if (_instance is StaticInstance staticMesh)
            {
                if (propertyName == "HP")
                    staticMesh.CustomProperties.SetProperty("HP", value);
                else if (propertyName == "OCB" && value is int ocb)
                {
                    // Clamp to short range to prevent overflow
                    if (ocb > short.MaxValue)
                        ocb = short.MaxValue;
                    else if (ocb < short.MinValue)
                        ocb = short.MinValue;
                    staticMesh.Ocb = (short)ocb;
                }
                else
                    staticMesh.CustomProperties.SetProperty(propertyName, value);
            }
        }

        /// <summary>
        /// Resets all properties to their default values
        /// </summary>
        private void ResetToDefaults()
        {
            foreach (var propDef in _propertyDefinitions)
            {
                if (!_propertyControls.ContainsKey(propDef.Name))
                    continue;

                var control = _propertyControls[propDef.Name];
                SetControlToDefault(control, propDef);
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
                    if (control is ComboBox cb && !string.IsNullOrEmpty(propDef.Default))
                    {
                        if (cb.Items.Contains(propDef.Default))
                            cb.SelectedItem = propDef.Default;
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
