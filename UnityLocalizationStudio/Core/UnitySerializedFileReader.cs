using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityLocalizationStudio.Core
{
    public class UnitySerializedFileReader
    {
        public struct UnityObjectInfo
        {
            public long PathId;
            public long ByteStart;
            public uint ByteSize;
            public int TypeId;
            public int ClassId;
        }

        public static List<UnityObjectInfo> ReadObjects(byte[] b)
        {
            var objects = new List<UnityObjectInfo>();
            if (b.Length < 64) return objects;

            try
            {
                // 1. Initial 16 bytes (Big Endian)
                uint metaSizeOld = ReadUInt32BE(b, 0);
                uint fileSizeOld = ReadUInt32BE(b, 4);
                uint ver = ReadUInt32BE(b, 8);
                uint dataOffsetOld = ReadUInt32BE(b, 12);

                int p = 16;
                bool endianBool = b[p] != 0;
                p += 1;
                p += 3; // reserved 3 bytes

                long metaSize = metaSizeOld;
                long fileSize = fileSizeOld;
                long dataOffset = dataOffsetOld;

                if (ver >= 22)
                {
                    metaSize = ReadUInt32BE(b, p);
                    p += 4;
                    fileSize = ReadInt64BE(b, p);
                    p += 8;
                    dataOffset = ReadInt64BE(b, p);
                    p += 8;
                    p += 8; // unknown
                }

                // Unity version null-terminated string at p
                int nullPos = p;
                while (nullPos < b.Length && b[nullPos] != 0) nullPos++;
                string unityVer = Encoding.ASCII.GetString(b, p, nullPos - p);
                p = nullPos + 1;

                int targetPlatform = BitConverter.ToInt32(b, p);
                p += 4;

                bool enableTypeTree = b[p] != 0;
                p += 1;

                int typeCount = BitConverter.ToInt32(b, p);
                p += 4;

                var types = new List<int>();
                for (int i = 0; i < typeCount; i++)
                {
                    int classId = BitConverter.ToInt32(b, p);
                    p += 4;
                    p += 1; // is_stripped_type
                    p += 2; // script_type_index
                    if (classId == 114)
                    {
                        p += 16; // script_id (Hash128)
                    }
                    p += 16; // old_type_hash (Hash128)
                    types.Add(classId);
                }

                int objectCount = BitConverter.ToInt32(b, p);
                p += 4;

                for (int i = 0; i < objectCount; i++)
                {
                    if (ver >= 14)
                    {
                        p = (p + 3) & ~3;
                    }
                    if (p + 24 > b.Length) break;

                    long pathId = BitConverter.ToInt64(b, p);
                    p += 8;

                    long byteStart = (ver >= 22) ? BitConverter.ToInt64(b, p) : BitConverter.ToUInt32(b, p);
                    p += (ver >= 22) ? 8 : 4;
                    byteStart += dataOffset;

                    uint byteSize = BitConverter.ToUInt32(b, p);
                    p += 4;

                    int typeId = BitConverter.ToInt32(b, p);
                    p += 4;

                    int classId = (typeId >= 0 && typeId < types.Count) ? types[typeId] : 0;
                    objects.Add(new UnityObjectInfo
                    {
                        PathId = pathId,
                        ByteStart = byteStart,
                        ByteSize = byteSize,
                        TypeId = typeId,
                        ClassId = classId
                    });
                }
            }
            catch
            {
                // Return whatever was successfully parsed
            }

            return objects;
        }

        private static uint ReadUInt32BE(byte[] b, int offset)
        {
            return (uint)((b[offset] << 24) | (b[offset + 1] << 16) | (b[offset + 2] << 8) | b[offset + 3]);
        }

        private static long ReadInt64BE(byte[] b, int offset)
        {
            return ((long)ReadUInt32BE(b, offset) << 32) | ReadUInt32BE(b, offset + 4);
        }
    }
}
