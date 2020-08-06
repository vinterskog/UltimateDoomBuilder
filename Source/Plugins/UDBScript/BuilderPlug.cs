using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Plugins;
using CodeImp.DoomBuilder.UDBScript.Windows;
using Jint;

namespace CodeImp.DoomBuilder.UDBScript
{
	public class BuilderPlug : Plug
	{
		#region ================== Variables

		private static BuilderPlug me;

		#endregion

		#region ================== Properties

		public static BuilderPlug Me { get { return me; } }

		#endregion

		public override void OnInitialize()
		{
			base.OnInitialize();

			me = this;

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

		#region ================== Actions

		[BeginAction("udbscriptexecute")]
		public void ScriptExecute()
		{
			ScriptForm form = new ScriptForm();

			if(form.ShowDialog() == DialogResult.OK)
			{
				List<Assembly> assemblies = General.GetPluginAssemblies();
				assemblies.Add(General.ThisAssembly);

				Engine engine = new Engine(cfg => cfg.AllowClr(assemblies.ToArray()));
				engine.SetValue("log", new Action<object>(Console.WriteLine));
				engine.SetValue("ShowMessage", new Action<string>(ShowMessage));
				engine.SetValue("DrawLines", new Func<object[], bool>(DrawLines));

				engine.Execute(form.ScriptText);
			}

			General.Map.Map.Update();
			General.Map.ThingsFilter.Update();
			General.Interface.RedrawDisplay();
		}

		#endregion
	}
}
