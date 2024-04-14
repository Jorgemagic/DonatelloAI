using Evergine.Bindings.Imgui;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.UI;
using NetTripoAI.ImGui;
using NetTripoAI.TripoAI;
using System;
using System.Text;
using System.Threading.Tasks;

namespace NetTripoAI.UI
{
    public class CreatePanel
    {
        private TripoAIService tripoAIService;

        private GraphicsContext graphicsContext;
        private ScreenContextManager screenContextManager;
        private AssetsService assetsService;

        private bool open_window = true;
        private byte[] textBuffer = new byte[256];

        private IntPtr image;
        private CustomImGuiManager imGuiManager;
        private int progress = 0;
        private string status = string.Empty;
        private TripoResponse tripoResponse;

        public CreatePanel(CustomImGuiManager manager)
        {
            this.imGuiManager = manager;
            this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
            this.screenContextManager = Application.Current.Container.Resolve<ScreenContextManager>();
            this.tripoAIService = Application.Current.Container.Resolve<TripoAIService>();
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (open_window)
            {
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.None, Vector2.One * 0.5f);
                ImguiNative.igSetNextWindowSize(new Vector2(400, 400), ImGuiCond.None);
                ImguiNative.igBegin("CreatePanel", this.open_window.Pointer(), ImGuiWindowFlags.None);

                var buttonSize = new Vector2(50, 19);
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

                    ImguiNative.igSameLine(0, 4);
                    if (ImguiNative.igButton("Create", buttonSize))
                    {
                        string prompt = Encoding.UTF8.GetString(buff, textBuffer.Length);
                        var index = prompt.IndexOf('\0');
                        if (index >= 0)
                        {
                            prompt = prompt.Substring(0, index);
                        }

                        // Create pressed
                        this.RequestDraftModel(prompt);
                    }

                    ImguiNative.igImage(this.image, Vector2.One * 315, Vector2.Zero, Vector2.One, Vector4.One, Vector4.Zero);
                    ImguiNative.igProgressBar(this.progress / 100.0f, new Vector2(315 - buttonSize.X, buttonSize.Y), this.status);
                    ImguiNative.igSameLine(0, 4);
                    if (ImguiNative.igButton("Go", buttonSize))
                    {
                        this.DownloadModel(this.tripoResponse.data.result.model.url);
                    }
                }

                ImguiNative.igEnd();
            }
        }

        private void RequestDraftModel(string prompt)
        {
            Task.Run(async () =>
            {
                // Request draft model
                this.progress = 0;
                var taskId = await this.tripoAIService.RequestADraftModel(prompt);

                // Waiting to task completed                
                string taskStatus = string.Empty;
                while (taskStatus == string.Empty ||
                       taskStatus == "queued" ||
                       taskStatus == "running")
                {
                    await Task.Delay(100);
                    this.tripoResponse = await this.tripoAIService.GetTaskStatus(taskId);
                    this.progress = this.tripoResponse.data.progress;
                    this.status = $"task status:{this.progress}";


                    taskStatus = this.tripoResponse.data.status;
                }

                // View draft model result                
                var imageUrl = this.tripoResponse.data.result.rendered_image.url;

                this.progress = 0;
                this.status = $"Download image:{this.progress}";

                var textureImage = await this.tripoAIService.DownloadTextureFromUrl(imageUrl);
                this.image = this.imGuiManager.CreateImGuiBinding(textureImage);

                this.progress = 100;
                this.status = $"Download image:{this.progress}";                
            });
        }     

        private void DownloadModel(string modelUrl)
        {
            Task.Run(async () =>
            {
                var model = await this.tripoAIService.DownloadModelFromURL(modelUrl);

                var currentScene = screenContextManager.CurrentContext[0];

                var entity = model.InstantiateModelHierarchy(this.assetsService);

                var root = new Entity()
                                .AddComponent(new Transform3D());
                root.AddChild(entity);

                var boundingBox = model.BoundingBox.Value;
                boundingBox.Transform(entity.FindComponent<Transform3D>().WorldTransform);
                root.FindComponent<Transform3D>().Scale = Vector3.One * (1.0f / boundingBox.HalfExtent.Length());
                root.AddComponent(new BoxCollider3D()
                {
                    Size = boundingBox.HalfExtent * 2,
                    Offset = boundingBox.Center,
                });
                root.AddComponent(new StaticBody3D());

                currentScene.Managers.EntityManager.Add(root);
            });
        }
    }
}
