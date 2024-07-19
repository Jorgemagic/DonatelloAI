using DonatelloAI.ImGui;
using DonatelloAI.SceneManagers;
using DonatelloAI.TripoAI;
using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;
using System;
using System.IO;
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
        private float minX, minY, maxX, maxY;
        private IntPtr previewPlaceholderPointer;

        private bool openWindow = false;

        public bool OpenWindow 
        {
            get => this.openWindow;
            set
            {
                this.openWindow = value;
                if (value)
                {
                    this.ResetImages(true);
                }
            }
        }    

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
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.Appearing, Vector2.One * 0.5f);
                ImguiNative.igSetNextWindowSize(windowSize, ImGuiCond.Appearing);
                ImguiNative.igBegin("Image to Model", this.openWindow.Pointer(), ImGuiWindowFlags.NoResize);

                var buttonSize = new Vector2(50, 19);
                ImguiNative.igPushStyleVar_Vec2(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
                if (ImguiNative.igButton("Select an input image (jpg/jpeg, png) ...", new Vector2(windowSize.X - buttonSize.X - 18, 19)))
                {
                    SelectImagePath();
                }
                ImguiNative.igPopStyleVar(1);
                ImguiNative.igSameLine(0, 4);
                if (ImguiNative.igButton("Create", buttonSize))
                {
                    this.ResetImages();
                    this.RequestImageToDraftModel();
                }

                ImguiNative.igProgressBar(this.progress / 100.0f, new Vector2(windowSize.X - 14, buttonSize.Y), this.msg);
                if (this.preview != IntPtr.Zero)
                {
                    if (ImguiNative.igImageButton(this.preview, Vector2.One * 315, new Vector2(minX, minY), new Vector2(maxX, maxY), 0, Vector4.Zero, Vector4.One))
                    {
                        this.SelectImagePath();
                    }
                }
                ImguiNative.igSameLine(0, 6);
                if (this.image != IntPtr.Zero)
                {
                    if (ImguiNative.igImageButton(this.image, Vector2.One * 315, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
                    {
                        if (this.tripoResponse != null)
                        {
                            var modelURL = tripoResponse.data.output.model;
                            var taskID = this.tripoResponse.data.task_id;
                            var thumbnailURL = this.tripoResponse.data.result.rendered_image.url;
                            this.modelCollectionManager.DownloadModel(modelURL, taskID, thumbnailURL, "From Image");
                            this.OpenWindow = false;
                        }
                    }
                }

                ImguiNative.igEnd();
            }
        }

        private void SelectImagePath()
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

        private void ResetImages(bool both = false)
        {
            Task.Run(() =>            
            {
                var noImage = ImguiHelper.SetNoPreviewImage(this.imGuiManager);
                this.image = noImage;
                if (both)
                {
                    this.preview = noImage;
                    this.minX = this.minY = 0;
                    this.maxX = this.maxY = 1;
                }
            });
        }

        private void LoadPreviewImage()
        {
            Task.Run(async () =>
            {
                var textureImage = await ImguiHelper.LoadTextureFromFile(this.imageFilePath);
                var width = textureImage.Description.Width;
                var height = textureImage.Description.Height;
                this.minX = this.minY = 0;
                this.maxX = this.maxY = 1;
                float square = 315.0f;
                if (width > height)
                {
                    var offset = (square - (height * (square / width))) / square;
                    var offsetOverTwo = offset / 2.0f;
                    this.minY = -offsetOverTwo;
                    this.maxY = 1.0f + offsetOverTwo;
                }
                else
                {
                    var offset = (square - (width * (square / height))) / square;
                    var offsetOverTwo = offset / 2.0f;
                    this.minX = -offsetOverTwo;
                    this.maxX = 1.0f + offsetOverTwo;
                }
                this.preview = this.imGuiManager.CreateImGuiBinding(textureImage);
            });
        }

        private void RequestImageToDraftModel()
        {
            if (this.isBusy || string.IsNullOrEmpty(this.imageFilePath)) return;

            Task.Run(async () =>
            {
                this.isBusy = true;

                try
                {
                    // Request draft model
                    this.progress = 0;

                    this.msg = $"Starting the request ...";
                    var imageToken = await this.tripoAIService.RequestUploadImage(this.imageFilePath);
                    var extension = Path.GetExtension(this.imageFilePath).Substring(1);
                    var taskId = await this.tripoAIService.RequestImageToDraftModel(imageToken, extension);

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
