using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ImagePng
{
    public struct ColorRGBA
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public ColorRGBA(byte r, byte g, byte b, byte a)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }
    }
    struct Chunk
    {
        public uint Length;
        public byte[] ChunkType;
        public byte[] ChunkData;
        public uint Crc;

        public Chunk(uint length, byte[] chunkType, byte[] chunkData, uint crc)
        {
            Length = length;
            ChunkType = chunkType;
            ChunkData = chunkData;
            Crc = crc;
        }

        public string GetChunkType()
        {
            return Encoding.ASCII.GetString(ChunkType);
        }

        public bool IsIHDR
        {
            get
            {
                if (GetChunkType() == "IHDR")
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsPLTE // Palette
        {
            get
            {
                if (GetChunkType() == "PLTE")
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsIDAT
        {
            get
            {
                if (GetChunkType() == "IDAT")
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsIEND
        {
            get
            {
                if (GetChunkType() == "IEND" )
                {
                    return true;
                }
                return false;
            }
        }
    }

    public class ImagePng
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public int BitDepth { get; set; }
        public int ColorType { get; set; }
        public int CompressionType { get; set; }
        public int FilterMethod { get; set; }
        public int InterlaceMethod { get; set; }

        List<ColorRGBA> pixelsRGBA = new List<ColorRGBA>();

        public ImagePng()
        {

        }

        public ImagePng(string fileName)
        {
            Load(fileName);
        }

        public ImagePng(Stream stream)
        {
            Load(stream);
        }

        public bool Load(string fileName)
        {
            Load(File.Open(fileName, FileMode.Open));

            return true;
        }

        public bool Load(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            if (ReadPngSignature(reader) == false)
            {
                return false;
            }

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Chunk chunk = ReadChunk(reader);
                if (chunk.IsIHDR)
                {
                    ReadIHDR(chunk);
                }
                else if (chunk.IsPLTE)
                {
                    ReadPLTE(chunk);
                }
                else if (chunk.IsIDAT)
                {
                    ReadIDAT(chunk);
                } else if (chunk.IsIEND)
                {
                    ReadIEND(chunk);
                }
            }

            reader.Close();

            return true;
        }

        private bool ReadPngSignature(BinaryReader reader)
        {
            byte[] signature = reader.ReadBytes(8);

            if (signature[0] == 137 && signature[1] == 80 && signature[2] == 78 && signature[3] == 71 &&
                signature[4] == 13 && signature[5] == 10 && signature[6] == 26 && signature[7] == 10)
            {
                return true;
            }

            return false;
        }

        private Chunk ReadChunk(BinaryReader reader)
        {
            byte[] bytesLength = reader.ReadBytes(4);
            byte[] bytesChunkType = reader.ReadBytes(4);

            uint length = BitConverter.ToUInt32(bytesLength.Reverse().ToArray());

            byte[] chunkData = reader.ReadBytes((int)length);

            byte[] bytesCrc = reader.ReadBytes(4);
            uint crc = BitConverter.ToUInt32(bytesCrc.Reverse().ToArray());

            return new Chunk(length, bytesChunkType, chunkData, crc);
        }

        private void ReadIHDR(Chunk chunk)
        {
            using MemoryStream stream = new MemoryStream(chunk.ChunkData);
            using BinaryReader reader = new BinaryReader(stream);

            byte[] bytesWidth = reader.ReadBytes(4);
            this.Width = (int)BitConverter.ToUInt32(bytesWidth.Reverse().ToArray());

            byte[] bytesHeight = reader.ReadBytes(4);
            this.Height = (int)BitConverter.ToUInt32(bytesHeight.Reverse().ToArray());

            this.BitDepth = (int)reader.ReadByte();
            this.ColorType = (int)reader.ReadByte();
            this.CompressionType = (int)reader.ReadByte();
            this.FilterMethod = (int)reader.ReadByte();
            this.InterlaceMethod = (int)reader.ReadByte();
        }

        private void ReadPLTE(Chunk chunk)
        {
            // TODO: Read PLTE chunk
            // Console.WriteLine("PLTE");
        }

        private void ReadIDAT(Chunk chunk)
        {
            // Skip gzip header (2bytes)
            byte[] data = chunk.ChunkData.Skip(2).ToArray();

            using MemoryStream stream = new MemoryStream(data);
            stream.Position = 0;

            using DeflateStream decompressStream = new DeflateStream(stream, CompressionMode.Decompress);
            using MemoryStream dataStream = new MemoryStream();
            decompressStream.CopyTo(dataStream);
            dataStream.Position = 0;
            using BinaryReader pixels = new BinaryReader(dataStream);

            byte R = 0;
            byte G = 0;
            byte B = 0;
            byte A = 0;

            if (BitDepth == 8 && (ColorType == 6 || ColorType == 2) && CompressionType == 0)
            {
                for (int y = 0; y < Height; y++)
                {
                    // filter type: 0 to
                    byte filterType = pixels.ReadByte();

                    if (filterType == 0) // none
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            R = pixels.ReadByte();
                            G = pixels.ReadByte();
                            B = pixels.ReadByte();

                            if (ColorType == 6)
                            {
                                A = pixels.ReadByte();
                            }
                            else
                            {
                                A = 255;
                            }

                            ColorRGBA color = new ColorRGBA(R, G, B, A);
                            pixelsRGBA.Add(color);
                        }
                    }
                    else if (filterType == 1) // sub
                    {
                        ColorRGBA previousColor = new ColorRGBA(0, 0, 0, 0);

                        for (int x = 0; x < Width; x++)
                        {
                            R = pixels.ReadByte();
                            G = pixels.ReadByte();
                            B = pixels.ReadByte();

                            if (ColorType == 6)
                            {
                                A = pixels.ReadByte();
                            } else
                            {
                                A = 0;
                                previousColor.A = 255;
                            }

                            ColorRGBA color = new ColorRGBA((byte)(previousColor.R + R), (byte)(previousColor.G + G), (byte)(previousColor.B + B), (byte)(previousColor.A + A));
                            pixelsRGBA.Add(color);

                            previousColor = color;
                        }
                    }
                    else if (filterType == 2) // up
                    {
                        // TODO: Filter type up
                    }
                    else if (filterType == 3) // average
                    {
                        // TODO: Filter type average
                    }
                    else if (filterType == 4) // paeth
                    {
                        // TODO: Filter type peath
                    }
                }
            }
        }

        private void ReadIEND(Chunk chunk)
        {

        }

        public ColorRGBA GetPixel(int x, int y)
        {
            return pixelsRGBA[y * Width + x];
        }
    }
}
