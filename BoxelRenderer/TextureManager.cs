using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelRenderer
{
    internal sealed class TextureManager : IDisposable
    {
        private IDictionary<string, IList<Texture2D>> TextureMap;
        private IList<Texture2D> TexturesByIndex;
        private Device1 Device;
        private ImagingFactory2 ImagingFactory;
        private readonly Size2 Size;
        private const bool UseMipMaps = true;

        public Texture2D this[string Name]
        {
            get
            {
                return this.TextureMap[Name][0];
            }
        }

        public TextureManager(Device1 Device, ImagingFactory2 Factory, Size2 ImageSize)
        {
            this.TextureMap = new Dictionary<string, IList<Texture2D>>();
            this.Device = Device;
            this.Size = ImageSize;
            this.ImagingFactory = Factory;
            this.TexturesByIndex = new List<Texture2D>();
        }

        public void Load(string Path)
        {
            if (this.TextureMap.ContainsKey(Path))
                throw new Exception("Key already exists.");

            using(var Source = LoadBitmap(this.ImagingFactory, Path))
            {
                this.Add(Path, Source);
            }
        }

        public void Add(string Path, BitmapSource Source)
        {
            this.TextureMap[Path] = new Texture2D[this.GetMipMapSizes(Source.Size).Count()];
            int i = 0;
            foreach (var Size in this.GetMipMapSizes(Source.Size))
            {
                using (var Scaler = new BitmapScaler(this.ImagingFactory))
                {
                    Scaler.Initialize(Source, Size.Width, Size.Height, BitmapInterpolationMode.Fant);
                    this.TextureMap[Path][i] = this.CreateTexture2DFromBitmap(this.Device, Scaler);
                    this.TextureMap[Path][i].DebugName = String.Format("{0}__MIP_{1}", Path, i);
                    var Desc = this.TextureMap[Path][i].Description;
                    if (i == 0 && (Desc.Width != this.Size.Width || Desc.Height != this.Size.Height))
                    {
                        this.TextureMap[Path][i].Dispose();
                        this.TextureMap.Remove(Path);
                        throw new InvalidOperationException(String.Format("Tried to load a {0}x{1} texture. This instance only supports {2}x{3}.",
                            Desc.Width, Desc.Height, this.Size.Width, this.Size.Height));
                    }
                }
                i++;
            }
        }

        public int GetTextureIndexInArray(Texture2D Texture)
        {
            return this.TexturesByIndex.IndexOf(Texture);
        }

        public ShaderResourceView GenerateTextureArrayView(out int TextureCount)
        {
            var MipCount = this.TextureMap.Values.ElementAt(0).Count;
            TextureCount = MipCount * this.TextureMap.Count;
            var Context = this.Device.ImmediateContext1;
            var DataBoxes = new DataBox[TextureCount];
            int i = 0;
            foreach (var Textures in this.TextureMap.Values)
            {
                DataBoxes[i] = Context.MapSubresource(Textures[0], 0, MapMode.Read, MapFlags.None);
                this.TexturesByIndex.Add(Textures[0]);
                i++;
                for (int j = 1; j < MipCount; i++ )
                {
                    DataBoxes[i] = Context.MapSubresource(Textures[j], 0, MapMode.Read, MapFlags.None);
                    j++;
                }
            }
            ShaderResourceView FinalTextureView;
            using (var FinalTexture = new Texture2D(this.Device, new Texture2DDescription()
                {
                    ArraySize = this.TextureMap.Count,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    OptionFlags = ResourceOptionFlags.None,
                    Usage = ResourceUsage.Immutable,
                    Width = this.Size.Width,
                    Height = this.Size.Height,
                    MipLevels = MipCount,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                }, DataBoxes))
            {
                FinalTextureView = new ShaderResourceView(this.Device, FinalTexture);
            }
            foreach (var Textures in this.TextureMap.Values)
            {
                foreach (var Texture in Textures)
                {
                    Context.UnmapSubresource(Texture, 0);
                }
            }
            return FinalTextureView;
        }

        /// <summary>
        /// Loads a bitmap using WIC.
        /// </summary>
        /// <param name="deviceManager"></param>
        /// <param name="Filename"></param>
        /// <returns>The image file as a BitmapSource. Make sure to Dispose() of it or use a using statement.</returns>
        public static BitmapSource LoadBitmap(ImagingFactory2 Factory, string Filename)
        {
            var formatConverter = new SharpDX.WIC.FormatConverter(Factory);
            using (var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(Factory, Filename, SharpDX.WIC.DecodeOptions.CacheOnDemand))
            {
                using (var Frame = bitmapDecoder.GetFrame(0))
                {
                    formatConverter.Initialize(Frame, SharpDX.WIC.PixelFormat.Format32bppPRGBA, SharpDX.WIC.BitmapDitherType.None,
                        null, 0.0, SharpDX.WIC.BitmapPaletteType.Custom);
                }
            }
            return formatConverter;
        }

        private Texture2D CreateTexture2DFromBitmap(Device Device, BitmapSource BitmapSource)
        {
            // Allocate DataStream to receive the WIC image pixels
            int stride = BitmapSource.Size.Width * 4;
            
            using (var buffer = new SharpDX.DataStream(BitmapSource.Size.Height * stride, true, true))
            {
                // Copy the content of the WIC to the buffer
                BitmapSource.CopyPixels(stride, buffer);
                return new SharpDX.Direct3D11.Texture2D(Device, new SharpDX.Direct3D11.Texture2DDescription()
                {
                    Width = BitmapSource.Size.Width,
                    Height = BitmapSource.Size.Height,
                    ArraySize = 1,
                    //BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                    Usage = ResourceUsage.Staging,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                }, new SharpDX.DataRectangle(buffer.DataPointer, stride));
            }
        }

        private IEnumerable<Size2> GetMipMapSizes(int Width, int Height)
        {
            if (!UseMipMaps)
            {
                yield return new Size2(Width, Height);
            }
            else
            {
                while (Width > 1 || Height > 1)
                {
                    yield return new Size2(Width, Height);
                    Width /= 2;
                    Height /= 2;
                }
            }
        }

        private IEnumerable<Size2> GetMipMapSizes(Size2 Size)
        {
            return this.GetMipMapSizes(Size.Width, Size.Height);
        }

        private void Dispose(bool Disposing)
        {
            foreach (var Textures in this.TextureMap.Values)
            {
                foreach (var Texture in Textures)
                {
                    Texture.Dispose();
                }
            }
            foreach (var Texture in this.TexturesByIndex)
            {
                Texture.Dispose();
            }
            if (Disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        ~TextureManager()
        {
            this.Dispose(false);
        }
    }
}
