using DonatelloAI.Importers.GLB;
using DonatelloAI.SceneManagers;
using DonatelloAI.TripoAI;
using Evergine.Bindings.Imgui;
using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.UI;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;
using static BulletSharp.DiscreteCollisionDetectorInterface;

namespace DonatelloAI.UI
{
    public class ConversionPanel
    {
        private TripoAIService tripoAIService;

        private TripoResponse tripoResponse;

        private bool openWindow = true;

        private int currentFormatIndex = 0;
        private bool quadEnabled = false;
        private int faceLimit = 10000;
        private bool flattenBottomEnabled = false;
        private float flattenBottomThreshold = 0.0f;
        private int currentTextureSize = 0;
        private int currentTextureFormatIndex = 3;
        private bool relocatePivot = false;

        private int progress = 0;
        private string msg = string.Empty;
        private bool isBusy;

        public bool OpenWindow
        {
            get => this.openWindow;
            set => this.openWindow = value;
        }

        public ModelData ModelData { get; set; }

        public ConversionPanel()
        {
            this.tripoAIService = Application.Current.Container.Resolve<TripoAIService>();
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (!this.openWindow) return;

            ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.Appearing, Vector2.One * 0.5f);
            ImguiNative.igSetNextWindowSize(new Vector2(400, 250), ImGuiCond.Appearing);
            ImguiNative.igBegin("Conversion", this.openWindow.Pointer(), ImGuiWindowFlags.NoResize);
            
            string formats = "GLTF \0USDZ \0FBX \0OBJ \0STL \0";
            int formatIndex = this.currentFormatIndex;
            ImguiNative.igCombo_Str("Format", &formatIndex, formats, 100);
            this.currentFormatIndex = formatIndex;

            ImguiNative.igCheckbox("Quad", this.quadEnabled.Pointer());

            int limit = this.faceLimit;
            ImguiNative.igInputInt("Face limit", &limit, 1, 10, ImGuiInputTextFlags.None);
            this.faceLimit = limit;

            ImguiNative.igCheckbox("Flatten bottom", this.flattenBottomEnabled.Pointer());

            float threshold = this.flattenBottomThreshold;
            ImguiNative.igInputFloat("Flatten threshold", &threshold, 0.01f, 0.05f, null, ImGuiInputTextFlags.None);
            this.flattenBottomThreshold = threshold;

            int sizeIndex = this.currentTextureSize;
            string textureSizes = "2048 \0 1024\0 512\0 256 \0";
            ImguiNative.igCombo_Str("Texture Size", &sizeIndex, textureSizes, 100);
            this.currentTextureSize = sizeIndex;

            int textureFormatIndex = this.currentTextureFormatIndex;
            string textureFormats = "BMP \0DPX \0HDR \0JPEG \0OPEN_EXR \0PNG \0TARGA \0TIFF \0WEBP";
            ImguiNative.igCombo_Str("Texture Format", &textureFormatIndex, textureFormats, 100);
            this.currentTextureFormatIndex = textureFormatIndex;

            ImguiNative.igCheckbox("Pivot to center bottom", this.relocatePivot.Pointer());

            ImguiNative.igSeparator();

            ImguiNative.igSpacing();
            ImguiNative.igSpacing();

            var buttonSize = new Vector2(60, 19);
            ImguiNative.igProgressBar(this.progress / 100.0f, new Vector2(383 - buttonSize.X - 4, buttonSize.Y), this.msg);
            ImguiNative.igSameLine(0, 4);
            if (ImguiNative.igButton("Convert", buttonSize))
            {                
                this.RequestConversionModel();
            }

            ImguiNative.igEnd();
        }

        private async void RequestConversionModel()
        {
            if (this.isBusy || this.ModelData == null) return;            

            string modelUri = null;
            await Task.Run(async () =>
            {
                this.isBusy = true;

                try
                {
                    // Request draft model
                    this.progress = 0;
                    this.msg = $"Starting the request ...";
                    var taskId = await this.tripoAIService.RequestConversion(
                                                                              "47c2ead9-ec6a-4f69-97e8-d1170f0c8fdf", //this.ModelData.TaskId,
                                                                              TripoAIService.ModelFormat.STL,
                                                                              this.quadEnabled,
                                                                              this.faceLimit,
                                                                              this.flattenBottomEnabled,
                                                                              this.flattenBottomThreshold,
                                                                              2048,
                                                                              TripoAIService.TextureFormat.JPEG,
                                                                              this.relocatePivot
                                                                              );

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
                        modelUri = this.tripoResponse.data.output.model;                        
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

            // Saving converted file
            if (!string.IsNullOrEmpty(modelUri))
            {
                string fileNameWithExtension = Path.GetFileName(modelUri);
                fileNameWithExtension = fileNameWithExtension.Substring(0, fileNameWithExtension.IndexOf("?"));
                string extension = Path.GetExtension(fileNameWithExtension);

                string filter = $"{extension.Substring(1)} File ({extension})|*{extension}";                

                using (System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog())
                {
                    saveFileDialog.Filter = filter;
                    saveFileDialog.RestoreDirectory = true;

                    if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var filepath = saveFileDialog.FileName;
                        await this.SaveModelAsync(modelUri, filepath);
                    }
                }                
            }

            this.openWindow = false;
        }
        
        private async Task SaveModelAsync(string uri, string filepath)
        {
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();

                    // Save file to disc
                    using (var s = await client.GetStreamAsync(uri))
                    {
                        using (var fs = new FileStream(filepath, FileMode.CreateNew))
                        {
                            await s.CopyToAsync(fs);
                        }
                    }
                }
            }
        }
    }
}
