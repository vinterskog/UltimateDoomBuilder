using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CodeImp.DoomBuilder.BuilderModes;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;

namespace CodeImp.DoomBuilder.VisualModes
{
	internal class VisualSidedefSlopeHandle : VisualSlopeHandle, IVisualEventReceiver
	{
		#region ================== Variables

		private readonly BaseVisualMode mode;
		private readonly Sidedef sidedef;
		private readonly SectorLevel level;
		private readonly bool up;
		private RectangleF bbox;
		private Vector3D pickintersect;
		private float pickrayu;
		private Plane plane;

		#endregion

		#region ================== Constants

		private const int SIZE = 8;

		#endregion

		#region ================== Properties

		public Sidedef Sidedef { get { return sidedef; } }
		public SectorLevel Level { get { return level; } }
		public int NormalizedAngleDeg { get { return (sidedef.Line.AngleDeg >= 180) ? (sidedef.Line.AngleDeg - 180) : sidedef.Line.AngleDeg; } }

		#endregion

		#region ================== Constructor / Destructor

		public VisualSidedefSlopeHandle(BaseVisualMode mode, SectorLevel level, Sidedef sidedef, bool up) : base()
		{
			this.mode = mode;
			this.sidedef = sidedef;
			this.level = level;
			this.up = up;

			plane = new Plane(level.plane.Normal, level.plane.Offset - 0.1f);

			if (!up)
				plane = plane.GetInverted();

			bbox = CreateBoundingBox();

			// We have no destructor
			GC.SuppressFinalize(this);
		}

		#endregion

		#region ================== Methods

		private RectangleF CreateBoundingBox()
		{
			Line2D l = sidedef.Line.Line;
			float left = l.v1.x;
			float right = l.v1.x;
			float top = l.v1.y;
			float bottom = l.v1.y;

			if (l.v2.x < left) left = l.v2.x;
			if (l.v2.x > right) right = l.v2.x;
			if (l.v2.y > bottom) bottom = l.v2.y;
			if (l.v2.y < top) top = l.v2.y;

			return new RectangleF(left - SIZE, top - SIZE, right - left + SIZE*2, bottom - top + SIZE*2);
		}

		public bool Setup() { return Setup(General.Colors.Vertices); }
		public bool Setup(PixelColor color)
		{
			if (sidedef == null)
				return false;

			plane = new Plane(level.plane.Normal, level.plane.Offset - 0.1f);

			if (!up)
				plane = plane.GetInverted();

			Linedef ld = sidedef.Line;
			SectorData sd = mode.GetSectorData(sidedef.Sector);

			// Make vertices
			WorldVertex[] verts = new WorldVertex[6];
			Vector2D offset = ld.Line.GetPerpendicular().GetNormal()*SIZE * (sidedef.IsFront ? -1 : 1);
			// Line2D line = new Line2D(ld.Line.GetCoordinatesAt(ld.LengthInv * SIZE), ld.Line.GetCoordinatesAt(1.0f - (ld.LengthInv * SIZE)));
			Line2D line = ld.Line;

			Vector3D v1;
			Vector3D v2;
			Vector3D v3;
			Vector3D v4;

			if(sidedef.IsFront)
			{
				
				v1 = line.v1;
				v2 = line.v2;
				v3 = line.v2 + offset;
				v4 = line.v1 + offset;
			}
			else
			{
				v1 = line.v1 + offset;
				v2 = line.v2 + offset;
				v3 = line.v2;
				v4 = line.v1;
			}

			int color1 = color.WithAlpha(255).ToInt();
			int color2 = color.WithAlpha(0).ToInt();

			if (level.type == SectorLevelType.Ceiling)
			{
				v1.z = level.plane.GetZ(v1) - 0.1f;
				v2.z = level.plane.GetZ(v2) - 0.1f;
				v3.z = level.plane.GetZ(v3) - 0.1f;
				v4.z = level.plane.GetZ(v4) - 0.1f;

				verts[0] = new WorldVertex(v3);
				verts[1] = new WorldVertex(v2);
				verts[2] = new WorldVertex(v1);
				verts[3] = new WorldVertex(v4);
				verts[4] = new WorldVertex(v3);
				verts[5] = new WorldVertex(v1);

				int coloron = color1;
				int coloroff = color2;

				if (!sidedef.IsFront)
				{
					coloron = color2;
					coloroff = color1;
				}

				verts[1].c = verts[2].c = verts[5].c = coloron;
				verts[0].c = verts[3].c = verts[4].c = coloroff;
			}
			else
			{
				v1.z = level.plane.GetZ(v1) + 0.1f;
				v2.z = level.plane.GetZ(v2) + 0.1f;
				v3.z = level.plane.GetZ(v3) + 0.1f;
				v4.z = level.plane.GetZ(v4) + 0.1f;

				verts[0] = new WorldVertex(v1);
				verts[1] = new WorldVertex(v2);
				verts[2] = new WorldVertex(v3);
				verts[3] = new WorldVertex(v1);
				verts[4] = new WorldVertex(v3);
				verts[5] = new WorldVertex(v4);

				int coloron = color1;
				int coloroff = color2;

				if(!sidedef.IsFront)
				{
					coloron = color2;
					coloroff = color1;
				}

				verts[0].c = verts[1].c = verts[3].c = coloron;
				verts[2].c = verts[4].c = verts[5].c = coloroff;
			}

			//verts[0] = new WorldVertex(line.v1.x, line.v1.y, 0.1f);
			//verts[1] = new WorldVertex(line.v2.x, line.v2.y, 0.1f);
			//verts[2] = new WorldVertex((line.v2 + offset).x, (line.v2 + offset).y, 0.1f);
			//verts[3] = new WorldVertex((line.v1 + offset).x, (line.v1 + offset).y, 0.1f);

			SetVertices(verts);

			return true;
		}

