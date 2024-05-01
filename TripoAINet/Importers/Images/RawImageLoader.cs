// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers;
using System.Runtime.InteropServices;

namespace TripoAINet.Importers.Images
{
    /// <summary>
    /// Helper class to read JPG as an array of bytes.
    /// </summary>
    public static class RawImageLoader
    {
        /// <summary>
        /// Reads a JPG image and returns an array of bytes respresenting it.
        /// </summary>
        /// <param name="image">The image to read.</param>
        /// <param name="dataLength">Size of the data (number of bytes).</param>
        /// <param name="data">The array of bytes representing the image.</param>
        public static void CopyImageToArrayPool(Image<Rgba32> image, out int dataLength, out byte[] data)
        {
            var bytesPerPixel = image.PixelType.BitsPerPixel / 8;
            dataLength = image.Width * image.Height * bytesPerPixel;
            var shared = ArrayPool<byte>.Shared;
            data = shared.Rent(dataLength);
            var dataPixels = MemoryMarshal.Cast<byte, Rgba32>(data);
            if (image.DangerousTryGetSinglePixelMemory(out var pixels))
            {
                pixels.Span.CopyTo(dataPixels);
            }
            else
            {
                for (int i = 0; i < image.Height; i++)
                {
                    var row = image.DangerousGetPixelRowMemory(i);
                    row.Span.CopyTo(dataPixels.Slice(i * image.Width, image.Width));
                }
            }

            shared.Return(data);
        }
    }
}
