using DonatelloAI.Settings;
using Evergine.Framework.Services;
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
        public event EventHandler<string> InfoEvent;

        /// <summary>
        /// Available post-processing styles.
        /// </summary>
        public enum Styles
        {
            Lego,
            Voxel,
            Voronoi,
        };

        /// <summary>
        /// Available animations
        /// </summary>
        public enum Animations
        {
            Walk,
            Run,
            Dive,
        }

        /// <summary>
        ///  Conversion available formats
        /// </summary>
        public enum ModelFormat
        {
            GLTF,
            USDZ,
            FBX,
            OBJ,
            STL
        }

        /// <summary>
        /// Diffuse color texture formats (default JPEG)
        /// </summary>
        public enum TextureFormat
        {
            BMP,
            DPX,
            HDR,
            JPEG,
            OPEN_EXR,
            PNG,
            TARGA,
            TIFF,
            WEBP
        }

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

        public async Task<string> RequestADraftModel(string promptText, string negativePrompt = default)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            string taskID = string.Empty;
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "text_to_model");
            parameters.Add("prompt", promptText);
            if (!string.IsNullOrEmpty(negativePrompt))
            {
                parameters.Add("negative_prompt", negativePrompt);
            }

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        taskID = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return taskID;
        }

        public async Task<string> RequestUploadImage(string imagePath)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            string imageToken = string.Empty;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/upload";

                MultipartFormDataContent requestContent = new MultipartFormDataContent();
                var imageData = await File.ReadAllBytesAsync(imagePath);
                var imageContent = new ByteArrayContent(imageData);
                var imageName = Path.GetFileName(imagePath);
                requestContent.Add(imageContent, "file", imageName);

                var result = await client.PostAsync(uri, requestContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<UploadResponse>(response);
                    if (tripoResponse != null)
                    {
                        imageToken = tripoResponse.data.image_token;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return imageToken;
        }

        public async Task<string> RequestImageToDraftModel(string imageToken, string format)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            string taskID = string.Empty;
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("type", "image_to_model");

            Dictionary<string, string> fileData = new Dictionary<string, string>();
            fileData.Add("type", format);
            fileData.Add("file_token", imageToken);

            parameters.Add("file", fileData);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        taskID = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return taskID;
        }

        public async Task<TripoResponse> GetTaskStatus(string task_id)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
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
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
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

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        refineTaskId = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return refineTaskId;
        }

        public async Task<string> RequestPreRigCheck(string task_id)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "animate_prerigcheck");
            parameters.Add("original_model_task_id", task_id);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string preRigCheckTaskId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        preRigCheckTaskId = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return preRigCheckTaskId;
        }

        public async Task<string> RequestRig(string task_id)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "animate_rig");
            parameters.Add("original_model_task_id", task_id);
            parameters.Add("out_format", "glb");

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string rigTaskId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        rigTaskId = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return rigTaskId;
        }

        public async Task<string> RequestRetarget(string rigTask_id, Animations animation)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "animate_retarget");
            parameters.Add("original_model_task_id", rigTask_id);
            parameters.Add("out_format", "glb");

            string animationString;
            switch (animation)
            {
                case Animations.Run:
                    animationString = "preset:run";
                    break;
                case Animations.Dive:
                    animationString = "preset:dive";
                    break;
                case Animations.Walk:
                default:
                    animationString = "preset:walk";
                    break;
            }

            parameters.Add("animation", animationString);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string retargetTaskId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        retargetTaskId = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return retargetTaskId;
        }

        public async Task<string> RequestStylization(string task_id, Styles style)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "stylize_model");
            parameters.Add("style", style.ToString().ToLowerInvariant());
            parameters.Add("original_model_task_id", task_id);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string stylizationTaskId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        stylizationTaskId = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return stylizationTaskId;
        }

        public async Task<string> RequestConversion(string task_id, ModelFormat format, bool quad = false, int face_limit = 10000, bool flatten_bottom = false, float flatten_bottom_threshold = 0.01f, int texture_size = 2048, TextureFormat texture_format = TextureFormat.JPEG, bool pivot_to_center_bottom = false)
        {
            if (string.IsNullOrEmpty(api_key))
            {
                this.InfoEvent?.Invoke(this, "You need to specify a valid TripoAI API_KEY");
                return null;
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("type", "convert_model");
            parameters.Add("format", format.ToString());
            parameters.Add("original_model_task_id", task_id);
            parameters.Add("quad", quad);
            parameters.Add("face_limit", face_limit);
            parameters.Add("flatten_bottom", flatten_bottom);
            parameters.Add("flatten_bottom_threshold", flatten_bottom_threshold);
            parameters.Add("texture_size", texture_size);
            parameters.Add("texture_format", texture_format.ToString());
            parameters.Add("pivot_to_center_bottom", pivot_to_center_bottom);

            string parametersJsonString = JsonConvert.SerializeObject(parameters);

            string conversionTaskId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", api_key);
                string uri = "https://api.tripo3d.ai/v2/openapi/task";
                StringContent jsonContent = new StringContent(parametersJsonString,
                     Encoding.UTF8,
                    "application/json");

                var result = await client.PostAsync(uri, jsonContent);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoResponse>(response);
                    if (tripoResponse != null)
                    {
                        conversionTaskId = tripoResponse.data.task_id;
                    }
                }
                else
                {
                    var response = await result.Content.ReadAsStringAsync();
                    var tripoResponse = JsonConvert.DeserializeObject<TripoErrorResponse>(response);
                    if (tripoResponse != null)
                    {
                        this.InfoEvent?.Invoke(this, $"Error Message: {tripoResponse.message}. \n\nSuggestion: {tripoResponse.suggestion}.");
                    }

                    return null;
                }
            };

            return conversionTaskId;
        }
    }
}
