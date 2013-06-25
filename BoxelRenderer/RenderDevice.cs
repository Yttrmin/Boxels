﻿using System;
using System.Diagnostics;
using System.Text;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Windows;
using SharpDX.Direct3D11;
using Device1 = SharpDX.DXGI.Device1;

namespace BoxelRenderer
{
    public sealed partial class RenderDevice
    {
        private Adapter2 Adapter;
        private Factory2 Factory;
        public SharpDX.Direct3D11.Device1 D3DDevice { get; private set; }
        public RenderDevice2D Device2D { get; private set; }
        private DeviceContext1 ImmediateContext;
        private Device2 DXGIDevice;
        private SwapChain1 SwapChain;
        private RenderTargetView BackBuffer;
        private DepthStencilView DepthBuffer;
        private ViewportF Viewport;
        private bool PlatformUpdate;
        private Stopwatch FPSWatch;
        /// <summary>
        /// Current number of frames rendered in the past second. Used for calculating FPS.
        /// </summary>
        private int FrameCount;
        /// <summary>
        /// Total frames rendered over the lifetime of the program.
        /// </summary>
        private ulong TotalFrameCount;
        private const bool UseFlipSequential = false;
        private Color ClearColor;
        public double FrameRate { get; private set; }

        public RenderDevice(RenderForm Window)
        {
            this.InitializeDirect3D(Window);
            this.Device2D = new RenderDevice2D(this.DXGIDevice);
            this.FPSWatch = new Stopwatch();
            this.FPSWatch.Start();
            this.D3DDevice.ImmediateContext1.Rasterizer.State = new RasterizerState1(this.D3DDevice, new RasterizerStateDescription1()
                {
                    CullMode = CullMode.Back,
                    FillMode = FillMode.Solid
                });
            this.ClearColor = new Color(119, 228, 255);
        }

        public void Render()
        {
            this.FrameCount++;
            this.TotalFrameCount++;
            var Elapsed = (double)FPSWatch.ElapsedTicks / (double)Stopwatch.Frequency;
            if (Elapsed >= 1.0f)
            {
                this.FrameRate = FrameCount / Elapsed;
                this.FrameCount = 0;
                Trace.WriteLine(String.Format("FPS: {0}", this.FrameRate));
                FPSWatch.Restart();
            }
            this.SwapChain.Present(0, PresentFlags.None);
            this.ImmediateContext.ClearRenderTargetView(this.BackBuffer, this.ClearColor);
            this.ImmediateContext.ClearDepthStencilView(this.DepthBuffer, DepthStencilClearFlags.Depth, 1, 0);
            this.ImmediateContext.OutputMerger.SetTargets(this.DepthBuffer, this.BackBuffer);
        }

        public void Resize(int NewWidth, int NewHeight)
        {
            System.Diagnostics.Trace.WriteLine(String.Format("Resizing from {0}x{1} to {2}x{3}", this.SwapChain.Description1.Width,
                this.SwapChain.Description1.Height, NewWidth, NewHeight));
            this.BackBuffer.Dispose();
            this.DepthBuffer.Dispose();

            this.SwapChain.ResizeBuffers(2, NewWidth, NewHeight, Format.B8G8R8A8_UNorm, SwapChainFlags.None);
            var BackBufferTexture = this.SwapChain.GetBackBuffer<Texture2D>(0);
            this.BackBuffer = new RenderTargetView(this.D3DDevice, BackBufferTexture);
            BackBufferTexture.Dispose();
            this.InitializeDepthBuffer(NewWidth, NewHeight);
            this.InitializeViewport();
            this.ImmediateContext.OutputMerger.SetRenderTargets(this.DepthBuffer, this.BackBuffer);
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
                    Scaling = this.PlatformUpdate || !UseFlipSequential ? Scaling.Stretch : Scaling.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Stereo = false,
                    SwapEffect = UseFlipSequential ? SwapEffect.FlipSequential : SwapEffect.Sequential,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = Usage.RenderTargetOutput
                };
            var FeatureLevels = new[]
                {FeatureLevel.Level_11_1, FeatureLevel.Level_11_0, FeatureLevel.Level_10_1, FeatureLevel.Level_10_0};
            var OldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            if (this.PlatformUpdate)
            {
                Trace.WriteLine("Not running on Windows 8 or above. No Level_11_1 even if hardware supports it.");
            }
            var Flags = DeviceCreationFlags.BgraSupport;
#if DEBUG
            Flags |= this.PlatformUpdate ? DeviceCreationFlags.None : DeviceCreationFlags.Debug;
#endif
            this.D3DDevice = new SharpDX.Direct3D11.Device(this.Adapter, Flags)
                .QueryInterface<SharpDX.Direct3D11.Device1>();
            this.D3DDevice.DebugName = "D3DDevice";
            this.ImmediateContext = this.D3DDevice.ImmediateContext1;
            this.ImmediateContext.DebugName = "ImmediateContext";
            Trace.WriteLine(String.Format("Success. Feature Level: {0}", this.D3DDevice.FeatureLevel));
            Console.ForegroundColor = OldColor;
            Trace.WriteLine("Creating DXGI1.2 Device...");
            this.DXGIDevice = this.D3DDevice.QueryInterface<Device2>();
            Trace.WriteLine("Success... Creating DXGI1.1 SwapChain...");
            this.SwapChain = this.Factory.CreateSwapChainForHwnd(this.DXGIDevice, Window.Handle, ref SwapDesc, null, null);
            this.SwapChain.DebugName = "SwapChain";
            var BackBufferTexture = this.SwapChain.GetBackBuffer<Texture2D>(0);
            this.BackBuffer = new RenderTargetView(this.D3DDevice, BackBufferTexture);
            this.BackBuffer.DebugName = "BackBufferRTV";
            this.ImmediateContext.OutputMerger.SetTargets(this.BackBuffer);
            Trace.WriteLine("Success.");
            //Trace.WriteLine(this.GetFeaturesString());
            this.InitializeViewport();
            this.InitializeDepthBuffer(BackBufferTexture.Description.Width, BackBufferTexture.Description.Height);
            BackBufferTexture.Dispose();
            Trace.WriteLine("-------------------End D3D11.1------------------------------");
        }

