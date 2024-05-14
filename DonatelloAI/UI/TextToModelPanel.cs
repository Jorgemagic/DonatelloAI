using Evergine.Bindings.Imgui;
using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.UI;
using DonatelloAI.ImGui;
using DonatelloAI.SceneManagers;
using DonatelloAI.TripoAI;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace DonatelloAI.UI
{
    public class TextToModelPanel
    {
        private TripoAIService tripoAIService;
        
        private byte[] promptTextBuffer = new byte[1024];
        private byte[] negativeTextBuffer = new byte[1024];

        private IntPtr image;
        private CustomImGuiManager imGuiManager;
        private ModelCollectionManager modelCollectionManager;
        private int progress = 0;
        private string msg = string.Empty;
        private bool isBusy;
        private TripoResponse tripoResponse;

        private bool openWindow = true;

        public bool OpenWindow
        {
            get => this.openWindow;
            set
            {
                this.openWindow = value;
                if (value)
                {
                    this.Reset();
                }
            }
        }

        public TextToModelPanel(CustomImGuiManager manager, ModelCollectionManager modelCollectionManager)
        {
            this.imGuiManager = manager;
            this.modelCollectionManager = modelCollectionManager;
            this.tripoAIService = Application.Current.Container.Resolve<TripoAIService>();
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (this.OpenWindow)
            {
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.Appearing, Vector2.One * 0.5f);
                ImguiNative.igSetNextWindowSize(new Vector2(333, 495), ImGuiCond.Appearing);
                ImguiNative.igBegin("Text to Model", this.openWindow.Pointer(), ImGuiWindowFlags.NoResize);

                var buttonSize = new Vector2(50, 19);
                fixed (byte* promptBuffer = promptTextBuffer)
                fixed (byte* negativeBuffer = negativeTextBuffer)
                {
                    ImguiNative.igText("Prompt");
                    ImguiNative.igInputTextMultiline(                  
                        "##prompt",                        
                        promptBuffer,
                        (uint)promptTextBuffer.Length,
                        new Vector2(315, 40),
                        ImGuiInputTextFlags.EnterReturnsTrue,
                        null,
                        null);

                    ImguiNative.igText("Negative (Optional)");
                    ImguiNative.igInputTextMultiline(
                        "##negative",
                        negativeBuffer,
                        (uint)negativeTextBuffer.Length,
                        new Vector2(315, 40),
                        ImGuiInputTextFlags.EnterReturnsTrue,
                        null,
                        null);

                    ImguiNative.igProgressBar(this.progress / 100.0f, new Vector2(315 - buttonSize.X - 4, buttonSize.Y), this.msg);
                    ImguiNative.igSameLine(0, 4);
                    if (ImguiNative.igButton("Create", buttonSize))
                    {
                        string prompt = Encoding.UTF8.GetString(promptBuffer, promptTextBuffer.Length);
                        var index = prompt.IndexOf('\0');
                        if (index >= 0)
                        {
                            prompt = prompt.Substring(0, index);
                        }

                        this.RequestDraftModel(prompt);
                    }
                    
                    if (this.image != IntPtr.Zero)
                    {
                        if (ImguiNative.igImageButton(this.image, Vector2.One * 315, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
                        {
                            this.modelCollectionManager.DownloadModel(this.tripoResponse);
                            this.OpenWindow = false;
                        }
                    }
                }

                ImguiNative.igEnd();
            }
        }

        private void Reset()
        {
            this.progress = 0;
            this.msg = string.Empty;
            this.promptTextBuffer = new byte[256];
            this.ResetImage();
        }

        private void ResetImage()
        {
            this.image = ImguiHelper.SetNoPreviewImage(this.imGuiManager);
        }

        private void RequestDraftModel(string prompt)
        {
            this.ResetImage();

            if (this.isBusy || string.IsNullOrEmpty(prompt)) return;

            Task.Run(async () =>
            {
                this.isBusy = true;

                try
                {
                    // Request draft model
                    this.progress = 0;
                    this.msg = $"Starting the request ...";
                    var taskId = await this.tripoAIService.RequestADraftModel(prompt);

                    if (string.IsNullOrEmpty(taskId))
                    {
                        this.isBusy = false;
                        return;
                    }

                    // Waiting to task completed                
                    string status = string.Empty;
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        this.tripoResponse = await this.tripoAIService.GetTaskStatus(taskId);
                        this.progress = this.tripoResponse.data.progress;
                        this.msg = $"status:{status} progress:{this.progress}";


                        status = this.tripoResponse.data.status;
                    }

                    if (status == "success")
                    {
                        // View draft model result                
                        var imageUrl = this.tripoResponse.data.result.rendered_image.url;

                        this.msg = $"Download image preview ...";

                        var textureImage = await ImguiHelper.DownloadTextureFromUrl(imageUrl);
                        this.image = this.imGuiManager.CreateImGuiBinding(textureImage);

                        this.msg = $"Done!";
                    }
                    else
                    {
                        this.msg = $"{status}";
                    }
                }
                catch (Exception) { }
                finally
                {
                    this.isBusy = false;
                }
            });
        }
    }
}
