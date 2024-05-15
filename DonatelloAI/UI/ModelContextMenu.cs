using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;
using DonatelloAI.SceneManagers;
using System;

namespace DonatelloAI.UI
{
    public class ModelContextMenu
    {
        private bool showContextMenu;
        private bool imguiBars;
        private Vector2 contextMenuPosition;

        private TaskManager taskManager = null;

        public ModelContextMenu(TaskManager taskManager)
        {
            this.taskManager = taskManager;
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
                ImguiNative.igSetNextWindowPos(this.contextMenuPosition, ImGuiCond.None, Vector2.Zero);
                ImguiNative.igSetNextWindowSize(new Vector2(125, 125), ImGuiCond.None);
                ImguiNative.igBegin("Context Menu", this.imguiBars.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);

                try
                {
                    ImguiNative.igText("Options");
                    ImguiNative.igSeparator();
                    if (ImguiNative.igMenuItem_Bool("Refine", null, false, true))
                    {
                        this.taskManager.RequestRefineModel();
                        this.showContextMenu = false;
                    }

                    if (ImguiNative.igMenuItem_Bool("Animate", null, false, true))
                    {
                        this.taskManager.RequestAnimateModel();
                        this.showContextMenu = false;
                    }

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
