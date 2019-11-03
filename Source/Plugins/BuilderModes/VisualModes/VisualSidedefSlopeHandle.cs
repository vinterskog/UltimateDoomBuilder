using System;
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

		#endregion

		#region ================== Properties

		public Sidedef Sidedef { get { return sidedef; } }

		#endregion

		#region ================== Constructor / Destructor

		public VisualSidedefSlopeHandle(BaseVisualMode mode, Sidedef sidedef) : base()
		{
			this.mode = mode;
			this.sidedef = sidedef;

			// We have no destructor
			GC.SuppressFinalize(this);
		}

		#endregion

		#region ================== Methods

		public bool Setup()
		{
			if (sidedef == null)
				return false;

			Linedef ld = sidedef.Line;
			SectorData sd = mode.GetSectorData(sidedef.Sector);

			// Make vertices
			WorldVertex[] verts = new WorldVertex[6];
			Vector2D offset = ld.Line.GetPerpendicular().GetNormal()*16 * (sidedef.IsFront ? -1 : 1);
			Line2D line = new Line2D(ld.Line.GetCoordinatesAt(ld.LengthInv * 16), ld.Line.GetCoordinatesAt(1.0f - (ld.LengthInv * 16)));

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

			v1.z = sd.Floor.plane.GetZ(v1) + 0.1f;
			v2.z = sd.Floor.plane.GetZ(v2) + 0.1f;
			v3.z = sd.Floor.plane.GetZ(v3) + 0.1f;
			v4.z = sd.Floor.plane.GetZ(v4) + 0.1f;

			verts[0] = new WorldVertex(v1);
			verts[1] = new WorldVertex(v2);
			verts[2] = new WorldVertex(v3);
			verts[3] = new WorldVertex(v1);
			verts[4] = new WorldVertex(v3);
			verts[5] = new WorldVertex(v4);

			//verts[0] = new WorldVertex(line.v1.x, line.v1.y, 0.1f);
			//verts[1] = new WorldVertex(line.v2.x, line.v2.y, 0.1f);
			//verts[2] = new WorldVertex((line.v2 + offset).x, (line.v2 + offset).y, 0.1f);
			//verts[3] = new WorldVertex((line.v1 + offset).x, (line.v1 + offset).y, 0.1f);

			SetVertices(verts);

			return true;
		}

		public override bool Update()
		{
			return Setup();
		}

		/// <summary>
		/// This is called when the thing must be tested for line intersection. This should reject
		/// as fast as possible to rule out all geometry that certainly does not touch the line.
		/// </summary>
		public override bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			return true;
		}

		/// <summary>
		/// This is called when the thing must be tested for line intersection. This should perform
		/// accurate hit detection and set u_ray to the position on the ray where this hits the geometry.
		/// </summary>
		public override bool PickAccurate(Vector3D from, Vector3D to, Vector3D dir, ref float u_ray)
		{
			float pickrayu = 0.0f;
			Vector3D pickintersect;

			SectorLevel level = mode.GetSectorData(sidedef.Sector).Floor;

			if (level.plane.Distance(from) > 0.0f && level.plane.GetIntersection(from, to, ref pickrayu))
			{
				if (pickrayu > 0.0f)
				{
					pickintersect = from + (to - from) * pickrayu;

					u_ray = pickrayu - 0.001f;

					Sidedef sd = MapSet.NearestSidedef(level.sector.Sidedefs, pickintersect);
					if (sd == sidedef) {
						float side = sd.Line.SideOfLine(pickintersect);

						if ((side <= 0.0f && sd.IsFront) || (side > 0.0f && !sd.IsFront))
						{
							if (sidedef.Line.DistanceTo(pickintersect, true) <= 16.0f)
								return true;
						}
					}
				}
			}

			return false;
		}

		#endregion

		#region ================== Events

		public void OnChangeTargetHeight(int amount)
		{
			VisualSlopeHandle pivothandle = null;

			foreach(VisualSlopeHandle handle in mode.VisualSlopeHandles)
			{
				if(handle.Pivot)
				{
					pivothandle = handle;
					break;
				}
			}

			if (pivothandle == null)
				return;

			SectorData sd = mode.GetSectorData(sidedef.Sector);
			SectorData sdpivot = mode.GetSectorData(((VisualSidedefSlopeHandle)pivothandle).sidedef.Sector);

			Plane originalplane = sd.Floor.plane;
			Plane pivotplane = sdpivot.Floor.plane;

			Vector3D p1 = new Vector3D(sidedef.Line.Start.Position, originalplane.GetZ(sidedef.Line.Start.Position));
			Vector3D p2 = new Vector3D(sidedef.Line.End.Position, originalplane.GetZ(sidedef.Line.End.Position));
			Vector3D p3 = new Vector3D(((VisualSidedefSlopeHandle)pivothandle).Sidedef.Line.Line.GetCoordinatesAt(0.5f), pivotplane.GetZ(((VisualSidedefSlopeHandle)pivothandle).Sidedef.Line.Line.GetCoordinatesAt(0.5f)));

			p1 += new Vector3D(0f, 0f, amount);
			p2 += new Vector3D(0f, 0f, amount);

			Plane plane = new Plane(p1, p2, p3, true);

			sd.Sector.FloorSlope = plane.Normal;
			sd.Sector.FloorSlopeOffset = plane.Offset;

			// Rebuild sector
			SectorLevel level = mode.GetSectorData(sidedef.Sector).Floor;
			BaseVisualSector vs;
			if (mode.VisualSectorExists(level.sector))
			{
				vs = (BaseVisualSector)mode.GetVisualSector(level.sector);
			}
			else
			{
				//mxd. Need this to apply changes to 3d-floor even if control sector doesn't exist as BaseVisualSector
				vs = mode.CreateBaseVisualSector(level.sector);
			}

			if (vs != null) vs.UpdateSectorGeometry(true);
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