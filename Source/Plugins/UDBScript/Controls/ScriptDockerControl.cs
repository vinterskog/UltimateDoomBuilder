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
		#region ================== Variables

		private ImageList images;

		#endregion

		#region ================== Properties

		public ImageList Images { get { return images; } }

		#endregion

		#region ================== Constructor

		public ScriptDockerControl(string foldername)
		{
			InitializeComponent();

			images = new ImageList();
			images.Images.Add("Folder", Properties.Resources.Folder);
			images.Images.Add("Script", Properties.Resources.Script);

			filetree.ImageList = images;

			FillTree(foldername);
		}

		#endregion

		#region ================== Methods

		/// <summary>
		/// Starts adding files to the file tree, starting from the "scripts" subfolders
		/// </summary>
		/// <param name="foldername">folder name inside the application directory to use as a base</param>
		private void FillTree(string foldername)
		{
			string path = Path.Combine(General.AppPath, foldername, "scripts");

			filetree.Nodes.AddRange(AddFiles(path));
			filetree.ExpandAll();
		}

		/// <summary>
		/// Adds elements (script files) to the file tree, based on the given path. Subfolders are processed recursively
		/// </summary>
		/// <param name="path">path to start at</param>
		/// <returns>Array of TreeNode</returns>
		private TreeNode[] AddFiles(string path)
		{
			List<TreeNode> newnodes = new List<TreeNode>();

			// Add files (and subfolders) in folders recursively
			foreach (string directory in Directory.GetDirectories(path))
			{
				TreeNode tn = new TreeNode(Path.GetFileName(directory), AddFiles(directory));
				tn.SelectedImageKey = tn.ImageKey = "Folder";

				newnodes.Add(tn);
			}

			// Add files
			foreach (string filename in Directory.GetFiles(path))
			{
				// Only add files with the .js extension. Us the file name as the node name. TODO: use a setting in the .cfg file (if there is one) as a name
				// The file name is stored in the Tag
				if (Path.GetExtension(filename).ToLowerInvariant() == ".js")
				{
					TreeNode tn = new TreeNode(GetScriptName(filename));
					tn.Tag = filename;
					tn.SelectedImageKey = tn.ImageKey = "Script";

					newnodes.Add(tn);
				}
			}

			return newnodes.ToArray();
		}

		/// <summary>
		/// Gets the name of the script file. This is either read from the .cfg file of the script or taken from the file name
		/// </summary>
		/// <param name="filename">Full path with file name of the script</param>
		/// <returns></returns>
		private string GetScriptName(string filename)
		{
			string configfile = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".cfg";

			if (File.Exists(configfile))
			{
				Configuration cfg = new Configuration(configfile, true);
				string name = cfg.ReadSetting("name", string.Empty);

				if (!string.IsNullOrEmpty(name))
					return name;
			}

			return Path.GetFileNameWithoutExtension(filename);
		}

		/// <summary>
		/// Ends editing the currently edited grid view cell. This is required so that the value is applied before running the script if the cell is currently
		/// being editing (i.e. typing in a value, then running the script without clicking somewhere else first)
		/// </summary>
		public void EndEdit()
		{
			scriptoptions.EndEdit();
		}

		/// <summary>
		/// Gets an object with all script options with their values. This can then be easily used to access script options by name in the script
		/// </summary>
		/// <returns>Object containing all script options with their values</returns>
		public ExpandoObject GetScriptOptions()
		{
			// We have to jump through some hoops here to be able to access the elements by name
			ExpandoObject eo = new ExpandoObject();
			var options = eo as IDictionary<string, object>;

			foreach (DataGridViewRow row in scriptoptions.ParametersView.Rows)
			{
				if (row.Tag is ScriptOption)
				{
					ScriptOption so = (ScriptOption)row.Tag;
					options[so.name] = so.typehandler.GetValue();
				}
			}

			return eo;
		}

		#endregion

		#region ================== Events

		/// <summary>
		/// Sets up the the script options control for the currently selected script
		/// </summary>
		/// <param name="sender">the sender</param>
		/// <param name="e">the event</param>
		private void filetree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node.Tag == null)
				return;

			// The Tag contains the file name of the script, so only continue if its set (and not if a folder is selected)
			if (e.Node.Tag is string)
			{
				BuilderPlug.Me.CurrentScriptFile = (string)e.Node.Tag;

				string configfile = Path.Combine(Path.GetDirectoryName(BuilderPlug.Me.CurrentScriptFile), Path.GetFileNameWithoutExtension(BuilderPlug.Me.CurrentScriptFile)) + ".cfg";

				if (File.Exists(configfile))
				{
					Configuration cfg = new Configuration(configfile, true);

					IDictionary options = cfg.ReadSetting("options", new Hashtable());

					scriptoptions.ParametersView.Rows.Clear();

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

						int index = scriptoptions.ParametersView.Rows.Add(); 
						scriptoptions.ParametersView.Rows[index].Tag = so;
						scriptoptions.ParametersView.Rows[index].Cells["Value"].Value = so.value;
						scriptoptions.ParametersView.Rows[index].Cells["Description"].Value = description;
					}

					// Make sure the browse button is shown if the first option has it
					scriptoptions.EndAddingOptions();
				}
				else
				{
					scriptoptions.ParametersView.Rows.Clear();
					scriptoptions.ParametersView.Refresh();
				}
			}
		}

		/// <summary>
		/// Makes sure the edited cell value is valid. Also stores the value in the editor's configuration file so that it is remembered
		/// </summary>
		/// <param name="sender">the sender</param>
		/// <param name="e">the event</param>
		private void parametersview_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex == 0 || scriptoptions.ParametersView.Rows[e.RowIndex].Tag == null)
				return;

			object newvalue = scriptoptions.ParametersView.Rows[e.RowIndex].Cells["Value"].Value;

			ScriptOption so = (ScriptOption)scriptoptions.ParametersView.Rows[e.RowIndex].Tag;

			// If the new value is empty reset it to the default value. Don't fire this event again, though
			if (newvalue == null || string.IsNullOrWhiteSpace(newvalue.ToString()))
			{
				newvalue = so.defaultvalue;
				scriptoptions.ParametersView.CellValueChanged -= parametersview_CellValueChanged;
				scriptoptions.ParametersView.Rows[e.RowIndex].Cells["Value"].Value = newvalue.ToString();
				scriptoptions.ParametersView.CellValueChanged += parametersview_CellValueChanged;
			}

			so.typehandler.SetValue(newvalue);

			so.value = newvalue;
			scriptoptions.ParametersView.Rows[e.RowIndex].Tag = so;

			// Make the text lighter if it's the default value, and store the setting in the config file if it's not the default
			if (so.value.ToString() == so.defaultvalue.ToString())
			{
				scriptoptions.ParametersView.Rows[e.RowIndex].Cells["Value"].Style.ForeColor = SystemColors.GrayText;
				General.Settings.DeletePluginSetting(BuilderPlug.Me.GetScriptPathHash() + "." + so.name);
			}
			else
			{
				scriptoptions.ParametersView.Rows[e.RowIndex].Cells["Value"].Style.ForeColor = SystemColors.WindowText;
				General.Settings.WritePluginSetting(BuilderPlug.Me.GetScriptPathHash() + "." + so.name, so.value);

			}

		}

		/// <summary>
		/// Runs the currently selected script immediately
		/// </summary>
		/// <param name="sender">the sender</param>
		/// <param name="e">the event</param>
		private void btnRunScript_Click(object sender, EventArgs e)
		{
			BuilderPlug.Me.ScriptExecute();
		}

		/// <summary>
		/// Resets all options of the currently selected script to their default values
		/// </summary>
		/// <param name="sender">the sender</param>
		/// <param name="e">the event</param>
		private void btnResetToDefaults_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow row in scriptoptions.ParametersView.Rows)
			{
				if (row.Tag is ScriptOption)
				{
					ScriptOption so = (ScriptOption)row.Tag;

					row.Cells["Value"].Value = so.defaultvalue.ToString();
					so.typehandler.SetValue(so.defaultvalue);

					General.Settings.DeletePluginSetting(BuilderPlug.Me.GetScriptPathHash() + "." + so.name);

					row.Tag = so;
				}
			}
		}

		#endregion
	}
}
