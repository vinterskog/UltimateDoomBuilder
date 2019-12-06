using System;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Rendering;
using SlimDX.Direct3D9;

namespace CodeImp.DoomBuilder.VisualModes
{
	public abstract class VisualSlopeHandle : IVisualPickable, ID3DResource, IDisposable
	{
		#region ================== Variables

		// Disposing
		private bool isdisposed;

		// Selected?
		protected bool selected;

		// Pivot?
		protected bool pivot;

		// Smart Pivot?
		protected bool smartpivot;

		// Was changed?
		private bool changed;

		// Geometry
		private WorldVertex[] vertices;
		private VertexBuffer geobuffer;


		#endregion

		#region ================== Properties

		/// <summary>
		/// Selected or not? This is only used by the core to determine what color to draw it with.
		/// </summary>
		public bool Selected { get { return selected; } set { selected = value; } }

		/// <summary>
		/// Pivot or not? This is only used by the core to determine what color to draw it with.
		/// </summary>
		public bool Pivot { get { return pivot; } set { pivot = value; } }

		/// <summary>
		/// Disposed or not?
		/// </summary>
		public bool IsDisposed { get { return isdisposed; } }

		public bool SmartPivot { get { return smartpivot; } set { smartpivot = value; } }

		public bool Changed { get { return changed; } set { changed = value; } }

		public VertexBuffer GeoBuffer { get { return geobuffer; } }

		#endregion

		#region ================== Constructor / Destructor

		public VisualSlopeHandle()
		{
			// Register as resource
			General.Map.Graphics.RegisterResource(this);

			pivot = false;
			smartpivot = false;
		}

		// Disposer
		public virtual void Dispose()
		{
			// Not already disposed?
			if (!isdisposed)
			{
				if (geobuffer != null)
				{
					geobuffer.Dispose();
					geobuffer = null;
				}

				// Unregister resource
				General.Map.Graphics.UnregisterResource(this);

				isdisposed = true;
			}
		}

		#endregion

		#region ================== Methods

		// This is called before a device is reset (when resized or display adapter was changed)
		public void UnloadResource()
		{
			if (geobuffer != null) geobuffer.Dispose();
			geobuffer = null;
		}

		// This is called resets when the device is reset
		// (when resized or display adapter was changed)
		public void ReloadResource()
		{
		}

		/// <summary>
		/// This is called when the thing must be tested for line intersection. This should reject
		/// as fast as possible to rule out all geometry that certainly does not touch the line.
		/// </summary>
		public virtual bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			return true;
		}

		/// <summary>
		/// This is called when the thing must be tested for line intersection. This should perform
		/// accurate hit detection and set u_ray to the position on the ray where this hits the geometry.
		/// </summary>
		public virtual bool PickAccurate(Vector3D from, Vector3D to, Vector3D dir, ref float u_ray)
		{
			return true;
		}

		protected void SetVertices(WorldVertex[] verts)
		{
			if (geobuffer != null) geobuffer.Dispose();

			vertices = verts;
			geobuffer = new VertexBuffer(General.Map.Graphics.Device, WorldVertex.Stride * vertices.Length, Usage.WriteOnly | Usage.Dynamic, VertexFormat.None, Pool.Default);
			geobuffer.Lock(0, WorldVertex.Stride * vertices.Length, LockFlags.None).WriteRange(vertices);
			geobuffer.Unlock();
		}

		public virtual bool Update(PixelColor color)
		{
			return true;
		}

		#endregion
	}
}