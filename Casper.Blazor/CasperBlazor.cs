using Microsoft.AspNetCore.Components.Web;
using System;
using System.Timers;

namespace Casper.Blazor {
    public class CasperBlazor : Spectrum {
        public event Action Interrupt;

        Timer timer;

        public CasperBlazor() {
        }

        public bool Running {
            get {
                return timer?.Enabled ?? false;
            }
            set {
                if (timer == null) {
                    timer = new Timer();
                    timer.Interval = 20; // 20ms is 50 interrupts per second
                    timer.Elapsed += Timer_Tick;
                    timer.AutoReset = false;
                    timer.Enabled = false;
                }
                timer.Enabled = value;
            }
        }

        void Timer_Tick(object sender, EventArgs e) {
            base.execute();
            Interrupt?.Invoke();
            timer.Start();
        }

        public void DoKey(KeyboardEventArgs args, bool down) {
            // Key codes from: https://developer.mozilla.org/en-US/docs/Web/API/Document/keydown_event#addEventListener_keydown_example
            switch (args.Code) {
                case "KeyA": base.DoKeys(down, Key.A); break;
                case "KeyB": base.DoKeys(down, Key.B); break;
                case "KeyC": base.DoKeys(down, Key.C); break;
                case "KeyD": base.DoKeys(down, Key.D); break;
                case "KeyE": base.DoKeys(down, Key.E); break;
                case "KeyF": base.DoKeys(down, Key.F); break;
                case "KeyG": base.DoKeys(down, Key.G); break;
                case "KeyH": base.DoKeys(down, Key.H); break;
                case "KeyI": base.DoKeys(down, Key.I); break;
                case "KeyJ": base.DoKeys(down, Key.J); break;
                case "KeyK": base.DoKeys(down, Key.K); break;
                case "KeyL": base.DoKeys(down, Key.L); break;
                case "KeyM": base.DoKeys(down, Key.M); break;
                case "KeyN": base.DoKeys(down, Key.N); break;
                case "KeyO": base.DoKeys(down, Key.O); break;
                case "KeyP": base.DoKeys(down, Key.P); break;
                case "KeyQ": base.DoKeys(down, Key.Q); break;
                case "KeyR": base.DoKeys(down, Key.R); break;
                case "KeyS": base.DoKeys(down, Key.S); break;
                case "KeyT": base.DoKeys(down, Key.T); break;
                case "KeyU": base.DoKeys(down, Key.U); break;
                case "KeyV": base.DoKeys(down, Key.V); break;
                case "KeyW": base.DoKeys(down, Key.W); break;
                case "KeyX": base.DoKeys(down, Key.X); break;
                case "KeyY": base.DoKeys(down, Key.Y); break;
                case "KeyZ": base.DoKeys(down, Key.Z); break;

                case "Digit0": base.DoKeys(down, Key.D0); break;
                case "Digit1": base.DoKeys(down, Key.D1); break;
                case "Digit2": base.DoKeys(down, Key.D2); break;
                case "Digit3": base.DoKeys(down, Key.D3); break;
                case "Digit4": base.DoKeys(down, Key.D4); break;
                case "Digit5": base.DoKeys(down, Key.D5); break;
                case "Digit6": base.DoKeys(down, Key.D6); break;
                case "Digit7": base.DoKeys(down, Key.D7); break;
                case "Digit8": base.DoKeys(down, Key.D8); break;
                case "Digit9": base.DoKeys(down, Key.D9); break;

                case "ShiftLeft": base.DoKeys(down, Key.CAPS); break;
                case "ShiftRight": base.DoKeys(down, Key.SYMB); break;
                case "Enter": base.DoKeys(down, Key.ENTER); break;
                case "Space": base.DoKeys(down, Key.SPACE); break;

                case "Backspace":
                case "Delete": base.DoKeys(down, Key.CAPS, Key.D0); break;

                case "ArrowLeft": base.DoKeys(down, Key.CAPS, Key.D5); break;
                case "ArrowDown": base.DoKeys(down, Key.CAPS, Key.D6); break;
                case "ArrowUp": base.DoKeys(down, Key.CAPS, Key.D7); break;
                case "ArrowRight": base.DoKeys(down, Key.CAPS, Key.D8); break;

                case "Period": base.DoKeys(down, Key.SYMB, Key.M); break;
                case "Comma": base.DoKeys(down, Key.SYMB, Key.N); break;
                case "Semicolon": base.DoKeys(down, Key.CAPS, Key.D8); break;

                case "Escape": Running = !Running; break;
            }
        }
    }
}
