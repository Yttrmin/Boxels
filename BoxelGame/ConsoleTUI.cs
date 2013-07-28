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
        private Vector2 DividerPoint0, DividerPoint1;
        [ConsoleCommand]
        private int ConsoleHeight { get; set; }
        public bool IsOpen { get; private set; }
        public ConsoleTUI(DeveloperConsole Console, BoxelRenderer.RenderDevice.RenderDevice2D RenderDevice)
        {
            this.Console = Console;
            this.ConsoleHistory = new StringBuilder();
            ConsoleArea = new RectangleF(0, 0, RenderDevice.Width, RenderDevice.Height * 0.25f);
            HistoryArea = new RectangleF(0, 0, RenderDevice.Width, RenderDevice.Height * 0.25f - 16);
            CursorArea = new RectangleF(0, RenderDevice.Height * 0.25f - 16, 16, 16);
            InputTextArea = new RectangleF(16, RenderDevice.Height * 0.25f - 16, RenderDevice.Width, 16);
            DividerPoint0 = new Vector2(0, RenderDevice.Height * 0.25f - 16);
            DividerPoint1 = new Vector2(RenderDevice.Width, RenderDevice.Height * 0.25f - 16);
        }
        public void Render(BoxelRenderer.RenderDevice.RenderDevice2D RenderDevice)
        {
            RenderDevice.FillRectangle(ConsoleArea, Color.Black);
            RenderDevice.DrawLine(DividerPoint0, DividerPoint1, Color.White);
            RenderDevice.DrawText(">", CursorArea, Color.White);
            RenderDevice.DrawText(this.InputString, InputTextArea, Color.White);
            RenderDevice.DrawText(this.ConsoleHistory.ToString(), this.HistoryArea, Color.White);
        }

        public void Tick(double DeltaTime)
        {
            if(IsOpen)
            {
                this.InputString = this.Input.TextInput;
                if (this.Input.WasPressed(Keys.Enter))
                {
                    this.Execute();
                    this.Input.ResetInputString();
                    this.InputString = String.Empty;
                }
                this.Input.ResetKeyPresses();
            }
        }

        public void Open(Input Input)
        {
            this.Input = Input;
            this.Input.BuildTextInput = true;
            this.InputString = String.Empty;
            this.IsOpen = true;
        }

        public void Close()
        {
            this.Input.BuildTextInput = false;
            this.Input = null;
            this.IsOpen = false;
        }

        public void Print(string Text)
        {
            this.ConsoleHistory.AppendLine(Text);
        }

        [ConsoleCommand]
        public void Clear()
        {
            this.ConsoleHistory.Clear();
        }

        //@TODO - Clean up. A lot.
        private void Execute()
        {
            System.Diagnostics.Trace.WriteLine(String.Format("Console: {0}", this.InputString));
            var Tokens = this.ConcatenateStringParameters(this.InputString.Split(' '));
            this.Print("> " + this.InputString);
            this.InputString = String.Empty;
            if (Tokens.Length > 0)
            {
                String[] StringParameters = null;
                if (Tokens.Length > 1)
                {
                    StringParameters = new String[Tokens.Length - 1];
                    Array.Copy(Tokens, 1, StringParameters, 0, StringParameters.Length);
                }
                else
                    StringParameters = new String[0];
                Object[] Parameters = new Object[0];
                var Command = Tokens[0];
                var Info = this.Console.Lookup(Command, StringParameters);
                if (Info == null)
                {
                    this.Print("No such command found.");
                    return;
                }
                if (Tokens.Length > 1)
                {
                    string ErrorMessage;
                    Parameters = ExtractParameters(Info, StringParameters, out ErrorMessage);
                    if(ErrorMessage != null)
                    {
                        this.Print(ErrorMessage);
                        return;
                    }
                }
                var Result = this.Console.Execute(Command, Parameters);
                if(Result != String.Empty)
                {
                    this.ConsoleHistory.AppendLine(Result);
                }
            }
        }

        private Object[] ExtractParameters(ConsoleCommandInfo Info, string[] SplitText, out string ErrorMessage)
        {
            var Result = new Object[SplitText.Length];
            int i = 0;
            foreach(var Parameter in Info.Method.GetParameters())
            {
                System.Diagnostics.Trace.WriteLine(String.Format("Converting parameter {0} of type {1}...", i, Parameter.ParameterType));
                try
                {
                    Result[i] = Convert.ChangeType(SplitText[i], Parameter.ParameterType);
                }
                catch(FormatException)
                {
                    ErrorMessage = String.Format(@"Call aborted. Parameter {0}, value ({1}) could not be converted to type ""{2}.{3}"".",
                        i + 1, SplitText[i], Parameter.ParameterType.Namespace, Parameter.ParameterType.Name);
                    return null;
                }
                catch (OverflowException)
                {
                    ErrorMessage = String.Format(@"Call aborted. Parameter {0}, value ({1}) can not fit inside type ""{2}.{3}"".",
                        i + 1, SplitText[i], Parameter.ParameterType.Namespace, Parameter.ParameterType.Name);
                    return null;
                }
                i++;
            }
            ErrorMessage = null;
            return Result;
        }

        private string[] ConcatenateStringParameters(string[] SplitText)
        {
            IList<string> Result = new List<string>();
            bool InText = false;
            int TextStart = int.MinValue;
            for(var i = 0; i < SplitText.Length; i++)
            {
                var Text = SplitText[i];
                if(Text.StartsWith("\""))
                {
                    if (InText)
                        return null;
                    InText = true;
                    TextStart = i;
                    SplitText[i] = SplitText[i].Substring(1);
                }
                if(Text.EndsWith("\""))
                {
                    if (!InText)
                        return null;
                    SplitText[i] = SplitText[i].Substring(0, SplitText[i].Length - 1);
                    var Builder = new StringBuilder();
                    for(var u = TextStart; u <= i; u++)
                    {
                        Builder.Append(SplitText[u]);
                        Builder.Append(" ");
                    }
                    Result.Add(Builder.ToString());
                    InText = false;
                    TextStart = int.MinValue;
                }
                else if(!InText)
                {
                    Result.Add(Text);
                }
            }
            return Result.ToArray();
        }
    }
}
