
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Reflection;
using System.Drawing;
using System.IO;

#endregion

namespace CodeImp.DoomBuilder.Data
{
	public class ResourceImage : ImageData
	{
		#region ================== Variables

		// Image source
		private readonly Assembly assembly;
		private readonly string resourcename;
		PixelData pixels;

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public ResourceImage(string resourcename)
		{
			// Initialize
			this.assembly = Assembly.GetCallingAssembly();
			this.resourcename = resourcename;
			this.AllowUnload = false;
			SetName(resourcename);

			// Temporarily load resource from memory
			using (Stream bitmapdata = assembly.GetManifestResourceStream(resourcename))
			using (Bitmap bmp = (Bitmap)Image.FromStream(bitmapdata))
			{
				pixels = PixelData.FromBitmap(bmp);
			}

			// Get width and height from image
			width = pixels.Width;
			height = pixels.Height;
			scale.x = 1.0f;
			scale.y = 1.0f;

            LoadImageNow();
        }

		#endregion

		#region ================== Methods

		// This loads the image
		protected override LocalLoadResult LocalLoadImage()
		{
			// No failure checking here. If anything fails here, it is not the user's fault,
			// because the resources this loads are in the assembly.
            return new LocalLoadResult(pixels.Clone());
        }

        //mxd
        public override Bitmap GetPreview() 
		{
            return pixels.CreateBitmap();
		}
		
		#endregion
	}
}
