﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct2D1.Effects;
using SharpDX.DirectWrite;
using SharpDX.WIC;
using System.Diagnostics;
using System.IO;

namespace BoxelRenderer
{
    public partial class RenderDevice
    {
        public sealed class RenderDevice2D : IDisposable
        {
            private Device Device;
            private DeviceContext Context;
            private SharpDX.DirectWrite.Factory1 DWriteFactory;
            private SolidColorBrush DefaultBrush;
            private TextFormat DefaultFont;
            public ImagingFactory2 Factory { get; private set; }

            public RenderDevice2D(SharpDX.DXGI.Device2 DXGIDevice)
            {
                Trace.WriteLine("Initializing Direct2D1...");
                this.Device = new Device(DXGIDevice, new CreationProperties()
                {
#if DEBUG
                    DebugLevel=DebugLevel.Information,
#else
                    DebugLevel = DebugLevel.None,
#endif
                    Options = DeviceContextOptions.EnableMultithreadedOptimizations,
                    ThreadingMode = ThreadingMode.MultiThreaded,
                });
                this.Factory = new ImagingFactory2();
                Trace.WriteLine("Done.");
                Trace.WriteLine("Initializing DirectWrite...");
                this.DWriteFactory = new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared).QueryInterface<SharpDX.DirectWrite.Factory1>();
                this.DefaultFont = new TextFormat(this.DWriteFactory, "Consolas", 36);
                Trace.WriteLine("Done.");
            }

            public void SaveTexture2DToFile(string FileName, SharpDX.Direct3D11.Texture2D Texture)
            {
                var Bitmap = new Bitmap1(this.Context, Texture.QueryInterface<SharpDX.DXGI.Surface2>());
                var BitmapEncoder = new BitmapEncoder(Factory, ContainerFormatGuids.Png);
                using (var File = new FileStream(FileName, FileMode.Create, FileAccess.Write))
                {
                    BitmapEncoder.Initialize(File);

                    var FrameEncoder = new BitmapFrameEncode(BitmapEncoder);
                    FrameEncoder.Initialize();

                    var ImageEncoder = new ImageEncoder(Factory, Device);

                    ImageEncoder.WriteFrame(Bitmap, FrameEncoder, new ImageParameters(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        AlphaMode.Ignore),
                        Bitmap.DotsPerInch.Width, Bitmap.DotsPerInch.Height, 0, 0, (int)Bitmap.PixelSize.Width, (int)Bitmap.PixelSize.Height)); ;

                    FrameEncoder.Commit();
                    BitmapEncoder.Commit();
                }
            }

            public void SetRenderTarget(SharpDX.DXGI.Surface2 NewTarget)
            {
                if (NewTarget != null)
                {
                    this.CreateContext(NewTarget);
                    NewTarget.Dispose();
                }
                else if(this.Context != null)
                {
                    this.DestroyContext();
                }
            }

            public void DrawText(string Text, RectangleF Position)
            {
                this.Context.BeginDraw();
                this.Context.DrawText(Text, this.DefaultFont, Position, this.DefaultBrush);
                this.Context.EndDraw();
            }

            public void Draw()
            {
                var SourceEffect = new Turbulence(this.Context);
                var BlurEffect = new DirectionalBlur(this.Context);
                BlurEffect.SetInputEffect(0, SourceEffect);
                var R = new Random();
                BlurEffect.Angle = R.NextFloat(0, 360);
                BlurEffect.BorderMode = BorderMode.Soft;
                BlurEffect.StandardDeviation = R.NextFloat(0, 20);
                this.Context.BeginDraw();
                //this.Context.Clear(Color.HotPink);
                this.Context.DrawImage(BlurEffect);
                this.Context.DrawEllipse(new Ellipse(new Vector2(100, 100), 200, 300), this.DefaultBrush);
                this.Context.EndDraw();
            }


            private void CreateContext(SharpDX.DXGI.Surface2 NewTarget)
            {
                if (this.Context != null)
                {
                    this.DestroyContext();
                }
                this.Context = new DeviceContext(NewTarget);
                this.DefaultBrush = new SolidColorBrush(this.Context, Color.Red);
            }

            private void DestroyContext()
            {
                this.DefaultBrush.Dispose();
                this.Context.Dispose();
                this.Context = null;
                this.DefaultBrush = null;
            }

            private void Dispose(bool Disposing)
            {
                Trace.WriteLine("Disposing RenderDevice2D...");
                this.Factory.Dispose();
                this.DefaultFont.Dispose();
                this.DefaultBrush.Dispose();
                this.DWriteFactory.Dispose();
                this.Context.Dispose();
                this.Device.Dispose();
                if (Disposing)
                {
                    GC.SuppressFinalize(this);
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
            }

            ~RenderDevice2D()
            {
                this.Dispose(false);
            }
        }
    }
}
