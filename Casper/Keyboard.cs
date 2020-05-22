using System;
using System.Collections.Generic;
using System.Text;

namespace Casper {
    public class Keyboard {
        readonly byte[] banks = new byte[8];

        public Keyboard() {
            Reset();
        }

        public void Reset() {
            for (var i = 0; i < 8; ++i) {
                banks[i] = 0xff;
            }
        }

        string logicalKeys = string.Empty;
        int? nextLogicalKey;

        public void ProcessLogicalKeys() {
            // Nothing to process
            if (!nextLogicalKey.HasValue) {
                return;
            }
            // Process key up from previous process
            if (nextLogicalKey == logicalKeys.Length) {
                Reset();
                logicalKeys = string.Empty;
                nextLogicalKey = null;
            }
            // Process next logical key
            else { 
                var keys = GetKeys(logicalKeys[nextLogicalKey.Value]);
                OnPhysicalKeys(down:true, keys);
                nextLogicalKey++;
            }
        }

        /// <summary>
        /// Press a sequence of keys. Can be used to paste a string into the Spectrum keyboard.
        /// </summary>
        /// <param name="logicalKeys">Sequence of characters to be pressed, once after the other.</param>
        public void OnLogicalKeys(string logicalKeys) {
            this.logicalKeys = logicalKeys ?? string.Empty;
            this.nextLogicalKey = 0;
            Reset();
        }

