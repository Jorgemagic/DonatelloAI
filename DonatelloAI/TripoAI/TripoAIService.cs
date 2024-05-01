using Evergine.Framework.Services;
using DonatelloAI.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DonatelloAI.TripoAI
{
    public class TripoAIService : Service
    {        
        private readonly string filePath = "appSettings.json";

        public string api_key;

        public TripoAIService()
        {            
            if (File.Exists(filePath))
            {
                using (var reader = new StreamReader(this.filePath))
                {
                    string json = reader.ReadToEnd();
                    var data = JsonConvert.DeserializeObject<ApiSettings>(json);
                    this.api_key = data.TripoAIApiKey;
                }
            }
        }  
        
        public void SetApiKey(string key)
        {
            this.api_key = key;
            var apiSettings = new ApiSettings()
            {
                 TripoAIApiKey = this.api_key,
            };

            string json = JsonConvert.SerializeObject(apiSettings);
            File.WriteAllText(this.filePath, json);
        }

        public async Task<string> RequestADraftModel(string promptText)
        {            
            if (string.IsNullOrEmpty(api_key))
            {
                throw new Exception("You need to specify a valid TripoAI API_KEY");
            }

            string taskID = string.Empty;
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "text_to_model");
            parameters.Add("prompt", promptText);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
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

        public async Task<string> RequestImageToDraftModel(string base64Image, string format)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                throw new Exception("You need to specify a valid TripoAI API_KEY");
            }

            string taskID = string.Empty;
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("type", "image_to_model");

            Dictionary<string, string> fileData = new Dictionary<string, string>();
            fileData.Add("type", format);
            fileData.Add("data", base64Image);

            parameters.Add("file", fileData);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
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
            if (string.IsNullOrEmpty(api_key))
            {
                throw new Exception("You need to specify a valid TripoAI API_KEY");
            }

            TripoResponse tripoResponse = null;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                var result = await client.GetAsync($"https://api.tripo3d.ai/v2/openapi/task/{task_id}");

                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                }
            }

            return tripoResponse;
        }

        public async Task<string> RequestRefineModel(string task_id)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                throw new Exception("You need to specify a valid TripoAI API_KEY");
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "refine_model");
            parameters.Add("draft_model_task_id", task_id);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string refineTaskId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
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
                            refineTaskId = tripoResponse.data.task_id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            };

            return refineTaskId;
        }

        public async Task<string> RequestAnimateModel(string task_id)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                throw new Exception("You need to specify a valid TripoAI API_KEY");
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "animate_model");
            parameters.Add("original_model_task_id", task_id);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string animateTaskId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
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
                            animateTaskId = tripoResponse.data.task_id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            };

            return animateTaskId;
        }
    }
}
