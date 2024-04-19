using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using NetTripoAI.TripoAI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetTripoAI.SceneManagers
{
    public class ModelCollectionManager : SceneManager
    {
        [BindService]
        private TripoAIService tripoAIService = null;

        [BindService]
        private AssetsService assetsService = null;

        [BindSceneManager]
        public ScreenContextManager screenContextManager = null;

        private Dictionary<string, string> collection = new Dictionary<string, string>();
        private bool isBusy = false;

        public event EventHandler<bool> IsBusyChanged;

        public void AddModel(string modelName, string task_id)
        {
            this.collection.Add(modelName, task_id);
        }

        public string FindTaskByModelName(string modelName)
        {
            return this.collection[modelName];
        }

        public void DownloadModel(string modelUrl)
        {
            if (this.isBusy) return;

            Task.Run(async () =>
            {
                this.IsBusyChanged?.Invoke(this, true);

                var model = await this.tripoAIService.DownloadModelFromURL(modelUrl);

                var currentScene = screenContextManager.CurrentContext[0];

                var entity = model.InstantiateModelHierarchy(this.assetsService);

                var root = new Entity()
                                .AddComponent(new Transform3D());
                root.AddChild(entity);

                var boundingBox = model.BoundingBox.Value;
                boundingBox.Transform(entity.FindComponent<Transform3D>().WorldTransform);
                root.FindComponent<Transform3D>().Scale = Vector3.One * (1.0f / boundingBox.HalfExtent.Length());
                root.AddComponent(new BoxCollider3D()
                {
                    Size = boundingBox.HalfExtent * 2,
                    Offset = boundingBox.Center,
                });
                root.AddComponent(new StaticBody3D());

                currentScene.Managers.EntityManager.Add(root);

                this.IsBusyChanged?.Invoke(this, false);
            });
        }
    }
}
