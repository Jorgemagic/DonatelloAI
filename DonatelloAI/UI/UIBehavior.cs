using DonatelloAI.ImGui;
using DonatelloAI.SceneManagers;
using Evergine.Bindings.Imgui;
using Evergine.Bindings.Imguizmo;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.UI;
using System;

namespace DonatelloAI.UI
{
    public unsafe class UIBehavior : Behavior
    {
        [BindSceneManager]
        private CustomImGuiManager imguiManager = null;

        [BindSceneManager]
        private ModelCollectionManager modelCollectionManager = null;

        [BindSceneManager]
        private TaskManager taskManager = null;

        private TextToModelPanel textToModelPanel;
        private ImageToModelPanel imageToModelPanel;
        private LoadingPanel loadingPanel;
        private InfoDialog infoDialog;
        private ModelContextMenu modelContextMenu;
        private TaskListPanel taskListPanel;
        private GalleryPanel galleryPanel;
        private MenuBar menuBar;
        private ConversionPanel conversionPanel;

        protected override void OnActivated()
        {
            base.OnActivated();

            this.textToModelPanel = new TextToModelPanel(imguiManager, modelCollectionManager);
            this.imageToModelPanel = new ImageToModelPanel(imguiManager, modelCollectionManager);
            this.loadingPanel = new LoadingPanel(modelCollectionManager);
            this.infoDialog = new InfoDialog(this.taskManager);
            this.modelContextMenu = new ModelContextMenu(taskManager, modelCollectionManager, this);
            this.taskListPanel = new TaskListPanel(taskManager);
            this.galleryPanel = new GalleryPanel(modelCollectionManager);
            this.menuBar = new MenuBar(this);
            this.conversionPanel = new ConversionPanel(this.loadingPanel);
        }

        public bool ShowTextToModelPanel
        {
            get => this.textToModelPanel.OpenWindow;
            set => this.textToModelPanel.OpenWindow = value;
        }

        public bool ShowImageToModelPanel
        {
            get => this.imageToModelPanel.OpenWindow;
            set => imageToModelPanel.OpenWindow = value;
        }

        public bool ShowTaskListPanel
        {
            get => this.taskListPanel.OpenWindow;
            set => taskListPanel.OpenWindow = value;
        }

        public bool ShowGalleryPanel
        {
            get => this.galleryPanel.OpenWindow;
            set => galleryPanel.OpenWindow = value;
        }

        protected override void Update(TimeSpan gameTime)
        {
            var io = ImguiNative.igGetIO();
            /*bool open = true;
            ImguiNative.igShowDemoWindow(open.Pointer());*/

            // Imguizmo
            ImguizmoNative.ImGuizmo_SetRect(0, 0, io->DisplaySize.X, io->DisplaySize.Y);

            var camera = this.Managers.RenderManager.ActiveCamera3D;
            Matrix4x4 view = camera.View;
            const int side = 128;
            ImguizmoNative.ImGuizmo_ViewManipulate(&view.M11, 2, new Vector2(io->DisplaySize.X - side, 8), Vector2.One * side, (uint)Color.Transparent.ToInt());
            Matrix4x4.Invert(ref view, out Matrix4x4 iview);
            var translation = iview.Translation;
            var rotation = iview.Rotation;

            Vector3* r = &rotation;
            camera.Transform.LocalRotation = *r;

            Vector3* t = &translation;
            camera.Transform.LocalPosition = *t;

            // Panels            
            this.textToModelPanel.Show(ref io);
            this.imageToModelPanel.Show(ref io);
            this.loadingPanel.Show(ref io);
            this.infoDialog.Show(ref io);
            this.modelContextMenu.Show(ref io);
            this.taskListPanel.Show(ref io);
            this.galleryPanel.Show(ref io);
            this.menuBar.Show(ref io);
            this.conversionPanel.Show(ref io);
        }

        public void ShowConversionPanel(ModelData modelData)
        {
            this.conversionPanel.ModelData = modelData;
            this.conversionPanel.OpenWindow = true;
        }
    }
}
