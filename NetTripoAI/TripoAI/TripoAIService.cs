using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using NetTripoAI.Importers.GLB;
using NetTripoAI.Importers.Images;
using Newtonsoft.Json;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NetTripoAI.TripoAI
{
    public class TripoAIService : Service
    {
        [BindService]
        private GraphicsContext graphicsContext = null;

        private string API_KEY = "{YOUR APIKEY}";

        public async Task<string> RequestADraftModel(string promptText)
        {
            string taskID = string.Empty;
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "text_to_model");
            parameters.Add("prompt", promptText);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                try
                {
                    var result = await client.PostAsync(uri, jsonContent);
                    if (result.EnsureSuccessStatusCode().IsSuccessStatusCode)
                    {
                        var response = await result.Content.ReadAsStringAsync();
                        var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                        if (tripoResponse != null)
                        {
                            taskID = tripoResponse.data.task_id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            };

            return taskID;
        }

        public async Task<TripoResponse> GetTaskStatus(string task_id)
        {
            TripoResponse tripoResponse = null;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);
                var result = await client.GetAsync($"https://api.tripo3d.ai/v2/openapi/task/{task_id}");

                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);                    
                }
            }

            return tripoResponse;
        }

        public async Task RefineModel(string task_id)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "refine_model");
            parameters.Add("draft_model_task_id", task_id);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                try
                {
                    var result = await client.PostAsync(uri, jsonContent);
                    if (result.EnsureSuccessStatusCode().IsSuccessStatusCode)
                    {
                        var response = await result.Content.ReadAsStringAsync();
                        var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            };
        }

        public async Task<Texture> DownloadTextureFromUrl(string url)
        {
            Texture result = null;
            using (HttpClient cliente = new HttpClient())
            {
                using (var response = await cliente.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    using (var fileStream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fileStream))
                        {
                            RawImageLoader.CopyImageToArrayPool(image, out _, out byte[] data);
                            await EvergineForegroundTask.Run(() =>
                            {
                                TextureDescription desc = new TextureDescription()
                                {
                                    Type = TextureType.Texture2D,
                                    Width = (uint)image.Width,
                                    Height = (uint)image.Height,
                                    Depth = 1,
                                    ArraySize = 1,
                                    Faces = 1,
                                    Usage = ResourceUsage.Default,
                                    CpuAccess = ResourceCpuAccess.None,
                                    Flags = TextureFlags.ShaderResource,
                                    Format = PixelFormat.R8G8B8A8_UNorm,
                                    MipLevels = 1,
                                    SampleCount = TextureSampleCount.None,
                                };
                                result = this.graphicsContext.Factory.CreateTexture(ref desc);

                                this.graphicsContext.UpdateTextureData(result, data);
                            });
                        }

                        fileStream.Flush();
                    }

                    return result;
                }
            }
        }

        public async Task<Evergine.Framework.Graphics.Model> DownloadModelFromURL(string url)
        {
            Evergine.Framework.Graphics.Model result = null;
            using (HttpClient cliente = new HttpClient())
            {
                using (var response = await cliente.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    using (var fileStream = await response.Content.ReadAsStreamAsync())
                    {
                        result = await GLBRuntime.Instance.Read(fileStream);                        
                    }
                }
            }

            return result;
        }
    }
}
