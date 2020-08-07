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
