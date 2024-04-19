using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Threading;
using NetTripoAI.Importers.Images;
using SixLabors.ImageSharp.PixelFormats;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetTripoAI.ImGui
{
    public static class ImguiHelper
    {
        private static GraphicsContext graphicsContext = null;

        public static async Task<Texture> DownloadTextureFromUrl(string url)
        {
            if (graphicsContext == null)
            {
                graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
            }

            Texture result = null;
            using (HttpClient cliente = new HttpClient())
            {
                using (var response = await cliente.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    using (var fileStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fileStream))
                        {
                            RawImageLoader.CopyImageToArrayPool(image, out _, out byte[] data);
                            await EvergineForegroundTask.Run(() =>
                            {
                                TextureDescription desc = new TextureDescription()
                                {
                                    Type = TextureType.Texture2D,
                                    Width = (uint)image.Width,
                                    Height = (uint)image.Height,
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

                                graphicsContext.UpdateTextureData(result, data);
                            });
                        }

                        fileStream.Flush();
                    }

                    return result;
                }
            }
        }
    }
}
