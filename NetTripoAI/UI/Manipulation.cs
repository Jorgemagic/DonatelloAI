using Evergine.Bindings.Imgui;
using Evergine.Bindings.Imguizmo;
using Evergine.Common.Input.Keyboard;
using Evergine.Common.Input;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Mathematics;
using Evergine.UI;
using System;
using Evergine.Framework.Services;

namespace NetTripoAI.UI
{
    public unsafe class Manipulation : Behavior
    {
        [BindService]
        protected GraphicsPresenter graphicsPresenter;

        private Entity selectedEntity = null;
        private Transform3D transform = null;

        private Matrix4x4 view;
        private Matrix4x4 projection;
        private Matrix4x4 world;

        private OPERATION operation;

        public Manipulation()
        {
            this.operation = OPERATION.TRANSLATE | OPERATION.ROTATE;
        }

        protected override void Update(TimeSpan gameTime)
        {            
            // Selected element
            var camera = this.Managers.RenderManager?.ActiveCamera3D;

            var mouse = camera.Display?.MouseDispatcher;
            if (mouse != null)
            {                
                var btn = mouse.ReadButtonState(Evergine.Common.Input.Mouse.MouseButtons.Left);
                if (btn == Evergine.Common.Input.ButtonState.Pressing)
                {
                    var pos = mouse.Position.ToVector2();

                    camera.CalculateRay(ref pos, out var ray);

                    var hitResult = this.Managers.PhysicManager3D.RayCast(ref ray, 100);
                    if (hitResult.Succeeded)
                    {
                        this.selectedEntity = hitResult.PhysicBody.BodyComponent.Owner;                        
                        this.transform = selectedEntity.FindComponent<Transform3D>();
                    }
                    else
                    {
                        this.transform = null;
                    }
                }


                // Show Manipulator
                if (this.transform != null)
                {
                    // Keyboard
                    KeyboardDispatcher keyboardDispatcher = this.graphicsPresenter.FocusedDisplay?.KeyboardDispatcher;

                    if (keyboardDispatcher?.ReadKeyState(Keys.Delete) == ButtonState.Pressing)
                    {
                        this.Managers.EntityManager.Remove(this.selectedEntity);        
                    }

                    var io = ImguiNative.igGetIO();
                    ImguizmoNative.ImGuizmo_SetRect(0, 0, io->DisplaySize.X, io->DisplaySize.Y);

                    this.view = camera.View;
                    this.projection = camera.Projection;
                    this.world = this.transform.WorldTransform;

                    ImguizmoNative.ImGuizmo_Manipulate(view.Pointer(), projection.Pointer(), this.operation, MODE.LOCAL, world.Pointer(), null, null, null, null);

                    this.transform.WorldTransform = this.world;
                }
            }
        }
    }
}