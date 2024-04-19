using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;
using NetTripoAI.SceneManagers;

namespace NetTripoAI.UI
{
    public class TaskListPanel
    {
        public bool OpenWindow = true;

        private TaskManager taskManager;

        public TaskListPanel(TaskManager taskManager)
        {
            this.taskManager = taskManager;
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (this.OpenWindow)
            {
                int windowsWidth = 600;
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X - 8, io->DisplaySize.Y - 8), ImGuiCond.None, Vector2.One);
                ImguiNative.igSetNextWindowSize(new Vector2(windowsWidth, 105), ImGuiCond.None);
                ImguiNative.igBegin("Task list", this.OpenWindow.Pointer(), ImGuiWindowFlags.NoResize);

                var tasks = this.taskManager.TaskCollection;

                if (tasks.Count > 0)
                {
                    ImguiNative.igBeginTable("##Tasks", 2, ImGuiTableFlags.None, Vector2.Zero, 0);
                    int textColumnWidth = 200;
                    ImguiNative.igTableSetupColumn("##AAA", ImGuiTableColumnFlags.WidthFixed, textColumnWidth, 0);

                    for (int i = tasks.Count - 1; i >= 0; i--)
                    {
                        SceneManagers.TaskStatus task = tasks[i];
                        ImguiNative.igTableNextRow(ImGuiTableRowFlags.None, 20);
                        ImguiNative.igTableNextColumn();
                        ImguiNative.igText($"T:{task.Type} M:{task.ModelName}");
                        ImguiNative.igTableNextColumn();
                        ImguiNative.igProgressBar(task.progress / 100.0f, new Vector2(windowsWidth - textColumnWidth - 25, 19), task.msg);
                    }

                    /*ImguiNative.igTableNextRow(ImGuiTableRowFlags.None, 20);
                    ImguiNative.igTableNextColumn();
                    ImguiNative.igText($"M:Test T:Refine");
                    ImguiNative.igTableNextColumn();
                    ImguiNative.igProgressBar(0.9f, new Vector2(windowsWidth - textColumnWidth - 25, 19), null);

                    ImguiNative.igTableNextRow(ImGuiTableRowFlags.None, 20);
                    ImguiNative.igTableNextColumn();
                    ImguiNative.igText($"M:a small cat T:Animate");
                    ImguiNative.igTableNextColumn();
                    ImguiNative.igProgressBar(1.0f, new Vector2(windowsWidth - textColumnWidth - 25, 19), null);*/

                    ImguiNative.igEndTable();
                }

                ImguiNative.igEnd();
            }
        }
    }
}
