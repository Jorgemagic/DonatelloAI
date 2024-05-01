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
                ImguiNative.igSetNextWindowSize(new Vector2(96, 80), ImGuiCond.None);
                ImguiNative.igBegin("Context Menu", this.imguiBars.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);

                try
                {
                    ImguiNative.igText("Options");
                    Vector2 buttonSize = new Vector2(80, 20);
                    if (ImguiNative.igButton("Refine", buttonSize))
                    {
                        this.taskManager.RequestRefineModel();
                        this.showContextMenu = false;
                    }

                    if (ImguiNative.igButton("Animate", buttonSize))
                    {
                        this.taskManager.RequestAnimateModel();
                        this.showContextMenu = false;
                    }
                }
                catch (Exception ex)
                {
                    // Required API_KEY
                }

                ImguiNative.igEnd();
            }            
        }            
    }
}
