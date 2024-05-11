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

namespace DonatelloAI.UI
{
    public class TextToModelPanel
    {
        private TripoAIService tripoAIService;

        public bool OpenWindow = true;
        private byte[] textBuffer = new byte[256];

        private bool firstTime = true;
        private IntPtr image;
        private CustomImGuiManager imGuiManager;
        private ModelCollectionManager modelCollectionManager;
        private int progress = 0;
        private string msg = string.Empty;
        private bool isBusy;
        private TripoResponse tripoResponse;

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
                ImguiNative.igSetNextWindowSize(new Vector2(332, 400), ImGuiCond.Appearing);
                ImguiNative.igBegin("Text to Model", this.OpenWindow.Pointer(), ImGuiWindowFlags.NoResize);

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
                        string prompt = Encoding.UTF8.GetString(buff, textBuffer.Length);
                        var index = prompt.IndexOf('\0');
                        if (index >= 0)
                        {
                            prompt = prompt.Substring(0, index);
                        }

                        this.RequestDraftModel(prompt);
                    }

                    ImguiNative.igProgressBar(this.progress / 100.0f, new Vector2(315, buttonSize.Y), this.msg);
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

        private void RequestDraftModel(string prompt)
        {
            if (this.isBusy || string.IsNullOrEmpty(prompt)) return;

            Task.Run(async () =>
            {
                this.isBusy = true;

                try
                {
                    // Request draft model
                    this.progress = 0;
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