		public override bool Update(PixelColor color)
		{
			return Setup(color);
		}

		/// <summary>
		/// This is called when the thing must be tested for line intersection. This should reject
		/// as fast as possible to rule out all geometry that certainly does not touch the line.
		/// </summary>
		public override bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			if ((up && plane.Distance(from) > 0.0f) || (!up && plane.Distance(from) < 0.0f))
			{
				if (plane.GetIntersection(from, to, ref pickrayu))
				{
					if (pickrayu > 0.0f)
					{
						pickintersect = from + (to - from) * pickrayu;

						return ((pickintersect.x >= bbox.Left) && (pickintersect.x <= bbox.Right) &&
								(pickintersect.y >= bbox.Top) && (pickintersect.y <= bbox.Bottom));
					}
				}
			}

			return false;
		}

		/// <summary>
		/// This is called when the thing must be tested for line intersection. This should perform
		/// accurate hit detection and set u_ray to the position on the ray where this hits the geometry.
		/// </summary>
		public override bool PickAccurate(Vector3D from, Vector3D to, Vector3D dir, ref float u_ray)
		{
			u_ray = pickrayu;

			Sidedef sd = MapSet.NearestSidedef(sidedef.Sector.Sidedefs, pickintersect);
			if (sd == sidedef) {
				float side = sd.Line.SideOfLine(pickintersect);

				if ((side <= 0.0f && sd.IsFront) || (side > 0.0f && !sd.IsFront))
				{
					if (sidedef.Line.DistanceTo(pickintersect, true) <= SIZE)
						return true;
				}
			}

			return false;
		}

