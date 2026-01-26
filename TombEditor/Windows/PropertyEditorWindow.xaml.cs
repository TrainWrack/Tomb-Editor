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
        private readonly Dictionary<string, Control> _propertyControls;
        private readonly bool _isTombEngine;

        public bool PropertiesChanged { get; private set; }

        public PropertyEditorWindow(ItemInstance instance, bool isTombEngine)
        {
            InitializeComponent();
            _instance = instance;
            _isTombEngine = isTombEngine;
            _propertyControls = new Dictionary<string, Control>();
            PropertiesChanged = false;

            // Load property definitions based on instance type
            if (instance is MoveableInstance moveable)
            {
                TitleText.Text = "Moveable Properties";
                SubtitleText.Text = $"Object: {moveable.WadObjectId}";
                
                var propertySet = PropertyManager.Instance.GetMoveableProperties(moveable.WadObjectId.ToString());
                _propertyDefinitions = propertySet.Properties;
            }
            else if (instance is StaticInstance staticMesh)
            {
                TitleText.Text = "Static Properties";
                SubtitleText.Text = $"Object: {staticMesh.WadObjectId}";
                
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
        /// </summary>
        private void BuildPropertyControls()
        {
            PropertiesPanel.Children.Clear();
            _propertyControls.Clear();

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
                        Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                        FontSize = 10,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    stackPanel.Children.Add(descLabel);
                }

                // Create control based on property type
                Control control = CreateControlForProperty(propDef);
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
        /// Creates the appropriate WPF control for a property
        /// </summary>
        private Control CreateControlForProperty(PropertyDefinition propDef)
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

                default:
                    return CreateTextControl(propDef, currentValue);
            }
        }

        private Control CreateIntegerControl(PropertyDefinition propDef, object currentValue)
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

        private Control CreateFloatControl(PropertyDefinition propDef, object currentValue)
        {
            var textBox = new TextBox
            {
                Text = currentValue?.ToString() ?? propDef.Default ?? "0.0"
            };

            // Add validation for floats
            textBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !IsValidFloat(((TextBox)s).Text + e.Text);
            };

            return textBox;
        }

        private Control CreateBooleanControl(PropertyDefinition propDef, object currentValue)
        {
            bool value = currentValue != null ? Convert.ToBoolean(currentValue) : 
                         bool.TryParse(propDef.Default, out bool defVal) ? defVal : false;

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var radioTrue = new RadioButton
            {
                Content = "True",
                GroupName = propDef.Name,
                IsChecked = value
            };

            var radioFalse = new RadioButton
            {
                Content = "False",
                GroupName = propDef.Name,
                IsChecked = !value
            };

            stackPanel.Children.Add(radioTrue);
            stackPanel.Children.Add(radioFalse);

            return stackPanel;
        }

        private Control CreateDropdownControl(PropertyDefinition propDef, object currentValue)
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

        private Control CreateCheckboxListControl(PropertyDefinition propDef, object currentValue)
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

        private Control CreateTextControl(PropertyDefinition propDef, object currentValue)
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
        private object ExtractValueFromControl(Control control, PropertyType type)
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
                    if (control is StackPanel sp)
                    {
                        foreach (var child in sp.Children)
                        {
                            if (child is RadioButton rb && rb.IsChecked == true)
                                return rb.Content.ToString() == "True";
                        }
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
                    moveable.Ocb = (short)ocb;
                else
                    moveable.CustomProperties.SetProperty(propertyName, value);
            }
            else if (_instance is StaticInstance staticMesh)
            {
                if (propertyName == "HP")
                    staticMesh.CustomProperties.SetProperty("HP", value);
                else if (propertyName == "OCB" && value is int ocb)
                    staticMesh.Ocb = (short)ocb;
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

        private void SetControlToDefault(Control control, PropertyDefinition propDef)
        {
            switch (propDef.Type)
            {
                case PropertyType.Integer:
                case PropertyType.Float:
                    if (control is TextBox tb)
                        tb.Text = propDef.Default ?? "0";
                    break;

                case PropertyType.Boolean:
                    if (control is StackPanel sp)
                    {
                        bool defVal = bool.TryParse(propDef.Default, out bool b) && b;
                        foreach (var child in sp.Children)
                        {
                            if (child is RadioButton rb)
                                rb.IsChecked = (rb.Content.ToString() == "True") == defVal;
                        }
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

                default:
                    if (control is TextBox tb2)
                        tb2.Text = propDef.Default ?? "";
                    break;
            }
        }

        // Validation helpers
        private bool IsValidInteger(string text)
        {
            return int.TryParse(text, out _) || text == "-" || string.IsNullOrEmpty(text);
        }

        private bool IsValidFloat(string text)
        {
            return float.TryParse(text, out _) || text == "-" || text.EndsWith(".") || string.IsNullOrEmpty(text);
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
