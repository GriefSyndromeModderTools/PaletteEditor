using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaletteEditor
{
    class CV2Palette
    {
        public static Color[] ReadPaletteFile(string filename)
        {
            using (FileStream stream = File.OpenRead(filename))
            {
                var ret = new Color[256];
                var b = stream.ReadByte();
                if (b != 16)
                {
                    return null;
                }
                byte[] buffer = new byte[2];
                for (int i = 0; i < ret.Length; ++i)
                {
                    stream.Read(buffer, 0, 2);
                    ret[i] = FromBGRA5551(BitConverter.ToInt16(buffer, 0));
                }
                return ret;
            }
        }

        public static void SavePaletteFile(Color[] pal, string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            using (var stream = File.Open(filename, FileMode.CreateNew))
            {
                stream.WriteByte(16);
                byte[] buffer = new byte[2];
                for (int i = 0; i < 256; ++i)
                {
                    stream.Write(BitConverter.GetBytes(ToBGRA5551(pal[i])), 0, 2);
                }
            }
        }

        private static Color FromBGRA5551(Int16 val)
        {
            int b = (val & ((1 << 5) - 1)) >> 0 << 3;
            int g = (val & ((1 << 10) - 1)) >> 5 << 3;
            int r = (val & ((1 << 15) - 1)) >> 10 << 3;
            int a = ((val & (1 << 15)) != 0) ? 255 : 0;
            return Color.FromArgb(a, r, g, b);
        }

        private static Int16 ToBGRA5551(Color c)
        {
            int bb = c.B >> 3 << 0;
            int bg = c.G >> 3 << 5;
            int br = c.R >> 3 << 10;
            int ba = (c.A & 128) != 0 ? 1 << 15 : 0;
            return (short)(bb | bg | br | ba);
        }
    }
}