		internal VisualSidedefSlopeHandle GetSmartPivotHandle(VisualSidedefSlopeHandle starthandle)
		{
			VisualSidedefSlopeHandle handle = starthandle;
			List<VisualSidedefSlopeHandle> potentialhandles = new List<VisualSidedefSlopeHandle>();
			int angle = starthandle.sidedef.Line.AngleDeg;
			int anglediff = 180;
			float distance = 0.0f;

			if (angle >= 180) angle -= 180;

			List<IVisualEventReceiver> selectedsectors = mode.GetSelectedObjects(true, false, false, false, false);

			if (selectedsectors.Count == 0)
			{
				foreach (VisualSidedefSlopeHandle checkhandle in mode.AllSlopeHandles[starthandle.Sidedef.Sector])
					if (checkhandle != starthandle && checkhandle.Level == starthandle.Level)
						potentialhandles.Add(checkhandle);
			}
			else
			{
				HashSet<Sector> sectors = new HashSet<Sector>();

				// Debug.WriteLine("\nAll levels:");

				foreach(Sector s in General.Map.Map.Sectors)
				{
					SectorData sd = mode.GetSectorData(s);
					// Debug.WriteLine(sd.Floor.GetHashCode());
					// Debug.WriteLine(sd.Ceiling.GetHashCode());
				}

				// Debug.WriteLine("\nLevels of selected sectors:");

				foreach (BaseVisualGeometrySector bvgs in selectedsectors)
				{
					sectors.Add(bvgs.Sector.Sector);
					// Debug.WriteLine(bvgs.Level.GetHashCode());
				}

				// Debug.WriteLine("\nChecking levels:");

				foreach (Sector s in sectors)
					foreach (VisualSidedefSlopeHandle checkhandle in mode.AllSlopeHandles[s])
						if(checkhandle != starthandle)
							foreach (BaseVisualGeometrySector bvgs in selectedsectors)
							{
								if (bvgs.Level == checkhandle.Level)
								{
									potentialhandles.Add(checkhandle);
									// Debug.WriteLine(checkhandle.Level.GetHashCode() + " <-- OK!");
								}
								//else
								//	Debug.WriteLine(checkhandle.Level.GetHashCode());
							}
			}

			//Debug.WriteLine("\npotential lines:");
			//foreach (VisualSidedefSlopeHandle vssh in potentialhandles)
			//	Debug.WriteLine(vssh.Sidedef.Line);


			foreach (KeyValuePair<Sector, List<VisualSlopeHandle>> kvp in mode.AllSlopeHandles)
			{
				foreach (VisualSidedefSlopeHandle checkhandle in kvp.Value)
					checkhandle.SmartPivot = false;
			}


			List<VisualSidedefSlopeHandle> anglediffsortedhandles = potentialhandles.OrderBy(h => Math.Abs(starthandle.NormalizedAngleDeg - h.NormalizedAngleDeg)).ToList();

			//Debug.WriteLine("\nSorted by angle diff:");

			//foreach (VisualSidedefSlopeHandle vssh in anglediffsortedhandles)
			//	Debug.WriteLine(vssh.Sidedef.Line + " (" + Math.Abs(starthandle.NormalizedAngleDeg - vssh.NormalizedAngleDeg) + ")");


			//Debug.WriteLine("\nSorted by distance:");

			//foreach (VisualSidedefSlopeHandle vssh in anglediffsortedhandles.Where(h => h.NormalizedAngleDeg == anglediffsortedhandles[0].NormalizedAngleDeg).OrderByDescending(h => Math.Abs(Vector2D.Distance(h.Sidedef.Line.GetCenterPoint(), starthandle.sidedef.Line.GetCenterPoint()))))
			//	Debug.WriteLine(vssh.Sidedef.Line + " (" + Math.Abs(Vector2D.Distance(vssh.Sidedef.Line.GetCenterPoint(), starthandle.sidedef.Line.GetCenterPoint())) + ")");

			if (anglediffsortedhandles.Count > 0)
			{
				// handle = anglediffsortedhandles.Where(h => h.NormalizedAngleDeg == anglediffsortedhandles[0].NormalizedAngleDeg).OrderByDescending(h => Math.Abs(Vector2D.Distance(h.Sidedef.Line.GetCenterPoint(), starthandle.sidedef.Line.GetCenterPoint()))).First();
				handle = anglediffsortedhandles.Where(h => h.NormalizedAngleDeg == anglediffsortedhandles[0].NormalizedAngleDeg).OrderByDescending(h => Math.Abs(starthandle.Sidedef.Line.Line.GetDistanceToLine(h.sidedef.Line.GetCenterPoint(), false))).First();
			}

			Debug.WriteLine("\nDecided on " + handle.Sidedef.Line + "(" + handle.Level.type + ")");

			/*
			foreach (VisualSidedefSlopeHandle checkhandle in potentialhandles)
			{
				checkhandle.SmartPivot = false;

				if (checkhandle == starthandle) continue;

				int checkangle = checkhandle.Sidedef.Line.AngleDeg;
				if (checkangle >= 180) checkangle -= 180;

				int checkanglediff = Math.Abs(angle - checkangle);
				if (checkanglediff <= anglediff)
				{
					// Compute distance between starthandle and checkhandle
					if (handle != null)
					{
						float checkdistance = Math.Abs(Vector2D.Distance(handle.Sidedef.Line.GetCenterPoint(), checkhandle.Sidedef.Line.GetCenterPoint()));

						if (checkdistance > distance)
						{
							anglediff = checkanglediff;
							handle = checkhandle;
							distance = checkdistance;
						}
					}
					else
					{
						anglediff = checkanglediff;
						handle = checkhandle;
						distance = Math.Abs(Vector2D.Distance(handle.Sidedef.Line.GetCenterPoint(), checkhandle.Sidedef.Line.GetCenterPoint()));
					}
				}
			}
			*/

			if (handle == starthandle)
				return null;

			if(handle != null)
				handle.SmartPivot = true;

			return handle;
		}

		#endregion

		#region ================== Events

