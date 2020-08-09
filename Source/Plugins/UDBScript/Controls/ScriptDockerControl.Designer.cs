namespace CodeImp.DoomBuilder.UDBScript
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
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.filetree = new CodeImp.DoomBuilder.Controls.MultiSelectTreeview();
			this.parametersview = new System.Windows.Forms.DataGridView();
			this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.parametersview)).BeginInit();
			this.SuspendLayout();
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
			this.splitContainer1.Panel2.Controls.Add(this.parametersview);
			this.splitContainer1.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.splitContainer1.Size = new System.Drawing.Size(540, 663);
			this.splitContainer1.SplitterDistance = 508;
			this.splitContainer1.TabIndex = 25;
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
			this.filetree.Size = new System.Drawing.Size(540, 508);
			this.filetree.TabIndex = 24;
			this.filetree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.filetree_AfterSelect);
			// 
			// parametersview
			// 
			this.parametersview.AllowUserToAddRows = false;
			this.parametersview.AllowUserToDeleteRows = false;
			this.parametersview.AllowUserToResizeRows = false;
			this.parametersview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.parametersview.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Description,
            this.Value});
			this.parametersview.Dock = System.Windows.Forms.DockStyle.Fill;
			this.parametersview.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.parametersview.Location = new System.Drawing.Point(0, 0);
			this.parametersview.MultiSelect = false;
			this.parametersview.Name = "parametersview";
			this.parametersview.RowHeadersVisible = false;
			this.parametersview.Size = new System.Drawing.Size(540, 151);
			this.parametersview.TabIndex = 25;
			this.parametersview.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.parametersview_CellValueChanged);
			// 
			// Description
			// 
			this.Description.FillWeight = 50F;
			this.Description.HeaderText = "Description";
			this.Description.Name = "Description";
			this.Description.ReadOnly = true;
			this.Description.Width = 200;
			// 
			// Value
			// 
			this.Value.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.Value.FillWeight = 50F;
			this.Value.HeaderText = "Value";
			this.Value.Name = "Value";
			// 
			// ScriptDockerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Name = "ScriptDockerControl";
			this.Size = new System.Drawing.Size(546, 669);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.parametersview)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer splitContainer1;
		private Controls.MultiSelectTreeview filetree;
		private System.Windows.Forms.DataGridView parametersview;
		private System.Windows.Forms.DataGridViewTextBoxColumn Description;
		private System.Windows.Forms.DataGridViewTextBoxColumn Value;
	}
}
