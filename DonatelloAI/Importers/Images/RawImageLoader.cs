// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace DonatelloAI.Importers.Images
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
        public static void CopyImageToArrayPool(Image<Rgba32> image, bool premultiplyAlpha, out int dataLength, out byte[] data)
        {
            var bytesPerPixel = image.PixelType.BitsPerPixel / 8;
            dataLength = image.Width * image.Height * bytesPerPixel;
            var shared = ArrayPool<byte>.Shared;
            data = shared.Rent(dataLength);
            var dataPixels = MemoryMarshal.Cast<byte, Rgba32>(data);
            if (image.DangerousTryGetSinglePixelMemory(out var pixels))
            {
                if (premultiplyAlpha)
                {
                    CopyToPremultiplied(pixels.Span, dataPixels);
                }
                else
                {
                    pixels.Span.CopyTo(dataPixels);
                }
            }
            else
            {
                for (int i = 0; i < image.Height; i++)
                {
                    var row = image.DangerousGetPixelRowMemory(i);
                    if (premultiplyAlpha)
                    {
                        CopyToPremultiplied(row.Span, dataPixels.Slice(i * image.Width, image.Width));
                    }
                    else
                    {
                        row.Span.CopyTo(dataPixels.Slice(i * image.Width, image.Width));
                    }
                }
            }

            shared.Return(data);
        }

        private static void CopyToPremultiplied(Span<Rgba32> pixels, Span<Rgba32> destination)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                ref Rgba32 pixel = ref pixels[i];
                ref Rgba32 destinationPixel = ref destination[i];
                ref var a = ref pixel.A;
                if (a == 0)
                {
                    destinationPixel.PackedValue = 0;
                }
                else
                {
                    destinationPixel.R = (byte)((pixel.R * a) >> 8);
                    destinationPixel.G = (byte)((pixel.G * a) >> 8);
                    destinationPixel.B = (byte)((pixel.B * a) >> 8);
                    destinationPixel.A = pixel.A;
                }
            }
        }
    }
}
