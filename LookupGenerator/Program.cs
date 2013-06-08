using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LookupGenerator
{
    class Program
    {
        private static int[] XBits;
        private static int[] YBits;
        private static int[] ZBits;

        private static int[] XWeave, XUnWeave;
        private static int[] YWeave, YUnWeave;
        private static int[] ZWeave, ZUnWeave;

        static Program()
        {
            ZBits = new[]
                {
                    0,
                    3,
                    6,
                    9,
                    12,
                    15,
                    18,
                    21
                };
            YBits = new[]
                {
                    1,
                    4,
                    7,
                    10,
                    13,
                    16,
                    19,
                    22
                };
            XBits = new[]
                {
                    2,
                    5,
                    8,
                    11,
                    14,
                    17,
                    20,
                    23
                };
            XWeave = new int[byte.MaxValue+1];
            YWeave = new int[byte.MaxValue+1];
            ZWeave = new int[byte.MaxValue+1];

            XUnWeave = new int[XWeave.Length];
            YUnWeave = new int[YWeave.Length];
            ZUnWeave = new int[ZWeave.Length];
        }

        static void Main(string[] args)
        {
            FillWeave(XWeave, XBits);
            FillWeave(YWeave, YBits);
            FillWeave(ZWeave, ZBits);
            Console.WriteLine("Start verification.");
            if (!Verify())
            {
                Console.WriteLine("Validation failed.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Validation succeeded! Holy moley.");
            var AllCombos = new int[(int)Math.Pow(XWeave.Length, 3)];
            int i = 0;
            for (var x = 0; x < XWeave.Length; x++)
            {
                for (var y = 0; y < XWeave.Length; y++)
                {
                    for (var z = 0; z < XWeave.Length; z++)
                    {
                        AllCombos[i] = XWeave[x] | YWeave[y] | ZWeave[z];
                        i++;
                    }
                }
            }
            var Set = new HashSet<int>(AllCombos);
            if (Set.Count < AllCombos.Length)
            {
                Console.WriteLine("Combinations aren't unique. Collisions: {0}",AllCombos.Length - Set.Count);
                Console.ReadLine();
                return;
            }
            Console.WriteLine("All unique combinations!");
            Console.WriteLine("Everything passed, writing to file...");
            OutputToFile();
            Console.WriteLine("Done! Press enter to exit.");
            Console.ReadLine();
        }

        static void OutputToFile()
        {
            using (var Writer = new StreamWriter("out.txt"))
            {
                Writer.WriteLine("XWeave = new[] {");
                for (var i = 0; i < XWeave.Length; i++)
                {
                    Writer.Write(XWeave[i]);
                    if(i != XWeave.Length-1)
                        Writer.Write(",");
                    Writer.WriteLine();
                }
                Writer.WriteLine("};");

                Writer.WriteLine("YWeave = new[] {");
                for (var i = 0; i < YWeave.Length; i++)
                {
                    Writer.Write(YWeave[i]);
                    if (i != YWeave.Length - 1)
                        Writer.Write(",");
                    Writer.WriteLine();
                }
                Writer.WriteLine("};");

                Writer.WriteLine("ZWeave = new[] {");
                for (var i = 0; i < ZWeave.Length; i++)
                {
                    Writer.Write(ZWeave[i]);
                    if (i != ZWeave.Length - 1)
                        Writer.Write(",");
                    Writer.WriteLine();
                }
                Writer.WriteLine("};");
            }
        }

        static bool Verify()
        {
            bool Success = true;
            var Set = new HashSet<int>();
            for (var i = 1; i < XWeave.Length; i++)
            {
                if (!Set.Add(XWeave[i]))
                {
                    Console.WriteLine("Collision adding X[{0}]", i);
                    Success = false;
                }
            }
            for (var i = 1; i < YWeave.Length; i++)
            {
                if (!Set.Add(YWeave[i]))
                {
                    Console.WriteLine("Collision adding Y[{0}]", i);
                    Success = false;
                }
            }
            for (var i = 1; i < ZWeave.Length; i++)
            {
                if (!Set.Add(ZWeave[i]))
                {
                    Console.WriteLine("Collision adding Z[{0}]", i);
                    Success = false;
                }
            }
            return Success;
        }

        static void FillWeave(int[] Weave, int[] Bits)
        {
            var Adder = new BitAdder(Bits);
            for (int i = 0; i < Weave.Length; i++)
            {
                Weave[i] = Adder.Value;
                if( i != Weave.Length-1)
                    Adder.Increment();
            }
        }
    }

    class BitAdder
    {
        private int[] Bits;
        private bool[] LogicalBits;

        public BitAdder(params int[] Bits)
        {
            this.Bits = Bits;
            this.LogicalBits = new bool[this.Bits.Length];
        }

        public int Value
        {
            get
            {
                int Result = 0;
                for (var i = 0; i < LogicalBits.Length; i++)
                {
                    if (this.LogicalBits[i])
                    {
                        Result += (int)Math.Pow(2, this.Bits[i]);
                    }
                }
                return Result;
            }
        }

        public void Increment()
        {
            for (var i = 0; i < LogicalBits.Length; i++)
            {
                if (this.LogicalBits[i]) continue;
                this.LogicalBits[i] = true;
                for (var u = i - 1; u >= 0; u--)
                {
                    this.LogicalBits[u] = false;
                }
                return;
            }
            throw new InvalidOperationException("Tried to increment MaxValue.");
        }
    }
}
