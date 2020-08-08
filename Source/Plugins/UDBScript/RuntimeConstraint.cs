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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeImp.DoomBuilder;
using Jint;

namespace CodeImp.DoomBuilder.UDBScript
{
	class RuntimeConstraint : IConstraint
	{
		private static long CHECK_SECONDS = 5;

		private long checktime;
		private long nextchecktime;

		public RuntimeConstraint()
		{
			checktime = 0;
		}

		public void Reset()
		{
		}

		public void Check()
		{
			if (checktime == 0 || Clock.CurrentTime < checktime)
			{
				checktime = Clock.CurrentTime;
				nextchecktime = checktime + CHECK_SECONDS * 1000;
			}
			else if (Clock.CurrentTime > nextchecktime)
			{
				DialogResult result = MessageBox.Show("The script has been running for some time, want to stop it?", "Script", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

				if (result == DialogResult.Yes)
					throw new UserScriptAbortException();
				else
				{
					checktime = Clock.CurrentTime;
					nextchecktime = checktime + CHECK_SECONDS * 1000;
				}
			}
		}
	}
}
