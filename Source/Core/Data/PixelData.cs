using CodeImp.DoomBuilder.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CodeImp.DoomBuilder.Data
{
    public class PixelData
    {
		public PixelData(int width, int height)
        {
			Width = width;
			Height = height;
			Data = new PixelColor[width * height];
		}

		public PixelData(int width, int height, PixelColor[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public PixelColor[] Data { get; private set; }

		public PixelColor GetPixel(int x, int y)
        {
			return Data[x + y * Width];
        }

		public PixelData Clone()
        {
			return new PixelData(Width, Height, (PixelColor[])Data.Clone());
        }

		unsafe public Bitmap CreateBitmap()
        {
			var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
			BitmapData bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Size.Width, bitmap.Size.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			PixelColor* pixels = (PixelColor*)bmpdata.Scan0.ToPointer();
			for (int i = 0; i < Data.Length; i++)
				pixels[i] = Data[i];

			bitmap.UnlockBits(bmpdata);
			return bitmap;
        }

		unsafe public static PixelData FromBitmap(Bitmap bitmap)
        {
			if (bitmap == null)
				return null;

			if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
			{
				Bitmap oldbitmap = bitmap;
				using (bitmap = new Bitmap(oldbitmap.Size.Width, oldbitmap.Size.Height, PixelFormat.Format32bppArgb))
				using (Graphics g = Graphics.FromImage(bitmap))
				{
					g.PageUnit = GraphicsUnit.Pixel;
					g.CompositingQuality = CompositingQuality.HighQuality;
					g.InterpolationMode = InterpolationMode.NearestNeighbor;
					g.SmoothingMode = SmoothingMode.None;
					g.PixelOffsetMode = PixelOffsetMode.None;
					g.Clear(Color.Transparent);
					g.DrawImage(oldbitmap, 0, 0, oldbitmap.Size.Width, oldbitmap.Size.Height);
					return FromBitmap(bitmap);
				}
			}
			else
            {
				var data = new PixelColor[bitmap.Size.Width * bitmap.Size.Height];
				BitmapData bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Size.Width, bitmap.Size.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				// maybe this could be done using Marshal.Copy somehow?
				PixelColor* pixels = (PixelColor*)bmpdata.Scan0.ToPointer();
				for (int i = 0; i < data.Length; i++)
					data[i] = pixels[i];

				bitmap.UnlockBits(bmpdata);
				return new PixelData(bitmap.Size.Width, bitmap.Size.Height, data);
			}
		}
	}
}
