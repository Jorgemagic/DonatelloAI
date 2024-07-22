using DonatelloAI.Components;
using DonatelloAI.SceneManagers;
using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;

namespace DonatelloAI.UI
{
    public class ToolbarPanel
    {
        public bool OpenWindow = true;

        private Manipulation manipulation;
        private ModelCollectionManager modelCollectionManager;

        public ToolbarPanel(Manipulation manipulation, ModelCollectionManager modelCollectionManager) 
        {
            this.manipulation = manipulation;
            this.modelCollectionManager = modelCollectionManager;
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (!this.OpenWindow)
            {
                return;
            }

            int windowsWidth = 247;
            int windowsHeight = 52;
            ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X - 128, 40), ImGuiCond.Appearing, new Vector2(1, 0));
            ImguiNative.igSetNextWindowSize(new Vector2(windowsWidth, windowsHeight), ImGuiCond.Appearing);
            ImguiNative.igBegin("Toolbar", this.OpenWindow.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);

            int buttonSize = 35;
            if (ImguiNative.igButton("M", Vector2.One * buttonSize))
            {
                this.manipulation.Operation = Evergine.Bindings.Imguizmo.OPERATION.TRANSLATE;
            }

            ImguiNative.igSameLine(0,4);
            if (ImguiNative.igButton("R", Vector2.One * buttonSize))
            {
                this.manipulation.Operation = Evergine.Bindings.Imguizmo.OPERATION.ROTATE;
            }

            ImguiNative.igSameLine(0, 4);
            if (ImguiNative.igButton("S", Vector2.One * buttonSize))
            {
                this.manipulation.Operation = Evergine.Bindings.Imguizmo.OPERATION.SCALE;
            }

            ImguiNative.igSameLine(0, 4);
            if (ImguiNative.igButton("U", Vector2.One * buttonSize))
            {
                this.manipulation.Operation = Evergine.Bindings.Imguizmo.OPERATION.UNIVERSAL;
            }

            ImguiNative.igSameLine(0, 8);
            if (ImguiNative.igButton("W", Vector2.One * buttonSize))
            {
                this.modelCollectionManager.RenderType = ModelCollectionManager.RenderMode.Wireframe;
            }

            ImguiNative.igSameLine(0, 4);
            if (ImguiNative.igButton("O", Vector2.One * buttonSize))
            {
                this.modelCollectionManager.RenderType = ModelCollectionManager.RenderMode.Solid;
            }

            ImguiNative.igEnd();
        }
    }
}
