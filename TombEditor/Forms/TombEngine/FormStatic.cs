using System.Windows.Forms;
using System.Windows.Forms.Integration;
using DarkUI.Forms;
using TombEditor.Windows;
using TombLib.LevelData;

namespace TombEditor.Forms.TombEngine
{
    /// <summary>
    /// Windows Forms wrapper for the WPF Property Editor for Statics
    /// This form is used when Level.IsTombEngine is true
    /// </summary>
    public partial class FormStatic : DarkForm
    {
        private readonly StaticInstance _staticMesh;
        private readonly Level _level;
        private PropertyEditorWindow _wpfWindow;
        private bool _wpfShown = false;

        public FormStatic(StaticInstance staticMesh, Level level = null)
        {
            _staticMesh = staticMesh;
            _level = level;
            InitializeComponent();

            // Set window property handlers
            Configuration.ConfigureWindow(this, Editor.Instance.Configuration);

            // Initialize property manager if not already done
            InitializePropertyManager();
        }

        private void InitializePropertyManager()
        {
            // Set the properties directory path
            string propertiesPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.ExecutablePath),
                "Resources", "Properties");

            if (System.IO.Directory.Exists(propertiesPath))
            {
                TombLib.LevelData.Properties.PropertyManager.Instance.SetPropertiesDirectory(propertiesPath);
            }
        }

        protected override void OnShown(System.EventArgs e)
        {
            base.OnShown(e);

            if (!_wpfShown)
            {
                _wpfShown = true;
                ShowWPFPropertyEditor();
            }
        }

        private void ShowWPFPropertyEditor()
        {
            _wpfWindow = new PropertyEditorWindow(_staticMesh, true, PropertyEditorContext.TombEditor, null, _level);
            
            // Convert WPF window result to WinForms DialogResult
            var wpfResult = _wpfWindow.ShowDialog();
            
            if (wpfResult == true)
            {
                // Properties were saved in the WPF window
                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Cancel;
            }

            Close();
        }
    }

    /// <summary>
    /// Designer portion of FormStatic
    /// </summary>
    partial class FormStatic
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FormStatic
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(100, 100);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FormStatic";
            this.Opacity = 0;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormStatic";
            this.ResumeLayout(false);
        }
    }
}