		public void OnChangeTargetHeight(int amount)
		{
			VisualSlopeHandle pivothandle = null;
			List<IVisualEventReceiver> selectedsectors = mode.GetSelectedObjects(true, false, false, false, false);
			List<SectorLevel> levels = new List<SectorLevel>();

			if (selectedsectors.Count == 0)
				levels.Add(level);
			else
				foreach (BaseVisualGeometrySector bvgs in selectedsectors)
					levels.Add(bvgs.Level);


			foreach (KeyValuePair<Sector, List<VisualSlopeHandle>> kvp in mode.AllSlopeHandles)
			{
				foreach (VisualSidedefSlopeHandle handle in kvp.Value)
				{
					if (handle.Pivot)
					{
						pivothandle = handle;
						break;
					}
				}
			}

			if(pivothandle == null)
			{
				pivothandle = GetSmartPivotHandle(this);
			}

			if (pivothandle == null)
				return;

			mode.CreateUndo("Change slope");

			SectorData sd = mode.GetSectorData(sidedef.Sector);
			SectorData sdpivot = mode.GetSectorData(level.sector);

			Plane originalplane = level.plane;
			Plane pivotplane = ((VisualSidedefSlopeHandle)pivothandle).Level.plane;

			Vector3D p1 = new Vector3D(sidedef.Line.Start.Position, (float)Math.Round(originalplane.GetZ(sidedef.Line.Start.Position)));
			Vector3D p2 = new Vector3D(sidedef.Line.End.Position, (float)Math.Round(originalplane.GetZ(sidedef.Line.End.Position)));
			Vector3D p3 = new Vector3D(((VisualSidedefSlopeHandle)pivothandle).Sidedef.Line.Line.GetCoordinatesAt(0.5f), (float)Math.Round(pivotplane.GetZ(((VisualSidedefSlopeHandle)pivothandle).Sidedef.Line.Line.GetCoordinatesAt(0.5f))));

			p1 += new Vector3D(0f, 0f, amount);
			p2 += new Vector3D(0f, 0f, amount);

			Plane plane = new Plane(p1, p2, p3, true);


			foreach (SectorLevel l in levels)
			{
				if (up)
				{
					l.sector.FloorSlope = plane.Normal;
					l.sector.FloorSlopeOffset = plane.Offset;

					Vector2D center = new Vector2D(l.sector.BBox.X + l.sector.BBox.Width / 2,
													   l.sector.BBox.Y + l.sector.BBox.Height / 2);

					l.sector.FloorHeight = (int)new Plane(l.sector.FloorSlope, l.sector.FloorSlopeOffset).GetZ(center);
				}
				else
				{
					plane = plane.GetInverted();
					l.sector.CeilSlope = plane.Normal;
					l.sector.CeilSlopeOffset = plane.Offset;

					Vector2D center = new Vector2D(l.sector.BBox.X + l.sector.BBox.Width / 2,
													   l.sector.BBox.Y + l.sector.BBox.Height / 2);

					l.sector.CeilHeight = (int)new Plane(l.sector.CeilSlope, l.sector.CeilSlopeOffset).GetZ(center);
				}

				// Rebuild sector
				BaseVisualSector vs;
				if (mode.VisualSectorExists(l.sector))
				{
					vs = (BaseVisualSector)mode.GetVisualSector(l.sector);
				}
				else
				{
					vs = mode.CreateBaseVisualSector(l.sector);
				}

				if (vs != null) vs.UpdateSectorGeometry(true);
			}

			mode.SetActionResult("Changed slope.");
		}

		// Select or deselect
		public void OnSelectEnd()
		{
			if (this.selected)
			{
				this.selected = false;
				mode.RemoveSelectedObject(this);
			}
			else
			{
				this.selected = true;
				mode.AddSelectedObject(this);
			}
		}

		// Return texture name
		public string GetTextureName() { return ""; }

		// Unused
		public void OnSelectBegin() { }
		public void OnEditBegin() { }
		public void OnChangeTargetBrightness(bool up) { }
		public void OnChangeTextureOffset(int horizontal, int vertical, bool doSurfaceAngleCorrection) { }
		public void OnSelectTexture() { }
		public void OnCopyTexture() { }
		public void OnPasteTexture() { }
		public void OnCopyTextureOffsets() { }
		public void OnPasteTextureOffsets() { }
		public void OnTextureAlign(bool alignx, bool aligny) { }
		public void OnToggleUpperUnpegged() { }
		public void OnToggleLowerUnpegged() { }
		public void OnProcess(long deltatime) { }
		public void OnTextureFloodfill() { }
		public void OnInsert() { }
		public void OnTextureFit(FitTextureOptions options) { } //mxd
		public void ApplyTexture(string texture) { }
		public void ApplyUpperUnpegged(bool set) { }
		public void ApplyLowerUnpegged(bool set) { }
		public void SelectNeighbours(bool select, bool withSameTexture, bool withSameHeight) { } //mxd
		public virtual void OnPaintSelectEnd() { } // biwa
		public void OnEditEnd() { }
		public void OnChangeScale(int x, int y) { }
		public void OnResetTextureOffset() { }
		public void OnResetLocalTextureOffset() { }
		public void OnCopyProperties() { }
		public void OnPasteProperties(bool usecopysetting) { }
		public void OnDelete() { }
		public void OnPaintSelectBegin() { }
		public void OnMouseMove(MouseEventArgs e) { }

		#endregion
	}
}