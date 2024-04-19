﻿using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using NetTripoAI.Importers.GLB;
using NetTripoAI.TripoAI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetTripoAI.SceneManagers
{
    public class ModelCollectionManager : SceneManager
    {
        public event EventHandler<bool> IsBusyChanged;

        [BindService]
        private AssetsService assetsService = null;

        [BindService]
        public ScreenContextManager screenContextManager = null;

        private Dictionary<string, string> collection = new Dictionary<string, string>();

        public Entity CurrentSelectedEntity = null;

        private bool isBusy = false;
        private string MODEL_FOLDER = "Models";

        public void AddModel(string modelName, string task_id)
        {
            this.collection.Add(modelName, task_id);
        }

        public string FindTaskByCurrentSelectedEntity()
        {
            if (this.CurrentSelectedEntity != null)
            {
                string tag = this.CurrentSelectedEntity.Tag;
                if (!string.IsNullOrEmpty(tag))
                {
                    if (this.collection.TryGetValue(tag, out string result))
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        public void DownloadModel(TripoResponse tripoResponse)
        {
            if (this.isBusy) return;

            Task.Run(async () =>
            {
                this.IsBusyChanged?.Invoke(this, true);

                string url = tripoResponse.data.result.model.url;
                var result = this.GetFilePathFromUrl(url);
                var model = await this.DownloadModelFromURL(url, result.filePath);

                this.collection.Add(result.fileName, tripoResponse.data.task_id);
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

                currentScene.Managers.EntityManager.Add(root);

                this.IsBusyChanged?.Invoke(this, false);
            });
        }

        private (string filePath, string fileName) GetFilePathFromUrl(string url)
        {
            string fileNameWithExtension = Path.GetFileName(url);
            fileNameWithExtension = fileNameWithExtension.Substring(0, fileNameWithExtension.IndexOf("?"));
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);
            string filePath = Path.Combine(MODEL_FOLDER, fileNameWithExtension);

            int index = 1;
            string fileName = fileNameWithoutExtension;
            while (File.Exists(filePath))
            {
                fileName = $"{fileNameWithoutExtension}{index++}";
                filePath = Path.Combine(MODEL_FOLDER, $"{fileName}.glb");
            }

            return (filePath, fileName);
        }

        private async Task<Evergine.Framework.Graphics.Model> DownloadModelFromURL(string url, string filePath)
        {
            Evergine.Framework.Graphics.Model result = null;
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    // Save file to disc
                    await this.DownloadFileTaskAsync(client, new Uri(url), filePath);

                    // Read file
                    using (var fileStream = await response.Content.ReadAsStreamAsync())
                    {
                        result = await GLBRuntime.Instance.Read(fileStream);
                    }
                }
            }

            return result;
        }

        private async Task DownloadFileTaskAsync(HttpClient client, Uri uri, string filePath)
        {
            using (var s = await client.GetStreamAsync(uri))
            {
                using (var fs = new FileStream(filePath, FileMode.CreateNew))
                {
                    await s.CopyToAsync(fs);
                }
            }
        }
    }
}
