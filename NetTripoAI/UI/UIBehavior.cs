using Evergine.Bindings.Imgui;
using Evergine.Common.Graphics;
using Evergine.Common.Input;
using Evergine.Common.Input.Keyboard;
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

        [BindService]
        protected GraphicsPresenter graphicsPresenter;

        private CreatePanel createPanel;
        private LoadingPanel loadingPanel;

        protected override void OnActivated()
        {
            base.OnActivated();

            this.loadingPanel = new LoadingPanel();
            this.createPanel = new CreatePanel(imguiManager, this.loadingPanel);
        }

        protected override void Update(TimeSpan gameTime)
        {            
            var io = ImguiNative.igGetIO();

            ////bool open = true;
            ////ImguiNative.igShowDemoWindow(open.Pointer());
            this.createPanel.Show(ref io);
            this.loadingPanel.Show(ref io);

            KeyboardDispatcher keyboardDispatcher = this.graphicsPresenter.FocusedDisplay?.KeyboardDispatcher;
            if (keyboardDispatcher?.ReadKeyState(Keys.Space) == ButtonState.Pressing)
            {
                this.createPanel.OpenWindow = true;
            }
        }
    }
}
