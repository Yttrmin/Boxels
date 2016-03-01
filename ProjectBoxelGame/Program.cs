using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using BoxelCommon;
using BoxelLib;
using System;
using BoxelRenderer;
using SharpDX;
using SharpDX.Windows;
using VBL;

namespace ProjectBoxelGame
{
    class Program
    {
        static void Main(string[] args)
        {
            SetupOutputRedirects();
            Trace.WriteLine(String.Format("Project Boxel v{0}", FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion));
            Trace.WriteLine("Creating RenderForm...");

            var TestLevel = new ConstantRandomContainer();
            TestLevel.Add(new BasicBoxel(Int3.Zero, 16, 0), Int3.Zero);
            TestLevel.Add(new BasicBoxel(new Int3(0,1,0), 16, 0), new Int3(0, 1, 0));
            TestLevel.Add(new BasicBoxel(new Int3(1, 1, 0), 16, 0), new Int3(1, 1, 0));
            TestLevel.Add(new BasicBoxel(new Int3(0, 0, 1), 16, 0), new Int3(0, 0, 1));
            using (var Game = new Game(LoadBoxels("level.bin")))
            {
                Trace.WriteLine("Close render window to exit.");
                Game.Run();
            }
            Trace.WriteLine(String.Format("Exiting normally at {0}", DateTime.Now.ToString()));
        }
        
        private static IBoxelContainer LoadBoxels(string Filename)
        {
            using ( var LoadFile = File.Open(Filename, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                return ConstantRandomContainer.Load(LoadFile);
            }
        }
        
        private static vbl LoadVBL(string Filename)
        {
            var Serializer = new XmlSerializer(typeof(vbl));
            using (var Reader = XmlReader.Create(Filename))
            {
                return (vbl)Serializer.Deserialize(Reader);
            }
        }

        private static void LogUnhandledException(Object Sender, UnhandledExceptionEventArgs Args)
        {
            Trace.WriteLine("------------------------UNHANDLED EXCEPTION-------------------------");
            Trace.WriteLine(Args.ExceptionObject.ToString());
#if !DEBUG
            Trace.WriteLine("--------------Press enter to continue (crashing)...-----------------");
            Console.ReadLine();
#endif
        }

        private static void SetupOutputRedirects()
        {
            using (TextWriterTraceListener twtl = new TextWriterTraceListener(Environment.UserName + "-Log-" + (DateTime.UtcNow - DateTime.MinValue).TotalSeconds + ".txt"))
            {
                twtl.Name = "TextLogger";
                twtl.TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime;

                using (ConsoleTraceListener ctl = new ConsoleTraceListener(false))
                {
                    ctl.TraceOutputOptions = TraceOptions.DateTime;

                    Trace.Listeners.Add(twtl);
                    Trace.Listeners.Add(ctl);
                    Trace.AutoFlush = true;
                }
            }

            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
            Trace.WriteLine(String.Format("Log started at {0}", DateTime.Now.ToString()));
        }
    }
}
