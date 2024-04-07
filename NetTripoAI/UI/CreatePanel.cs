using Evergine.Bindings.Imgui;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.UI;
using NetTripoAI.Helpers;
using NetTripoAI.ImGui;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NetTripoAI.UI
{
    public class CreatePanel
    {
        private GraphicsContext graphicsContext;

        private bool open_window = true;
        private byte[] textBuffer = new byte[256];

        private IntPtr image;
        private CustomImGuiManager imGuiManager;

        public CreatePanel(CustomImGuiManager manager)
        {            
            this.imGuiManager = manager;   
            this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (open_window)
            {
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.None, Vector2.One * 0.5f);
                ImguiNative.igSetNextWindowSize(new Vector2(400, 400), ImGuiCond.None);
                ImguiNative.igBegin("CreatePanel", this.open_window.Pointer(), ImGuiWindowFlags.None);

                fixed (byte* buff = textBuffer)
                {
                    ImguiNative.igInputTextWithHint(
                        "##prompt",
                        "Prompt text here",
                        buff,
                        (uint)textBuffer.Length,
                        ImGuiInputTextFlags.None,
                        null,
                        null);

                    ImguiNative.igSameLine(0, 0);
                    if (ImguiNative.igButton("Create", new Vector2(50, 20)))
                    {
                        string prompt = Encoding.UTF8.GetString(buff, textBuffer.Length);
                        var index = prompt.IndexOf('\0');
                        if (index >= 0)
                        {
                            prompt = prompt.Substring(0, index);
                        }

                        // Create pressed
                        this.DownloadImage();
                    }

                    ImguiNative.igImage(this.image, Vector2.One * 100, Vector2.Zero, Vector2.One, Vector4.One, Vector4.Zero);
                }

                ImguiNative.igEnd();
            }
        }

        private void DownloadImage()
        {
            Task.Run(async () =>
            {
                var textureImage = await this.DownloadTextureFromUrl(@"https://cdn.pixabay.com/photo/2015/10/01/17/17/car-967387_640.png");
                this.image = this.imGuiManager.CreateImGuiBinding(textureImage);
            });
        }

        public async Task<Texture> DownloadTextureFromUrl(string url)
        {
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
                                result = this.graphicsContext.Factory.CreateTexture(ref desc);

                                this.graphicsContext.UpdateTextureData(result, data);
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
