#region ================== Copyright (c) 2020 Boris Iwanski

/*
 * This program is free software: you can redistribute it and/or modify
 *
 * it under the terms of the GNU General Public License as published by
 * 
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.If not, see<http://www.gnu.org/licenses/>.
 */

#endregion

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
		private ImageList images;

		public ImageList Images { get { return images; } }

		public ScriptDockerControl(string foldername)
		{
			InitializeComponent();

			images = new ImageList();
			images.Images.Add("Folder", Properties.Resources.Folder);
			images.Images.Add("Script", Properties.Resources.Script);

			filetree.ImageList = images;

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
				TreeNode tn = new TreeNode(Path.GetFileName(directory), AddFiles(directory));
				tn.SelectedImageKey = tn.ImageKey = "Folder";
				

				newnodes.Add(tn);
			}

			foreach (string filename in Directory.GetFiles(path))
			{
				if (Path.GetExtension(filename).ToLowerInvariant() == ".js")
				{
					TreeNode tn = new TreeNode(Path.GetFileNameWithoutExtension(filename));
					tn.Tag = filename;
					tn.SelectedImageKey = tn.ImageKey = "Script";

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
