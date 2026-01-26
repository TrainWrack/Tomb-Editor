using System;
using System.Windows;
using System.Windows.Media;

namespace TombEditor.Windows
{
    /// <summary>
    /// Color picker window with RGB sliders, hex input, and RGB text inputs
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        private bool _updating = false;

        public Color SelectedColor { get; private set; }

        public ColorPickerWindow(Color initialColor)
        {
            InitializeComponent();
            SelectedColor = initialColor;
            InitializeColor(initialColor);
        }

        private void InitializeColor(Color color)
        {
            _updating = true;
            
            RedSlider.Value = color.R;
            GreenSlider.Value = color.G;
            BlueSlider.Value = color.B;
            
            RedTextBox.Text = color.R.ToString();
            GreenTextBox.Text = color.G.ToString();
            BlueTextBox.Text = color.B.ToString();
            
            HexTextBox.Text = ColorToHex(color);
            ColorPreview.Fill = new SolidColorBrush(color);
            
            _updating = false;
        }

        private void RgbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updating) return;
            
            _updating = true;
            
            byte r = (byte)RedSlider.Value;
            byte g = (byte)GreenSlider.Value;
            byte b = (byte)BlueSlider.Value;
            
            RedTextBox.Text = r.ToString();
            GreenTextBox.Text = g.ToString();
            BlueTextBox.Text = b.ToString();
            
            var color = Color.FromRgb(r, g, b);
            HexTextBox.Text = ColorToHex(color);
            ColorPreview.Fill = new SolidColorBrush(color);
            SelectedColor = color;
            
            _updating = false;
        }

        private void RgbTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_updating) return;
            
            try
            {
                _updating = true;
                
                if (byte.TryParse(RedTextBox.Text, out byte r) &&
                    byte.TryParse(GreenTextBox.Text, out byte g) &&
                    byte.TryParse(BlueTextBox.Text, out byte b))
                {
                    RedSlider.Value = r;
                    GreenSlider.Value = g;
                    BlueSlider.Value = b;
                    
                    var color = Color.FromRgb(r, g, b);
                    HexTextBox.Text = ColorToHex(color);
                    ColorPreview.Fill = new SolidColorBrush(color);
                    SelectedColor = color;
                }
            }
            finally
            {
                _updating = false;
            }
        }

        private void HexTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_updating) return;
            
            try
            {
                _updating = true;
                
                var color = ParseHexColor(HexTextBox.Text);
                
                RedSlider.Value = color.R;
                GreenSlider.Value = color.G;
                BlueSlider.Value = color.B;
                
                RedTextBox.Text = color.R.ToString();
                GreenTextBox.Text = color.G.ToString();
                BlueTextBox.Text = color.B.ToString();
                
                ColorPreview.Fill = new SolidColorBrush(color);
                SelectedColor = color;
            }
            catch
            {
                // Invalid hex color, ignore
            }
            finally
            {
                _updating = false;
            }
        }

        private Color ParseHexColor(string hex)
        {
            hex = hex.Trim();
            
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);
            
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return Color.FromRgb(r, g, b);
            }
            
            throw new FormatException("Invalid hex color format");
        }

        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
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
