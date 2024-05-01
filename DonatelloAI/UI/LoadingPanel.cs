using Evergine.Bindings.Imgui;
using Evergine.Common.Graphics;
using Evergine.Mathematics;
using Evergine.UI;
using DonatelloAI.SceneManagers;

namespace DonatelloAI.UI
{
    public unsafe class LoadingPanel
    {
        private bool imguiBars = true;
        private bool isBusy;

        public LoadingPanel(ModelCollectionManager modelCollectionManager)
        {
            modelCollectionManager.IsBusyChanged += (s,e) => this.isBusy = e;
        }

        public void Show(ref ImGuiIO* io)
        {
            if (!this.isBusy)
            {
                return;
            }

            int radius = 30;
            int radiusOverTwo = radius / 2;

            ImguiNative.igSetNextWindowPos(new Vector2((io->DisplaySize.X * 0.5f) - 50, (io->DisplaySize.Y * 0.5f) - 50), ImGuiCond.None, Vector2.Zero);
            ImguiNative.igSetNextWindowSize(new Vector2(70), ImGuiCond.None);
            ImguiNative.igBegin("Loading", this.imguiBars.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground);

            var cmdList = ImguiNative.igGetWindowDrawList();

            Vector2 middlePos = new Vector2((io->DisplaySize.X * 0.5f) - radiusOverTwo, (io->DisplaySize.Y * 0.5f) - radiusOverTwo);

            cmdList->AddCircleFilled(middlePos, radius, (uint)Color.Blue.ToInt(), 0);

            float time = (float)ImguiNative.igGetTime();

            Vector2 pos = middlePos - (Vector2.One * radius);
            int numSegments = 30;

            Vector2 centre = new Vector2(pos.X + radius, pos.Y + radius);
            for (int i = 0; i < numSegments; i++)
            {
                float a = i / (float)numSegments;
                cmdList->PathLineTo(new Vector2(
                                                centre.X + ((float)System.Math.Cos(a + (time * 8.0f)) * radius),
                                                centre.Y + ((float)System.Math.Sin(a + (time * 8.0f)) * radius)));
            }

            cmdList->PathStroke((uint)Color.White.ToInt(), ImDrawFlags.None, 3);

            ImguiNative.igEnd();
        }
    }
}
