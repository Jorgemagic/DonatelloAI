using DonatelloAI.Components;
using DonatelloAI.ImGui;
using DonatelloAI.SceneManagers;
using Evergine.Bindings.Imgui;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.UI;
using System;

namespace DonatelloAI.UI
{
    public class ToolbarPanel
    {
        public bool OpenWindow = true;

        private readonly uint SelectedColor = (uint)new Color("#4296fa").ToInt();

        private CustomImGuiManager imguiManager;
        private Manipulation manipulation;
        private ModelCollectionManager modelCollectionManager;

        private IntPtr moveIcon;
        private IntPtr rotateIcon;
        private IntPtr scaleIcon;
        private IntPtr universalIcon;
        private IntPtr wireframeIcon;
        private IntPtr solidIcon;

        private bool initialized = false;

        public ToolbarPanel(CustomImGuiManager imGuiManager, Manipulation manipulation, ModelCollectionManager modelCollectionManager)
        {
            this.imguiManager = imGuiManager;
            this.manipulation = manipulation;
            this.modelCollectionManager = modelCollectionManager;
        }

        public void Initialized()
        {
            var assetsService = Application.Current.Container.Resolve<AssetsService>();

            var moveTexture = assetsService.Load<Texture>(EvergineContent.Textures.Move_png);
            this.moveIcon = this.imguiManager.CreateImGuiBinding(moveTexture);

            var rotateTexture = assetsService.Load<Texture>(EvergineContent.Textures.Rotate_png);
            this.rotateIcon = this.imguiManager.CreateImGuiBinding(rotateTexture);

            var scaleTexture = assetsService.Load<Texture>(EvergineContent.Textures.scale_png);
            this.scaleIcon = this.imguiManager.CreateImGuiBinding(scaleTexture);

            var universalTexture = assetsService.Load<Texture>(EvergineContent.Textures.Universal_png);
            this.universalIcon = this.imguiManager.CreateImGuiBinding(universalTexture);

            var wireframeTexture = assetsService.Load<Texture>(EvergineContent.Textures.wireframe_png);
            this.wireframeIcon = this.imguiManager.CreateImGuiBinding(wireframeTexture);

            var solidTexture = assetsService.Load<Texture>(EvergineContent.Textures.solid_png);
            this.solidIcon = this.imguiManager.CreateImGuiBinding(solidTexture);

            this.initialized = true;
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (!this.OpenWindow)
            {
                return;
            }

            if (!this.initialized)
            {
                this.Initialized();
            }

            int windowsWidth = 300;
            int windowsHeight = 60;
            ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X - 128, 50), ImGuiCond.None, new Vector2(1, 0));
            ImguiNative.igSetNextWindowSize(new Vector2(windowsWidth, windowsHeight), ImGuiCond.None);
            ImguiNative.igBegin("Toolbar", this.OpenWindow.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground);

            ImguiNative.igPushStyleColor_U32(ImGuiCol.Button, (uint)Color.Black.ToInt());

            int buttonSize = 40;
            int spaceBetweenButtons = 8;
            int verticalSeparator = 12;

            int additionalStylePushed = 0;

            // Move
            if (this.manipulation.Operation == Evergine.Bindings.Imguizmo.OPERATION.TRANSLATE)
            {
                ImguiNative.igPushStyleColor_U32(ImGuiCol.Button, SelectedColor);
                additionalStylePushed++;
            }

            if (ImguiNative.igImageButton(this.moveIcon, Vector2.One * buttonSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
            {
                this.manipulation.Operation = Evergine.Bindings.Imguizmo.OPERATION.TRANSLATE;
            }

            if (additionalStylePushed > 0)
            {
                ImguiNative.igPopStyleColor(additionalStylePushed);
                additionalStylePushed = 0;
            }

            // Rotate
            ImguiNative.igSameLine(0, spaceBetweenButtons);
            if (this.manipulation.Operation == Evergine.Bindings.Imguizmo.OPERATION.ROTATE)
            {
                ImguiNative.igPushStyleColor_U32(ImGuiCol.Button, SelectedColor);
                additionalStylePushed++;
            }

            if (ImguiNative.igImageButton(this.rotateIcon, Vector2.One * buttonSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
            {
                this.manipulation.Operation = Evergine.Bindings.Imguizmo.OPERATION.ROTATE;
            }

            if (additionalStylePushed > 0)
            {
                ImguiNative.igPopStyleColor(additionalStylePushed);
                additionalStylePushed = 0;
            }

            // Scale
            ImguiNative.igSameLine(0, spaceBetweenButtons);
            if (this.manipulation.Operation == Evergine.Bindings.Imguizmo.OPERATION.SCALE)
            {
                ImguiNative.igPushStyleColor_U32(ImGuiCol.Button, SelectedColor);
                additionalStylePushed++;
            }

            if (ImguiNative.igImageButton(this.scaleIcon, Vector2.One * buttonSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
            {
                this.manipulation.Operation = Evergine.Bindings.Imguizmo.OPERATION.SCALE;
            }

            if (additionalStylePushed > 0)
            {
                ImguiNative.igPopStyleColor(additionalStylePushed);
                additionalStylePushed = 0;
            }

            // Universal
            ImguiNative.igSameLine(0, spaceBetweenButtons);
            if (this.manipulation.Operation == Evergine.Bindings.Imguizmo.OPERATION.UNIVERSAL)
            {
                ImguiNative.igPushStyleColor_U32(ImGuiCol.Button, SelectedColor);
                additionalStylePushed++;
            }

            if (ImguiNative.igImageButton(this.universalIcon, Vector2.One * buttonSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
            {
                this.manipulation.Operation = Evergine.Bindings.Imguizmo.OPERATION.UNIVERSAL;
            }

            if (additionalStylePushed > 0)
            {
                ImguiNative.igPopStyleColor(additionalStylePushed);
                additionalStylePushed = 0;
            }

            // Wireframe
            ImguiNative.igSameLine(0, verticalSeparator);
            if (this.modelCollectionManager.RenderType == ModelCollectionManager.RenderMode.Wireframe)
            {
                ImguiNative.igPushStyleColor_U32(ImGuiCol.Button, SelectedColor);
                additionalStylePushed++;
            }

            if (ImguiNative.igImageButton(this.wireframeIcon, Vector2.One * buttonSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
            {
                this.modelCollectionManager.RenderType = ModelCollectionManager.RenderMode.Wireframe;
            }

            if (additionalStylePushed > 0)
            {
                ImguiNative.igPopStyleColor(additionalStylePushed);
                additionalStylePushed = 0;
            }

            // Solid
            ImguiNative.igSameLine(0, spaceBetweenButtons);
            if (this.modelCollectionManager.RenderType == ModelCollectionManager.RenderMode.Solid)
            {
                ImguiNative.igPushStyleColor_U32(ImGuiCol.Button, SelectedColor);
                additionalStylePushed++;
            }

            if (ImguiNative.igImageButton(this.solidIcon, Vector2.One * buttonSize, Vector2.Zero, Vector2.One, 0, Vector4.Zero, Vector4.One))
            {
                this.modelCollectionManager.RenderType = ModelCollectionManager.RenderMode.Solid;
            }

            if (additionalStylePushed > 0)
            {
                ImguiNative.igPopStyleColor(additionalStylePushed);
                additionalStylePushed = 0;
            }

            ImguiNative.igPopStyleColor(1);

            ImguiNative.igEnd();
        }
    }
}
