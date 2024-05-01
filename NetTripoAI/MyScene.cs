using Evergine.Framework;
using NetTripoAI.Components;
using NetTripoAI.ImGui;
using NetTripoAI.SceneManagers;
using NetTripoAI.TripoAI;
using NetTripoAI.UI;
using System;
using System.IO;

namespace NetTripoAI
{
    public class MyScene : Scene
    {
        public override void RegisterManagers()
        {
            base.RegisterManagers();
            
            this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());
            this.Managers.AddManager(new ModelCollectionManager());
            this.Managers.AddManager(new TaskManager());
            this.Managers.AddManager(new CustomImGuiManager()
            {
                ImGuizmoEnabled = true,
            });
            
        }

        protected async override void CreateScene()
        {
            var tripoAI = Application.Current.Container.Resolve<TripoAIService>();

            var image = File.ReadAllBytes("spiderman.png");
            string base64Image = Convert.ToBase64String(image);

            await tripoAI.RequestImageToDraftModel(base64Image, "png");

            Entity ui = new Entity()
                .AddComponent(new UIBehavior());
            this.Managers.EntityManager.Add(ui);            

            Entity manipulation = new Entity()
                .AddComponent(new Manipulation());
            this.Managers.EntityManager.Add(manipulation);                        
        }
    }
}


