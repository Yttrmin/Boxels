using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.WIC;
using System.Diagnostics;
using System.IO;

namespace BoxelRenderer
{
    public partial class RenderDevice
    {
        public sealed class RenderDevice2D
        {
            private Device Device;
            private DeviceContext Context;
            private ImagingFactory2 Factory;

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
                this.Context = new DeviceContext(this.Device, DeviceContextOptions.EnableMultithreadedOptimizations);
                this.Factory = new ImagingFactory2();
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
        }
    }
}
