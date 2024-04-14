using Evergine.Bindings.Imgui;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.UI;
using NetTripoAI.ImGui;
using System;

namespace NetTripoAI.UI
{
    public unsafe class UIBehavior : Behavior
    {
        [BindSceneManager]
        private CustomImGuiManager imguiManager;

        private CreatePanel createPanel;

        protected override void OnActivated()
        {
            base.OnActivated();

            this.createPanel = new CreatePanel(imguiManager);
        }

        protected override void Update(TimeSpan gameTime)
        {            
            var io = ImguiNative.igGetIO();

            ////bool open = true;
            ////ImguiNative.igShowDemoWindow(open.Pointer());
            this.createPanel.Show(ref io);
        }
    }
}
