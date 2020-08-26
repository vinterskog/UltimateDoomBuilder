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
			this.btnResetToDefaults = new System.Windows.Forms.Button();
			this.btnRunScript = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.filetree = new CodeImp.DoomBuilder.Controls.MultiSelectTreeview();
			this.parametersview = new System.Windows.Forms.DataGridView();
			this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.scriptOptionsControl1 = new CodeImp.DoomBuilder.UDBScript.ScriptOptionsControl();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.parametersview)).BeginInit();
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
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 465);
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
			this.splitContainer1.Panel2.Controls.Add(this.scriptOptionsControl1);
			this.splitContainer1.Panel2.Controls.Add(this.parametersview);
			this.splitContainer1.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.splitContainer1.Size = new System.Drawing.Size(312, 456);
			this.splitContainer1.SplitterDistance = 168;
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
			this.filetree.Size = new System.Drawing.Size(312, 168);
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
			this.parametersview.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.parametersview.Location = new System.Drawing.Point(0, 0);
			this.parametersview.MultiSelect = false;
			this.parametersview.Name = "parametersview";
			this.parametersview.RowHeadersVisible = false;
			this.parametersview.Size = new System.Drawing.Size(312, 104);
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
			// scriptOptionsControl1
			// 
			this.scriptOptionsControl1.Location = new System.Drawing.Point(3, 110);
			this.scriptOptionsControl1.Name = "scriptOptionsControl1";
			this.scriptOptionsControl1.Size = new System.Drawing.Size(306, 171);
			this.scriptOptionsControl1.TabIndex = 26;
			// 
			// ScriptDockerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.splitContainer1);
			this.Name = "ScriptDockerControl";
			this.Size = new System.Drawing.Size(318, 501);
			this.tableLayoutPanel1.ResumeLayout(false);
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
		private System.Windows.Forms.Button btnResetToDefaults;
		private System.Windows.Forms.Button btnRunScript;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private ScriptOptionsControl scriptOptionsControl1;
	}
}
