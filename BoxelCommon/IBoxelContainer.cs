﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace BoxelLib
{
    public interface IBoxelContainer
    {
        IBoxel AtOrDefault(Int3 Position);
        void Add(IBoxel Boxel, Int3 Position);
        IEnumerable<IBoxel> AllBoxels { get; }
        IEnumerable<IBoxel> BoxelsInRadius(Byte3 ChunkPosition, int RadiusInBoxels);
        int Count { get; }
        void Compact();
    }

    public class ConstantRandomContainer : IBoxelContainer
    {
        internal const int ChunkSize = 16;
        private readonly IDictionary<int, ConstantChunk> Chunks;

        public ConstantRandomContainer()
        {
            this.Chunks = new Dictionary<int, ConstantChunk>();
        }

        public int ChunkCount
        {
            get { return this.Chunks.Count; }
        }

        public int Count
        {
            get
            {
                var Result = 0;
                foreach (var Chunk in this.Chunks.Values)
                {
                    Result += Chunk.Count;
                }
                return Result;
            }
        }

        public IBoxel AtOrDefault(Int3 Position)
        {
            throw new NotImplementedException();
        }

        public void Add(IBoxel Boxel, Int3 Position)
        {
            Byte3 ChunkPos;
            Byte3 InternPos;
            this.FullPosition(Position, out ChunkPos, out InternPos);
            var Chunk = this.LazyGetChunk(ChunkPos.GetHashCode());
            Chunk.Add(Boxel, InternPos);
        }

        public IEnumerable<IBoxel> AllBoxels
        {
            get
            {
                IEnumerable<IBoxel> Result = new IBoxel[0];
                foreach (var Chunk in this.Chunks.Values)
                {
                    Result = Result.Union(Chunk.AllBoxels);
                }
                return Result;
            }
        }

        public IEnumerable<IBoxel> BoxelsInRadius(Byte3 Position, int RadiusInBoxels)
        {
            return AllBoxels;
            //return this.Chunks[Position.GetHashCode()].AllBoxels;
        }

        public void Compact()
        {
            foreach (var Chunk in this.Chunks.Values)
            {
                Chunk.Compact();
            }
        }

        private ConstantChunk LazyGetChunk(int Position)
        {
            if(!this.Chunks.ContainsKey(Position))
                this.Chunks[Position] = new ConstantChunk(ChunkSize);
            return this.Chunks[Position];
        }

        private void FullPosition(Int3 Position, out Byte3 ChunkPosition, out Byte3 InternalPosition)
        {
            ChunkPosition = new Byte3(Position / ChunkSize);
            InternalPosition = new Byte3(Position.Remainder(ChunkSize));
        }
    }

    class ConstantChunk
    {
        private readonly IDictionary<int, IBoxel> Boxels;
        public int Count { get { return this.Boxels.Count; } }
        public IEnumerable<IBoxel> AllBoxels { get { return this.Boxels.Values; } }

        public ConstantChunk(int Size)
        {
            this.Boxels = new Dictionary<int, IBoxel>((int)Math.Pow(Size, 3));
        }

        public void Compact()
        {
            throw new NotImplementedException();
        }

        public void Add(IBoxel Boxel, Byte3 Position)
        {
            Debug.Assert(!this.Boxels.ContainsKey(Position.GetHashCode()));
            this.Boxels[Position.GetHashCode()] = Boxel;
        }
    }

    public static class ChunkPosition
    {
        public static Byte3 From(Vector3 Vect)
        {
            checked
            {
                return new Byte3((byte) (((byte) Vect.X)/ConstantRandomContainer.ChunkSize),
                                 (byte) (((byte) Vect.Y)/ConstantRandomContainer.ChunkSize),
                                 (byte) (((byte) Vect.Z)/ConstantRandomContainer.ChunkSize));
            }
        }

        public static int HashSphere(Byte3 Center, int Radius)
        {
            checked
            {
                var ChunkRadius = (byte) (Radius/ConstantRandomContainer.ChunkSize);
                var LowX = new Byte3((byte) (Center.X - Math.Min(ChunkRadius, Center.X)), Center.Y, Center.Z);
                var HighX = new Byte3((byte) (Center.X + Math.Min(ChunkRadius, byte.MaxValue - Center.X)), Center.Y,
                                      Center.Z);

                var LowY = new Byte3(Center.X, (byte) (Center.Y - Math.Min(ChunkRadius, Center.Y)), Center.Z);
                var HighY = new Byte3(Center.X, (byte) (Center.Y + Math.Min(ChunkRadius, byte.MaxValue - Center.Y)),
                                      Center.Z);

                var LowZ = new Byte3(Center.X, Center.Y, (byte) (Center.Z - Math.Min(ChunkRadius, Center.Z)));
                var HighZ = new Byte3(Center.X, Center.Y,
                                      (byte) (Center.Z + Math.Min(ChunkRadius, byte.MaxValue - Center.Z)));
                return LowX.GetHashCode() + HighX.GetHashCode() + LowY.GetHashCode() + HighY.GetHashCode() +
                       LowZ.GetHashCode() + HighZ.GetHashCode();
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    public struct Byte3
    {
        public readonly byte X;
        public readonly byte Y;
        public readonly byte Z;

        public Byte3(byte X, byte Y, byte Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Byte3(int HashCode)
        {
            throw new NotImplementedException();
        }

        public Byte3(Int3 XYZ)
        {
            this.X = (byte)XYZ.X;
            this.Y = (byte)XYZ.Y;
            this.Z = (byte)XYZ.Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override int GetHashCode()
        {
            return ZOrderCurveHelper.XWeave[this.X] | ZOrderCurveHelper.YWeave[this.Y] | ZOrderCurveHelper.ZWeave[this.Z];
        }
    }

    static class ZOrderCurveHelper
    {
        public static readonly int[] XWeave;
        public static readonly int[] YWeave;
        public static readonly int[] ZWeave;

        static ZOrderCurveHelper()
        {
            XWeave = new[] {
0,
4,
32,
36,
256,
260,
288,
292,
2048,
2052,
2080,
2084,
2304,
2308,
2336,
2340,
16384,
16388,
16416,
16420,
16640,
16644,
16672,
16676,
18432,
18436,
18464,
18468,
18688,
18692,
18720,
18724,
131072,
131076,
131104,
131108,
131328,
131332,
131360,
131364,
133120,
133124,
133152,
133156,
133376,
133380,
133408,
133412,
147456,
147460,
147488,
147492,
147712,
147716,
147744,
147748,
149504,
149508,
149536,
149540,
149760,
149764,
149792,
149796,
1048576,
1048580,
1048608,
1048612,
1048832,
1048836,
1048864,
1048868,
1050624,
1050628,
1050656,
1050660,
1050880,
1050884,
1050912,
1050916,
1064960,
1064964,
1064992,
1064996,
1065216,
1065220,
1065248,
1065252,
1067008,
1067012,
1067040,
1067044,
1067264,
1067268,
1067296,
1067300,
1179648,
1179652,
1179680,
1179684,
1179904,
1179908,
1179936,
1179940,
1181696,
1181700,
1181728,
1181732,
1181952,
1181956,
1181984,
1181988,
1196032,
1196036,
1196064,
1196068,
1196288,
1196292,
1196320,
1196324,
1198080,
1198084,
1198112,
1198116,
1198336,
1198340,
1198368,
1198372,
8388608,
8388612,
8388640,
8388644,
8388864,
8388868,
8388896,
8388900,
8390656,
8390660,
8390688,
8390692,
8390912,
8390916,
8390944,
8390948,
8404992,
8404996,
8405024,
8405028,
8405248,
8405252,
8405280,
8405284,
8407040,
8407044,
8407072,
8407076,
8407296,
8407300,
8407328,
8407332,
8519680,
8519684,
8519712,
8519716,
8519936,
8519940,
8519968,
8519972,
8521728,
8521732,
8521760,
8521764,
8521984,
8521988,
8522016,
8522020,
8536064,
8536068,
8536096,
8536100,
8536320,
8536324,
8536352,
8536356,
8538112,
8538116,
8538144,
8538148,
8538368,
8538372,
8538400,
8538404,
9437184,
9437188,
9437216,
9437220,
9437440,
9437444,
9437472,
9437476,
9439232,
9439236,
9439264,
9439268,
9439488,
9439492,
9439520,
9439524,
9453568,
9453572,
9453600,
9453604,
9453824,
9453828,
9453856,
9453860,
9455616,
9455620,
9455648,
9455652,
9455872,
9455876,
9455904,
9455908,
9568256,
9568260,
9568288,
9568292,
9568512,
9568516,
9568544,
9568548,
9570304,
9570308,
9570336,
9570340,
9570560,
9570564,
9570592,
9570596,
9584640,
9584644,
9584672,
9584676,
9584896,
9584900,
9584928,
9584932,
9586688,
9586692,
9586720,
9586724,
9586944,
9586948,
9586976,
9586980
};
            YWeave = new[] {
0,
2,
16,
18,
128,
130,
144,
146,
1024,
1026,
1040,
1042,
1152,
1154,
1168,
1170,
8192,
8194,
8208,
8210,
8320,
8322,
8336,
8338,
9216,
9218,
9232,
9234,
9344,
9346,
9360,
9362,
65536,
65538,
65552,
65554,
65664,
65666,
65680,
65682,
66560,
66562,
66576,
66578,
66688,
66690,
66704,
66706,
73728,
73730,
73744,
73746,
73856,
73858,
73872,
73874,
74752,
74754,
74768,
74770,
74880,
74882,
74896,
74898,
524288,
524290,
524304,
524306,
524416,
524418,
524432,
524434,
525312,
525314,
525328,
525330,
525440,
525442,
525456,
525458,
532480,
532482,
532496,
532498,
532608,
532610,
532624,
532626,
533504,
533506,
533520,
533522,
533632,
533634,
533648,
533650,
589824,
589826,
589840,
589842,
589952,
589954,
589968,
589970,
590848,
590850,
590864,
590866,
590976,
590978,
590992,
590994,
598016,
598018,
598032,
598034,
598144,
598146,
598160,
598162,
599040,
599042,
599056,
599058,
599168,
599170,
599184,
599186,
4194304,
4194306,
4194320,
4194322,
4194432,
4194434,
4194448,
4194450,
4195328,
4195330,
4195344,
4195346,
4195456,
4195458,
4195472,
4195474,
4202496,
4202498,
4202512,
4202514,
4202624,
4202626,
4202640,
4202642,
4203520,
4203522,
4203536,
4203538,
4203648,
4203650,
4203664,
4203666,
4259840,
4259842,
4259856,
4259858,
4259968,
4259970,
4259984,
4259986,
4260864,
4260866,
4260880,
4260882,
4260992,
4260994,
4261008,
4261010,
4268032,
4268034,
4268048,
4268050,
4268160,
4268162,
4268176,
4268178,
4269056,
4269058,
4269072,
4269074,
4269184,
4269186,
4269200,
4269202,
4718592,
4718594,
4718608,
4718610,
4718720,
4718722,
4718736,
4718738,
4719616,
4719618,
4719632,
4719634,
4719744,
4719746,
4719760,
4719762,
4726784,
4726786,
4726800,
4726802,
4726912,
4726914,
4726928,
4726930,
4727808,
4727810,
4727824,
4727826,
4727936,
4727938,
4727952,
4727954,
4784128,
4784130,
4784144,
4784146,
4784256,
4784258,
4784272,
4784274,
4785152,
4785154,
4785168,
4785170,
4785280,
4785282,
4785296,
4785298,
4792320,
4792322,
4792336,
4792338,
4792448,
4792450,
4792464,
4792466,
4793344,
4793346,
4793360,
4793362,
4793472,
4793474,
4793488,
4793490
};
            ZWeave = new[] {
0,
1,
8,
9,
64,
65,
72,
73,
512,
513,
520,
521,
576,
577,
584,
585,
4096,
4097,
4104,
4105,
4160,
4161,
4168,
4169,
4608,
4609,
4616,
4617,
4672,
4673,
4680,
4681,
32768,
32769,
32776,
32777,
32832,
32833,
32840,
32841,
33280,
33281,
33288,
33289,
33344,
33345,
33352,
33353,
36864,
36865,
36872,
36873,
36928,
36929,
36936,
36937,
37376,
37377,
37384,
37385,
37440,
37441,
37448,
37449,
262144,
262145,
262152,
262153,
262208,
262209,
262216,
262217,
262656,
262657,
262664,
262665,
262720,
262721,
262728,
262729,
266240,
266241,
266248,
266249,
266304,
266305,
266312,
266313,
266752,
266753,
266760,
266761,
266816,
266817,
266824,
266825,
294912,
294913,
294920,
294921,
294976,
294977,
294984,
294985,
295424,
295425,
295432,
295433,
295488,
295489,
295496,
295497,
299008,
299009,
299016,
299017,
299072,
299073,
299080,
299081,
299520,
299521,
299528,
299529,
299584,
299585,
299592,
299593,
2097152,
2097153,
2097160,
2097161,
2097216,
2097217,
2097224,
2097225,
2097664,
2097665,
2097672,
2097673,
2097728,
2097729,
2097736,
2097737,
2101248,
2101249,
2101256,
2101257,
2101312,
2101313,
2101320,
2101321,
2101760,
2101761,
2101768,
2101769,
2101824,
2101825,
2101832,
2101833,
2129920,
2129921,
2129928,
2129929,
2129984,
2129985,
2129992,
2129993,
2130432,
2130433,
2130440,
2130441,
2130496,
2130497,
2130504,
2130505,
2134016,
2134017,
2134024,
2134025,
2134080,
2134081,
2134088,
2134089,
2134528,
2134529,
2134536,
2134537,
2134592,
2134593,
2134600,
2134601,
2359296,
2359297,
2359304,
2359305,
2359360,
2359361,
2359368,
2359369,
2359808,
2359809,
2359816,
2359817,
2359872,
2359873,
2359880,
2359881,
2363392,
2363393,
2363400,
2363401,
2363456,
2363457,
2363464,
2363465,
2363904,
2363905,
2363912,
2363913,
2363968,
2363969,
2363976,
2363977,
2392064,
2392065,
2392072,
2392073,
2392128,
2392129,
2392136,
2392137,
2392576,
2392577,
2392584,
2392585,
2392640,
2392641,
2392648,
2392649,
2396160,
2396161,
2396168,
2396169,
2396224,
2396225,
2396232,
2396233,
2396672,
2396673,
2396680,
2396681,
2396736,
2396737,
2396744,
2396745
};


        }
    }

    public static class Int3Extensions
    {
        public static Int3 Remainder(this Int3 This, int Other)
        {
            return new Int3(This.X % Other, This.Y % Other, This.Z % Other);
        }
    }
}
