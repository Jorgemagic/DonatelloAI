using Evergine.Bindings.Imgui;
using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.UI;
using DonatelloAI.TripoAI;
using System.Diagnostics;
using System.Text;

namespace DonatelloAI.UI
{
    public unsafe class MenuBar
    {
        private bool showAbout;
        private bool showSettings;
        private byte[] textBuffer = new byte[256];
        private TripoAIService tripoAIService;
        private UIBehavior uiBehavior;

        public MenuBar(UIBehavior uiBehavior)
        {
            this.tripoAIService = Application.Current.Container.Resolve<TripoAIService>();
            this.uiBehavior = uiBehavior;
        }

        public void Show(ref ImGuiIO* io)
        {
            if (ImguiNative.igBeginMainMenuBar())
            {
                if (ImguiNative.igBeginMenu("File", true))
                {

                    if (ImguiNative.igMenuItem_Bool("Settings", null, false, true))
                    {
                        this.showSettings = true;
                    }

                    if (ImguiNative.igMenuItem_Bool("About", null, false, true))
                    {
                        this.showAbout = true;
                    }

                    ImguiNative.igEndMenu();
                }

                if (ImguiNative.igBeginMenu("Views", true))
                {
                    if (ImguiNative.igMenuItem_Bool("Text to Model", null, false, true))
                    {
                        this.uiBehavior.ShowTextToModelPanel = true;
                    }

                    if (ImguiNative.igMenuItem_Bool("Image to Model", null, false, true))
                    {
                        this.uiBehavior.ShowImageToModelPanel = true;
                    }

                    if (ImguiNative.igMenuItem_Bool("Task list", null, false, true))
                    {
                        this.uiBehavior.ShowTaskListPanel = true;
                    }

                    ImguiNative.igEndMenu();
                }

                ImguiNative.igEndMainMenuBar();
            }

            // Show windows
            this.ShowAboutWindow(ref io);
            this.ShowSettings(ref io);
        }

        private void ShowAboutWindow(ref ImGuiIO* io)
        {
            if (!this.showAbout)
            {
                return;
            }

            ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X / 2.0f, io->DisplaySize.Y / 2.0f), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImguiNative.igSetNextWindowSize(new Vector2(450, 170), ImGuiCond.None);
            if (!ImguiNative.igBegin("About", this.showAbout.Pointer(), ImGuiWindowFlags.NoResize))
            {
                ImguiNative.igEnd();
            }
            else
            {
                ImguiNative.igText("DonatelloAI 0.1.0 Generate 3D models using AI");
                ImguiNative.igSeparator();
                ImguiNative.igText("Created by Jorge Canton");
                ImguiNative.igSpacing();
                ImguiNative.igSpacing();
                ImguiNative.igSpacing();
                ImguiNative.igSpacing();
                if (ImguiNative.igButton("Twitter: @jorge_magic", new Vector2(200, 20)))
                {
                    Process.Start("explorer.exe", "https://twitter.com/jorge_magic");
                }
                if (ImguiNative.igButton("Github: jorgemagic", new Vector2(200, 20)))
                {
                    Process.Start("explorer.exe", "https://github.com/Jorgemagic");
                }
                if (ImguiNative.igButton("Linkedin: ~in/jorgecanton", new Vector2(200, 20)))
                {
                    Process.Start("explorer.exe", "https://www.linkedin.com/in/jorgecanton/");
                }

                ImguiNative.igEnd();
            }
        }

        private void ShowSettings(ref ImGuiIO* io)
        {
            if (!this.showSettings)
            {
                return;
            }

            ImguiNative.igSetNextWindowPos(new Vector2(io->DisplaySize.X / 2.0f, io->DisplaySize.Y / 2.0f), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImguiNative.igSetNextWindowSize(new Vector2(470, 55), ImGuiCond.None);
            if (!ImguiNative.igBegin("Settings", this.showSettings.Pointer(), ImGuiWindowFlags.NoResize))
            {
                ImguiNative.igEnd();
            }
            else
            {
                fixed (byte* buff = this.textBuffer)
                {
                    ImguiNative.igInputText("Tripo API_KEY", buff, (uint)this.textBuffer.Length, ImGuiInputTextFlags.None, null, null);
                    ImguiNative.igSameLine(0, 4);
                    var buttonSize = new Vector2(50, 19);
                    if (ImguiNative.igButton("Set", buttonSize))
                    {
                        string apikey = Encoding.UTF8.GetString(buff, textBuffer.Length);
                        var index = apikey.IndexOf('\0');
                        if (index >= 0)
                        {
                            apikey = apikey.Substring(0, index);
                        }

                        this.tripoAIService.SetApiKey(apikey);
                        this.showSettings = false;
                    }
                }

                ImguiNative.igEnd();
            }
        }
    }
}
