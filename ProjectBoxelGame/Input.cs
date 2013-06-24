using SharpDX.Multimedia;
using SharpDX.RawInput;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectBoxelGame
{
    class Input
    {
        private IDictionary<Keys, KeyState> KeyStates;

        public Input()
        {
            this.KeyStates = new Dictionary<Keys, KeyState>();
            foreach(Keys KeyEnum in Enum.GetValues(typeof(Keys)))
            {
                this.KeyStates[KeyEnum] = KeyState.KeyUp;
            }
            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.None);
            Device.KeyboardInput += this.OnKeyEvent;
        }

        public KeyState GetKeyState(Keys Key)
        {
            return this.KeyStates[Key];
        }

        public bool IsDown(Keys Key)
        {
            return this.KeyStates[Key] == KeyState.KeyDown;
        }

        public bool IsUp(Keys Key)
        {
            return this.KeyStates[Key] == KeyState.KeyUp;
        }

        private void OnKeyEvent(Object Sender, KeyboardInputEventArgs Args)
        {
            KeyStates[Args.Key] = Args.State;
            //System.Diagnostics.Trace.WriteLine(String.Format("Input: {0} is now {1}", Args.Key, Args.State));
        }
    }
}
