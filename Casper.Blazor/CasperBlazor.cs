using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
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
            base.Step();
            Interrupt?.Invoke();
            timer.Start();
        }

        /// <summary>
        /// If true, use logical key press based on the attached keyboard,
        /// i.e. on an AZERTY keyboard pressing the first physical alphabet key
        ///      will type an 'A' into the spectrum.
        /// In this mode only one key can be pressed at a time.
        /// This allows for natural typing and is best for BASIC and other non-game applications.
        /// 
        /// If false, then the physical keyboard layout will be used.
        /// i.e. on an AZERTY keyboard pressing the first physical alphabet key
        ///      will type an 'Q' into the spectrum.
        /// </summary>
        public bool UseLogicalKeyboardLayout { get; set; }

        internal void OnKey(KeyboardEventArgs args, bool down) {
            // Emulator control keys
            switch (args.KeyCode()) {
                case KeyCode.Pause: if (down) { Running = !Running; }; return;
                case KeyCode.Escape: if (down) { UseLogicalKeyboardLayout = !UseLogicalKeyboardLayout; }; return;
            }

            // KeyCode is the PHYSICAL key pressed so Keys.Q would be the first letter on the first row of letters.
            // For an AZERTY keyboard when the "A" key is pressed the KeyCode value is Keys.Q.
            if (UseLogicalKeyboardLayout && args.Key.Length == 1) {
                if (down) {
                    Keyboard.OnLogicalKeys(args.Key);
                }
            }
            else {
                Keyboard.OnPhysicalKey(down, args.KeyCode());
            }
        }
    }

    public static class KeyboardEventArgsExtensions {
        static Dictionary<string, KeyCode> KeyCodeMap = Enum.GetValues(typeof(KeyCode))
            .Cast<KeyCode>()
            .ToDictionary(kc => kc.ToString(), kc => kc);

        public static KeyCode KeyCode(this KeyboardEventArgs args) {
            if (KeyCodeMap.TryGetValue(args.Code, out var keyCode)) {
                return keyCode;
            }
            return Casper.KeyCode.Unidentified;
        }
    }

}
