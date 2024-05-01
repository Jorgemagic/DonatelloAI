using Evergine.Components.Animation;
using Evergine.Framework;
using Evergine.Framework.Assets;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using glTFLoader.Schema;
using TripoAINet.Importers.GLB;
using TripoAINet.TripoAI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TripoAINet.SceneManagers
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
        private const string MODEL_FOLDER = "Models";
        private const string TEMP_FOLDER = "Temp";
        private const string FBX2GLB_Path = "FBX2glTF.exe";

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

        public void DownloadModel(TripoResponse tripoResponse, string entityTag = null)
        {
            if (this.isBusy) return;

            Task.Run(async () =>
            {
                this.IsBusyChanged?.Invoke(this, true);

                string url = tripoResponse.data.result.model.url;
                var result = this.GetFilePathFromUrl(url, entityTag);
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

            int index = 1;            
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(MODEL_FOLDER, $"{fileName}{index++}{extension}");
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

                    // Extract files
                    if (Path.GetExtension(filePath) == ".zip")
                    {                        
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                        string outputPath = Path.Combine(TEMP_FOLDER, fileNameWithoutExt);
                        ZipFile.ExtractToDirectory(filePath, outputPath);

                        string fbxPath = Path.Combine(outputPath, "out.FBX");
                        string glbPath = Path.Combine(MODEL_FOLDER, $"{Path.GetFileNameWithoutExtension(filePath)}.glb");
                        await this.FBXtoGLB(fbxPath, glbPath);
                        filePath = glbPath;
                    }

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
            using (var s = await client.GetStreamAsync(uri))
            {
                using (var fs = new FileStream(filePath, FileMode.CreateNew))
                {
                    await s.CopyToAsync(fs);
                }
            }
        }

        private Task<int> FBXtoGLB(string fbxPath, string glbPath)
        {
            var tcs = new TaskCompletionSource<int>();
            string arguments = $"-b --anim-framerate bake30 -i {fbxPath} -o {glbPath}";

            var process = new Process
            {
                StartInfo = { FileName = FBX2GLB_Path, Arguments = arguments },
                EnableRaisingEvents = true,
            };

            process.Exited += (s, e) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
    }
}
