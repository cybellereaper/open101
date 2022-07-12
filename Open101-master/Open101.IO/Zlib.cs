using System.IO;

namespace Open101.IO
{
    public class Zlib
    {
        public static ByteBuffer Decompress(Stream stream)
        {
            var compressedSize = (int)(stream.Length-stream.Position);
            byte[] compressed = new byte[compressedSize];
            stream.Read(compressed, 0, compressed.Length);
            
            return Decompress(compressed);
        }

        public static ByteBuffer Decompress(byte[] compressed)
        {
            return new ByteBuffer(DecompressBytes(compressed));
        }
    
        public static byte[] DecompressBytes(byte[] compressed)
        {
            var uncompressed = Ionic.Zlib.ZlibStream.UncompressBuffer(compressed);
            return uncompressed;
        }
        
        public static byte[] Compress(byte[] bytes)
        {
            var compressed = Ionic.Zlib.ZlibStream.CompressBuffer(bytes);
            return compressed;
        }
    }
}