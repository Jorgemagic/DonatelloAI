using DonatelloAI.ImGui;
using DonatelloAI.SceneManagers;
using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;

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
                ImguiNative.igSetNextWindowPos(new Vector2(8, 27), ImGuiCond.Appearing, Vector2.Zero);
                ImguiNative.igSetNextWindowSize(new Vector2(windowsWidth, 600), ImGuiCond.Appearing);
                ImguiNative.igBegin("Gallery", this.OpenWindow.Pointer(), ImGuiWindowFlags.None);

                var models = this.modelCollectionManager.Models;
                
                if (models.Count > 0)
                {

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
                                ImguiNative.igImageButton(model.ThumbnailPointer, new Vector2(100), Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One);
                            }
                        }
                    }

                    /*ImguiNative.igBeginTable("##Models", 2, ImGuiTableFlags.None, Vector2.Zero, 0);
                    int textColumnWidth = 200;
                    ImguiNative.igTableSetupColumn("##AAA", ImGuiTableColumnFlags.WidthFixed, textColumnWidth, 0);

                    int imagesPerRow = windowsWidth / thumbnailWidth;                    
                    for (int i = 0; i <= models.Length / imagesPerRow; i++)
                    {
                        ImguiNative.igTableNextRow(ImGuiTableRowFlags.None, 20);

                        for (int j = 0; j < imagesPerRow; j++)
                        {
                            int index = i * imagesPerRow + j;
                            if (models.Length > index)
                            {
                                var model = models[index];

                                ImguiNative.igTableNextColumn();
                                ImguiNative.igText($"{model.TaskId}");
                                ImguiNative.igImageButton(System.IntPtr.Zero, new Vector2(100), Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One);
                            }
                        }
                    }

                    ImguiNative.igEndTable();*/
                }

                ImguiNative.igEnd();
            }
        }
    }
}
