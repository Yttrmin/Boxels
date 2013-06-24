using BoxelCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxelCommon
{
    public enum Axis
    {
        PosX,
        NegX,
        PosY,
        NegY,
        PosZ,
        NegZ
    }

    public interface IBoxelType
    {
        
    }

    public interface ICubeBoxelType : IBoxelType
    {
        IDictionary<Axis, string> PerSideTexture {get;}
    }

    public class CubeBoxelType : ICubeBoxelType
    {
        private IDictionary<Axis, string> SideTextures;

        private CubeBoxelType()
        {
            this.SideTextures = new Dictionary<Axis, string>();
        }

        public CubeBoxelType(string PosXTexture, string NegXTexture, string PosYTexture,
            string NegYTexture, string PosZTexture, string NegZTexture)
            : this()
        {
            this.SideTextures[Axis.PosX] = PosXTexture;
            this.SideTextures[Axis.NegX] = NegXTexture;
            this.SideTextures[Axis.PosY] = PosYTexture;
            this.SideTextures[Axis.NegY] = NegYTexture;
            this.SideTextures[Axis.PosZ] = PosZTexture;
            this.SideTextures[Axis.NegZ] = NegZTexture;
        }

        public IDictionary<Axis, string> PerSideTexture { get { return new Dictionary<Axis, string>(this.SideTextures); } }
    }

    public class BoxelTypes<T> where T: IBoxelType
    {
        private IDictionary<int, T> TypeDictionary;

        public BoxelTypes()
        {
            this.TypeDictionary = new Dictionary<int, T>();
        }

        public void Add(T NewType)
        {
            this.TypeDictionary[this.TypeDictionary.Count] = NewType;
        }

        public T GetTypeFromInt(int Type)
        {
            return this.TypeDictionary[Type];
        }

        public IEnumerable<string> GetTextureNames()
        {
            foreach (ICubeBoxelType Type in this.TypeDictionary.Values)
            {
                foreach (var Name in Type.PerSideTexture.Values)
                {
                    yield return Name;
                }
            }
        }
    }
}
