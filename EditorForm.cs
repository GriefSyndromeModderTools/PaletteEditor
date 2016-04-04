using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaletteEditor
{
    public partial class EditorForm : Form
    {
        public EditorForm()
        {
            InitializeComponent();
            this.MouseWheel += pictureBox2_MouseWheel;
        }

        private Color[] _Palette;
        private CV2Image _Image;

        private float _Scale = 1.0f;
        private float _OffsetX, _OffsetY;

        private const int PalettePixelSize = 11;

        private void RefreshImageList()
        {
            _Image.ApplyPalette(_Palette);
            pictureBox2.Invalidate();
        }

        private void RefreshPalette(Color[] pal = null)
        {
            pictureBox1.Invalidate();

            if (_Image != null)
            {
                _Image.ApplyPalette(pal == null ? _Palette : pal);
            }
            pictureBox2.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);

            if (_Palette == null)
            {
                return;
            }

            for (int y = 0; y < 16; ++y)
            {
                for (int x = 0; x < 16; ++x)
                {
                    var index = y * 16 + x;
                    var color = _Palette[index];
                    e.Graphics.FillRectangle(new SolidBrush(color),
                        x * PalettePixelSize, y * PalettePixelSize, PalettePixelSize, PalettePixelSize);
                    if (index == _SelectedIndex)
                    {
                        var cc = color.R + color.G + color.B > 255 * 3 / 2 ? Pens.Black : Pens.White;
                        e.Graphics.DrawRectangle(cc,
                            x * PalettePixelSize, y * PalettePixelSize, PalettePixelSize - 1, PalettePixelSize - 1);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            if (_Image != null)
            {
                var img = _Image.Image;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.DrawImage(img, _OffsetX, _OffsetY, _Scale * img.Width, _Scale * img.Height);
            }
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            var pos = pictureBox1.PointToClient(Control.MousePosition);
            int x = pos.X / PalettePixelSize, y = pos.Y / PalettePixelSize;
            if (x >= 0 && x < 16 && y >= 0 && y < 16)
            {
                int index = y * 16 + x;
                colorDialog1.Color = _Palette[index];
                if (colorDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _Palette[index] = colorDialog1.Color;
                    RefreshPalette();
                }
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (_Palette == null)
            {
                return;
            }

            var pos = pictureBox1.PointToClient(Control.MousePosition);
            int x = pos.X / PalettePixelSize, y = pos.Y / PalettePixelSize;
            if (x >= 0 && x < 16 && y >= 0 && y < 16)
            {
                int index = y * 16 + x;
                Switch(index);
            }
            else
            {
                Switch(-1);
            }
        }

        private void Switch(int index)
        {
            foreach(var c in _Palette)
            {
                Console.WriteLine(c.Name);
            }
            if (index == -1)
            {
                _SwitchSelected = null;
                RefreshPalette();
                return;
            }

            var pp = (Color[])_Palette.Clone();
            bool switcher = false;

            var original = pp[index];
            var switchColor = original.R + original.G + original.B > 255 * 3 / 2 ? Color.Black : Color.White;

            _SwitchSelected = delegate()
            {
                pp[index] = switcher ? original : switchColor;
                _SelectedIndex = switcher ? index : -1;
                switcher = !switcher;
                RefreshPalette(pp);
            };

            _SwitchSelected();
        }

        private Action _SwitchSelected;
        private int _SelectedIndex = -1;

        private Color ReverseColor(Color c)
        {
            return Color.FromArgb(c.A, 255 - c.R, 255 - c.G, 255 - c.B);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_SwitchSelected != null)
            {
                _SwitchSelected();
            }
        }

        private int _DownX, _DownY;
        private bool _DownState;

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_DownState && e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                _DownState = true;
                _DownX = e.X;
                _DownY = e.Y;
            }
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            if (_DownState && e.Button.HasFlag(System.Windows.Forms.MouseButtons.Middle))
            {
                _DownState = false;
            }
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (_DownState)
            {
                _OffsetX += e.X - _DownX;
                _OffsetY += e.Y - _DownY;
                _DownX = e.X;
                _DownY = e.Y;
                pictureBox2.Invalidate();
            }
        }

        private void pictureBox2_MouseWheel(object sender, MouseEventArgs e)
        {
            var oldScale = _Scale;
            var newScale = oldScale * (e.Delta > 0 ? 1.2f : 1 / 1.2f);

            var pc = pictureBox2.PointToClient(Control.MousePosition);
            if (!pictureBox2.ClientRectangle.Contains(pc))
            {
                return;
            }

            var psx = (pc.X - _OffsetX) / oldScale;
            var psy = (pc.Y - _OffsetY) / oldScale;
            var nox = pc.X - newScale * psx;
            var noy = pc.Y - newScale * psy;

            _OffsetX = nox;
            _OffsetY = noy;
            _Scale = newScale;
            pictureBox2.Invalidate();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (_Image == null)
            {
                return;
            }
            var pc = pictureBox2.PointToClient(Control.MousePosition);
            var psx = (int)Math.Round((pc.X - _OffsetX) / _Scale);
            var psy = (int)Math.Round((pc.Y - _OffsetY) / _Scale);
            if (psx >= 0 && psx < _Image.Image.Width &&
                psy >= 0 && psy < _Image.Image.Height)
            {
                byte index = _Image[psx, psy];
                Switch(index);
                Text = psx.ToString() + ", " + psy + " : " + index;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (openFileDialogPal.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    _Palette = CV2Palette.ReadPaletteFile(openFileDialogPal.FileName);
                    if (_Palette == null)
                    {
                        throw new Exception();
                    }
                    RefreshPalette();
                }
                catch
                {
                    SystemSounds.Beep.Play();
                }
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (openFileDialogCV2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var list = openFileDialogCV2.FileName;
                _Image = null;
                if (list != null && File.Exists(list))
                {
                    try
                    {
                        _Image = new CV2Image(openFileDialogCV2.FileName);
                    }
                    catch
                    {
                    }
                }
                RefreshImageList();
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            toolStripButton4.Checked = !toolStripButton4.Checked;
            timer1.Enabled = toolStripButton4.Checked;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (_Palette != null &&
                saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CV2Palette.SavePaletteFile(_Palette, saveFileDialog1.FileName);
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            string[] helpMessage = new[] {
                "This tool allows you to generate a new palette from an existing one by displacing colors in it.",
                "Author: acaly(wuzhenwei35@gmail.com)",
                "",
                "Toolbar buttons:",
                "  Open \tOpen a palette to edit.",
                "  Save \tSave current palette to file.",
                "  Image\tChoose a cv2 file to preview the palette.",
                "  Flash\tWhether or not to display flash effect for selected color.",
                "",
                "Usage:",
                "  Click on the palette or the image to select a color.",
                "  Double click on the palette to edit the color.",
                "  Use mouse middle button and mouse wheel to move or resize the image in preview window.",
            };
            MessageBox.Show(String.Join("\n", helpMessage), "PaletteEditor");
        }
    }
}
