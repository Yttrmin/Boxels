using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Windows;
using SharpDX.Direct3D11;
using Device1 = SharpDX.DXGI.Device1;

namespace BoxelRenderer
{
    public class RenderDevice
    {
        private Adapter2 Adapter;
        private Factory2 Factory;
        private SharpDX.Direct3D11.Device1 D3DDevice;
        private DeviceContext1 ImmediateContext;
        private Device2 DXGIDevice;
        private SwapChain1 SwapChain;
        private RenderTargetView BackBuffer;
        private bool PlatformUpdate;

        public RenderDevice(RenderForm Window)
        {
            this.InitializeDirect3D(Window);
        }

        private void InitializeDirect3D(RenderForm Window)
        {
            Trace.WriteLine("------------------Start D3D11.1-----------------------------");
            this.PlatformUpdate = Environment.OSVersion.Version < new Version(6, 2);
            Trace.WriteLine("Creating DXGI1.1 Factory...");
            var Factory1 = new Factory1();
            Trace.WriteLine("Querying to DXGI1.2 Factory. Crashing here means no DXGI1.2 support, which isn't supported below Windows 7 Platform Update.");
            this.Factory = Factory1.QueryInterface<Factory2>();
            Trace.WriteLine("Success. The rest should likely work, and won't be done in multiple steps.");
            Trace.WriteLine("Creating DXGI1.2 Adapter (from DXGI1.1)...");
            this.Adapter = this.Factory.GetAdapter1(0).QueryInterface<Adapter2>();
            Trace.WriteLine("Success. Creating Direct3D11.1 Device (from Direct3D11)...");
            var SwapDesc = new SwapChainDescription1()
                {
                    BufferCount = 2,
                    Width = 0,
                    Height = 0,
                    Scaling = this.PlatformUpdate ? Scaling.Stretch : Scaling.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SwapEffect = SwapEffect.FlipSequential,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = Usage.RenderTargetOutput
                };
            var FeatureLevels = new FeatureLevel[]
                {FeatureLevel.Level_11_1, FeatureLevel.Level_11_0, FeatureLevel.Level_10_1, FeatureLevel.Level_10_0};
            var OldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            if (this.PlatformUpdate)
            {
                Trace.WriteLine("Not running on Windows 8 or above. No Level_11_1 even if hardware supports it.");
            }
            var Flags = DeviceCreationFlags.BgraSupport;
#if DEBUG
            //Flags |= this.PlatformUpdate ? DeviceCreationFlags.None : DeviceCreationFlags.Debug;
#endif
            this.D3DDevice = new SharpDX.Direct3D11.Device(this.Adapter, Flags)
                .QueryInterface<SharpDX.Direct3D11.Device1>();
            this.ImmediateContext = this.D3DDevice.ImmediateContext1;
            Trace.WriteLine(String.Format("Success. Feature Level: {0}", this.D3DDevice.FeatureLevel));
            Console.ForegroundColor = OldColor;
            Trace.WriteLine("Creating DXGI1.2 Device...");
            this.DXGIDevice = this.D3DDevice.QueryInterface<Device2>();
            Trace.WriteLine("Success... Creating DXGI1.1 SwapChain...");
            this.SwapChain = this.Factory.CreateSwapChainForHwnd(this.DXGIDevice, Window.Handle, ref SwapDesc, null, null);
            var BackBufferTexture = this.SwapChain.GetBackBuffer<Texture2D>(0);
            this.BackBuffer = new RenderTargetView(this.D3DDevice, BackBufferTexture);
            Trace.WriteLine("Success.");
            this.Render();
            Trace.WriteLine("-------------------End D3D11.1------------------------------");
        }

        public void Render()
        {
            //this.ImmediateContext.OutputMerger.SetTargets(this.BackBuffer);
            this.ImmediateContext.ClearRenderTargetView(this.BackBuffer, Color.HotPink);
            this.SwapChain.Present(1, PresentFlags.None);
        }
    }
}
