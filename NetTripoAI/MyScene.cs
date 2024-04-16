using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.UI;
using NetTripoAI.ImGui;
using NetTripoAI.UI;

namespace NetTripoAI
{
    public class MyScene : Scene
    {
        public override void RegisterManagers()
        {
            base.RegisterManagers();
            
            this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());
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
        }
    }
}


