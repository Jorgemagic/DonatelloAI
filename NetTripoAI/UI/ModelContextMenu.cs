﻿using Evergine.Bindings.Imgui;
using Evergine.Mathematics;
using Evergine.UI;
using NetTripoAI.SceneManagers;

namespace NetTripoAI.UI
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

            // Context menu UI
            if (this.showContextMenu)
            {
                ImguiNative.igSetNextWindowPos(this.contextMenuPosition, ImGuiCond.None, Vector2.Zero);
                ImguiNative.igSetNextWindowSize(new Vector2(96, 80), ImGuiCond.None);
                ImguiNative.igBegin("Context Menu", this.imguiBars.Pointer(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);

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

                ImguiNative.igEnd();
            }

            //if (ImguiNative.igIsMouseClicked(ImGuiMouseButton.Left, false))
            //{
            //    this.showContextMenu = false;
            //}
        }            
    }
}