        private void InitializeViewport()
        {
            this.Viewport = new Viewport(0, 0, this.SwapChain.Description1.Width,
                this.SwapChain.Description1.Height, 0, 1);
            this.ImmediateContext.Rasterizer.SetViewport(this.Viewport);
        }

        private void InitializeDepthBuffer(int Width, int Height)
        {
            var DepthBufferTexture = new Texture2D(this.D3DDevice, new Texture2DDescription()
                {
                    Width = Width,
                    Height = Height,
                    ArraySize = 1,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1,0),
                    Format = Format.D24_UNorm_S8_UInt,
                    BindFlags = BindFlags.DepthStencil,
                });
            this.DepthBuffer = new DepthStencilView(this.D3DDevice, DepthBufferTexture);
            DepthBufferTexture.Dispose();
        }

        private string GetFeaturesString()
        {
            var Builder = new StringBuilder();
            Builder.AppendLine("==========Printing Features.==========");
            Builder.AppendFormat("Feature Level: {0}", this.D3DDevice.FeatureLevel);
            Builder.AppendLine();
            Builder.AppendLine("*Importantest:");
            bool ConcurrentResources, CommandList;
            this.D3DDevice.CheckThreadingSupport(out ConcurrentResources, out CommandList);
            Builder.AppendFormat("Concurrent Resource Creation support: {0}", ConcurrentResources);
            Builder.AppendLine();
            Builder.AppendFormat("Command List support: {0}", CommandList);
            Builder.AppendLine();
            Builder.AppendFormat("Compute Shader support: {0}",
                                this.D3DDevice.CheckFeatureSupport(Feature.ComputeShaders));
            Builder.AppendLine();
            Builder.AppendLine("*Other:");
            var Features = this.D3DDevice.CheckD3D11Feature();
            Builder.AppendFormat("Non-power-of-2 texture support: {0}", this.D3DDevice.CheckFullNonPow2TextureSupport());
            Builder.AppendLine();
            Builder.AppendFormat("Partial update of constant buffers support: {0}", Features.ConstantBufferPartialUpdate);
            Builder.AppendLine();
            Builder.AppendFormat("Offset into constant buffers support: {0}", Features.ConstantBufferOffsetting);
            Builder.AppendLine();
            Builder.AppendFormat("Extended resource sharing support: {0}", Features.ExtendedResourceSharing);
            Builder.AppendLine();
            Builder.AppendFormat("Can map no-overwrite dynamic SRVs: {0}", Features.MapNoOverwriteOnDynamicBufferSRV);
            Builder.AppendLine();
            Builder.AppendFormat("Can map no-overwrite dynamic constant buffers: {0}", Features.MapNoOverwriteOnDynamicConstantBuffer);
            Builder.AppendLine();
            Builder.AppendFormat("Can call DeviceContext1.ClearView(): {0}", Features.ClearView);
            Builder.AppendLine();
            Builder.AppendFormat("Can call DeviceContext1.CopySubsourceRegion1() with overlaps: {0}",
                                 Features.CopyWithOverlap);
            Builder.AppendLine();
            Builder.AppendFormat("Logic operations supported in blend state: {0}", Features.OutputMergerLogicOp);
            Builder.AppendLine();
            Builder.AppendFormat("UAV-only rendering: {0}", Features.UAVOnlyRenderingForcedSampleCount);
            Builder.AppendLine();
            Builder.AppendFormat("SAD4 instruction support: {0}", Features.SAD4ShaderInstructions);
            Builder.AppendLine();
            Builder.AppendFormat("Extended doubles instruction support: {0}", Features.ExtendedDoublesShaderInstructions);
            Builder.AppendLine();
            Builder.AppendFormat("Driver supports *.Discard*() functions: {0}", Features.DiscardAPIsSeenByDriver);
            Builder.AppendLine();
            Builder.AppendFormat("Driver supports CopyFlags: {0}", Features.FlagsForUpdateAndCopySeenByDriver);
            Builder.AppendLine();
            Builder.AppendLine("=========End Features.==========");
            return Builder.ToString();
        }
    }
}
