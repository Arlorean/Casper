using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Casper.Forms {
    [System.ComponentModel.DesignerCategory("")] // Disable Windows Forms Designer in Visual Studio
    public class CasperForm : Form {
        Spectrum spectrum;
        Graphics graphics;
        Timer interrupt;
        Image image;

        public CasperForm() {
            spectrum = new Spectrum();
            spectrum.Screen.RenderPixel += RenderPixel;

            interrupt = new Timer();
            interrupt.Interval = 20; // 20ms is 50 interrupts per second
            interrupt.Tick += Interrupt_Tick;

            image = new Bitmap(256, 192);
            graphics = Graphics.FromImage(image);
            graphics.Clear(Color.Black);

            this.Text = "Casper";
            this.ClientSize = image.Size;

            spectrum.LoadROM(Casper.Shared.Resources.Spectrum);
            spectrum.LoadSnapshot(Casper.Shared.Resources.ManicMiner);
            interrupt.Enabled = true;
        }

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CasperForm());
        }

        static readonly Brush[] brushes = Colors.Palette.Select(c => new SolidBrush(c)).ToArray();

        void Interrupt_Tick(object sender, EventArgs e) {
            spectrum.execute();
            Invalidate();
        }

        public void RenderPixel(int x, int y, ColorIndex colorIndex) {
            var brush = brushes[(int)colorIndex];

            var s = 1f;
            var rect = new RectangleF(x * s, y * s, s, s);
            graphics.FillRectangle(brush, rect);
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.DrawImageUnscaled(image, Point.Empty);
        }

        protected override void OnPaintBackground(PaintEventArgs e) {}

        protected override void OnKeyDown(KeyEventArgs e) {
            DoKey(e, true);
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            DoKey(e, false);
        }

        static KeysConverter converter = new KeysConverter();

        void DoKey(KeyEventArgs args, bool down) {
            switch (args.KeyCode) {
            case Keys.A: spectrum.DoKeys(down, Key.A); break;
            case Keys.B: spectrum.DoKeys(down, Key.B); break;
            case Keys.C: spectrum.DoKeys(down, Key.C); break;
            case Keys.D: spectrum.DoKeys(down, Key.D); break;
            case Keys.E: spectrum.DoKeys(down, Key.E); break;
            case Keys.F: spectrum.DoKeys(down, Key.F); break;
            case Keys.G: spectrum.DoKeys(down, Key.G); break;
            case Keys.H: spectrum.DoKeys(down, Key.H); break;
            case Keys.I: spectrum.DoKeys(down, Key.I); break;
            case Keys.J: spectrum.DoKeys(down, Key.J); break;
            case Keys.K: spectrum.DoKeys(down, Key.K); break;
            case Keys.L: spectrum.DoKeys(down, Key.L); break;
            case Keys.M: spectrum.DoKeys(down, Key.M); break;
            case Keys.N: spectrum.DoKeys(down, Key.N); break;
            case Keys.O: spectrum.DoKeys(down, Key.O); break;
            case Keys.P: spectrum.DoKeys(down, Key.P); break;
            case Keys.Q: spectrum.DoKeys(down, Key.Q); break;
            case Keys.R: spectrum.DoKeys(down, Key.R); break;
            case Keys.S: spectrum.DoKeys(down, Key.S); break;
            case Keys.T: spectrum.DoKeys(down, Key.T); break;
            case Keys.U: spectrum.DoKeys(down, Key.U); break;
            case Keys.V: spectrum.DoKeys(down, Key.V); break;
            case Keys.W: spectrum.DoKeys(down, Key.W); break;
            case Keys.X: spectrum.DoKeys(down, Key.X); break;
            case Keys.Y: spectrum.DoKeys(down, Key.Y); break;
            case Keys.Z: spectrum.DoKeys(down, Key.Z); break;

            case Keys.D0: spectrum.DoKeys(down, Key.D0); break;
            case Keys.D1: spectrum.DoKeys(down, Key.D1); break;
            case Keys.D2: spectrum.DoKeys(down, Key.D2); break;
            case Keys.D3: spectrum.DoKeys(down, Key.D3); break;
            case Keys.D4: spectrum.DoKeys(down, Key.D4); break;
            case Keys.D5: spectrum.DoKeys(down, Key.D5); break;
            case Keys.D6: spectrum.DoKeys(down, Key.D6); break;
            case Keys.D7: spectrum.DoKeys(down, Key.D7); break;
            case Keys.D8: spectrum.DoKeys(down, Key.D8); break;
            case Keys.D9: spectrum.DoKeys(down, Key.D9); break;

            case Keys.LShiftKey: spectrum.DoKeys(down, Key.CAPS); break;
            case Keys.RShiftKey: spectrum.DoKeys(down, Key.SYMB); break;
            case Keys.Enter: spectrum.DoKeys(down, Key.ENTER); break;
            case Keys.Space: spectrum.DoKeys(down, Key.SPACE); break;

            case Keys.Back:
            case Keys.Delete: spectrum.DoKeys(down, Key.CAPS, Key.D0); break;

            case Keys.Left:  spectrum.DoKeys(down, Key.CAPS, Key.D5); break;
            case Keys.Down:  spectrum.DoKeys(down, Key.CAPS, Key.D6); break;
            case Keys.Up:    spectrum.DoKeys(down, Key.CAPS, Key.D7); break;
            case Keys.Right: spectrum.DoKeys(down, Key.CAPS, Key.D8); break;

            case Keys.OemPeriod:    spectrum.DoKeys(down, Key.SYMB, Key.M); break;
            case Keys.Oemcomma:     spectrum.DoKeys(down, Key.SYMB, Key.N); break;
            case Keys.OemSemicolon: spectrum.DoKeys(down, Key.CAPS, Key.D8); break;

            }
        }
    }
}
