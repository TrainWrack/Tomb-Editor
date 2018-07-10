﻿namespace TombEditor.ToolWindows
{
    partial class ItemBrowser
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
            this.panelStaticMeshColor = new System.Windows.Forms.Panel();
            this.darkLabel14 = new DarkUI.Controls.DarkLabel();
            this.panelItem = new TombEditor.Controls.PanelRenderingItem();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.comboItems = new DarkUI.Controls.DarkComboBox();
            this.panelHeaderRight = new System.Windows.Forms.Panel();
            this.butSearch = new DarkUI.Controls.DarkButton();
            this.butAddItem = new DarkUI.Controls.DarkButton();
            this.butFindItem = new DarkUI.Controls.DarkButton();
            this.panelRightBottom = new System.Windows.Forms.Panel();
            this.panelRight = new System.Windows.Forms.Panel();
            this.panelHeader.SuspendLayout();
            this.panelHeaderRight.SuspendLayout();
            this.panelRightBottom.SuspendLayout();
            this.panelRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelStaticMeshColor
            // 
            this.panelStaticMeshColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelStaticMeshColor.Location = new System.Drawing.Point(68, 6);
            this.panelStaticMeshColor.Name = "panelStaticMeshColor";
            this.panelStaticMeshColor.Size = new System.Drawing.Size(67, 23);
            this.panelStaticMeshColor.TabIndex = 4;
            this.panelStaticMeshColor.Click += new System.EventHandler(this.panelStaticMeshColor_Click);
            // 
            // darkLabel14
            // 
            this.darkLabel14.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.darkLabel14.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel14.Location = new System.Drawing.Point(0, 11);
            this.darkLabel14.Name = "darkLabel14";
            this.darkLabel14.Size = new System.Drawing.Size(70, 17);
            this.darkLabel14.TabIndex = 67;
            this.darkLabel14.Text = "Static color:";
            // 
            // panelItem
            // 
            this.panelItem.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelItem.AutoSize = true;
            this.panelItem.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelItem.Location = new System.Drawing.Point(3, 2);
            this.panelItem.Name = "panelItem";
            this.panelItem.Size = new System.Drawing.Size(279, 165);
            this.panelItem.TabIndex = 62;
            // 
            // colorDialog
            // 
            this.colorDialog.AnyColor = true;
            this.colorDialog.FullOpen = true;
            // 
            // panelHeader
            // 
            this.panelHeader.Controls.Add(this.comboItems);
            this.panelHeader.Controls.Add(this.panelHeaderRight);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 25);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Padding = new System.Windows.Forms.Padding(1, 2, 0, 0);
            this.panelHeader.Size = new System.Drawing.Size(284, 27);
            this.panelHeader.TabIndex = 72;
            // 
            // comboItems
            // 
            this.comboItems.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboItems.DropDownHeight = 400;
            this.comboItems.IntegralHeight = false;
            this.comboItems.ItemHeight = 18;
            this.comboItems.Location = new System.Drawing.Point(3, 2);
            this.comboItems.Name = "comboItems";
            this.comboItems.Size = new System.Drawing.Size(221, 24);
            this.comboItems.TabIndex = 1;
            this.comboItems.SelectedIndexChanged += new System.EventHandler(this.comboItems_SelectedIndexChanged);
            this.comboItems.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.comboItems_Format);
            // 
            // panelHeaderRight
            // 
            this.panelHeaderRight.Controls.Add(this.butSearch);
            this.panelHeaderRight.Controls.Add(this.butAddItem);
            this.panelHeaderRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelHeaderRight.Location = new System.Drawing.Point(222, 2);
            this.panelHeaderRight.Name = "panelHeaderRight";
            this.panelHeaderRight.Size = new System.Drawing.Size(62, 25);
            this.panelHeaderRight.TabIndex = 76;
            // 
            // butSearch
            // 
            this.butSearch.Image = global::TombEditor.Properties.Resources.general_search_16;
            this.butSearch.Location = new System.Drawing.Point(7, 0);
            this.butSearch.Name = "butSearch";
            this.butSearch.Size = new System.Drawing.Size(24, 24);
            this.butSearch.TabIndex = 2;
            this.butSearch.Click += new System.EventHandler(this.butSearch_Click);
            // 
            // butAddItem
            // 
            this.butAddItem.Image = global::TombEditor.Properties.Resources.general_plus_math_16;
            this.butAddItem.Location = new System.Drawing.Point(36, 0);
            this.butAddItem.Name = "butAddItem";
            this.butAddItem.Size = new System.Drawing.Size(24, 24);
            this.butAddItem.TabIndex = 3;
            this.butAddItem.Click += new System.EventHandler(this.butAddItem_Click);
            // 
            // butFindItem
            // 
            this.butFindItem.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.butFindItem.Image = global::TombEditor.Properties.Resources.general_target_16;
            this.butFindItem.Location = new System.Drawing.Point(192, 6);
            this.butFindItem.Name = "butFindItem";
            this.butFindItem.Size = new System.Drawing.Size(91, 23);
            this.butFindItem.TabIndex = 5;
            this.butFindItem.Text = "Locate item";
            this.butFindItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.butFindItem.Click += new System.EventHandler(this.butFindItem_Click);
            // 
            // panelRightBottom
            // 
            this.panelRightBottom.Controls.Add(this.panelStaticMeshColor);
            this.panelRightBottom.Controls.Add(this.butFindItem);
            this.panelRightBottom.Controls.Add(this.darkLabel14);
            this.panelRightBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelRightBottom.Location = new System.Drawing.Point(0, 167);
            this.panelRightBottom.Name = "panelRightBottom";
            this.panelRightBottom.Size = new System.Drawing.Size(284, 33);
            this.panelRightBottom.TabIndex = 1;
            // 
            // panelRight
            // 
            this.panelRight.Controls.Add(this.panelRightBottom);
            this.panelRight.Controls.Add(this.panelItem);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Location = new System.Drawing.Point(0, 52);
            this.panelRight.Name = "panelRight";
            this.panelRight.Size = new System.Drawing.Size(284, 200);
            this.panelRight.TabIndex = 73;
            // 
            // ItemBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelHeader);
            this.DockText = "Item Browser";
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MinimumSize = new System.Drawing.Size(237, 168);
            this.Name = "ItemBrowser";
            this.SerializationKey = "ItemBrowser";
            this.Size = new System.Drawing.Size(284, 252);
            this.panelHeader.ResumeLayout(false);
            this.panelHeaderRight.ResumeLayout(false);
            this.panelRightBottom.ResumeLayout(false);
            this.panelRight.ResumeLayout(false);
            this.panelRight.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private DarkUI.Controls.DarkButton butFindItem;
        private System.Windows.Forms.Panel panelStaticMeshColor;
        private DarkUI.Controls.DarkLabel darkLabel14;
        private Controls.PanelRenderingItem panelItem;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Panel panelHeaderRight;
        private DarkUI.Controls.DarkButton butAddItem;
        private DarkUI.Controls.DarkComboBox comboItems;
        private DarkUI.Controls.DarkButton butSearch;
        private System.Windows.Forms.Panel panelRightBottom;
        private System.Windows.Forms.Panel panelRight;
    }
}
