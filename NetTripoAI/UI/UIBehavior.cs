using Evergine.Bindings.Imgui;
using Evergine.Bindings.Imguizmo;
using Evergine.Common.Graphics;
using Evergine.Common.Input;
using Evergine.Common.Input.Keyboard;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Mathematics;
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

            // Imguizmo
            ImguizmoNative.ImGuizmo_SetRect(0, 0, io->DisplaySize.X, io->DisplaySize.Y);

            var camera = this.Managers.RenderManager.ActiveCamera3D;
            Matrix4x4 view = camera.View;
            Matrix4x4 project = camera.Projection;

            ImguizmoNative.ImGuizmo_ViewManipulate(view.Pointer(), 2, Vector2.Zero, new Vector2(128, 128), 0x10101010);

            Matrix4x4.Invert(ref view, out Matrix4x4 iview);
            var translation = iview.Translation;
            var rotation = iview.Rotation;

            Vector3* r = &rotation;
            camera.Transform.LocalRotation = *r;

            Vector3* t = &translation;
            camera.Transform.LocalPosition = *t;

            // Panels            
            this.createPanel.Show(ref io);
            this.loadingPanel.Show(ref io);

            // Input
            KeyboardDispatcher keyboardDispatcher = this.graphicsPresenter.FocusedDisplay?.KeyboardDispatcher;
            if (keyboardDispatcher?.ReadKeyState(Keys.Space) == ButtonState.Pressing)
            {
                this.createPanel.OpenWindow = true;
            }
        }
    }
}
