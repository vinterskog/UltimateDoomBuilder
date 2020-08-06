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
			this.filetree = new System.Windows.Forms.TreeView();
			this.SuspendLayout();
			// 
			// filetree
			// 
			this.filetree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.filetree.Location = new System.Drawing.Point(3, 3);
			this.filetree.Name = "filetree";
			this.filetree.Size = new System.Drawing.Size(296, 326);
			this.filetree.TabIndex = 0;
			this.filetree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.filetree_AfterSelect);
			// 
			// ScriptDockerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.filetree);
			this.Name = "ScriptDockerControl";
			this.Size = new System.Drawing.Size(302, 372);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TreeView filetree;
	}
}
