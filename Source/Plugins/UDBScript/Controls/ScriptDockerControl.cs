using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.UDBScript
{
	public partial class ScriptDockerControl : UserControl
	{
		public ScriptDockerControl(string foldername)
		{
			InitializeComponent();

			FillTree(foldername);
		}

		private void FillTree(string foldername)
		{
			string path = Path.Combine(General.AppPath, foldername);
			string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

			filetree.Nodes.AddRange(AddFiles(Path.Combine(path, "scripts")));
			filetree.ExpandAll();
		}

		private TreeNode[] AddFiles(string path)
		{
			List<TreeNode> newnodes = new List<TreeNode>();

			foreach (string directory in Directory.GetDirectories(path))
			{
				newnodes.Add(new TreeNode(Path.GetFileName(directory), AddFiles(directory)));
			}

			foreach (string filename in Directory.GetFiles(path))
			{
				if (Path.GetExtension(filename).ToLowerInvariant() == ".js")
				{
					TreeNode tn = new TreeNode(Path.GetFileNameWithoutExtension(filename));
					tn.Tag = filename;

					newnodes.Add(tn);
				}
			}

			return newnodes.ToArray();
		}

		private void filetree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node.Tag == null)
				return;

			if (e.Node.Tag is string)
			{
				BuilderPlug.Me.CurrentScriptFile = (string)e.Node.Tag;
			}
		}
	}
}
