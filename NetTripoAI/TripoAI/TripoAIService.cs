using Evergine.Framework.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NetTripoAI.TripoAI
{
    public class TripoAIService : Service
    {
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

        public async Task<string> RequestRefineModel(string task_id)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "refine_model");
            parameters.Add("draft_model_task_id", task_id);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string refineTaskId = string.Empty;
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
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "animate_model");
            parameters.Add("original_model_task_id", task_id);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string animateTaskId = string.Empty;
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
