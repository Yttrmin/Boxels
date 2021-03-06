﻿using BoxelCommon;
using BoxelRenderer;
using SharpDX;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keys = System.Windows.Forms.Keys;

namespace BoxelGame
{
    public sealed class ConsoleTUI : ITickable
    {
        private DeveloperConsole Console;
        private Input Input;
        private string InputString;
        private RectangleF ConsoleArea, CursorArea, InputTextArea, HistoryArea;
        private Vector2 DividerPoint0, DividerPoint1;
        private readonly Color ConsoleBackgroundColor;
        private TextLayout ConsoleHistoryLayout;
        private float HorizontalSizePerCharacter;
        private readonly IList<string> HistoryLines;
        private readonly IList<string> InputHistory;
        private int HistoryIndex;
        private int _ConsoleLineHeight;
        [ConsoleCommand]
        private int ConsoleLineHeight { get { return _ConsoleLineHeight; } set { _ConsoleLineHeight = value; ConsoleBoundsNeedUpdate = true; } }
        private bool ConsoleBoundsNeedUpdate;
        public bool IsOpen { get; private set; }
        public ConsoleTUI(DeveloperConsole Console, RenderDevice2D RenderDevice)
        {
            this.Console = Console;
            this.ConsoleLineHeight = 16;
            this.ConsoleBackgroundColor = new Color(0, 0, 0, 128);
            this.HorizontalSizePerCharacter = RenderDevice.GetHorizontalSize(RenderDevice.DefaultFont);
            this.HistoryLines = new List<string>();
        }
        public void Render(RenderDevice2D RenderDevice)
        {
            if(this.ConsoleBoundsNeedUpdate)
            {
                this.CalculateConsoleBounds(RenderDevice);
                this.ConsoleBoundsNeedUpdate = false;
            }
            //@TODO - Dispose. Only update when dirty.
            var Builder = new StringBuilder();
            var StartIndex = Math.Max(this.HistoryIndex - this.ConsoleLineHeight, 0);
            for (var i = StartIndex; i < this.HistoryIndex; i++ )
            {
                Builder.AppendLine(this.HistoryLines[i]);
            }
            this.ConsoleHistoryLayout = new TextLayout(RenderDevice.DWriteFactory, Builder.ToString(), RenderDevice.DefaultFont,
                    HistoryArea.Right, HistoryArea.Bottom);
            RenderDevice.FillRectangle(ConsoleArea, this.ConsoleBackgroundColor);
            RenderDevice.DrawLine(DividerPoint0, DividerPoint1, Color.White);
            RenderDevice.DrawText(">", CursorArea, Color.White);
            RenderDevice.DrawText(this.InputString, InputTextArea, Color.White);
            RenderDevice.DrawTextLayout(ConsoleHistoryLayout, Vector2.Zero, Color.White);
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
                if (this.Input.WasPressed(Keys.PageUp))
                    this.Scroll(-1);
                if (this.Input.WasPressed(Keys.PageDown))
                    this.Scroll(1);
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
            foreach (var Line in RenderDevice2D.BreakIntoLines(Text, (int)Math.Floor(this.HistoryArea.Right / this.HorizontalSizePerCharacter)))
                this.HistoryLines.Add(Line);
            this.HistoryIndex = this.HistoryLines.Count;
        }

        [ConsoleCommand]
        public void Clear()
        {
            this.HistoryLines.Clear();
            this.HistoryIndex = 0;
        }

        private void Scroll(int Delta)
        {
            this.HistoryIndex = Math.Min(this.HistoryLines.Count, Math.Max(this.ConsoleLineHeight, this.HistoryIndex + Delta));
        }

        private void CalculateConsoleBounds(RenderDevice2D RenderDevice)
        {
            this.ConsoleArea = new RectangleF(0, 0, RenderDevice.Width, RenderDevice2D.GetVerticalSpaceNeeded(RenderDevice.DefaultFont, this.ConsoleLineHeight + 1));
            this.HistoryArea = new RectangleF(0, 0, RenderDevice.Width, RenderDevice2D.GetVerticalSpaceNeeded(RenderDevice.DefaultFont, this.ConsoleLineHeight));
            this.CursorArea = new RectangleF(0, this.HistoryArea.Bottom, 16, 20);
            this.InputTextArea = new RectangleF(this.CursorArea.X + 16, this.CursorArea.Y, 256, 20);
            this.DividerPoint0 = new Vector2(0, this.HistoryArea.Bottom);
            this.DividerPoint1 = new Vector2(RenderDevice.Width, this.DividerPoint0.Y);
        }

        //@TODO - Clean up. A lot.
        private void Execute()
        {
            System.Diagnostics.Trace.WriteLine(String.Format("Console: {0}", this.InputString));        
            this.Print("> " + this.InputString);
            if (String.IsNullOrWhiteSpace(this.InputString))
                return;
            var Tokens = this.ConcatenateStringParameters(this.InputString.Split(' '));
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
                    if (this.Console.CommandNameExists(Command))
                        this.Print(String.Format("No overload exists that takes {0} parameters.", StringParameters.Length));
                    else
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
                    this.Print(Result);
                }
            }
        }

        private Object[] ExtractParameters(ConsoleCommandInfo Info, string[] SplitText, out string ErrorMessage)
        {
            var Result = new Object[SplitText.Length];
            int i = 0;
            foreach(var Parameter in Info.Method.GetParameters())
            {
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
