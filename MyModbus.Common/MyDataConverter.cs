using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyModbus.Common
{
    public class MyDataConverter
    {
        // only use for protocol meta byte (TCP length, start address, quantity,)
        public static byte[] GetProtocolBytesFromUInt16(ushort value)
        {
            var byteA = (byte)(value >> 8);
            var byteB = (byte)(value & 0xFF);

            return new byte[] { byteA, byteB };
        }

        public static byte[] GetBytesFromUInt16(ushort value)
        {
            var byteA = (byte)(value >> 8);
            var byteB = (byte)(value & 0xFF);

            return new byte[] { byteA, byteB };
        }

        public static ushort GetUInt16FromBytes(byte[] bytes)
        {
            return (ushort)((ushort)bytes[0] << 8 | (ushort)bytes[1]);
        }

        public static byte[] GetBytesFromUInt16s(ushort[] values, ByteOrder byteOrder = ByteOrder.HighByteFirst)
        {
            var list = new List<byte>();

            foreach (var value in values)
            {
                if (byteOrder == ByteOrder.HighByteFirst)
                {
                    list.AddRange(GetBytesFromUInt16(value));
                }
                else
                {
                    list.AddRange(GetBytesFromUInt16(value).Reverse());
                }
            }

            return list.ToArray();
        }

        public static byte[] GetBytesFromInt16(short value)
        {
            // for -xxx, must convert to ushort from short first, then right move 8 bits
            ushort temp = (ushort)value;
            var byteA = (byte)(temp >> 8);
            var byteB = (byte)(temp & 0xFF);

            return new byte[] { byteA, byteB };
        }

        public static short GetInt16FromBytes(byte[] bytes)
        {
            return (short)((ushort)bytes[0] << 8 | (ushort)bytes[1]);
        }

        public static byte[] GetBytesFromInt16s(short[] values, ByteOrder byteOrder = ByteOrder.HighByteFirst)
        {
            var list = new List<byte>();

            foreach (var value in values)
            {
                if (byteOrder == ByteOrder.HighByteFirst)
                {
                    list.AddRange(GetBytesFromInt16(value));
                }
                else
                {
                    list.AddRange(GetBytesFromInt16(value).Reverse());
                }
            }

            return list.ToArray();
        }

        public static byte[] GetBytesFromUInt32(uint value)
        {
            var byteA = (byte)(value >> 24);
            var byteB = (byte)(value >> 16);
            var byteC = (byte)(value >> 8);
            var byteD = (byte)(value & 0xFF);

            return new byte[] { byteA, byteB, byteC, byteD };
        }

        public static uint GetUInt32FromBytes(byte[] bytes)
        {
            return (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | (uint)bytes[3];
        }

        public static byte[] GetBytesFromUInt32s(uint[] values, ByteOrder byteOrder = ByteOrder.HighByteFirst, WordOrder wordOrder = WordOrder.HighWordFirst)
        {
            var list = new List<byte>();

            foreach (var value in values)
            {
                var bytes = GetBytesFromUInt32(value);

                if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //ABCD, siments/huichuan equipments
                    list.AddRange(new byte[] { bytes[0], bytes[1], bytes[2], bytes[3] });
                }
                else if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.LowWordFirst)
                {
                    //CDAB, domestic equipments
                    list.AddRange(new byte[] { bytes[2], bytes[3], bytes[0], bytes[1] });
                }
                else if (byteOrder == ByteOrder.LowByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //BADC, mizubichi equipments
                    list.AddRange(new byte[] { bytes[1], bytes[0], bytes[3], bytes[2] });
                }
                else
                {
                    //DCBA, other equipments
                    list.AddRange(new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] });
                }
            }

            return list.ToArray();
        }

        public static byte[] GetBytesFromInt32(int value)
        {
            uint temp = (uint)value;
            var byteA = (byte)(temp >> 24);
            var byteB = (byte)(temp >> 16);
            var byteC = (byte)(temp >> 8);
            var byteD = (byte)(temp & 0xFF);

            return new byte[] { byteA, byteB, byteC, byteD };
        }

        public static int GetInt32FromBytes(byte[] bytes)
        {
            return (int)((uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | (uint)bytes[3]);
        }

        public static byte[] GetBytesFromInt32s(int[] values, ByteOrder byteOrder = ByteOrder.HighByteFirst, WordOrder wordOrder = WordOrder.HighWordFirst)
        {
            var list = new List<byte>();

            foreach (var value in values)
            {
                var bytes = GetBytesFromInt32(value);

                if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //ABCD, siments/huichuan equipments
                    list.AddRange(new byte[] { bytes[0], bytes[1], bytes[2], bytes[3] });
                }
                else if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.LowWordFirst)
                {
                    //CDAB, domestic equipments
                    list.AddRange(new byte[] { bytes[2], bytes[3], bytes[0], bytes[1] });
                }
                else if (byteOrder == ByteOrder.LowByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //BADC, mizubichi equipments
                    list.AddRange(new byte[] { bytes[1], bytes[0], bytes[3], bytes[2] });
                }
                else
                {
                    //DCBA, other equipments
                    list.AddRange(new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] });
                }
            }

            return list.ToArray();
        }

        public static byte[] GetBytesFromFloat32(float value)
        {
            // x86/64 cpu uses small endian (small byte order) & small word order, it means we need to get bytes from BitConverter output DCBA mode to ABCD mode
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static float GetFloat32FromBytes(byte[] bytes)
        {
            // x86/64 cpu uses small endian (small byte order) & small word order, it means we need to put bytes from ABCD mode to DCBA mode
            return BitConverter.ToSingle(bytes.Reverse().ToArray(), 0);
        }

        public static byte[] GetBytesFromFloat32s(float[] values, ByteOrder byteOrder = ByteOrder.HighByteFirst, WordOrder wordOrder = WordOrder.HighWordFirst)
        {
            var list = new List<byte>();

            foreach (var value in values)
            {
                var bytes = GetBytesFromFloat32(value);

                if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //ABCD, 西门子等欧美国际品牌/汇川 equipments
                    list.AddRange(new byte[] { bytes[0], bytes[1], bytes[2], bytes[3] });
                }
                else if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.LowWordFirst)
                {
                    //CDAB, 国内 equipments
                    list.AddRange(new byte[] { bytes[2], bytes[3], bytes[0], bytes[1] });
                }
                else if (byteOrder == ByteOrder.LowByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //BADC, 三菱日系 equipments
                    list.AddRange(new byte[] { bytes[1], bytes[0], bytes[3], bytes[2] });
                }
                else
                {
                    //DCBA, other equipments
                    list.AddRange(new byte[] { bytes[3], bytes[2], bytes[1], bytes[0] });
                }
            }

            return list.ToArray();
        }

        static bool[] GetBoolsFromByte(byte b)
        {
            bool[] values = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                values[i] = (b & (1 << i)) != 0;
            }

            return values;
        }

        public static bool[] GetBoolsFromBytes(byte[] bytes)
        {
            var list = new List<bool>();
            for (int i = 0; i < bytes.Length; i++)
            {
                list.AddRange(GetBoolsFromByte(bytes[i]).ToList());
            }
            return list.ToArray();
        }

        public static byte[] GetBytesFromBools(bool[] bools)
        {
            var bytesLength = bools.Length % 8 == 0 ? bools.Length / 8 : bools.Length / 8 + 1;
            var bytes = new byte[bytesLength];
            for (int i = 0; i < bytes.Length; i++)
            {
                var currentByte = bytes[i];
                var temp = bools.Length - i * 8;
                var currentLoopCount = temp > 8 ? 8 : temp;

                for (int j = 0; j < currentLoopCount; j++)
                {
                    if (bools[i * 8 + j])
                    {
                        currentByte = (byte)(currentByte | (byte)(1 << j));
                    }
                    //else
                    //{
                    //    currentByte = (byte)(currentByte |  0x00);
                    //}
                }

                bytes[i] = currentByte;
            }
            return bytes;
        }

        public static ushort[] GetUInt16sFromBytes(byte[] bytes, ByteOrder byteOrder = ByteOrder.HighByteFirst)
        {
            var list = new List<ushort>();
            for (int i = 0; i < bytes.Length; i = i + 2)
            {
                if (byteOrder == ByteOrder.HighByteFirst)
                {
                    list.Add(GetUInt16FromBytes(new byte[] { bytes[i], bytes[i + 1] }));
                }
                else
                {
                    list.Add(GetUInt16FromBytes(new byte[] { bytes[i + 1], bytes[i] }));
                }
            }
            return list.ToArray();
        }

        public static short[] GetInt16sFromBytes(byte[] bytes, ByteOrder byteOrder = ByteOrder.HighByteFirst)
        {
            var list = new List<short>();
            for (int i = 0; i < bytes.Length; i = i + 2)
            {
                if (byteOrder == ByteOrder.HighByteFirst)
                {
                    list.Add(GetInt16FromBytes(new byte[] { bytes[i], bytes[i + 1] }));
                }
                else
                {
                    list.Add(GetInt16FromBytes(new byte[] { bytes[i + 1], bytes[i] }));
                }
            }
            return list.ToArray();
        }

        public static uint[] GetUInt32sFromBytes(byte[] bytes, ByteOrder byteOrder = ByteOrder.HighByteFirst, WordOrder wordOrder = WordOrder.HighWordFirst)
        {
            var list = new List<uint>();
            for (int i = 0; i < bytes.Length; i = i + 4)
            {
                if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //ABCD, siments/huichuan equipments
                    list.Add(GetUInt32FromBytes(new byte[] { bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3] }));
                }
                else if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.LowWordFirst)
                {
                    //CDAB, domestic equipments
                    list.Add(GetUInt32FromBytes(new byte[] { bytes[i + 2], bytes[i + 3], bytes[i], bytes[i + 1] }));
                }
                else if (byteOrder == ByteOrder.LowByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //BADC, mizubichi equipments
                    list.Add(GetUInt32FromBytes(new byte[] { bytes[i + 1], bytes[i], bytes[i + 3], bytes[i + 2] }));
                }
                else
                {
                    //DCBA, other equipments
                    list.Add(GetUInt32FromBytes(new byte[] { bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i] }));
                }
            }
            return list.ToArray();
        }

        public static int[] GetInt32sFromBytes(byte[] bytes, ByteOrder byteOrder = ByteOrder.HighByteFirst, WordOrder wordOrder = WordOrder.HighWordFirst)
        {
            var list = new List<int>();
            for (int i = 0; i < bytes.Length; i = i + 4)
            {
                if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //ABCD, siments/huichuan equipments
                    list.Add(GetInt32FromBytes(new byte[] { bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3] }));
                }
                else if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.LowWordFirst)
                {
                    //CDAB, domestic equipments
                    list.Add(GetInt32FromBytes(new byte[] { bytes[i + 2], bytes[i + 3], bytes[i], bytes[i + 1] }));
                }
                else if (byteOrder == ByteOrder.LowByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //BADC, mizubichi equipments
                    list.Add(GetInt32FromBytes(new byte[] { bytes[i + 1], bytes[i], bytes[i + 3], bytes[i + 2] }));
                }
                else
                {
                    //DCBA, other equipments
                    list.Add(GetInt32FromBytes(new byte[] { bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i] }));
                }
            }
            return list.ToArray();
        }

        public static float[] GetFloat32sFromBytes(byte[] bytes, ByteOrder byteOrder = ByteOrder.HighByteFirst, WordOrder wordOrder = WordOrder.HighWordFirst)
        {
            var list = new List<float>();
            for (int i = 0; i < bytes.Length; i = i + 4)
            {
                if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //ABCD, siments/huichuan equipments
                    list.Add(GetFloat32FromBytes(new byte[] { bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3] }));
                }
                else if (byteOrder == ByteOrder.HighByteFirst && wordOrder == WordOrder.LowWordFirst)
                {
                    //CDAB, domestic equipments
                    list.Add(GetFloat32FromBytes(new byte[] { bytes[i + 2], bytes[i + 3], bytes[i], bytes[i + 1] }));
                }
                else if (byteOrder == ByteOrder.LowByteFirst && wordOrder == WordOrder.HighWordFirst)
                {
                    //BADC, mizubichi equipments
                    list.Add(GetFloat32FromBytes(new byte[] { bytes[i + 1], bytes[i], bytes[i + 3], bytes[i + 2] }));
                }
                else
                {
                    //DCBA, other equipments
                    list.Add(GetFloat32FromBytes(new byte[] { bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i] }));
                }
            }
            return list.ToArray();
        }

        public static string GetStringFromBytes(byte[] bytes)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in bytes)
            {
                stringBuilder.Append($"{item.ToString()} ");
            }

            return stringBuilder.ToString();
        }
    }
}
