using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;
using DonatelloAI.SceneManagers;
using System;
using Evergine.Framework;

namespace DonatelloAI.UI
{
    public class ModelContextMenu
    {
        private bool showContextMenu;
        private bool imguiBars;
        private Vector2 contextMenuPosition;

        private TaskManager taskManager = null;
        private ModelCollectionManager modelCollectionManager = null;

        public ModelContextMenu(TaskManager taskManager, ModelCollectionManager modelCollectionManager)
        {
            this.taskManager = taskManager;
            this.modelCollectionManager = modelCollectionManager;
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            // Mouse event
            if (ImguiNative.igIsMouseClicked(ImGuiMouseButton.Right, false))
            {
                Vector2 mousePos;
                ImguiNative.igGetMousePos(&mousePos);
                this.contextMenuPosition = mousePos;

                this.showContextMenu = true;
                return;
            }
            var isOverUI = Convert.ToBoolean(io->WantCaptureMouse);
            if (ImguiNative.igIsMouseClicked(ImGuiMouseButton.Left, false) && !isOverUI)
            {
                this.showContextMenu = false;
            }

            // Context menu UI
            if (this.showContextMenu)
            {
                var modelData = this.modelCollectionManager.FindModelDataByCurrentSelectedEntity();

                if (modelData != null)
                {
                    ImguiNative.igSetNextWindowPos(this.contextMenuPosition, ImGuiCond.None, Vector2.Zero);
                    ImguiNative.igSetNextWindowSize(new Vector2(125, 200), ImGuiCond.None);
                    ImguiNative.igBegin("Context Menu", this.imguiBars.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);

                    try
                    {
                        ImguiNative.igText("Options");
                        ImguiNative.igSeparator();

                        // Refine
                        if (ImguiNative.igMenuItem_Bool("Refine", null, false, true))
                        {
                            this.taskManager.RequestRefineModel();
                            this.showContextMenu = false;
                        }

                        ImguiNative.igSeparator();

                        // PreRigChechk                        
                        ImguiNative.igBeginDisabled(modelData.IsRiggeable.HasValue);
                        if (ImguiNative.igMenuItem_Bool("PreRigCheck", null, false, true))
                        {
                            this.taskManager.RequestPreRigCheckModel();
                            this.showContextMenu = false;
                        }
                        ImguiNative.igEndDisabled();

                        // Rig
                        var condiction = !modelData.IsRiggeable.HasValue || !modelData.IsRiggeable.Value || !string.IsNullOrEmpty(modelData.RigTaskId);
                        ImguiNative.igBeginDisabled(condiction);
                        if (ImguiNative.igMenuItem_Bool("Rig", null, false, true))
                        {
                            this.taskManager.RequestRigModel();
                            this.showContextMenu = false;
                        }
                        ImguiNative.igEndDisabled();

                        // Animations
                        ImguiNative.igSeparator();

                        ImguiNative.igBeginDisabled(string.IsNullOrEmpty(modelData.RigTaskId));

                        if (ImguiNative.igMenuItem_Bool("Animate - Walk", null, false, true))
                        {
                            this.taskManager.RequestAnimateModel(TripoAI.TripoAIService.Animations.Walk);
                            this.showContextMenu = false;
                        }

                        if (ImguiNative.igMenuItem_Bool("Animate - Run", null, false, true))
                        {
                            this.taskManager.RequestAnimateModel(TripoAI.TripoAIService.Animations.Run);
                            this.showContextMenu = false;
                        }

                        if (ImguiNative.igMenuItem_Bool("Animate - Dive", null, false, true))
                        {
                            this.taskManager.RequestAnimateModel(TripoAI.TripoAIService.Animations.Dive);
                            this.showContextMenu = false;
                        }
                        ImguiNative.igEndDisabled();

                        // Styles
                        ImguiNative.igSeparator();

                        if (ImguiNative.igMenuItem_Bool("Style - Lego", null, false, true))
                        {
                            this.taskManager.RequestStylization(TripoAI.TripoAIService.Styles.Lego);
                            this.showContextMenu = false;
                        }
                        if (ImguiNative.igMenuItem_Bool("Style - Voxel", null, false, true))
                        {
                            this.taskManager.RequestStylization(TripoAI.TripoAIService.Styles.Voxel);
                            this.showContextMenu = false;
                        }
                        if (ImguiNative.igMenuItem_Bool("Style - Voronoi", null, false, true))
                        {
                            this.taskManager.RequestStylization(TripoAI.TripoAIService.Styles.Voronoi);
                            this.showContextMenu = false;
                        }
                    }
                    catch (Exception)
                    {
                        // Required API_KEY
                    }

                    ImguiNative.igEnd();
                }
            }
        }
    }
}
