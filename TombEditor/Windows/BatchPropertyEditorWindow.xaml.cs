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
    /// WPF window for batch editing properties across multiple objects
    /// </summary>
    public partial class BatchPropertyEditorWindow : Window
    {
        private readonly List<ItemInstance> _instances;
        private readonly List<PropertyDefinition> _propertyDefinitions;
        private readonly Dictionary<string, FrameworkElement> _propertyControls;
        private readonly bool _isTombEngine;

        public bool PropertiesChanged { get; private set; }

        public BatchPropertyEditorWindow(List<ItemInstance> instances, bool isTombEngine)
        {
            if (instances == null || instances.Count == 0)
                throw new ArgumentException("No instances provided for batch editing");

            // Verify all instances are of the same type
            var firstType = instances[0].GetType();
            if (!instances.All(i => i.GetType() == firstType))
                throw new ArgumentException("All instances must be of the same type for batch editing");

            InitializeComponent();
            _instances = instances;
            _isTombEngine = isTombEngine;
            _propertyControls = new Dictionary<string, FrameworkElement>();
            PropertiesChanged = false;

            // Load property definitions based on first instance type
            if (instances[0] is MoveableInstance moveable)
            {
                TitleText.Text = $"Batch Edit {instances.Count} Moveables";
                SubtitleText.Text = "Changes will be applied to all selected moveables";
                
                var propertySet = PropertyManager.Instance.GetMoveableProperties(moveable.WadObjectId.ToString());
                _propertyDefinitions = propertySet.Properties;
            }
            else if (instances[0] is StaticInstance)
            {
                TitleText.Text = $"Batch Edit {instances.Count} Statics";
                SubtitleText.Text = "Changes will be applied to all selected statics";
                
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
        /// Dynamically creates controls for each property definition
        /// Note: For batch editing, we show placeholder text when values differ
        /// </summary>
        private void BuildPropertyControls()
        {
            PropertiesPanel.Children.Clear();
            _propertyControls.Clear();

            // Add info message
            var infoBlock = new TextBlock
            {
                Text = "Only properties that you modify will be updated across all selected objects. Leave unchanged to keep individual values.",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170)),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            PropertiesPanel.Children.Add(infoBlock);

            foreach (var propDef in _propertyDefinitions)
            {
                // Create a group box for each property
                var groupBox = new GroupBox
                {
                    Header = propDef.Name,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                var stackPanel = new StackPanel();

                // Add description if available
                if (!string.IsNullOrEmpty(propDef.Description))
                {
                    var descLabel = new TextBlock
                    {
                        Text = propDef.Description,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170)),
                        FontSize = 10,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    stackPanel.Children.Add(descLabel);
                }

                // Create control based on property type
                FrameworkElement control = CreateBatchControlForProperty(propDef);
                if (control != null)
                {
                    stackPanel.Children.Add(control);
                    _propertyControls[propDef.Name] = control;
                }

                groupBox.Content = stackPanel;
                PropertiesPanel.Children.Add(groupBox);
            }
        }

        /// <summary>
        /// Creates controls for batch editing with placeholder text when values differ
        /// </summary>
        private FrameworkElement CreateBatchControlForProperty(PropertyDefinition propDef)
        {
            // Get values from all instances to determine if they're consistent
            var values = _instances.Select(inst => GetPropertyValue(inst, propDef.Name)).ToList();
            bool valuesAreSame = values.Distinct().Count() == 1;
            object commonValue = valuesAreSame ? values.First() : null;

            // Use the same control creation logic but with placeholder for mixed values
            var propertyEditor = new PropertyEditorWindow(_instances[0], _isTombEngine);
            
            // Reuse control creation logic but mark as batch mode
            switch (propDef.Type)
            {
                case PropertyType.Integer:
                case PropertyType.Float:
                    var textBox = new TextBox
                    {
                        Tag = new { IsBatch = true, PropDef = propDef }
                    };
                    
                    if (valuesAreSame && commonValue != null)
                        textBox.Text = commonValue.ToString();
                    else
                        textBox.Text = "<Mixed Values>";
                    
                    textBox.GotFocus += (s, e) =>
                    {
                        if (textBox.Text == "<Mixed Values>")
                            textBox.Text = "";
                    };
                    
                    return textBox;

                case PropertyType.Boolean:
                    var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    var radioTrue = new RadioButton { Content = "True", GroupName = propDef.Name };
                    var radioFalse = new RadioButton { Content = "False", GroupName = propDef.Name };
                    var radioMixed = new RadioButton { Content = "<Mixed>", GroupName = propDef.Name };

                    if (valuesAreSame && commonValue != null)
                    {
                        bool val = Convert.ToBoolean(commonValue);
                        radioTrue.IsChecked = val;
                        radioFalse.IsChecked = !val;
                    }
                    else
                    {
                        radioMixed.IsChecked = true;
                    }

                    stackPanel.Children.Add(radioTrue);
                    stackPanel.Children.Add(radioFalse);
                    stackPanel.Children.Add(radioMixed);
                    return stackPanel;

                case PropertyType.Dropdown:
                    var comboBox = new ComboBox();
                    comboBox.Items.Add("<Mixed Values>");
                    
                    if (propDef.Options != null)
                    {
                        foreach (var option in propDef.Options)
                            comboBox.Items.Add(option);
                    }

                    if (valuesAreSame && commonValue != null)
                    {
                        string valStr = commonValue.ToString();
                        if (comboBox.Items.Contains(valStr))
                            comboBox.SelectedItem = valStr;
                    }
                    else
                    {
                        comboBox.SelectedIndex = 0;
                    }

                    return comboBox;

                case PropertyType.Color:
                    var colorMainPanel = new StackPanel();
                    
                    // Parse color
                    string colorStr = (valuesAreSame && commonValue != null) ? commonValue.ToString() : "#FFFFFF";
                    var color = valuesAreSame ? ParseColor(colorStr) : System.Windows.Media.Colors.Gray;

                    // Create color preview
                    var colorPreview = new System.Windows.Shapes.Rectangle
                    {
                        Height = 40,
                        Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85)),
                        StrokeThickness = 1,
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    if (valuesAreSame && commonValue != null)
                    {
                        colorPreview.Fill = new System.Windows.Media.SolidColorBrush(color);
                    }
                    else
                    {
                        // Show gradient for mixed values
                        colorPreview.Fill = new System.Windows.Media.LinearGradientBrush(
                            System.Windows.Media.Colors.Red,
                            System.Windows.Media.Colors.Blue,
                            0);
                    }

                    // RGB Sliders Panel
                    var rgbPanel = new StackPanel();

                    // Red slider
                    var redPanel = CreateBatchColorSliderPanel("Red:", valuesAreSame ? color.R : (byte)128, System.Windows.Media.Colors.Red,
                        (value) => UpdateColorFromSliders(colorPreview, rgbPanel));

                    // Green slider
                    var greenPanel = CreateBatchColorSliderPanel("Green:", valuesAreSame ? color.G : (byte)128, System.Windows.Media.Colors.Green,
                        (value) => UpdateColorFromSliders(colorPreview, rgbPanel));

                    // Blue slider
                    var bluePanel = CreateBatchColorSliderPanel("Blue:", valuesAreSame ? color.B : (byte)128, System.Windows.Media.Colors.Blue,
                        (value) => UpdateColorFromSliders(colorPreview, rgbPanel));

                    rgbPanel.Children.Add(redPanel);
                    rgbPanel.Children.Add(greenPanel);
                    rgbPanel.Children.Add(bluePanel);

                    // Hex display
                    var hexPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    var hexLabel = new TextBlock
                    {
                        Text = valuesAreSame ? "Hex: " : "Hex: <Mixed> → ",
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 241, 241)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    var hexValue = new TextBlock
                    {
                        Text = valuesAreSame ? ColorToHex(color) : ColorToHex(System.Windows.Media.Color.FromRgb(128, 128, 128)),
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170)),
                        VerticalAlignment = VerticalAlignment.Center,
                        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                        Tag = "HexDisplay"
                    };
                    hexPanel.Children.Add(hexLabel);
                    hexPanel.Children.Add(hexValue);

                    // Store references
                    colorPreview.Tag = new { RedPanel = redPanel, GreenPanel = greenPanel, BluePanel = bluePanel, HexDisplay = hexValue };

                    colorMainPanel.Children.Add(colorPreview);
                    colorMainPanel.Children.Add(rgbPanel);
                    colorMainPanel.Children.Add(hexPanel);

                    return colorMainPanel;

                default:
                    return new TextBox { Text = valuesAreSame && commonValue != null ? commonValue.ToString() : "<Mixed Values>" };
            }
        }

        /// <summary>
        /// Gets property value from a specific instance
        /// </summary>
        private object GetPropertyValue(ItemInstance instance, string propertyName)
        {
            if (instance is MoveableInstance moveable)
            {
                if (propertyName == "HP")
                    return moveable.CustomProperties.GetProperty("HP", 100);
                if (propertyName == "OCB")
                    return moveable.Ocb;
                return moveable.CustomProperties.GetProperty(propertyName);
            }
            else if (instance is StaticInstance staticMesh)
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
        /// Saves all modified properties to all instances
        /// </summary>
        private void SaveBatchProperties()
        {
            foreach (var propDef in _propertyDefinitions)
            {
                if (!_propertyControls.ContainsKey(propDef.Name))
                    continue;

                var control = _propertyControls[propDef.Name];
                
                // Check if the control value was modified (not placeholder)
                if (IsValueModified(control, propDef.Type))
                {
                    object value = ExtractValueFromControl(control, propDef.Type);
                    
                    // Apply to all instances
                    foreach (var instance in _instances)
                    {
                        SetPropertyValue(instance, propDef.Name, value);
                    }
                }
            }

            PropertiesChanged = true;
        }

        private bool IsValueModified(FrameworkElement control, PropertyType type)
        {
            if (control is TextBox tb)
                return !string.IsNullOrEmpty(tb.Text) && tb.Text != "<Mixed Values>" && tb.Text != "<Mixed>";
            if (control is ComboBox cb)
                return cb.SelectedIndex > 0; // Index 0 is "<Mixed Values>"
            if (control is StackPanel sp)
            {
                if (type == PropertyType.Boolean)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is RadioButton rb && rb.Content.ToString() == "<Mixed>" && rb.IsChecked == true)
                            return false;
                    }
                    return true;
                }
                else if (type == PropertyType.Color)
                {
                    // Color is always considered modified since we show sliders
                    // User intention to change is implied by opening the dialog
                    return true;
                }
            }
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
                    if (control is StackPanel spInt)
                    {
                        foreach (var child in spInt.Children)
                        {
                            if (child is TextBox tbInt && int.TryParse(tbInt.Text, out int intVal))
                                return intVal;
                        }
                    }
                    return 0;

                case PropertyType.Float:
                    if (control is StackPanel spFloat)
                    {
                        foreach (var child in spFloat.Children)
                        {
                            if (child is TextBox tbFloat && float.TryParse(tbFloat.Text, out float floatVal))
                                return floatVal;
                        }
                    }
                    return 0.0f;

                case PropertyType.Boolean:
                    if (control is StackPanel sp)
                    {
                        foreach (var child in sp.Children)
                        {
                            if (child is RadioButton rb && rb.IsChecked == true && rb.Content.ToString() != "<Mixed>")
                                return rb.Content.ToString() == "True";
                        }
                    }
                    return false;

                case PropertyType.Dropdown:
                    if (control is ComboBox cb && cb.SelectedIndex > 0)
                        return cb.SelectedItem?.ToString() ?? "";
                    return "";

                case PropertyType.Color:
                    if (control is StackPanel spColor)
                    {
                        // Find the color preview rectangle which has the color info
                        foreach (var child in spColor.Children)
                        {
                            if (child is System.Windows.Shapes.Rectangle rect && rect.Fill is SolidColorBrush brush)
                            {
                                return ColorToHex(brush.Color);
                            }
                        }
                    }
                    return "#FFFFFF";

                default:
                    if (control is TextBox tb)
                        return tb.Text;
                    return "";
            }
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

        private void SetPropertyValue(ItemInstance instance, string propertyName, object value)
        {
            if (instance is MoveableInstance moveable)
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
            else if (instance is StaticInstance staticMesh)
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SaveBatchProperties();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will reset all properties in all selected objects to their default values. Continue?",
                "Batch Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var instance in _instances)
                {
                    foreach (var propDef in _propertyDefinitions)
                    {
                        var defaultValue = ParseDefaultValue(propDef);
                        SetPropertyValue(instance, propDef.Name, defaultValue);
                    }
                }
                BuildPropertyControls(); // Refresh UI
            }
        }

        /// <summary>
        /// Parses the default value string based on property type
        /// </summary>
        private object ParseDefaultValue(PropertyDefinition definition)
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
        private object GetTypeDefaultValue(PropertyType type)
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

        /// <summary>
        /// Creates a color slider panel for batch editing
        /// </summary>
        private StackPanel CreateBatchColorSliderPanel(string label, byte initialValue, System.Windows.Media.Color accentColor, Action<byte> onValueChanged)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

            // Header with label and value
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var labelText = new TextBlock
            {
                Text = label,
                Width = 50,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 241, 241)),
                VerticalAlignment = VerticalAlignment.Center
            };
            var valueText = new TextBlock
            {
                Text = initialValue.ToString(),
                Width = 35,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170)),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Tag = "ValueDisplay"
            };
            headerPanel.Children.Add(labelText);
            headerPanel.Children.Add(valueText);

            // Slider
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = initialValue,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 2, 0, 0),
                Tag = "ColorSlider"
            };

            slider.Foreground = new System.Windows.Media.SolidColorBrush(accentColor);

            slider.ValueChanged += (s, e) =>
            {
                valueText.Text = ((int)slider.Value).ToString();
                onValueChanged((byte)slider.Value);
            };

            panel.Children.Add(headerPanel);
            panel.Children.Add(slider);
            panel.Tag = new { Slider = slider, ValueDisplay = valueText };

            return panel;
        }

        /// <summary>
        /// Updates the color preview and hex display when sliders change
        /// </summary>
        private void UpdateColorFromSliders(System.Windows.Shapes.Rectangle preview, StackPanel rgbPanel)
        {
            byte r = 0, g = 0, b = 0;

            foreach (var child in rgbPanel.Children)
            {
                if (child is StackPanel sliderPanel && sliderPanel.Tag != null)
                {
                    dynamic tag = sliderPanel.Tag;
                    if (tag.Slider is Slider slider)
                    {
                        byte value = (byte)slider.Value;

                        if (rgbPanel.Children.IndexOf(sliderPanel) == 0)
                            r = value;
                        else if (rgbPanel.Children.IndexOf(sliderPanel) == 1)
                            g = value;
                        else if (rgbPanel.Children.IndexOf(sliderPanel) == 2)
                            b = value;
                    }
                }
            }

            var newColor = System.Windows.Media.Color.FromRgb(r, g, b);
            preview.Fill = new System.Windows.Media.SolidColorBrush(newColor);

            // Update hex display
            if (preview.Tag != null)
            {
                dynamic previewTag = preview.Tag;
                if (previewTag.HexDisplay is TextBlock hexDisplay)
                {
                    hexDisplay.Text = ColorToHex(newColor);
                }
            }
        }
    }
}
