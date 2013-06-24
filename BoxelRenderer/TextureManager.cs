using CommonDX;
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
    internal class TextureManager
    {
        private IDictionary<string, Texture2D> TextureMap;
        private IList<Texture2D> TexturesByIndex;
        private Device1 Device;
        private ImagingFactory2 ImagingFactory;
        private int Width=-1;
        private int Height=-1;

        public Texture2D this[string Name]
        {
            get
            {
                if (!this.TextureMap.ContainsKey(Name))
                {
                    this.Load(Name);
                }
                return this.TextureMap[Name];
            }
        }

        public TextureManager(Device1 Device)
        {
            this.TextureMap = new Dictionary<string, Texture2D>();
            this.TexturesByIndex = new List<Texture2D>();
            this.Device = Device;
            this.ImagingFactory = new ImagingFactory2();
        }

        public void Load(string Path)
        {
            this.TextureMap[Path] = TextureLoader.CreateTexture2DFromBitmap(this.Device, 
                        TextureLoader.LoadBitmap(this.ImagingFactory, Path), true);
            if (this.TextureMap.Count == 1)
            {
                var Desc = this.TextureMap.Values.ElementAt(0).Description;
                this.Width = Desc.Width;
                this.Height = Desc.Height;
            }
        }

        public int GetTextureIndexInArray(Texture2D Texture)
        {
            return this.TexturesByIndex.IndexOf(Texture);
        }

        public ShaderResourceView GenerateTextureArrayView(out int TextureCount)
        {
            TextureCount = TextureMap.Count;
            var Context = this.Device.ImmediateContext1;
            var DataBoxes = new DataBox[TextureCount];
            int i = 0;
            foreach (var Texture in this.TextureMap.Values)
            {
                DataBoxes[i] = Context.MapSubresource(Texture, 0, MapMode.Read, MapFlags.None);
                this.TexturesByIndex.Add(Texture);
                i++;
            }
            var FinalTexture = new Texture2D(this.Device, new Texture2DDescription()
                {
                    ArraySize = TextureCount,
                    BindFlags=BindFlags.ShaderResource,
                    CpuAccessFlags=CpuAccessFlags.None,
                    Format=SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    OptionFlags=ResourceOptionFlags.None,
                    Usage=ResourceUsage.Immutable,
                    Width=this.Width,
                    Height=this.Height,
                    MipLevels=1,
                    SampleDescription=new SharpDX.DXGI.SampleDescription(1,0),
                }, DataBoxes);
            foreach (var Texture in this.TextureMap.Values)
            {
                Context.UnmapSubresource(Texture, 0);
            }
            return new ShaderResourceView(this.Device, FinalTexture);
        }
    }
}
