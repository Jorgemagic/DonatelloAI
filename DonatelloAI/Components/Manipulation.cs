using Evergine.Bindings.Imgui;
using Evergine.Bindings.Imguizmo;
using Evergine.Common.Input;
using Evergine.Common.Input.Keyboard;
using Evergine.Common.Input.Mouse;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.UI;
using DonatelloAI.SceneManagers;
using System;

namespace DonatelloAI.Components
{
    public unsafe class Manipulation : Behavior
    {
        [BindService]
        protected GraphicsPresenter graphicsPresenter = null;

        [BindSceneManager]
        private ModelCollectionManager modelCollectionManager = null;

        private Transform3D transform = null;

        private Matrix4x4 view;
        private Matrix4x4 projection;
        private Matrix4x4 world;

        public bool PickingEnabled { get; set; } = true;

        public OPERATION Operation { get; set; }

        public Manipulation()
        {
            this.Operation = OPERATION.TRANSLATE;
        }

        protected override void Update(TimeSpan gameTime)
        {
            // Selected element
            var camera = Managers.RenderManager?.ActiveCamera3D;            

            var mouseDispatcher = camera.Display?.MouseDispatcher;
            if (mouseDispatcher != null)
            {
                if (this.PickingEnabled)
                {
                    if (mouseDispatcher.ReadButtonState(MouseButtons.Left) == ButtonState.Pressing ||
                        mouseDispatcher.ReadButtonState(MouseButtons.Right) == ButtonState.Pressing)
                    {
                        var pos = mouseDispatcher.Position.ToVector2();

                        camera.CalculateRay(ref pos, out var ray);

                        Entity selectedEntity = null;
                        var hitResult = Managers.PhysicManager3D.RayCast(ref ray, 100);
                        if (hitResult.Succeeded)
                        {
                            selectedEntity = hitResult.PhysicBody.BodyComponent.Owner;
                            transform = selectedEntity.FindComponent<Transform3D>();

                        }
                        else
                        {
                            transform = null;
                            selectedEntity = null;
                        }
                        this.modelCollectionManager.CurrentSelectedEntity = selectedEntity;
                    }
                }


                // Show Manipulator
                if (transform != null)
                {
                    // Keyboard
                    KeyboardDispatcher keyboardDispatcher = graphicsPresenter.FocusedDisplay?.KeyboardDispatcher;

                    if (keyboardDispatcher?.ReadKeyState(Keys.Delete) == ButtonState.Pressing)
                    {
                        Managers.EntityManager.Remove(this.modelCollectionManager.CurrentSelectedEntity);
                        transform = null;
                        return;
                    }

                    var io = ImguiNative.igGetIO();
                    ImguizmoNative.ImGuizmo_SetRect(0, 0, io->DisplaySize.X, io->DisplaySize.Y);

                    view = camera.View;
                    projection = camera.Projection;
                    world = transform.WorldTransform;

                    ImguizmoNative.ImGuizmo_Manipulate(view.Pointer(), projection.Pointer(), this.Operation, MODE.LOCAL, world.Pointer(), null, null, null, null);

                    transform.WorldTransform = world;
                }
            }
        }
    }
}