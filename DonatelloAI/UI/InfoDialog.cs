using DonatelloAI.SceneManagers;
using DonatelloAI.TripoAI;
using Evergine.Bindings.Imgui;
using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.UI;

namespace DonatelloAI.UI
{
    public class InfoDialog
    {
        private TripoAIService tripoAIService;

        private bool showDialog = false;

        public bool ShowDialog
        {
            get => this.showDialog;
            set => this.showDialog = value;
        }

        public string Message { get; set; }

        public InfoDialog(TaskManager taskManager)
        {
            this.tripoAIService = Application.Current.Container.Resolve<TripoAIService>();
            this.tripoAIService.InfoEvent += this.ShowInfoEvent;
            taskManager.InfoEvent += this.ShowInfoEvent;
        }

        private void ShowInfoEvent(object sender, string message)
        {
            this.showDialog = true;
            this.Message = message;
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (!this.showDialog)
            {
                return;
            }

            Vector2 textSize;
            ImguiNative.igCalcTextSize(&textSize, this.Message, null, false, 320);

            Vector2 windowsSize = new Vector2(340, textSize.Y + 70);
            ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X * 0.5f, io->DisplaySize.Y * 0.5f), ImGuiCond.Appearing, Vector2.One * 0.5f);
            ImguiNative.igSetNextWindowSize(windowsSize, ImGuiCond.None);
            ImguiNative.igBegin("Info Dialog", this.showDialog.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);

            ImguiNative.igTextWrapped(this.Message);
            ImguiNative.igSpacing();
            ImguiNative.igSpacing();

            Vector2 cursor = Vector2.Zero;
            ImguiNative.igGetCursorPos(&cursor);
            cursor.X += (windowsSize.X * 0.5f) - 40;
            ImguiNative.igSetCursorPos(cursor);
            if (ImguiNative.igButton("Ok", new Vector2(60, 20)))
            {
                this.showDialog = false;
            }

            ImguiNative.igEnd();
        }
    }
}