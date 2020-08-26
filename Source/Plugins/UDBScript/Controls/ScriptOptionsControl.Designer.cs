namespace CodeImp.DoomBuilder.UDBScript
{
	partial class ScriptOptionsControl
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
			this.parametersview = new System.Windows.Forms.DataGridView();
			this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.enumscombo = new System.Windows.Forms.ComboBox();
			((System.ComponentModel.ISupportInitialize)(this.parametersview)).BeginInit();
			this.SuspendLayout();
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
			this.parametersview.Size = new System.Drawing.Size(657, 446);
			this.parametersview.TabIndex = 26;
			this.parametersview.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.parametersview_CellBeginEdit);
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
			// enumscombo
			// 
			this.enumscombo.FormattingEnabled = true;
			this.enumscombo.Location = new System.Drawing.Point(177, 256);
			this.enumscombo.Name = "enumscombo";
			this.enumscombo.Size = new System.Drawing.Size(121, 21);
			this.enumscombo.TabIndex = 27;
			// 
			// ScriptOptionsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.enumscombo);
			this.Controls.Add(this.parametersview);
			this.Name = "ScriptOptionsControl";
			this.Size = new System.Drawing.Size(657, 446);
			((System.ComponentModel.ISupportInitialize)(this.parametersview)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView parametersview;
		private System.Windows.Forms.DataGridViewTextBoxColumn Description;
		private System.Windows.Forms.DataGridViewTextBoxColumn Value;
		private System.Windows.Forms.ComboBox enumscombo;
	}
}
