using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MML.Enterprise.Common.Extensions
{
    public static class StreamExtensions
    {
        public static Stream Compress(this Stream input)
        {
            var output = new MemoryStream();
            input.Position = 0;
            using (var compressor = new GZipStream(output,CompressionMode.Compress))
            {
                input.CopyTo(compressor);
                compressor.Close();                
                return new MemoryStream(output.ToArray());
            }
        }

        public static Stream Decompress(this Stream input)
        {
            var output = new MemoryStream();
            input.Position = 0;
            using (var decompressor = new GZipStream(input, CompressionMode.Decompress))            
            {
                decompressor.CopyTo(output);
                decompressor.Close();
                output.Position = 0;
                return output;
            }
        }

        public static byte[] GetArray(this Stream input)
        {
            using (var temp = new MemoryStream())
            {
                input.CopyTo(temp);
                return temp.ToArray();
            }
        }

    }
}
