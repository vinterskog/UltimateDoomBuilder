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

		#region ================== Actions

		[BeginAction("udbscriptexecute")]
		public void ScriptExecute()
		{
			if (string.IsNullOrEmpty(currentscriptfile))
				return;

			ScriptRunner sr = new ScriptRunner(currentscriptfile);
			sr.Run();
		}

		#endregion
	}
}
