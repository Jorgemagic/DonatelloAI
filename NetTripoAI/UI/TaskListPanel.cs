using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;

namespace NetTripoAI.UI
{
    public class TaskListPanel
    {
        public bool OpenWindow = true;

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (this.OpenWindow)
            {
                int windowsWidth = 600;
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X - 8, io->DisplaySize.Y - 8), ImGuiCond.None, Vector2.One);
                ImguiNative.igSetNextWindowSize(new Vector2(windowsWidth, 105), ImGuiCond.None);
                ImguiNative.igBegin("Task list", this.OpenWindow.Pointer(), ImGuiWindowFlags.NoResize);

                ImguiNative.igBeginTable("Tasks", 2, ImGuiTableFlags.None, Vector2.Zero, 0);
                int textColumnWidth = 100;
                ImguiNative.igTableSetupColumn("AAA", ImGuiTableColumnFlags.WidthFixed, textColumnWidth, 0);
                for (int i = 0; i < 3; i++)
                {                    
                    ImguiNative.igTableNextRow(ImGuiTableRowFlags.None, 20);
                    ImguiNative.igTableNextColumn();
                    ImguiNative.igText($"Model {i} dfa dfa dasf");
                    ImguiNative.igTableNextColumn();
                    ImguiNative.igProgressBar(0.5f, new Vector2(windowsWidth - textColumnWidth - 8, 19), "progress");
                }
                ImguiNative.igEndTable();

                ImguiNative.igEnd();
            }
        }
    }
}
