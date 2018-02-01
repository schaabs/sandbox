using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sandbox.tools
{
    public static class RandomEx
    {
        public static string File(this Random rand, long cbyte = -1, string path = null)
        {
            path = path ?? Path.GetTempFileName();

            using (var fStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                if (cbyte == 0)
                {
                    cbyte = rand.Next(1024 * 1024);
                }

                var buff = new byte[1024];

                for (long rbyte = cbyte; rbyte > 0; rbyte -= 1024)
                {
                    rand.NextBytes(buff);

                    fStream.Write(buff, 0, Convert.ToInt32(Math.Min(rbyte, buff.LongLength)));
                }

                fStream.Flush();
            }

            return path;
        }

        public static Int32 NextInt32(this Random rand)
        {
            unchecked
            {
                return rand.Next(Int32.MinValue, Int32.MaxValue) ^ rand.Next(Int32.MinValue, Int32.MaxValue);
            }
        }

        public static Int32 NextInt32(this Random rand, Int32 min, Int32 max)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException("min");
            }

            int next;

            if (max < int.MaxValue)
            {
                next = rand.Next(min, max + 1);
            }
            else if (min > int.MinValue)
            {
                next = rand.Next(min - 1, max) + 1;
            }
            else
            {
                next = rand.NextInt32();
            }

            return next;
        }

        public static UInt32 NextUInt32(this Random rand)
        {
            unchecked
            {
                return (UInt32)rand.NextInt32();
            }
        }

        public static UInt32 NextUInt32(this Random rand, UInt32 min, UInt32 max)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException("min");
            }

            uint urand = 0;

            urand = (uint)rand.NextUInt32();

            uint diff = max - min;

            uint offset;

            if (diff == uint.MaxValue)
            {
                return urand;
            }

            offset = urand % (diff + 1U);

            return min + offset;
        }

        public static Int16 NextInt16(this Random rand)
        {
            unchecked
            {
                return (Int16)rand.Next();
            }
        }

        public static Int16 NextInt16(this Random rand, Int16 min, Int16 max)
        {
            return Convert.ToInt16(rand.NextInt32(min, max));
        }

        public static UInt16 NextUInt16(this Random rand)
        {
            unchecked
            {
                return (UInt16)rand.Next();
            }
        }

        public static UInt16 NextUInt16(this Random rand, UInt16 min, UInt16 max)
        {
            return Convert.ToUInt16(rand.NextInt32(min, max));
        }

        public static Int64 NextInt64(this Random rand)
        {
            unchecked
            {
                return (Int64)rand.NextUInt64();
            }
        }

        public static Int64 NextInt64(this Random rand, Int64 min, Int64 max)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException("min");
            }

            ulong urand = rand.NextUInt64();

            if (min == Int64.MinValue && max == Int64.MaxValue)
            {
                unchecked
                {
                    return (long)urand;
                }
            }

            ulong diff = CalculateULongDifference(min, max);

            ulong offset = urand % (diff + 1);

            return AddULongOffsetToLongMin(min, offset);
        }

        public static UInt64 NextUInt64(this Random rand)
        {
            unchecked
            {
                ulong lower32 = (ulong)rand.NextUInt32();

                ulong upper32 = ((ulong)rand.NextUInt32() << 32);

                return lower32 | upper32;
            }
        }

        public static UInt64 NextUInt64(this Random rand, UInt64 min, UInt64 max)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException("min");
            }

            ulong urand = rand.NextUInt64();

            ulong diff = max - min;

            ulong offset;

            if (diff == ulong.MaxValue)
            {
                return urand;
            }

            offset = urand % (diff + 1);

            return min + offset;
        }

        public static double NextDouble(this Random rand, double min, double max)
        {
            if ((min > max) || double.IsInfinity(min) || double.IsNaN(min))
            {
                throw new ArgumentOutOfRangeException("min");
            }

            if (double.IsInfinity(max) || double.IsNaN(max))
            {
                throw new ArgumentOutOfRangeException("max");
            }

            if (double.IsInfinity(max - min))
            {
                ChooseFiniteRange(rand, ref min, ref max);
            }

            double relative = rand.NextDouble();

            return (relative * (max - min)) + min;
        }

        public static Single NextSingle(this Random rand)
        {
            return rand.NextSingle(Single.MinValue, Single.MaxValue);
        }

        public static Single NextSingle(this Random rand, Single min, Single max)
        {
            if ((min > max) || float.IsInfinity(min) || float.IsNaN(min))
            {
                throw new ArgumentOutOfRangeException("min");
            }

            if (float.IsInfinity(max) || float.IsNaN(max))
            {
                throw new ArgumentOutOfRangeException("max");
            }

            return Convert.ToSingle(rand.NextDouble(min, max));
        }

        public static Boolean NextBoolean(this Random rand)
        {
            return Convert.ToBoolean(rand.NextInt32(0, 1));
        }

        public static Char NextChar(this Random rand)
        {

            return rand.NextChar(Char.MinValue, Char.MaxValue);
        }

        public static Char NextChar(this Random rand, Char min, Char max)
        {
            if (min > max)
            {
                throw new ArgumentOutOfRangeException("min");
            }

            return (Char)rand.NextInt32(min, max);
        }

        /// <summary>
        /// Chooses a finite range falling between the specified minimum and maximum
        /// </summary>
        /// <param name="min">The inclusive minimum</param>
        /// <param name="max">The inclusive maximum</param>
        private static void ChooseFiniteRange(Random rand, ref double min, ref double max)
        {
            double median = (Math.Abs(min) / 2) + (max / 2) + min;

            if (rand.NextBoolean())
            {
                min = median;
            }
            else
            {
                max = median;
            }
        }

        /// <summary>
        /// Subtracts the specified inputs and returns the abs(difference) as a ulong
        /// </summary>
        /// <param name="a">Right operand</param>
        /// <param name="b">Left operand</param>
        /// <returns>The abs(difference) of the specified inputs as a ulong</returns>
        private static ulong CalculateULongDifference(long a, long b)
        {

            ulong diff = 0;

            long max = Math.Max(a, b);

            long min = Math.Min(a, b);

            if (!(((min < 0) && (max > 0)) && ((long.MaxValue + min) >= max)))
            {
                diff = (ulong)(max - min);
            }
            else
            {
                diff = Convert.ToUInt64(max) + CalcLongAbsoluteValue(min);
            }

            return diff;
        }

        /// <summary>
        /// Calculates the absolute value of a long and returns as a ulong without overflow for long.MinValue
        /// </summary>
        /// <param name="a">Value to determin the absolute value of</param>
        /// <returns>The absolute value of the specified parameter as a ulong</returns>
        private static ulong CalcLongAbsoluteValue(long a)
        {
            ulong ret;

            if (a == long.MinValue)
            {
                ret = Convert.ToUInt64(long.MaxValue) + 1;
            }
            else
            {
                ret = Convert.ToUInt64(Math.Abs(a));
            }

            return ret;
        }

        /// <summary>
        /// Adds the specified offset to the specified min.  It is assumed that adding the offset to the min will not result in an overflow.
        /// </summary>
        /// <param name="min">Value to add the offset to.</param>
        /// <param name="offset">Value to be added to the specified min</param>
        /// <returns>The sum of min and offset as a long</returns>
        private static long AddULongOffsetToLongMin(long min, ulong offset)
        {
            long ret;

            if (offset < long.MaxValue)
            {
                ret = min + Convert.ToInt64(offset);
            }
            else
            {
                offset = offset - CalcLongAbsoluteValue(min);

                ret = Convert.ToInt64(offset);
            }

            return ret;
        }
    }
}
