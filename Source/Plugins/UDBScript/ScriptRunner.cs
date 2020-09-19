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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Geometry;
using Jint;

namespace CodeImp.DoomBuilder.UDBScript
{
	class ScriptRunner
	{
		#region ================== Variables

		private string scriptfile;

		#endregion

		#region ================== Constructor

		public ScriptRunner(string scriptfile)
		{
			this.scriptfile = scriptfile;
		}

		#endregion

		#region ================== Methods

		public void ShowMessage(string s)
		{
			MessageBox.Show(s);
		}

		public Dictionary<string, object> QueryParameters(object input)
		{
			QueryParametersForm qpf = new QueryParametersForm();

			object[] parameters = input as object[];

			for (int i = 0; i < parameters.Length; i++)
			{
				object[] setting = parameters[i] as object[];

				//qpf.AddParameter(setting[0].ToString(), setting[1].ToString(), setting[2]);
			}

			if (qpf.ShowDialog() == DialogResult.OK)
				return qpf.GetParameters();

			throw new UserScriptAbortException("Query parameters dialog was canceled");
		}

		/// <summary>
		/// Imports the code of all script library files in a single string
		/// </summary>
		/// <param name="engine">Scripting engine to load the code into</param>
		/// <param name="errortext">Errors that occured while loading the library code</param>
		/// <returns>true if there were no errors, false if there were errors</returns>
		private bool ImportLibraryCode(Engine engine, out string errortext)
		{
			bool success = true;
			string path = Path.Combine(General.AppPath, "UDBScript", "Libraries");
			string[] files = Directory.GetFiles(path, "*.js", SearchOption.AllDirectories);

			errortext = string.Empty;

			foreach (string file in files)
			{
				try
				{
					engine.Execute(File.ReadAllText(file));
				}
				catch (Esprima.ParserException e)
				{
					if (!string.IsNullOrEmpty(errortext))
						errortext += "\n----------\n";

					errortext += file + ": " + e.Message;

					success = false;
				}
				catch (Jint.Runtime.JavaScriptException e)
				{
					if (!string.IsNullOrEmpty(errortext))
						errortext += "\n----------\n";

					errortext +=  file + " in line " + e.LineNumber + ": " + e.Message;

					success = false;
				}
			}

			return success;
		}

		/// <summary>
		/// Runs the script
		/// </summary>
		public void Run()
		{
			string importlibraryerrors;
			bool abort = false;

			// Read the current script file
			string script = File.ReadAllText(scriptfile);

			// Make sure the option value gets saved if an option is currently being edited
			BuilderPlug.Me.EndOptionEdit();
			General.Interface.Focus();

			// Get the script assemblies (and the one from Builder) to make them available to the script
			List<Assembly> assemblies = General.GetPluginAssemblies();
			assemblies.Add(General.ThisAssembly);

			// Create the script engine
			Engine engine = new Engine(cfg => {
				cfg.AllowClr(assemblies.ToArray());
				cfg.Constraint(new RuntimeConstraint());
			});
			engine.SetValue("log", new Action<object>(Console.WriteLine));
			engine.SetValue("ShowMessage", new Action<string>(ShowMessage));
			//engine.SetValue("QueryParameters", new Func<object, Dictionary<string, object>>(QueryParameters));
			engine.SetValue("QueryParameters", new QueryParametersForm());
			engine.SetValue("ScriptOptions", BuilderPlug.Me.GetScriptOptions());

			// We'll always need to import the UDB namespace anyway, so do it here instead in every single script
			engine.Execute("var UDB = importNamespace('CodeImp.DoomBuilder');");

			// Import all library files into the current engine
			if(ImportLibraryCode(engine, out importlibraryerrors) == false)
			{
				MessageBox.Show("There were one or more errors while loading the library files. Aborting.\n\n" + importlibraryerrors, "Script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Tell the mode that a script is about to be run
			General.Editing.Mode.OnScriptRunBegin();

			// Run the script file
			try
			{
				General.Map.UndoRedo.CreateUndo("Run script " + BuilderPlug.GetScriptName(scriptfile));
				engine.Execute(script);
			}
			catch (UserScriptAbortException e)
			{
				abort = true;
			}
			catch (Esprima.ParserException e)
			{
				MessageBox.Show("There is an error while parsing the script:\n\n" + e.Message, "Script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				abort = true;
			}
			catch (Jint.Runtime.JavaScriptException e)
			{
				if (e.Error.Type != Jint.Runtime.Types.String)
					MessageBox.Show("There is an error in the script in line " + e.LineNumber + ":\n\n" + e.Message, "Script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				else
					General.Interface.DisplayStatus(Windows.StatusType.Warning, e.Message);

				abort = true;
			}

			if (abort)
			{
				General.Map.UndoRedo.WithdrawUndo();
			}

			// Do some updates
			General.Map.Map.Update();
			General.Map.ThingsFilter.Update();
			General.Interface.RedrawDisplay();

			// Tell the mode that running the script ended
			General.Editing.Mode.OnScriptRunEnd();
		}

		#endregion
	}
}
