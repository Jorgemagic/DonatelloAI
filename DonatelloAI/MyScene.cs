using DonatelloAI.Components;
using DonatelloAI.ImGui;
using DonatelloAI.SceneManagers;
using DonatelloAI.UI;
using Evergine.Framework;

namespace DonatelloAI
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

        protected override void CreateScene()
        {
            Entity ui = new Entity()
                .AddComponent(new UIBehavior());
            this.Managers.EntityManager.Add(ui);            

            Entity manipulation = new Entity() { Tag = "Picking"}
                .AddComponent(new Manipulation());
            this.Managers.EntityManager.Add(manipulation);                        
        }
    }
}


