﻿namespace Waap
{
    partial class CreateWorktree
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateWorktree));
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.dialogCreateWorktree1 = new Waap.DialogCreateWorktree();
            this.SuspendLayout();
            // 
            // elementHost1
            // 
            this.elementHost1.AutoSize = true;
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.elementHost1.Location = new System.Drawing.Point(1, 1);
            this.elementHost1.Margin = new System.Windows.Forms.Padding(1);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Padding = new System.Windows.Forms.Padding(1);
            this.elementHost1.Size = new System.Drawing.Size(498, 198);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.dialogCreateWorktree1;
            // 
            // CreateWorktree
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.Purple;
            this.ClientSize = new System.Drawing.Size(500, 200);
            this.Controls.Add(this.elementHost1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CreateWorktree";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "CreateWorktree";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private DialogCreateWorktree dialogCreateWorktree1;
    }
}