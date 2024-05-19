using DonatelloAI.SceneManagers;
using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;

namespace DonatelloAI.UI
{
    public class InfoDialog
    {
        private bool showDialog = false;

        public bool ShowDialog { get; set; }

        public string Message { get; set; }

        public InfoDialog(TaskManager taskManager)
        {
            taskManager.InfoEvent += (s, m) =>
            {
                this.showDialog = true;
                this.Message = m;
            };
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (!this.showDialog)
            {
                return;
            }

            Vector2 textSize;
            ImguiNative.igCalcTextSize(&textSize, this.Message, null, false, 320);

            ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.Appearing, Vector2.One * 0.5f);
            ImguiNative.igSetNextWindowSize(new Vector2(340, textSize.Y + 150), ImGuiCond.None);
            ImguiNative.igBegin("Info Dialog", this.showDialog.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);

            ImguiNative.igTextWrapped(this.Message);
            if (ImguiNative.igButton("Ok", new Vector2(60,20)))
            {
                this.showDialog = false;
            }

            ImguiNative.igEnd();
        }
    }
}