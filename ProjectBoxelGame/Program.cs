﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
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
            Trace.WriteLine("Creating RenderForm...");
            RenderForm Window = new RenderForm("Project Boxel (Open PV Editor)");
            Window.FormBorderStyle = FormBorderStyle.Fixed3D;
            Window.MaximizeBox = false;

            Trace.WriteLine("Creating D3D11.1 Renderer... If it crashes here and you're not on Windows 8 you probably need the platform update (http://support.microsoft.com/kb/2670838).");
            var Renderer = new BoxelRenderer.RenderDevice(Window);
            Trace.WriteLine("D3D11.1 boxel renderer created.");
            Window.Show();
            var Serializer = new XmlSerializer(typeof(vbl));
            vbl Level;
            using (var Reader = XmlReader.Create("test_map_big.vbl"))
            {
                Level = (vbl)Serializer.Deserialize(Reader);
            }
            var Manager = new BoxelManager(new BoxelManager.BoxelManagerSettings()
                {
                    Width = Level.properties.width,
                    Length = Level.properties.depth,
                    Height = Level.properties.depth
                });
            foreach (var Voxel in Level.voxels)
            {
                Manager.Add(new BasicBoxel(new Int3(Voxel.x, Voxel.y, Voxel.z), 1), new Int3(Voxel.x, Voxel.y, Voxel.z));
            }
            Trace.WriteLine(String.Format("Done adding Boxels. Total Boxels: {0}     Total Voxels in VBL: {1}", Manager.BoxelCount, Level.voxels.Length));
            Trace.WriteLine(String.Format("Count for good measure: {0}", Manager.AllBoxels.Count()));
            Trace.WriteLine("Close render window to exit.");
            RenderLoop.Run(Window, Renderer.Render);
        }

        private static void LogUnhandledException(Object Sender, UnhandledExceptionEventArgs Args)
        {
            Trace.WriteLine("------------------------UNHANDLED EXCEPTION-------------------------");
            Trace.WriteLine(Args.ExceptionObject.ToString());
            Trace.WriteLine("--------------Press enter to continue (crashing)...-----------------");
            Console.ReadLine();
        }

        private static void SetupOutputRedirects()
        {
            TextWriterTraceListener twtl = new TextWriterTraceListener(Environment.UserName+"-Log-"+(DateTime.UtcNow - DateTime.MinValue).TotalSeconds+".txt");
            twtl.Name = "TextLogger";
            twtl.TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime;

            ConsoleTraceListener ctl = new ConsoleTraceListener(false);
            ctl.TraceOutputOptions = TraceOptions.DateTime;

            Trace.Listeners.Add(twtl);
            Trace.Listeners.Add(ctl);
            Trace.AutoFlush = true;

            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
        }
    }
}
