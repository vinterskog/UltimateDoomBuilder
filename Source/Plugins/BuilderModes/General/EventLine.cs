using CodeImp.DoomBuilder.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public class EventLine
	{
		private Vector3D start;
		private Vector3D end;
		private Line3D line;

		public Line3D Line { get { return line; } }

		public EventLine(Vector3D start, Vector3D end)
		{
			this.start = start;
			this.end = end;

			line = new Line3D(start, end);
		}
	}
}
