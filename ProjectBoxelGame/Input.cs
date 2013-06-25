using SharpDX.Multimedia;
using SharpDX.RawInput;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectBoxelGame
{
    class Input
    {
        private IDictionary<Keys, KeyState> KeyStates;
        private RenderForm Window;
        private int CenterX, CenterY;
        public int DeltaX {get; private set;}
        public int DeltaY { get; private set; }

        public Input(RenderForm Window)
        {
            this.Window = Window;
            this.CenterX = this.Window.DesktopLocation.X + this.Window.Width/2;
            this.CenterY = this.Window.DesktopLocation.Y + this.Window.Height/2;
            //SetCursorPos(this.CenterX, this.CenterY);
            this.KeyStates = new Dictionary<Keys, KeyState>();
            foreach(Keys KeyEnum in Enum.GetValues(typeof(Keys)))
            {
                this.KeyStates[KeyEnum] = KeyState.KeyUp;
            }
            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.None);
            Device.KeyboardInput += this.OnKeyEvent;
            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericMouse, DeviceFlags.None);
            Device.MouseInput += this.OnMouseInput;
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

        public void ResetMouse(bool ResetCursor)
        {
            this.DeltaX = 0;
            this.DeltaY = 0;
            if(ResetCursor)
                SetCursorPos(this.CenterX, this.CenterY);
        }

        private void OnKeyEvent(Object Sender, KeyboardInputEventArgs Args)
        {
            KeyStates[Args.Key] = Args.State;
            //System.Diagnostics.Trace.WriteLine(String.Format("Input: {0} is now {1}", Args.Key, Args.State));
        }

        private void OnMouseInput(Object Sender, MouseInputEventArgs Args)
        {
            this.DeltaX += Args.X;
            this.DeltaY += Args.Y;
        }

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
    }
}
