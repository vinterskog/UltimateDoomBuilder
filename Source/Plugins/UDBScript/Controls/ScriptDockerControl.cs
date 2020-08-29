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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeImp.DoomBuilder.IO;

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

		public void EndEdit()
		{
			scriptOptionsControl1.EndEdit();
		}

		private void filetree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node.Tag == null)
				return;

			if (e.Node.Tag is string)
			{
				BuilderPlug.Me.CurrentScriptFile = (string)e.Node.Tag;

				string configfile = Path.Combine(Path.GetDirectoryName(BuilderPlug.Me.CurrentScriptFile), Path.GetFileNameWithoutExtension(BuilderPlug.Me.CurrentScriptFile)) + ".cfg";

				if (File.Exists(configfile))
				{
					Configuration cfg = new Configuration(configfile, true);

					IDictionary options = cfg.ReadSetting("options", new Hashtable());

					scriptOptionsControl1.ParametersView.Rows.Clear();

					foreach (DictionaryEntry de in options)
					{
						string description = cfg.ReadSetting(string.Format("options.{0}.description", de.Key), "no description");
						int type = cfg.ReadSetting(string.Format("options.{0}.type", de.Key), 0);
						string defaultvaluestr = cfg.ReadSetting(string.Format("options.{0}.default", de.Key), string.Empty);

						ScriptOption so = new ScriptOption((string)de.Key, description, type, defaultvaluestr);

						// Try to read a saved script option value from the config
						string savedvalue = General.Settings.ReadPluginSetting(BuilderPlug.Me.GetScriptPathHash() + "." + so.name, so.defaultvalue.ToString());

						if (string.IsNullOrWhiteSpace(savedvalue))
							so.value = so.defaultvalue;
						else
							so.value = savedvalue;

						so.typehandler.SetValue(so.value);

						int index = scriptOptionsControl1.ParametersView.Rows.Add(); 
						scriptOptionsControl1.ParametersView.Rows[index].Tag = so;
						scriptOptionsControl1.ParametersView.Rows[index].Cells["Value"].Value = so.value;
						scriptOptionsControl1.ParametersView.Rows[index].Cells["Description"].Value = description;
					}
				}
				else
				{
					scriptOptionsControl1.ParametersView.Rows.Clear();
					scriptOptionsControl1.ParametersView.Refresh();
				}
			}
		}

		public ExpandoObject GetScriptOptions()
		{
			ExpandoObject eo = new ExpandoObject();
			var options = eo as IDictionary<string, object>;

			foreach (DataGridViewRow row in scriptOptionsControl1.ParametersView.Rows)
			{
				if(row.Tag is ScriptOption)
				{
					ScriptOption so = (ScriptOption)row.Tag;
					//options[so.name] = so.value;
					options[so.name] = so.typehandler.GetValue();
				}
			}

			return eo;
		}

		private void parametersview_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex == 0 || scriptOptionsControl1.ParametersView.Rows[e.RowIndex].Tag == null)
				return;

			object newvalue = scriptOptionsControl1.ParametersView.Rows[e.RowIndex].Cells["Value"].Value;

			ScriptOption so = (ScriptOption)scriptOptionsControl1.ParametersView.Rows[e.RowIndex].Tag;

			// If the new value is empty reset it to the default value. Don't fire this event again, though
			if (newvalue == null || string.IsNullOrWhiteSpace(newvalue.ToString()))
			{
				newvalue = so.defaultvalue;
				scriptOptionsControl1.ParametersView.CellValueChanged -= parametersview_CellValueChanged;
				scriptOptionsControl1.ParametersView.Rows[e.RowIndex].Cells["Value"].Value = newvalue.ToString();
				scriptOptionsControl1.ParametersView.CellValueChanged += parametersview_CellValueChanged;
			}

			so.typehandler.SetValue(newvalue);

			so.value = newvalue;
			scriptOptionsControl1.ParametersView.Rows[e.RowIndex].Tag = so;

			// Make the text lighter if it's the default value, and store the setting in the config file if it's not the default
			if (so.value.ToString() == so.defaultvalue.ToString())
			{
				scriptOptionsControl1.ParametersView.Rows[e.RowIndex].Cells["Value"].Style.ForeColor = SystemColors.GrayText;
				General.Settings.DeletePluginSetting(BuilderPlug.Me.GetScriptPathHash() + "." + so.name);
			}
			else
			{
				scriptOptionsControl1.ParametersView.Rows[e.RowIndex].Cells["Value"].Style.ForeColor = SystemColors.WindowText;
				General.Settings.WritePluginSetting(BuilderPlug.Me.GetScriptPathHash() + "." + so.name, so.value);

			}

		}

		private void btnRunScript_Click(object sender, EventArgs e)
		{
			BuilderPlug.Me.ScriptExecute();
		}

		private void btnResetToDefaults_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow row in scriptOptionsControl1.ParametersView.Rows)
			{
				if (row.Tag is ScriptOption)
				{
					ScriptOption so = (ScriptOption)row.Tag;

					row.Cells["Value"].Value = so.defaultvalue.ToString();
				}
			}
		}
	}
}
