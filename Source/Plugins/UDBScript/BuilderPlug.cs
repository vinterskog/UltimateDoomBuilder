using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Plugins;
using Jint;

namespace CodeImp.DoomBuilder.UDBScript
{
	public class BuilderPlug : Plug
	{
		#region ================== Constants

		static private string SCRIPT_FOLDER = "udbscript";

		#endregion

		#region ================== Variables

		private static BuilderPlug me;
		private ScriptDockerControl panel;
		private Docker docker;
		private string currentscriptfile;

		#endregion

		#region ================== Properties

		public static BuilderPlug Me { get { return me; } }
		public string CurrentScriptFile { get { return currentscriptfile; } set { currentscriptfile = value; } }

		#endregion

		public override void OnInitialize()
		{
			base.OnInitialize();

			me = this;

			panel = new ScriptDockerControl(SCRIPT_FOLDER);
			docker = new Docker("udbscript", "Scripts", panel);
			General.Interface.AddDocker(docker);

			General.Actions.BindMethods(this);
		}

		// This is called when the plugin is terminated
		public override void Dispose()
		{
			base.Dispose();

			// This must be called to remove bound methods for actions.
			General.Actions.UnbindMethods(this);
		}

		public void WriteLine(string s)
		{
			Debug.WriteLine(s);
		}

		public void ShowMessage(string s)
		{
			MessageBox.Show(s);
		}

		public bool DrawLines(object[] vertices)
		{
			List<DrawnVertex> dvl = new List<DrawnVertex>();

			for(int i = 0; i < vertices.Length; i++)
			{
				DrawnVertex dv = new DrawnVertex();
				dv.pos = (Vector2D)vertices[i];
				dv.stitch = true;
				dv.stitchline = true;

				dvl.Add(dv);
			}

			return Tools.DrawLines(dvl);
		}

		public static Dictionary<string, object> QueryParameters(object input)
		{
			QueryParametersForm qpf = new QueryParametersForm();

			object[] parameters = input as object[];

			for (int i = 0; i < parameters.Length; i++)
			{
				object[] setting = parameters[i] as object[];

				qpf.AddParameter(setting[0].ToString(), setting[1].ToString(), setting[2]);
			}

			if(qpf.ShowDialog() == DialogResult.OK)
				return qpf.GetParameters();

			throw new UserScriptAbortException("Query parameters dialog was canceled");
		}

		#region ================== Actions

		[BeginAction("udbscriptexecute")]
		public void ScriptExecute()
		{
			bool abort = false;

			if (string.IsNullOrEmpty(currentscriptfile))
				return;

			// Read the current script file
			string script = File.ReadAllText(currentscriptfile);

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

			// Run the script file
			try
			{
				General.Map.UndoRedo.CreateUndo("Run script " + Path.GetFileNameWithoutExtension(currentscriptfile));
				engine.Execute(script);
			}
			catch(UserScriptAbortException e)
			{
				abort = true;
			}
			catch(Esprima.ParserException e)
			{
				MessageBox.Show("There is an error while parsing the script:\n\n" + e.Message, "Script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				abort = true;
			}
			catch(Jint.Runtime.JavaScriptException e)
			{
				MessageBox.Show("There is an error in the script in line " + e.LineNumber + ":\n\n" + e.Message, "Script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				abort = true;
			}

			if(abort)
			{
				General.Map.UndoRedo.WithdrawUndo();
			}

			// Do some updates
			General.Map.Map.Update();
			General.Map.ThingsFilter.Update();
			General.Interface.RedrawDisplay();
		}

		#endregion
	}
}
