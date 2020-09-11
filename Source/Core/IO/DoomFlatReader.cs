
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
using System.IO;
using System.Drawing;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Rendering;
using System.Drawing.Imaging;

#endregion

namespace CodeImp.DoomBuilder.IO
{
	internal unsafe class DoomFlatReader : IImageReader
	{
		#region ================== Variables

		// Palette to use
		private readonly Playpal palette;

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public DoomFlatReader(Playpal palette)
		{
			// Initialize
			this.palette = palette;

			// We have no destructor
			GC.SuppressFinalize(this);
		}

		#endregion

		#region ================== Methods

		// This validates the data as doom flat
		public bool Validate(Stream stream)
		{
			// Check if the flat is square
			float sqrlength = (float)Math.Sqrt(stream.Length);
			if(sqrlength == (float)Math.Truncate(sqrlength))
			{
				// Success when not 0
				return ((int)sqrlength > 0);
			}
			// Valid if the data is more than 4096
			return stream.Length > 4096;
		}

		// This creates a Bitmap from the given data
		// Returns null on failure
		public PixelData ReadAsBitmap(Stream stream, out int offsetx, out int offsety)
		{
			int width, height;
			PixelColor[] pixeldata = ReadAsPixelData(stream, out width, out height, out offsetx, out offsety);
			if (pixeldata != null)
				return new PixelData(width, height, pixeldata);
			else
				return null;
		}

		// This creates pixel color data from the given data
		// Returns null on failure
		private PixelColor[] ReadAsPixelData(Stream stream, out int width, out int height, out int offsetx, out int offsety)
		{
			offsetx = int.MinValue;
			offsety = int.MinValue;

			// Check if the flat is square
			float sqrlength = (float)Math.Sqrt(stream.Length);
			if(sqrlength == (float)Math.Truncate(sqrlength))
			{
				// Calculate image size
				width = (int)sqrlength;
				height = (int)sqrlength;
			}
			// Check if the data is more than 4096
			else if(stream.Length > 4096)
			{
				// Image will be 64x64
				width = 64;
				height = 64;
			}
			else
			{
				// Invalid
				width = 0;
				height = 0;
				return null;
			}

			#if !DEBUG
			try
			{
			#endif
			
			// Valid width and height?
			if((width <= 0) || (height <= 0)) return null;

			// Allocate memory
			PixelColor[] pixeldata = new PixelColor[width * height];

			// Read flat bytes from stream
			byte[] bytes = new byte[width * height];
			stream.Read(bytes, 0, width * height);

			// Convert bytes with palette
			for(uint i = 0; i < width * height; i++) pixeldata[i] = palette[bytes[i]];

			// Return pointer
			return pixeldata;
			
			#if !DEBUG
			}
			catch(Exception)
			{
				// Return nothing
				return null;
			}
			#endif
		}
		
		#endregion

	}
}
