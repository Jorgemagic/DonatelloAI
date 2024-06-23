using DonatelloAI.ImGui;
using DonatelloAI.Importers.GLB;
using Evergine.Components.Animation;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DonatelloAI.SceneManagers
{
    public class ModelCollectionManager : SceneManager
    {
        public event EventHandler<bool> IsBusyChanged;

        [BindService]
        private AssetsService assetsService = null;

        [BindService]
        public ScreenContextManager screenContextManager = null;

        [BindSceneManager]
        public CustomImGuiManager customImGuiManager = null;

        public List<ModelData> Models = new List<ModelData>();
        private Dictionary<string, int> collection = new Dictionary<string, int>();

        public Entity CurrentSelectedEntity = null;

        private bool isBusy = false;
        private const string MODEL_FOLDER = "Models";
        private const string THUMBNAIL_FOLDER = "Thumbnail";
        private const string JSON_COLLECTION = "modelCollection.json";

        public ModelCollectionManager()
        {
            /*this.modelCollection.Add(new ModelData() { TaskId = "522351fa-3258-4df9-9484-073c7e247d73" });
            this.modelCollection.Add(new ModelData() { TaskId = "47c2ead9-ec6a-4f69-97e8-d1170f0c8fdf" });
            this.modelCollection.Add(new ModelData() { TaskId = "bc6322ec-6466-48d1-9a6c-b4faff273898" });
            this.modelCollection.Add(new ModelData() { TaskId = "874b5f45-8534-43c8-85cf-1c166707525a" });*/
        }

        protected override async void OnLoaded()
        {
            if (File.Exists(JSON_COLLECTION))
            {
                var json = await File.ReadAllTextAsync(JSON_COLLECTION);
                this.Models = JsonSerializer.Deserialize<List<ModelData>>(json);
                foreach (var model in this.Models)
                {
                    var textureImage = await ImguiHelper.CreateTextureFromFile(model.Thumbnail);
                    var thumbnail = this.customImGuiManager.CreateImGuiBinding(textureImage);
                    model.ThumbnailTexture = textureImage;
                    model.ThumbnailPointer = thumbnail;
                }
            }

            base.OnLoaded();
        }

        public ModelData FindModelDataByCurrentSelectedEntity()
        {
            if (this.CurrentSelectedEntity != null)
            {
                string tag = this.CurrentSelectedEntity.Tag;
                if (!string.IsNullOrEmpty(tag))
                {
                    if (this.collection.TryGetValue(tag, out int index))
                    {
                        return this.Models[index];
                    }
                }
            }

            return null;
        }

        public void DownloadModel(string modelURL, string taskId, string thumbnailURL, string entityTag = null)
        {
            if (this.isBusy) return;

            Task.Run(async () =>
            {
                this.IsBusyChanged?.Invoke(this, true);

                var result = this.GetFilePathFromUrl(modelURL, entityTag);
                var model = await this.DownloadModelFromURL(modelURL, result.filePath);

                if (!string.IsNullOrEmpty(taskId))
                {
                    ModelData modelData = new ModelData();
                    modelData.TaskId = taskId;

                    string filePath = Path.Combine(THUMBNAIL_FOLDER, $"{result.fileName}.webp");

                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await ImguiHelper.DownloadThumbnailFromUrl(thumbnailURL, filePath);
                    var textureImage = await ImguiHelper.CreateTextureFromFile(filePath);                    
                    var thumbnail = this.customImGuiManager.CreateImGuiBinding(textureImage);
                    modelData.Thumbnail = filePath;
                    modelData.ThumbnailTexture = textureImage;
                    modelData.ThumbnailPointer = thumbnail;
                    this.Models.Add(modelData);
                    this.collection.Add(result.fileName, this.Models.Count - 1);

                    await this.SaveCollectionAsJsonFile();
                }

                var currentScene = screenContextManager.CurrentContext[0];

                var entity = model.InstantiateModelHierarchy(this.assetsService);

                // Remove previous model
                /*var previous = this.Managers.EntityManager.FindAllByTag(result.fileName);
                foreach ( var p in previous ) 
                {
                    this.Managers.EntityManager.Remove(p);
                }*/

                var root = new Entity() { Tag = result.fileName }
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

                // Animated models
                var animate3D = entity.FindComponentInChildren<Animation3D>();
                if (animate3D != null && animate3D.AnimationNames.Count() > 0)
                {
                    animate3D.CurrentAnimation = animate3D.AnimationNames.First();
                    animate3D.PlayAutomatically = true;
                }

                currentScene.Managers.EntityManager.Add(root);

                this.IsBusyChanged?.Invoke(this, false);
            });
        }

        private (string filePath, string fileName) GetFilePathFromUrl(string url, string modelName = null)
        {
            string fileNameWithExtension = Path.GetFileName(url);
            fileNameWithExtension = fileNameWithExtension.Substring(0, fileNameWithExtension.IndexOf("?"));
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);

            string fileName = modelName == null ? fileNameWithoutExtension : modelName;
            string extension = Path.GetExtension(fileNameWithExtension);

            string filePath = Path.Combine(MODEL_FOLDER, $"{fileName}{extension}");

            int index = 0;
            while (File.Exists(filePath))
            {
                index++;
                filePath = Path.Combine(MODEL_FOLDER, $"{fileName}{index}{extension}");
            }

            return (filePath, $"{fileName}{index}");
        }

        private async Task<Model> DownloadModelFromURL(string url, string filePath)
        {
            Model result = null;
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    // Save file to disc
                    await this.DownloadFileTaskAsync(client, new Uri(url), filePath);

                    // Read file
                    if (Path.GetExtension(filePath) == ".glb")
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Open))
                        {
                            result = await GLBRuntime.Instance.Read(fileStream);
                        }
                    }
                }
            }

            return result;
        }

        private async Task DownloadFileTaskAsync(HttpClient client, Uri uri, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var s = await client.GetStreamAsync(uri))
            {
                using (var fs = new FileStream(filePath, FileMode.CreateNew))
                {
                    await s.CopyToAsync(fs);
                }
            }
        }

        private async Task SaveCollectionAsJsonFile()
        {
            var json = JsonSerializer.Serialize(this.Models);
            await File.WriteAllTextAsync(JSON_COLLECTION, json);
        }
    }
}
