﻿namespace TombEditor.ToolWindows
{
    partial class TexturePanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panelTextureMap = new TombEditor.Controls.PanelTextureMap();
            this.panelTextureTools = new System.Windows.Forms.Panel();
            this.panelTileSizeSelector = new System.Windows.Forms.Panel();
            this.darkLabel1 = new DarkUI.Controls.DarkLabel();
            this.rbTileSize256 = new DarkUI.Controls.DarkRadioButton();
            this.rbTileSize128 = new DarkUI.Controls.DarkRadioButton();
            this.rbTileSize64 = new DarkUI.Controls.DarkRadioButton();
            this.butBump = new DarkUI.Controls.DarkButton();
            this.butAnimationRanges = new DarkUI.Controls.DarkButton();
            this.butTextureSounds = new DarkUI.Controls.DarkButton();
            this.panelTextureTools.SuspendLayout();
            this.panelTileSizeSelector.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTextureMap
            // 
            this.panelTextureMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTextureMap.Location = new System.Drawing.Point(0, 25);
            this.panelTextureMap.Name = "panelTextureMap";
            this.panelTextureMap.Size = new System.Drawing.Size(284, 732);
            this.panelTextureMap.TabIndex = 9;
            // 
            // panelTextureTools
            // 
            this.panelTextureTools.Controls.Add(this.panelTileSizeSelector);
            this.panelTextureTools.Controls.Add(this.butBump);
            this.panelTextureTools.Controls.Add(this.butAnimationRanges);
            this.panelTextureTools.Controls.Add(this.butTextureSounds);
            this.panelTextureTools.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelTextureTools.Location = new System.Drawing.Point(0, 757);
            this.panelTextureTools.Name = "panelTextureTools";
            this.panelTextureTools.Size = new System.Drawing.Size(284, 57);
            this.panelTextureTools.TabIndex = 10;
            // 
            // panelTileSizeSelector
            // 
            this.panelTileSizeSelector.Controls.Add(this.darkLabel1);
            this.panelTileSizeSelector.Controls.Add(this.rbTileSize256);
            this.panelTileSizeSelector.Controls.Add(this.rbTileSize128);
            this.panelTileSizeSelector.Controls.Add(this.rbTileSize64);
            this.panelTileSizeSelector.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTileSizeSelector.Location = new System.Drawing.Point(0, 0);
            this.panelTileSizeSelector.Name = "panelTileSizeSelector";
            this.panelTileSizeSelector.Size = new System.Drawing.Size(284, 31);
            this.panelTileSizeSelector.TabIndex = 3;
            // 
            // darkLabel1
            // 
            this.darkLabel1.AutoSize = true;
            this.darkLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel1.Location = new System.Drawing.Point(22, 9);
            this.darkLabel1.Name = "darkLabel1";
            this.darkLabel1.Size = new System.Drawing.Size(98, 13);
            this.darkLabel1.TabIndex = 3;
            this.darkLabel1.Text = "Selection tile size:";
            // 
            // rbTileSize256
            // 
            this.rbTileSize256.AutoSize = true;
            this.rbTileSize256.Location = new System.Drawing.Point(218, 8);
            this.rbTileSize256.Name = "rbTileSize256";
            this.rbTileSize256.Size = new System.Drawing.Size(43, 17);
            this.rbTileSize256.TabIndex = 2;
            this.rbTileSize256.TabStop = true;
            this.rbTileSize256.Text = "256";
            this.rbTileSize256.CheckedChanged += new System.EventHandler(this.rbTileSize256_CheckedChanged);
            // 
            // rbTileSize128
            // 
            this.rbTileSize128.AutoSize = true;
            this.rbTileSize128.Location = new System.Drawing.Point(169, 8);
            this.rbTileSize128.Name = "rbTileSize128";
            this.rbTileSize128.Size = new System.Drawing.Size(43, 17);
            this.rbTileSize128.TabIndex = 1;
            this.rbTileSize128.TabStop = true;
            this.rbTileSize128.Text = "128";
            this.rbTileSize128.CheckedChanged += new System.EventHandler(this.rbTileSize128_CheckedChanged);
            // 
            // rbTileSize64
            // 
            this.rbTileSize64.AutoSize = true;
            this.rbTileSize64.Location = new System.Drawing.Point(126, 8);
            this.rbTileSize64.Name = "rbTileSize64";
            this.rbTileSize64.Size = new System.Drawing.Size(37, 17);
            this.rbTileSize64.TabIndex = 0;
            this.rbTileSize64.TabStop = true;
            this.rbTileSize64.Text = "64";
            this.rbTileSize64.CheckedChanged += new System.EventHandler(this.rbTileSize64_CheckedChanged);
            // 
            // butBump
            // 
            this.butBump.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.butBump.Location = new System.Drawing.Point(217, 31);
            this.butBump.Name = "butBump";
            this.butBump.Padding = new System.Windows.Forms.Padding(5);
            this.butBump.Size = new System.Drawing.Size(65, 23);
            this.butBump.TabIndex = 2;
            this.butBump.Text = "Bump";
            this.butBump.Visible = false;
            // 
            // butAnimationRanges
            // 
            this.butAnimationRanges.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.butAnimationRanges.Location = new System.Drawing.Point(110, 31);
            this.butAnimationRanges.Name = "butAnimationRanges";
            this.butAnimationRanges.Padding = new System.Windows.Forms.Padding(5);
            this.butAnimationRanges.Size = new System.Drawing.Size(103, 23);
            this.butAnimationRanges.TabIndex = 1;
            this.butAnimationRanges.Text = "Animation ranges";
            this.butAnimationRanges.Click += new System.EventHandler(this.butAnimationRanges_Click);
            // 
            // butTextureSounds
            // 
            this.butTextureSounds.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.butTextureSounds.Location = new System.Drawing.Point(3, 31);
            this.butTextureSounds.Name = "butTextureSounds";
            this.butTextureSounds.Padding = new System.Windows.Forms.Padding(5);
            this.butTextureSounds.Size = new System.Drawing.Size(103, 23);
            this.butTextureSounds.TabIndex = 0;
            this.butTextureSounds.Text = "Texture sounds";
            this.butTextureSounds.Click += new System.EventHandler(this.butTextureSounds_Click);
            // 
            // TexturePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panelTextureMap);
            this.Controls.Add(this.panelTextureTools);
            this.DefaultDockArea = DarkUI.Docking.DarkDockArea.Right;
            this.DockText = "Texture Panel";
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MinimumSize = new System.Drawing.Size(284, 100);
            this.Name = "TexturePanel";
            this.SerializationKey = "TexturePanel";
            this.Size = new System.Drawing.Size(284, 814);
            this.panelTextureTools.ResumeLayout(false);
            this.panelTileSizeSelector.ResumeLayout(false);
            this.panelTileSizeSelector.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.PanelTextureMap panelTextureMap;
        private System.Windows.Forms.Panel panelTextureTools;
        private DarkUI.Controls.DarkButton butBump;
        private DarkUI.Controls.DarkButton butAnimationRanges;
        private DarkUI.Controls.DarkButton butTextureSounds;
        private System.Windows.Forms.Panel panelTileSizeSelector;
        private DarkUI.Controls.DarkLabel darkLabel1;
        private DarkUI.Controls.DarkRadioButton rbTileSize256;
        private DarkUI.Controls.DarkRadioButton rbTileSize128;
        private DarkUI.Controls.DarkRadioButton rbTileSize64;
    }
}
