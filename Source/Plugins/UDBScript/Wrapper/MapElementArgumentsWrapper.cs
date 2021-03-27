using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Map;

namespace CodeImp.DoomBuilder.UDBScript.Wrapper
{
	public sealed class MapElementArgumentsWrapper : IEnumerable<int>
	{
		private MapElement element;

		public MapElementArgumentsWrapper(MapElement element)
		{
			this.element = element;
		}

		/*
		public MapElementArgumentsWrapper(int numargs, IEnumerable<int> newdata) : this(numargs)
		{
			int i = 0;

			foreach (int d in newdata)
			{
				if (i < numargs)
					data[i] = d;

				i++;
			}
		}
		*/

		public int this[int i]
		{
			get
			{
				if (element is Thing) return ((Thing)element).Args[i];
				else if (element is Linedef) return ((Linedef)element).Args[i];
				else return 0;
			}
			set
			{
				if (element is Thing) ((Thing)element).Args[i] = value;
				else if (element is Linedef) ((Linedef)element).Args[i] = value;
			}
		}

		public int length
		{
			get
			{
				if (element is Thing) return ((Thing)element).Args.Length;
				else if (element is Linedef) return ((Linedef)element).Args.Length;
				else return 0;
			}
		}

		public IEnumerator<int> GetEnumerator()
		{
			if(element is Thing)
			{
				foreach (int i in ((Thing)element).Args)
					yield return ((Thing)element).Args[i];
			}
			else if (element is Linedef)
			{
				foreach (int i in ((Linedef)element).Args)
					yield return ((Linedef)element).Args[i];
			}

			yield return 0;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public MapElementArgumentsWrapper Clone()
		{
			return new MapElementArgumentsWrapper(element);
		}
	}
}
