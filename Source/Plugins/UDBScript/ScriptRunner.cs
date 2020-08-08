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
		private string scriptfile;

		public ScriptRunner(string scriptfile)
		{
			this.scriptfile = scriptfile;
		}

		public void ShowMessage(string s)
		{
			MessageBox.Show(s);
		}

		public bool DrawLines(object[] vertices)
		{
			List<DrawnVertex> dvl = new List<DrawnVertex>();

			for (int i = 0; i < vertices.Length; i++)
			{
				DrawnVertex dv = new DrawnVertex();
				dv.pos = (Vector2D)vertices[i];
				dv.stitch = true;
				dv.stitchline = true;

				dvl.Add(dv);
			}

			return Tools.DrawLines(dvl);
		}

		public Dictionary<string, object> QueryParameters(object input)
		{
			QueryParametersForm qpf = new QueryParametersForm();

			object[] parameters = input as object[];

			for (int i = 0; i < parameters.Length; i++)
			{
				object[] setting = parameters[i] as object[];

				qpf.AddParameter(setting[0].ToString(), setting[1].ToString(), setting[2]);
			}

			if (qpf.ShowDialog() == DialogResult.OK)
				return qpf.GetParameters();

			throw new UserScriptAbortException("Query parameters dialog was canceled");
		}

		public void Test(IList<DrawnVertex> input)
		{
			int x = 0;
			Type t = input.GetType();
			Tools.DrawLines(input);
		}

		private string GetLibraryCode()
		{
			string code = "";

			string path = Path.Combine(General.AppPath, "UDBScript", "Libraries");
			string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

			foreach(string file in files)
				code += File.ReadAllText(file);

			return code;
		}

		public void Run()
		{
			bool abort = false;

			// Read the current script file
			string script = File.ReadAllText(scriptfile);

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
			engine.SetValue("DrawLines", new Func<object[], bool>(DrawLines));
			engine.SetValue("QueryParameters", new Func<object, Dictionary<string, object>>(QueryParameters));
			engine.SetValue("Test", new Action<List<DrawnVertex>>(Test));

			engine.Execute(GetLibraryCode());

			// Run the script file
			try
			{
				General.Map.UndoRedo.CreateUndo("Run script " + Path.GetFileNameWithoutExtension(scriptfile));
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
				MessageBox.Show("There is an error in the script in line " + e.LineNumber + ":\n\n" + e.Message, "Script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
		}
	}
}
