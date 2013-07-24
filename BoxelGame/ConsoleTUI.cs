using BoxelCommon;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keys = System.Windows.Forms.Keys;

namespace BoxelGame
{
    public class ConsoleTUI : ITickable
    {
        private DeveloperConsole Console;
        private Input Input;
        private string InputString;
        private StringBuilder ConsoleHistory;
        private RectangleF ConsoleArea, CursorArea, InputTextArea, HistoryArea;
        public bool IsOpen { get; private set; }
        public ConsoleTUI(DeveloperConsole Console, BoxelRenderer.RenderDevice.RenderDevice2D RenderDevice)
        {
            this.Console = Console;
            this.ConsoleHistory = new StringBuilder();
            ConsoleArea = new RectangleF(0, 0, RenderDevice.Width, RenderDevice.Height * 0.25f);
            HistoryArea = new RectangleF(0, 0, RenderDevice.Width, RenderDevice.Height * 0.25f - 16);
            CursorArea = new RectangleF(0, RenderDevice.Height * 0.25f - 16, 16, 16);
            InputTextArea = new RectangleF(16, RenderDevice.Height * 0.25f - 16, RenderDevice.Width, 16);
        }
        public void Render(BoxelRenderer.RenderDevice.RenderDevice2D RenderDevice)
        {
            RenderDevice.FillRectangle(ConsoleArea, Color.Black);
            RenderDevice.DrawText(">", CursorArea, Color.White);
            RenderDevice.DrawText(this.InputString, InputTextArea, Color.White);
            RenderDevice.DrawText(this.ConsoleHistory.ToString(), this.HistoryArea, Color.White);
        }
        public void Tick(double DeltaTime)
        {
            if(IsOpen)
            {
                this.InputString += this.Input.TextInput;
                if (this.Input.WasPressed(Keys.Enter))
                    this.Execute();
                this.Input.ResetKeyPresses();
            }
        }
        public void Open(Input Input)
        {
            this.Input = Input;
            this.InputString = String.Empty;
            this.IsOpen = true;
        }
        public void Close()
        {
            this.Input = null;
            this.IsOpen = false;
        }
        private void Execute()
        {
            var Tokens = this.InputString.Split(' ');
            if (Tokens.Length > 0)
            {
                var Command = Tokens[0];
                var Result = this.Console.Execute(Command, null);
                this.ConsoleHistory.AppendLine("> " + Command);
                if(Result != null)
                {
                    this.ConsoleHistory.AppendLine(Result);
                }
            }
            this.InputString = String.Empty;
        }

        private void ExtractParameters(string[] SplitText)
        {

        }

        private string[] ConcatenateStringParameters(string[] SplitText)
        {
            throw new NotImplementedException();
            bool InText = false;
            int TextStart = int.MinValue;
            for(var i = 1; i < SplitText.Length; i++)
            {
                var Text = SplitText[i];
                if(Text.StartsWith("\""))
                {
                    if (InText)
                        return null;
                    InText = true;
                    TextStart = i;
                }
                else if(Text.EndsWith("\""))
                {
                    if (!InText)
                        return null;
                    InText = false;
                    //
                }
            }
        }
    }
}
