using DonatelloAI.ImGui;
using DonatelloAI.SceneManagers;
using DonatelloAI.TripoAI;
using Evergine.Bindings.Imgui;
using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.UI;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DonatelloAI.UI
{
    public class ImageToModelPanel
    {
        private TripoAIService tripoAIService;
        private CustomImGuiManager imGuiManager;
        private ModelCollectionManager modelCollectionManager;

        private TripoResponse tripoResponse;
        private int progress = 0;
        private bool isBusy;
        private string imageFilePath;
        private string msg = string.Empty;
        private IntPtr image;
        private IntPtr preview;

        public bool OpenWindow = false;

        public ImageToModelPanel(CustomImGuiManager manager, ModelCollectionManager modelCollectionManager)
        {
            this.imGuiManager = manager;
            this.modelCollectionManager = modelCollectionManager;
            this.tripoAIService = Evergine.Framework.Application.Current.Container.Resolve<TripoAIService>();
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (this.OpenWindow)
            {
                var windowSize = new Vector2(650, 398);
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.None, Vector2.One * 0.5f);
                ImguiNative.igSetNextWindowSize(windowSize, ImGuiCond.None);
                ImguiNative.igBegin("Image to Model", this.OpenWindow.Pointer(), ImGuiWindowFlags.NoResize);

                var buttonSize = new Vector2(50, 19);
                if (ImguiNative.igButton("Choose input image (jpg/jpeg, png)", new Vector2(windowSize.X - buttonSize.X - 18, 19)))
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "Images files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*";
                        openFileDialog.RestoreDirectory = true;

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {

                            this.imageFilePath = openFileDialog.FileName;
                            this.LoadPreviewImage();
                        }
                    }
                }
                ImguiNative.igSameLine(0, 4);
                if (ImguiNative.igButton("Create", buttonSize))
                {
                    this.RequestImageToDraftModel();
                }

                ImguiNative.igProgressBar(this.progress / 100.0f, new Vector2(windowSize.X - 14, buttonSize.Y), this.msg);
                if (this.preview != IntPtr.Zero)
                {
                    ImguiNative.igImageButton(this.preview, Vector2.One * 315, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One);
                }
                ImguiNative.igSameLine(0, 6);
                if (this.image != IntPtr.Zero)
                {
                    if (ImguiNative.igImageButton(this.image, Vector2.One * 315, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
                    {
                        this.modelCollectionManager.DownloadModel(this.tripoResponse);
                        this.OpenWindow = false;
                    }
                }

                ImguiNative.igEnd();
            }
        }

        private void LoadPreviewImage()
        {
            Task.Run(async () =>
            {
                var textureImage = await ImguiHelper.LoadTextureFromFile(this.imageFilePath);
                this.preview = this.imGuiManager.CreateImGuiBinding(textureImage);
            });
        }


        private void RequestImageToDraftModel()
        {
            if (this.isBusy) return;

            Task.Run(async () =>
            {
                this.isBusy = true;

                try
                {
                    // Request draft model
                    this.progress = 0;

                    ImageData imageData = new ImageData(this.imageFilePath);
                    var taskId = await this.tripoAIService.RequestImageToDraftModel(imageData.Base64String, imageData.Extension);                    

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
