using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;
using DonatelloAI.SceneManagers;

namespace DonatelloAI.UI
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
                ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X - 8, io->DisplaySize.Y - 8), ImGuiCond.Appearing, Vector2.One);
                ImguiNative.igSetNextWindowSize(new Vector2(windowsWidth, 105), ImGuiCond.Appearing);
                ImguiNative.igBegin("Task list", this.OpenWindow.Pointer(), ImGuiWindowFlags.None);

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

                    ImguiNative.igEndTable();
                }

                ImguiNative.igEnd();
            }
        }
    }
}
