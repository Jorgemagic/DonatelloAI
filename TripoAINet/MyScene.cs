using Evergine.Framework;
using System.IO;
using TripoAINet.Components;
using TripoAINet.ImGui;
using TripoAINet.SceneManagers;
using TripoAINet.TripoAI;
using TripoAINet.UI;

namespace TripoAINet
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
            string base64Image = System.Convert.ToBase64String(image);

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


