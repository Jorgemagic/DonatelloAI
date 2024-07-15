using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using SkiaSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DonatelloAI.ImGui
{
    public static class ImguiHelper
    {
        private static GraphicsContext graphicsContext = null;
        private static IntPtr previewPlaceholderPointer = IntPtr.Zero;

        public static async Task DownloadThumbnailFromUrl(string url, string filePath)
        {
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    using (var s = await response.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(filePath, FileMode.CreateNew))
                    {
                        await s.CopyToAsync(fs);
                        s.Flush();
                    }
                }
            }
        }

        public static async Task<Texture> CreateTextureFromFile(string filePath)
        {
            Texture result = null;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                result = await GenerateTexture(stream);
            }

            return result;
        }

        public static async Task<Texture> DownloadTextureFromUrl(string url)
        {
            Texture result = null;
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    using (var fileStream = await response.Content.ReadAsStreamAsync())
                    {
                        result = await GenerateTexture(fileStream);
                        fileStream.Flush();
                    }

                    return result;
                }
            }
        }

        public static async Task<Texture> LoadTextureFromFile(string filepath)
        {
            Texture result = null;
            using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                result = await GenerateTexture(fileStream);
                fileStream.Flush();
            }

            return result;
        }

        private static async Task<Texture> GenerateTexture(Stream stream)
        {
            if (graphicsContext == null)
            {
                graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
            }

            Texture result = null;

            var codec = SKCodec.Create(stream);
            var bitmap = new SKBitmap(codec.Info);
            var imageInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var decodeResult = codec.GetPixels(imageInfo, bitmap.GetPixels());
            await EvergineForegroundTask.Run(() =>
            {
                TextureDescription desc = new TextureDescription()
                {
                    Type = TextureType.Texture2D,
                    Width = (uint)bitmap.Width,
                    Height = (uint)bitmap.Height,
                    Depth = 1,
                    ArraySize = 1,
                    Faces = 1,
                    Usage = ResourceUsage.Default,
                    CpuAccess = ResourceCpuAccess.None,
                    Flags = TextureFlags.ShaderResource,
                    Format = PixelFormat.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    SampleCount = TextureSampleCount.None,
                };
                result = graphicsContext.Factory.CreateTexture(ref desc);

                graphicsContext.UpdateTextureData(result, bitmap.GetPixels(), (uint)bitmap.ByteCount, 0);
            });

            return result;
        }

        public static IntPtr SetNoPreviewImage(CustomImGuiManager imguiManager)
        {
            if (previewPlaceholderPointer == IntPtr.Zero)
            {
                var assetsService = Evergine.Framework.Application.Current.Container.Resolve<AssetsService>();
                var previewPlaceholder = assetsService.Load<Texture>(EvergineContent.Textures.ImageIcon_png);
                previewPlaceholderPointer = imguiManager.CreateImGuiBinding(previewPlaceholder);
            }

            return previewPlaceholderPointer;
        }
    }
}
