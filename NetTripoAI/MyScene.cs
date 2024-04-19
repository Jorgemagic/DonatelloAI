using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using NetTripoAI.Components;
using NetTripoAI.ImGui;
using NetTripoAI.SceneManagers;
using NetTripoAI.UI;

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

        protected override void CreateScene()
        {
            Entity ui = new Entity()
                .AddComponent(new UIBehavior());
            this.Managers.EntityManager.Add(ui);            

            Entity manipulation = new Entity()
                .AddComponent(new Manipulation());
            this.Managers.EntityManager.Add(manipulation);

            // Debug
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var defaultMaterial = assetsService.Load<Material>(DefaultResourcesIDs.DefaultMaterialID);
            Entity cube = new Entity() { Tag = "Cube1"}
                .AddComponent(new Transform3D())
                .AddComponent(new MaterialComponent() { Material = defaultMaterial})
                .AddComponent(new BoxCollider3D())
                .AddComponent(new StaticBody3D())
                .AddComponent(new CubeMesh())
                .AddComponent(new MeshRenderer());
            this.Managers.EntityManager.Add(cube);
        }
    }
}


