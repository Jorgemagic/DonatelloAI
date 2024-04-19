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
using System.Runtime.CompilerServices;
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
        private LoadingPanel loadingPanel;
        public bool OpenWindow = true;
        private byte[] textBuffer = new byte[256];

        private bool firstTime = true;
        private IntPtr image;
        private CustomImGuiManager imGuiManager;
        private int progress = 0;
        private string status = string.Empty;
        private bool isBusy;
        private TripoResponse tripoResponse;

        public CreatePanel(CustomImGuiManager manager, LoadingPanel loadingPanel)
        {
            this.imGuiManager = manager;
            this.loadingPanel = loadingPanel;
            this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
            this.screenContextManager = Application.Current.Container.Resolve<ScreenContextManager>();
            this.tripoAIService = Application.Current.Container.Resolve<TripoAIService>();
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (this.OpenWindow)
            {
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.None, Vector2.One * 0.5f);
                ImguiNative.igSetNextWindowSize(new Vector2(332, 400), ImGuiCond.None);
                ImguiNative.igBegin("Create Panel", this.OpenWindow.Pointer(), ImGuiWindowFlags.NoResize);

                var buttonSize = new Vector2(50, 19);
                fixed (byte* buff = textBuffer)
                {
                    ImguiNative.igSetNextItemWidth(315 - buttonSize.X - 4);
                    if (this.firstTime)
                    {
                        ImguiNative.igSetKeyboardFocusHere(0);
                        this.firstTime = false;
                    }

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
                        ////this.RequestAnimateModel("70984fdb-103d-4bb7-9e72-7d5d821d70a9");

                        string prompt = Encoding.UTF8.GetString(buff, textBuffer.Length);
                        var index = prompt.IndexOf('\0');
                        if (index >= 0)
                        {
                            prompt = prompt.Substring(0, index);
                        }

                        // Create pressed
                        this.RequestDraftModel(prompt);
                    }

                    ImguiNative.igProgressBar(this.progress / 100.0f, new Vector2(315, buttonSize.Y), this.status);
                    if (this.image != IntPtr.Zero)
                    {
                        if (ImguiNative.igImageButton(this.image, Vector2.One * 315, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
                        {                                                        
                            this.DownloadModel(this.tripoResponse.data.result.model.url);
                            this.OpenWindow = false;
                        }
                    }
                }

                ImguiNative.igEnd();
            }
        }

        private void RequestDraftModel(string prompt)
        {
            if (this.isBusy) return;

            Task.Run(async () =>
            {
                this.isBusy = true;
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
                    this.status = $"task status:{taskStatus} progress:{this.progress}";


                    taskStatus = this.tripoResponse.data.status;
                }

                if (taskStatus == "success")
                {
                    // View draft model result                
                    var imageUrl = this.tripoResponse.data.result.rendered_image.url;

                    this.status = $"Download image preview ...";

                    var textureImage = await this.tripoAIService.DownloadTextureFromUrl(imageUrl);
                    this.image = this.imGuiManager.CreateImGuiBinding(textureImage);

                    this.status = $"Done!";
                }
                else
                {
                    this.status = $"{taskStatus}";
                }

                this.isBusy = false;
            });
        }

        private void DownloadModel(string modelUrl)
        {
            if (this.isBusy) return;

            Task.Run(async () =>
            {
                this.loadingPanel.Enabled = true;

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

                this.loadingPanel.Enabled = false;
            });
        }

        private void RequestAnimateModel(string task_id)
        {
            if (this.isBusy) return;

            Task.Run(async () =>
            {
                this.isBusy = true;

                // Request animate Model
                this.progress = 0;
                var animateTaskId = await this.tripoAIService.RequestAnimateModel(task_id);

                // Waiting to task completed                
                string taskStatus = string.Empty;
                while (taskStatus == string.Empty ||
                       taskStatus == "queued" ||
                       taskStatus == "running")
                {
                    await Task.Delay(100);
                    this.tripoResponse = await this.tripoAIService.GetTaskStatus(animateTaskId);
                    this.progress = this.tripoResponse.data.progress;
                    this.status = $"task status:{taskStatus} progress:{this.progress}";


                    taskStatus = this.tripoResponse.data.status;
                }

                if (taskStatus == "success")
                {
                    var r = this.tripoResponse;                    
                }
                else
                {
                    this.status = $"{taskStatus}";
                }

                this.isBusy = false;
            });
        }
    }
}
