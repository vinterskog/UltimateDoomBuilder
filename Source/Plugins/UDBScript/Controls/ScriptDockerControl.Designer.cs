﻿namespace CodeImp.DoomBuilder.UDBScript
{
	partial class ScriptDockerControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.btnResetToDefaults = new System.Windows.Forms.Button();
			this.btnRunScript = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.label1 = new System.Windows.Forms.Label();
			this.filetree = new CodeImp.DoomBuilder.Controls.MultiSelectTreeview();
			this.label2 = new System.Windows.Forms.Label();
			this.scriptoptions = new CodeImp.DoomBuilder.UDBScript.ScriptOptionsControl();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnResetToDefaults
			// 
			this.btnResetToDefaults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnResetToDefaults.Location = new System.Drawing.Point(3, 3);
			this.btnResetToDefaults.Name = "btnResetToDefaults";
			this.btnResetToDefaults.Size = new System.Drawing.Size(153, 27);
			this.btnResetToDefaults.TabIndex = 26;
			this.btnResetToDefaults.Text = "Reset";
			this.btnResetToDefaults.UseVisualStyleBackColor = true;
			this.btnResetToDefaults.Click += new System.EventHandler(this.btnResetToDefaults_Click);
			// 
			// btnRunScript
			// 
			this.btnRunScript.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnRunScript.Location = new System.Drawing.Point(162, 3);
			this.btnRunScript.Name = "btnRunScript";
			this.btnRunScript.Size = new System.Drawing.Size(153, 27);
			this.btnRunScript.TabIndex = 27;
			this.btnRunScript.Text = "Run";
			this.btnRunScript.UseVisualStyleBackColor = true;
			this.btnRunScript.Click += new System.EventHandler(this.btnRunScript_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.Controls.Add(this.btnResetToDefaults, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.btnRunScript, 1, 0);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 505);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(318, 33);
			this.tableLayoutPanel1.TabIndex = 28;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.Location = new System.Drawing.Point(3, 3);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.filetree);
			this.splitContainer1.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.label1);
			this.splitContainer1.Panel2.Controls.Add(this.label2);
			this.splitContainer1.Panel2.Controls.Add(this.scriptoptions);
			this.splitContainer1.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.splitContainer1.Size = new System.Drawing.Size(312, 496);
			this.splitContainer1.SplitterDistance = 239;
			this.splitContainer1.TabIndex = 25;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(3, 4);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(71, 13);
			this.label1.TabIndex = 27;
			this.label1.Text = "Script options";
			// 
			// filetree
			// 
			this.filetree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.filetree.HideSelection = false;
			this.filetree.Location = new System.Drawing.Point(0, 0);
			this.filetree.Margin = new System.Windows.Forms.Padding(8, 8, 9, 8);
			this.filetree.Name = "filetree";
			this.filetree.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			this.filetree.SelectionMode = CodeImp.DoomBuilder.Controls.TreeViewSelectionMode.SingleSelect;
			this.filetree.ShowNodeToolTips = true;
			this.filetree.Size = new System.Drawing.Size(312, 239);
			this.filetree.TabIndex = 24;
			this.filetree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.filetree_AfterSelect);
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label2.Location = new System.Drawing.Point(0, 11);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(313, 2);
			this.label2.TabIndex = 2;
			// 
			// scriptoptions
			// 
			this.scriptoptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.scriptoptions.Location = new System.Drawing.Point(0, 20);
			this.scriptoptions.Name = "scriptoptions";
			this.scriptoptions.Size = new System.Drawing.Size(312, 230);
			this.scriptoptions.TabIndex = 26;
			// 
			// ScriptDockerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.splitContainer1);
			this.Name = "ScriptDockerControl";
			this.Size = new System.Drawing.Size(318, 541);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer splitContainer1;
		private Controls.MultiSelectTreeview filetree;
		private System.Windows.Forms.Button btnResetToDefaults;
		private System.Windows.Forms.Button btnRunScript;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private ScriptOptionsControl scriptoptions;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
	}
}
