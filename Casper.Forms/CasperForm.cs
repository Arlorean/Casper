using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Casper.Forms {
    [System.ComponentModel.DesignerCategory("")] // Disable Windows Forms Designer in Visual Studio
    public class CasperForm : Form {
        readonly Spectrum spectrum;
        readonly Graphics graphics;
        readonly Timer timer;
        readonly Image image;
        Joystick joystick;

        public CasperForm() {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            this.ShowIcon = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            spectrum = new Spectrum();
            spectrum.Screen.RenderPixel += RenderPixel;

            timer = new Timer {
                Interval = 20 // 20ms is 50 interrupts per second
            };
            timer.Tick += Timer_Tick;

            image = new Bitmap(Screen.Width, Screen.Height);
            graphics = Graphics.FromImage(image);
            graphics.Clear(Color.Black);

            this.Text = "Casper";
            this.ClientSize = Screen.OuterRectangle.Size + Screen.OuterRectangle.Size;

            InitializeController();

            spectrum.LoadROM(Casper.Shared.Resources.Spectrum);
            spectrum.LoadSnapshot(Casper.Shared.Resources.ManicMiner);
            Running = true;
        }

        public bool Running {
            get { return timer.Enabled; }
            set { timer.Enabled = value; }
        }

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CasperForm());
        }

        static readonly Brush[] brushes = Colors.Palette.Select(c => new SolidBrush(c)).ToArray();

        void Timer_Tick(object sender, EventArgs e) {
            UpdateController();
            spectrum.Step();
            Invalidate();
        }

        void InitializeController() {
            var directInput = new DirectInput();

            // Find a Gamepad
            var joystickGuid = directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices)
                .Select(d => d.InstanceGuid)
                .FirstOrDefault();

            // Find a Joystick
            if (joystickGuid == Guid.Empty) {
                joystickGuid = directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices)
                    .Select(d => d.InstanceGuid)
                    .FirstOrDefault();
            }

            // Find a FirstPerson controller
            if (joystickGuid == Guid.Empty) {
                joystickGuid = directInput.GetDevices(DeviceType.FirstPerson, DeviceEnumerationFlags.AllDevices)
                    .Select(d => d.InstanceGuid)
                    .FirstOrDefault();
            }

            // If Joystick not found, throws an error
            if (joystickGuid != Guid.Empty) {
                // Instantiate the joystick
                this.joystick = new Joystick(directInput, joystickGuid);

                // Acquire the joystick
                joystick.Acquire();
            }
        }

        void UpdateController() {
            var controller = this.joystick;
            if (controller == null) {
                return;
            }

            var state = controller.GetCurrentState();
            spectrum.Keyboard.OnPhysicalKey(state.Buttons[1], Key.SPACE);

            var axis = NormalizeAxis(state.X);
            spectrum.Keyboard.OnPhysicalKey(axis < -0.9, Key.Q);
            spectrum.Keyboard.OnPhysicalKey(axis > +0.9, Key.W);
        }

        static float NormalizeAxis(int axis) {
            axis += short.MinValue;
            return (axis < 0) ? -((float)axis / short.MinValue) : ((float)axis / short.MaxValue);
        }

        public void RenderPixel(int x, int y, ColorIndex colorIndex) {
            var brush = brushes[(int)colorIndex];

            var s = 1f;
            var rect = new RectangleF(x * s, y * s, s, s);
            graphics.FillRectangle(brush, rect);
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.ScaleTransform(2, 2);
            e.Graphics.FillRectangle(brushes[(int)spectrum.Screen.Border], Screen.OuterRectangle);
            e.Graphics.DrawImage(image, Screen.InnerRectangle.Location);
        }

        protected override void OnPaintBackground(PaintEventArgs e) {}

        protected override void OnKeyDown(KeyEventArgs e) {
            OnPhysicalKey(e, true);
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            OnPhysicalKey(e, false);
        }

        protected override void OnKeyPress(KeyPressEventArgs e) {
            OnLogicalKey(e);    
        }

        public bool UseLogicalKeyboardLayout { get; set; }

        void OnPhysicalKey(KeyEventArgs args, bool down) {
            // Emulator control keys
            switch (args.KeyCode) {
                case Keys.Pause: if (down) { Running = !Running; }; return;
                case Keys.Escape: if (down) { UseLogicalKeyboardLayout = !UseLogicalKeyboardLayout; }; return;
            }

            // KeyCode is the PHYSICAL key pressed so Keys.Q would be the first letter on the first row of letters.
            // For an AZERTY keyboard when the "A" key is pressed the KeyCode value is Keys.Q.
            if (KeyCodeMap.TryGetValue(args.KeyCode, out var keyCode)) {
                spectrum.Keyboard.OnPhysicalKey(down, keyCode);
            }
        }

        void OnLogicalKey(KeyPressEventArgs args) {
            if (UseLogicalKeyboardLayout) {
                spectrum.Keyboard.OnLogicalKeys(args.KeyChar.ToString());
            }
        }

        static readonly Dictionary<Keys, KeyCode> KeyCodeMap = new Dictionary<Keys, KeyCode>() {

        // https://www.w3.org/TR/uievents-code/#key-alphanumeric-writing-system
            { Keys.OemPipe, KeyCode.Backquote },
            { Keys.OemBackslash, KeyCode.Backslash },
            { Keys.Back, KeyCode.Backspace },
            { Keys.OemOpenBrackets, KeyCode.BracketLeft },
            { Keys.OemCloseBrackets, KeyCode.BracketRight },
            { Keys.Oemcomma, KeyCode.Comma },

            { Keys.D0, KeyCode.Digit0 },
            { Keys.D1, KeyCode.Digit1 },
            { Keys.D2, KeyCode.Digit2 },
            { Keys.D3, KeyCode.Digit3 },
            { Keys.D4, KeyCode.Digit4 },
            { Keys.D5, KeyCode.Digit5 },
            { Keys.D6, KeyCode.Digit6 },
            { Keys.D7, KeyCode.Digit7 },
            { Keys.D8, KeyCode.Digit8 },
            { Keys.D9, KeyCode.Digit9 },

        //Equal,
        //IntlBackslash,
        //IntlRo,
        //IntlYen,

            { Keys.A, KeyCode.KeyA },
            { Keys.B, KeyCode.KeyB },
            { Keys.C, KeyCode.KeyC },
            { Keys.D, KeyCode.KeyD },
            { Keys.E, KeyCode.KeyE },
            { Keys.F, KeyCode.KeyF },
            { Keys.G, KeyCode.KeyG },
            { Keys.H, KeyCode.KeyH },
            { Keys.I, KeyCode.KeyI },
            { Keys.J, KeyCode.KeyJ },
            { Keys.K, KeyCode.KeyK },
            { Keys.L, KeyCode.KeyL },
            { Keys.M, KeyCode.KeyM },
            { Keys.N, KeyCode.KeyN },
            { Keys.O, KeyCode.KeyO },
            { Keys.P, KeyCode.KeyP },
            { Keys.Q, KeyCode.KeyQ },
            { Keys.R, KeyCode.KeyR },
            { Keys.T, KeyCode.KeyT },
            { Keys.U, KeyCode.KeyU },
            { Keys.V, KeyCode.KeyV },
            { Keys.W, KeyCode.KeyW },
            { Keys.X, KeyCode.KeyX },
            { Keys.Y, KeyCode.KeyY },
            { Keys.Z, KeyCode.KeyZ },

            //{ Keys.OemMinus, KeyCode.Equal },
            //{ Keys.OemPeriod, KeyCode.Period },
            //{ Keys.OemQuotes, KeyCode.Quote },
            //{ Keys.OemSemicolon, KeyCode.Semicolon },

        //Slash,

        // https://www.w3.org/TR/uievents-code/#key-alphanumeric-functional
            //{ Keys.LMenu, KeyCode.AltLeft },
            //{ Keys.Menu, KeyCode.AltRight },
            //{ Keys.CapsLock, KeyCode.CapsLock },

        // ContextMenu,

            { Keys.LControlKey, KeyCode.ControlLeft },
            { Keys.RControlKey, KeyCode.ControlRight },
            { Keys.Enter, KeyCode.Enter },
            //{ Keys.LWin, KeyCode.MetaLeft },
            //{ Keys.RWin, KeyCode.MetaRight },
            { Keys.LShiftKey, KeyCode.ShiftLeft },
            { Keys.RShiftKey, KeyCode.ShiftRight },
            { Keys.Space, KeyCode.Space },
            //{ Keys.Tab, KeyCode.Tab },

        // https://www.w3.org/TR/uievents-code/#key-controlpad-section
            //{ Keys.Delete, KeyCode.Delete },
            //{ Keys.End, KeyCode.End },
            //{ Keys.Help, KeyCode.Help },
            //{ Keys.Home, KeyCode.Home },
            //{ Keys.Insert, KeyCode.Insert },
            //{ Keys.PageDown, KeyCode.PageDown },
            //{ Keys.PageUp, KeyCode.PageUp },

        // https://www.w3.org/TR/uievents-code/#key-arrowpad-section
            { Keys.Down, KeyCode.ArrowDown },
            { Keys.Left, KeyCode.ArrowLeft },
            { Keys.Right, KeyCode.ArrowRight },
            { Keys.Up, KeyCode.ArrowUp },

        // https://www.w3.org/TR/uievents-code/#key-numpad-section
            //{ Keys.NumLock, KeyCode.NumLock },
            { Keys.NumPad0, KeyCode.Numpad0 },
            { Keys.NumPad1, KeyCode.Numpad1 },
            { Keys.NumPad2, KeyCode.Numpad2 },
            { Keys.NumPad3, KeyCode.Numpad3 },
            { Keys.NumPad4, KeyCode.Numpad4 },
            { Keys.NumPad5, KeyCode.Numpad5 },
            { Keys.NumPad6, KeyCode.Numpad6 },
            { Keys.NumPad7, KeyCode.Numpad7 },
            { Keys.NumPad8, KeyCode.Numpad8 },
            { Keys.NumPad9, KeyCode.Numpad9 },

            //{ Keys.Oemplus, KeyCode.NumpadAdd },

        //NumpadAdd,
        //NumpadBackspace,
        //NumpadClear,
        //NumpadClearEntry,
        //NumpadComma,
        //NumpadDecimal,
        //NumpadDivide,
        //NumpadEnter,
        //NumpadEqual,
        //NumpadHash,
        //NumpadMemoryAdd,
        //NumpadMemoryClear,
        //NumpadMemoryRecall,
        //NumpadMemoryStore,
        //NumpadMemorySubtract,
        //NumpadMultiply,
        //NumpadParenLeft,
        //NumpadParenRight,
        //NumpadStar,
        //NumpadSubtract,

        // https://www.w3.org/TR/uievents-code/#key-function-section
            //{ Keys.Escape, KeyCode.Escape },
            //{ Keys.F1, KeyCode.F1 },
            //{ Keys.F2, KeyCode.F2 },
            //{ Keys.F3, KeyCode.F3 },
            //{ Keys.F4, KeyCode.F4 },
            //{ Keys.F5, KeyCode.F5 },
            //{ Keys.F6, KeyCode.F6 },
            //{ Keys.F7, KeyCode.F7 },
            //{ Keys.F8, KeyCode.F8 },
            //{ Keys.F9, KeyCode.F9 },
            //{ Keys.F10, KeyCode.F10 },
            //{ Keys.F11, KeyCode.F11 },
            //{ Keys.F12, KeyCode.F12 },
        //Fn,
        //FnLock,
            //{ Keys.PrintScreen, KeyCode.PrintScreen },
            //{ Keys.Scroll, KeyCode.ScrollLock },
            //{ Keys.Pause, KeyCode.Pause },

        // https://www.w3.org/TR/uievents-code/#key-media
            //{ Keys.BrowserBack, KeyCode.BrowserBack },
            //{ Keys.BrowserFavorites, KeyCode.BrowserFavorites },
            //{ Keys.BrowserForward, KeyCode.BrowserForward },
            //{ Keys.BrowserHome, KeyCode.BrowserHome },
            //{ Keys.BrowserRefresh, KeyCode.BrowserRefresh },
            //{ Keys.BrowserSearch, KeyCode.BrowserSearch },
            //{ Keys.BrowserStop, KeyCode.BrowserStop },
        //Eject,
            //{ Keys.LaunchApplication1, KeyCode.LaunchApp1 },
            //{ Keys.LaunchApplication2, KeyCode.LaunchApp2 },
            //{ Keys.LaunchMail, KeyCode.LaunchMail },
            //{ Keys.MediaPlayPause, KeyCode.MediaPlayPause },
            //{ Keys.SelectMedia, KeyCode.MediaSelect },
            //{ Keys.MediaStop, KeyCode.MediaStop },
            //{ Keys.MediaNextTrack, KeyCode.MediaTrackNext },
            //{ Keys.MediaPreviousTrack, KeyCode.MediaTrackPrevious },
        //Power,
            //{ Keys.Sleep, KeyCode.Sleep },
            //{ Keys.VolumeDown, KeyCode.AudioVolumeDown },
            //{ Keys.VolumeMute, KeyCode.AudioVolumeMute },
            //{ Keys.VolumeUp, KeyCode.AudioVolumeUp },
        //WakeUp,

        // https://www.w3.org/TR/uievents-code/#key-legacy
        //Hyper,
        //Super,
        //Turbo,
        //Abort,
        //Resume,
        //Suspend,
        //Again,
        //Copy,
        //Cut,
        //Find,
        //Open,
        //Paste,
        //Props,
        //Select,
        //Undo,
        //Hiragana,
        //Katakana,
        //Unidentified,
        };

        #region Detect Left or Right, Shift or Control
        // StackOverflow: .net difference between right shift and left shift keys
        // https://stackoverflow.com/a/27698458/256627

        // Keyboard Scan Codes
        // https://download.microsoft.com/download/1/6/1/161ba512-40e2-4cc9-843a-923143f3456c/scancode.doc
        const int LShift = 0x2A;
        const int RShift = 0x36;
        const int LControl = 0x1D;
        const int RControl = 0x11D;

        // Windows Keyboard Messages
        // https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-keydown
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;

        // Virtual KeyCodes
        // https://docs.microsoft.com/en-gb/windows/win32/inputdev/virtual-key-codes
        const int VK_SHIFT = 0x10;
        const int VK_CONTROL = 0x11;

        protected override bool ProcessKeyMessage(ref Message m) {
            if ((m.Msg == WM_KEYDOWN || m.Msg == WM_KEYUP) && ((int)m.WParam == VK_CONTROL || (int)m.WParam == VK_SHIFT)) {
                Keys? key = null;
                switch (((int)m.LParam >> 16) & 0x1FF) {
                    case LControl: key = Keys.LControlKey; break;
                    case RControl: key = Keys.RControlKey; break;
                    case LShift: key = Keys.LShiftKey; break;
                    case RShift: key = Keys.RShiftKey; break;
                }
                if (key.HasValue) {
                    if (m.Msg == WM_KEYDOWN)
                        OnKeyDown(new KeyEventArgs(key.Value));
                    else
                        OnKeyUp(new KeyEventArgs(key.Value));
                    return true;
                }
            }
            return base.ProcessKeyMessage(ref m);
        }
        #endregion
    }
}
