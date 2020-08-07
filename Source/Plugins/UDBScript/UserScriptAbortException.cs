using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeImp.DoomBuilder.UDBScript
{
	[Serializable]
	public class UserScriptAbortException : Exception
	{
		public UserScriptAbortException()
		{
		}
	}
}
