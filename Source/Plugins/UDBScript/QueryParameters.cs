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

using System.Diagnostics;
using System.Dynamic;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.UDBScript
{
	class QueryParameters
	{
		#region ================== Variables

		private Stopwatch stopwatch;
		private QueryParametersForm form;

		#endregion

		#region ================== Properties

		public ExpandoObject Parameters { get { return form.GetScriptOptions(); } }

		#endregion

		#region ================== Constructor

		public QueryParameters(Stopwatch stopwatch)
		{
			this.stopwatch = stopwatch;
			form = new QueryParametersForm(stopwatch);
		}

		#endregion

		#region ================== Methods

		/// <summary>
		/// Adds a parameter to query
		/// </summary>
		/// <param name="name">Name of the variable that the queried value is stored in</param>
		/// <param name="description">Textual description of the parameter</param>
		/// <param name="type">UniversalType value of the parameter</param>
		/// <param name="defaultvalue">Default value of the parameter</param>
		public void AddParameter(string name, string description, int type, object defaultvalue)
		{
			form.AddParameter(name, description, type, defaultvalue);
		}

		/// <summary>
		/// Removes all parameters
		/// </summary>
		public void Clear()
		{
			form.Clear();
		}

		/// <summary>
		/// Queries all parameters
		/// </summary>
		/// <returns>True if OK was pressed, otherwise false</returns>
		public bool Query()
		{
			// Stop the timer so that the time spent in the dialog is not added to the script runtime constraint
			stopwatch.Stop();

			DialogResult dr = form.ShowDialog();

			// Start the timer again
			stopwatch.Start();

			return dr == DialogResult.OK;
		}

		#endregion
	}
}