        public bool OnPhysicalKey(bool down, KeyCode keyCode) {
            // KeyCode is the PHYSICAL key pressed so Keys.Q would be the first letter on the first row of letters.
            // For an AZERTY keyboard when the "A" key is pressed the KeyCode value is Keys.Q.
            switch (keyCode) {
                // The 40 physical keys of the Spectrum keyboard.
                case KeyCode.KeyA: OnPhysicalKeys(down, Key.A); break;
                case KeyCode.KeyB: OnPhysicalKeys(down, Key.B); break;
                case KeyCode.KeyC: OnPhysicalKeys(down, Key.C); break;
                case KeyCode.KeyD: OnPhysicalKeys(down, Key.D); break;
                case KeyCode.KeyE: OnPhysicalKeys(down, Key.E); break;
                case KeyCode.KeyF: OnPhysicalKeys(down, Key.F); break;
                case KeyCode.KeyG: OnPhysicalKeys(down, Key.G); break;
                case KeyCode.KeyH: OnPhysicalKeys(down, Key.H); break;
                case KeyCode.KeyI: OnPhysicalKeys(down, Key.I); break;
                case KeyCode.KeyJ: OnPhysicalKeys(down, Key.J); break;
                case KeyCode.KeyK: OnPhysicalKeys(down, Key.K); break;
                case KeyCode.KeyL: OnPhysicalKeys(down, Key.L); break;
                case KeyCode.KeyM: OnPhysicalKeys(down, Key.M); break;
                case KeyCode.KeyN: OnPhysicalKeys(down, Key.N); break;
                case KeyCode.KeyO: OnPhysicalKeys(down, Key.O); break;
                case KeyCode.KeyP: OnPhysicalKeys(down, Key.P); break;
                case KeyCode.KeyQ: OnPhysicalKeys(down, Key.Q); break;
                case KeyCode.KeyR: OnPhysicalKeys(down, Key.R); break;
                case KeyCode.KeyS: OnPhysicalKeys(down, Key.S); break;
                case KeyCode.KeyT: OnPhysicalKeys(down, Key.T); break;
                case KeyCode.KeyU: OnPhysicalKeys(down, Key.U); break;
                case KeyCode.KeyV: OnPhysicalKeys(down, Key.V); break;
                case KeyCode.KeyW: OnPhysicalKeys(down, Key.W); break;
                case KeyCode.KeyX: OnPhysicalKeys(down, Key.X); break;
                case KeyCode.KeyY: OnPhysicalKeys(down, Key.Y); break;
                case KeyCode.KeyZ: OnPhysicalKeys(down, Key.Z); break;

                case KeyCode.Digit0: OnPhysicalKeys(down, Key.D0); break;
                case KeyCode.Digit1: OnPhysicalKeys(down, Key.D1); break;
                case KeyCode.Digit2: OnPhysicalKeys(down, Key.D2); break;
                case KeyCode.Digit3: OnPhysicalKeys(down, Key.D3); break;
                case KeyCode.Digit4: OnPhysicalKeys(down, Key.D4); break;
                case KeyCode.Digit5: OnPhysicalKeys(down, Key.D5); break;
                case KeyCode.Digit6: OnPhysicalKeys(down, Key.D6); break;
                case KeyCode.Digit7: OnPhysicalKeys(down, Key.D7); break;
                case KeyCode.Digit8: OnPhysicalKeys(down, Key.D8); break;
                case KeyCode.Digit9: OnPhysicalKeys(down, Key.D9); break;

                case KeyCode.Enter: OnPhysicalKeys(down, Key.ENTER); break;
                case KeyCode.Space: OnPhysicalKeys(down, Key.SPACE); break;
                case KeyCode.ShiftLeft: OnPhysicalKeys(down, Key.CAPS); break;
                case KeyCode.ShiftRight: OnPhysicalKeys(down, Key.SYMB); break;

                // Joystick emulation mode for arrow keys.
                // TODO: Add support for different joystick -> keyboard emulation mappings
                case KeyCode.ArrowLeft: OnPhysicalKeys(down, Key.CAPS, Key.D5); break;
                case KeyCode.ArrowDown: OnPhysicalKeys(down, Key.CAPS, Key.D6); break;
                case KeyCode.ArrowUp: OnPhysicalKeys(down, Key.CAPS, Key.D7); break;
                case KeyCode.ArrowRight: OnPhysicalKeys(down, Key.CAPS, Key.D8); break;

                // Sensible extra Physical keys.
                case KeyCode.Backspace: OnPhysicalKeys(down, Key.CAPS, Key.D0); break;
                case KeyCode.Delete: OnPhysicalKeys(down, Key.CAPS, Key.D0); break;
                case KeyCode.Comma: OnPhysicalKeys(down, Key.SYMB, Key.N); break;
                case KeyCode.Period: OnPhysicalKeys(down, Key.SYMB, Key.M); break;

                // Key not handled
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Press keys directly using the Spectrum Keyboard key codes.
        /// </summary>
        /// <param name="down">Whether the keys are down or not.</param>
        /// <param name="keys">Array of keys to be handled.</param>
        public void OnPhysicalKeys(bool down, params Key[] keys) {
            foreach (var key in keys) {
                OnPhysicalKey(down, key);
            }
        }

        /// <summary>
        /// Press a key directly using the Spectrum Keyboard key codes.
        /// </summary>
        /// <param name="down">Whether the key is down or not.</param>
        /// <param name="key">The key to be handled.</param>
        public void OnPhysicalKey(bool down, Key key) {
            var bank = ((int)key / 5);
            var bit = 1 << ((int)key % 5);
            if (down) {
                banks[bank] &= (byte)(~bit);
            }
            else {
                banks[bank] |= (byte)(bit);
            }
        }

        /// <summary>
        /// What keys are currently pressed down?
        /// </summary>
        /// <returns>Array of keys pressed.</returns>
        public Key[] GetKeysPressed() {
            var pressed = new List<Key>();
            foreach (Key key in Enum.GetValues(typeof(Key))) {
                var bank = ((int)key / 5);
                var bit = 1 << ((int)key % 5);
                if ((banks[bank] & (byte)(bit)) == 0) {
                    pressed.Add(key);
                }
            }
            return pressed.ToArray();
        }

        internal byte InPort(ushort port) {
            byte res = 0xff;

            if ((port & 0x0001) == 0) {
                if ((port & 0x8000) == 0) { res &= banks[7]; }
                if ((port & 0x4000) == 0) { res &= banks[6]; }
                if ((port & 0x2000) == 0) { res &= banks[5]; }
                if ((port & 0x1000) == 0) { res &= banks[4]; }
                if ((port & 0x0800) == 0) { res &= banks[3]; }
                if ((port & 0x0400) == 0) { res &= banks[2]; }
                if ((port & 0x0200) == 0) { res &= banks[1]; }
                if ((port & 0x0100) == 0) { res &= banks[0]; }
            }

            return (res);
        }

        /// <summary>
        /// Lookup the key combination for the given Unicode character on the Spectrum 
        /// These mappings only make sense in the BASIC editor or other applications that use those combinations.
        /// </summary>
        /// <param name="c">The character to find the combination for.</param>
        /// <returns>An array of keys that when pressed produce the Unicode character, or an empty array if it can't be typed.</returns>
        public static Key[] GetKeys(char c) {
            if (KeysMap.TryGetValue(c, out var keys)) {
                return keys;
            }
            return Array.Empty<Key>();
        }

        static readonly Dictionary<char, Key[]> KeysMap = new Dictionary<char, Key[]> {
            { ' ', new [] { Key.SPACE } },
            { '\r', new [] { Key.ENTER } },
            { '\b', new [] { Key.CAPS, Key.D0 } },

            { '!', new [] { Key.SYMB, Key.D1 } },
            { '@', new [] { Key.SYMB, Key.D2 } },
            { '#', new [] { Key.SYMB, Key.D3 } },
            { '$', new [] { Key.SYMB, Key.D4 } },
            { '%', new [] { Key.SYMB, Key.D5 } },

            { '&', new [] { Key.SYMB, Key.D6 } },
            { '\'', new [] { Key.SYMB, Key.D7 } },
            { '(', new [] { Key.SYMB, Key.D8 } },
            { ')', new [] { Key.SYMB, Key.D9 } },
            { '_', new [] { Key.SYMB, Key.D0 } },

            { '<', new [] { Key.SYMB, Key.R } },
            { '>', new [] { Key.SYMB, Key.T } },

            { ';', new [] { Key.SYMB, Key.O } },
            { '"', new [] { Key.SYMB, Key.P } },

            { '^', new [] { Key.SYMB, Key.H } },
            { '-', new [] { Key.SYMB, Key.J } },
            { '+', new [] { Key.SYMB, Key.K } },
            { '=', new [] { Key.SYMB, Key.L } },

            { ':', new [] { Key.SYMB, Key.Z } },
            { '£', new [] { Key.SYMB, Key.X } },
            { '?', new [] { Key.SYMB, Key.C } },
            { '/', new [] { Key.SYMB, Key.V } },

            { '*', new [] { Key.SYMB, Key.B } },
            { ',', new [] { Key.SYMB, Key.N } },
            { '.', new [] { Key.SYMB, Key.M } },

            { '1', new [] { Key.D1 } },
            { '2', new [] { Key.D2 } },
            { '3', new [] { Key.D3 } },
            { '4', new [] { Key.D4 } },
            { '5', new [] { Key.D5 } },
            { '6', new [] { Key.D6 } },
            { '7', new [] { Key.D7 } },
            { '8', new [] { Key.D8 } },
            { '9', new [] { Key.D9 } },
            { '0', new [] { Key.D0 } },

            { 'a', new [] { Key.A } },
            { 'b', new [] { Key.B } },
            { 'c', new [] { Key.C } },
            { 'd', new [] { Key.D } },
            { 'e', new [] { Key.E } },
            { 'f', new [] { Key.F } },
            { 'g', new [] { Key.G } },
            { 'h', new [] { Key.H } },
            { 'i', new [] { Key.I } },
            { 'j', new [] { Key.J } },
            { 'k', new [] { Key.K } },
            { 'l', new [] { Key.L } },
            { 'm', new [] { Key.M } },
            { 'n', new [] { Key.N } },
            { 'o', new [] { Key.O } },
            { 'p', new [] { Key.P } },
            { 'q', new [] { Key.Q } },
            { 'r', new [] { Key.R } },
            { 's', new [] { Key.S } },
            { 't', new [] { Key.T } },
            { 'u', new [] { Key.U } },
            { 'v', new [] { Key.V } },
            { 'w', new [] { Key.W } },
            { 'x', new [] { Key.X } },
            { 'y', new [] { Key.Y } },
            { 'z', new [] { Key.Z } },

            { 'A', new [] { Key.CAPS, Key.A } },
            { 'B', new [] { Key.CAPS, Key.B } },
            { 'C', new [] { Key.CAPS, Key.C } },
            { 'D', new [] { Key.CAPS, Key.D } },
            { 'E', new [] { Key.CAPS, Key.E } },
            { 'F', new [] { Key.CAPS, Key.F } },
            { 'G', new [] { Key.CAPS, Key.G } },
            { 'H', new [] { Key.CAPS, Key.H } },
            { 'I', new [] { Key.CAPS, Key.I } },
            { 'J', new [] { Key.CAPS, Key.J } },
            { 'K', new [] { Key.CAPS, Key.K } },
            { 'L', new [] { Key.CAPS, Key.L } },
            { 'M', new [] { Key.CAPS, Key.M } },
            { 'N', new [] { Key.CAPS, Key.N } },
            { 'O', new [] { Key.CAPS, Key.O } },
            { 'P', new [] { Key.CAPS, Key.P } },
            { 'Q', new [] { Key.CAPS, Key.Q } },
            { 'R', new [] { Key.CAPS, Key.R } },
            { 'S', new [] { Key.CAPS, Key.S } },
            { 'T', new [] { Key.CAPS, Key.T } },
            { 'U', new [] { Key.CAPS, Key.U } },
            { 'V', new [] { Key.CAPS, Key.V } },
            { 'W', new [] { Key.CAPS, Key.W } },
            { 'X', new [] { Key.CAPS, Key.X } },
            { 'Y', new [] { Key.CAPS, Key.Y } },
            { 'Z', new [] { Key.CAPS, Key.Z } },
        };
    }
}