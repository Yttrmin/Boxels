using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace BoxelGame
{
    partial class Input
    {
        internal static class KeyMap
        {
            private enum MapType : uint
            {
                MAPVK_VK_TO_VSC = 0x0,
                MAPVK_VSC_TO_VK = 0x1,
                MAPVK_VK_TO_CHAR = 0x2,
                MAPVK_VSC_TO_VK_EX = 0x3,
            }

            private static IDictionary<Keys, char> CharMap;
            private static byte[] KeyboardState;

            [DllImport("user32.dll")]
            private static extern int ToUnicode(
                uint wVirtKey,
                uint wScanCode,
                byte[] lpKeyState,
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] 
            StringBuilder pwszBuff,
                int cchBuff,
                uint wFlags);

            [DllImport("user32.dll")]
            private static extern bool GetKeyboardState(byte[] lpKeyState);

            [DllImport("user32.dll")]
            private static extern uint MapVirtualKey(uint uCode, MapType uMapType);

            static KeyMap()
            {
                KeyboardState = new byte[256];
                CharMap = new Dictionary<Keys, char>();
                foreach (Keys Key in Enum.GetValues(typeof(Keys)))
                {
                    CharMap[Key] = GetCharFromKey(KeyInterop.KeyFromVirtualKey((int)Key));
                }
            }

            public static char KeysToChar(Keys Key)
            {
                return GetCharFromKey(KeyInterop.KeyFromVirtualKey((int)Key));
            }

            private static char GetCharFromKey(Key key)
            {
                char ch = (char)0;

                int VirtualKey = KeyInterop.VirtualKeyFromKey(key);
                GetKeyboardState(KeyboardState);

                uint ScanCode = MapVirtualKey((uint)VirtualKey, MapType.MAPVK_VK_TO_VSC);
                StringBuilder StringBuilder = new StringBuilder(2);

                int Result = ToUnicode((uint)VirtualKey, ScanCode, KeyboardState, StringBuilder, StringBuilder.Capacity, 0);
                switch (Result)
                {
                    case -1:
                        break;
                    case 0:
                        break;
                    case 1:
                        {
                            ch = StringBuilder[0];
                            break;
                        }
                    default:
                        {
                            ch = StringBuilder[0];
                            break;
                        }
                }
                return ch;
            }
        }
    }
}
