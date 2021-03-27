#region ================== Copyright (c) 2021 Boris Iwanski

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

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;

#endregion

namespace CodeImp.DoomBuilder.UDBScript.Wrapper
{
	internal class MapWrapper
	{
		#region ================== Variables

		#endregion

		#region ================== Properties

		/// <summary>
		/// `true` if the map is in Doom format, `false` if it isn't. Read-only.
		/// </summary>
		public bool isDoom { get { return General.Map.DOOM; } }

		/// <summary>
		/// `true` if the map is in Hexen format, `false` if it isn't. Read-only.
		/// </summary>
		public bool isHexen { get { return General.Map.HEXEN; } }

		/// <summary>
		/// `true` if the map is in UDMF, `false` if it isn't. Read-only.
		/// </summary>
		public bool isUDMF { get { return General.Map.UDMF; } }

		#endregion

		#region ================== Constructors

		public MapWrapper()
		{
		}

		#endregion

		#region ================== Methods

		/// <summary>
		/// Returns an array of all things in the map
		/// </summary>
		/// <returns></returns>
		public ThingWrapper[] getThings()
		{
			List<ThingWrapper> things = new List<ThingWrapper>(General.Map.Map.Things.Count);

			foreach (Thing t in General.Map.Map.Things)
				if(!t.IsDisposed)
					things.Add(new ThingWrapper(t));

			return things.ToArray();
		}

		public SectorWrapper[] getSectors()
		{
			List<SectorWrapper> sectors = new List<SectorWrapper>(General.Map.Map.Sectors.Count);

			foreach (Sector s in General.Map.Map.Sectors)
				if (!s.IsDisposed)
					sectors.Add(new SectorWrapper(s));

			return sectors.ToArray();
		}

		/// <summary>
		/// Returns an array of all sidedefs in the map
		/// </summary>
		/// <returns></returns>
		public SidedefWrapper[] getSidedefs()
		{
			List<SidedefWrapper> sidedefs = new List<SidedefWrapper>(General.Map.Map.Sidedefs.Count);

			foreach (Sidedef sd in General.Map.Map.Sidedefs)
				if (!sd.IsDisposed)
					sidedefs.Add(new SidedefWrapper(sd));

			return sidedefs.ToArray();
		}

		public LinedefWrapper[] getLinedefs()
		{
			List<LinedefWrapper> linedefs = new List<LinedefWrapper>(General.Map.Map.Linedefs.Count);

			foreach (Linedef ld in General.Map.Map.Linedefs)
				if (!ld.IsDisposed)
					linedefs.Add(new LinedefWrapper(ld));

			return linedefs.ToArray();
		}

		public VertexWrapper createVertex(object pos)
		{
			try
			{
				Vector2D v = (Vector2D)MapElementWrapper.GetVectorFromObject(pos, false);
				Vertex newvertex = General.Map.Map.CreateVertex(v);

				if(newvertex == null)
					throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException("Failed to create new vertex");

				return new VertexWrapper(newvertex);
			}
			catch (CantConvertToVectorException e)
			{
				throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException(e.Message);
			}
		}

		public ThingWrapper createThing(object pos, int type=0)
		{
			try
			{
				if(type < 0)
					throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException("Thing type can not be negative.");

				object v = MapElementWrapper.GetVectorFromObject(pos, true);
				Thing t = General.Map.Map.CreateThing();

				if(t == null)
					throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException("Failed to create new thing.");

				General.Settings.ApplyDefaultThingSettings(t);

				if (type > 0)
					t.Type = type;

				if(v is Vector2D)
					t.Move((Vector2D)v);
				else if(v is Vector3D)
					t.Move((Vector3D)v);

				t.UpdateConfiguration();

				return new ThingWrapper(t);
			}
			catch (CantConvertToVectorException e)
			{
				throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException(e.Message);
			}
		}

		#endregion
	}
}
