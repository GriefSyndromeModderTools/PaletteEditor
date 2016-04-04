using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PaletteEditor
{
    class CV2Image
    {
        private readonly bool _UsePalette;
        private Bitmap _Bitmap;
        private byte[,] _Index;

        public CV2Image(string filename)
        {
            using (FileStream f_in = File.OpenRead(filename))
            {
                byte[] header = new byte[1 + 4 + 4 + 4 + 4];
                f_in.Read(header, 0, header.Length);

                int width = BitConverter.ToInt32(header, 1);
                int height = BitConverter.ToInt32(header, 5);
                int stride = BitConverter.ToInt32(header, 9);

                this._UsePalette = (header[0] == 8);

                if (header[0] == 8)
                {
                    this._Index = new byte[height, width];
                    this._Bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                    var data = this._Bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                    byte[] readBuffer = new byte[stride];
                    for (int i = 0; i < height; ++i)
                    {
                        f_in.Read(readBuffer, 0, stride);
                        Marshal.Copy(readBuffer, 0, data.Scan0 + data.Stride * i, width * 1);
                        Buffer.BlockCopy(readBuffer, 0, _Index, width * i, width);
                    }
                    this._Bitmap.UnlockBits(data);
                }
                else if (header[0] == 16)
                {
                    this._Bitmap = new Bitmap(width, height, PixelFormat.Format16bppArgb1555);
                    var data = this._Bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
                    byte[] readBuffer = new byte[2 * stride];
                    for (int j = 0; j < height; ++j)
                    {
                        f_in.Read(readBuffer, 0, 2 * stride);
                        Marshal.Copy(readBuffer, 0, data.Scan0 + data.Stride * j, width * 2);
                    }
                    this._Bitmap.UnlockBits(data);
                }
                else if (header[0] == 24 || header[0] == 32)
                {
                    this._Bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    var data = this._Bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    byte[] readBuffer = new byte[4 * stride];
                    for (int j = 0; j < height; ++j)
                    {
                        f_in.Read(readBuffer, 0, 4 * stride);
                        Marshal.Copy(readBuffer, 0, data.Scan0 + data.Stride * j, width * 4);
                    }
                    this._Bitmap.UnlockBits(data);
                }
            }
        }

        public void ApplyPalette(Color[] pal)
        {
            if (_UsePalette && pal != null)
            {
                var palette = _Bitmap.Palette;
                for (int i = 0; i < 256; ++i)
                {
                    palette.Entries[i] = pal[i];
                }
                _Bitmap.Palette = palette;
            }
        }

        public Bitmap Image { get { return _Bitmap; } }

        public byte this[int x, int y]
        {
            get
            {
                return _Index == null ? (byte)0 : _Index[y, x];
            }
        }
    }
}
