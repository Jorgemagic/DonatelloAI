using DonatelloAI.ImGui;
using DonatelloAI.SceneManagers;
using Evergine.Bindings.Imgui;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.UI;
using System.Threading.Tasks;

namespace DonatelloAI.UI
{
    public class GalleryPanel
    {
        public bool OpenWindow = true;

        private CustomImGuiManager imGuiManager;
        private ModelCollectionManager modelCollectionManager;
        
        public GalleryPanel(ModelCollectionManager modelCollectionManager)
        {        
            this.modelCollectionManager = modelCollectionManager;         
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (this.OpenWindow)
            {
                int thumbnailWidth = 100;
                int windowsWidth = 220;
                int windowsHeight = 660;
                ImguiNative.igSetNextWindowPos(new Vector2(8, io->DisplaySize.Y * 0.5f), ImGuiCond.Appearing, new Vector2(0,0.5f));
                ImguiNative.igSetNextWindowSize(new Vector2(windowsWidth, windowsHeight), ImGuiCond.Appearing);
                ImguiNative.igBegin("Gallery", this.OpenWindow.Pointer(), ImGuiWindowFlags.None);

                var models = this.modelCollectionManager.Models;
                
                if (models.Count > 0)
                {
                    windowsWidth = (int)ImguiNative.igGetWindowWidth();
                    int imagesPerRow = windowsWidth / thumbnailWidth;
                    for (int i = 0; i <= models.Count / imagesPerRow; i++)
                    {
                        for (int j = 0; j < imagesPerRow; j++)
                        {
                            int index = i * imagesPerRow + j;
                            if (models.Count > index)
                            {
                                var model = models[index];
                                
                                if (j != 0) ImguiNative.igSameLine(0, 4);
                                if (ImguiNative.igImageButton(model.ThumbnailPointer, new Vector2(100), Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
                                {
                                    this.LoadModelFromGallery(model);
                                }
                            }
                        }
                    }
                }

                ImguiNative.igEnd();
            }            
        }

        private void LoadModelFromGallery(ModelData modelData)
        {
            EvergineForegroundTask.Run(async () =>
            {
                await this.modelCollectionManager.LoadModel(modelData);
            });
        }
    }
}
