using Evergine.Framework;
using Evergine.Framework.Managers;
using DonatelloAI.TripoAI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace DonatelloAI.SceneManagers
{
    public class TaskManager : SceneManager
    {
        [BindService]
        private TripoAIService tripoAIService = null;

        [BindSceneManager]
        private ModelCollectionManager modelCollectionManager = null;

        public List<TaskStatus> TaskCollection = new List<TaskStatus>();

        public void RequestRefineModel()
        {
            Task.Run(async () =>
            {
                // Get task id
                string task_id = this.modelCollectionManager.FindTaskByCurrentSelectedEntity();
                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;

                if (!string.IsNullOrEmpty(task_id) && !string.IsNullOrEmpty(entityTag))
                {
                    TaskStatus taskStatus = new TaskStatus()
                    {
                        TaskId = task_id,
                        Type = TaskStatus.TaskType.Refine,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);

                    // Request refine Model
                    var refineTaskId = await this.tripoAIService.RequestRefineModel(task_id);

                    if (string.IsNullOrEmpty(refineTaskId)) return;

                    TripoResponse tripoResponse = null;
                    // Waiting to task completed                
                    string status = string.Empty;
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        tripoResponse = await this.tripoAIService.GetTaskStatus(refineTaskId);

                        var data = tripoResponse.data;
                        status = data.status;
                        taskStatus.progress = data.progress;
                        taskStatus.msg = $"status:{status} progress:{data.progress}";
                    }

                    if (status == "success")
                    {
                        taskStatus.progress = 100;
                        taskStatus.msg = $"status:{status}";

                        this.modelCollectionManager.DownloadModel(tripoResponse, entityTag + "_refined");
                    }
                }
            });
        }

        public void OldRequestAnimateModel()
        {
            Task.Run(async () =>
            {
                // Get task id
                string task_id = this.modelCollectionManager.FindTaskByCurrentSelectedEntity();
                //task_id = "7ee8d6b8-dad8-46f8-d26933e32dee";
                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;

                if (!string.IsNullOrEmpty(task_id) && !string.IsNullOrEmpty(entityTag))
                {
                    TaskStatus taskStatus = new TaskStatus()
                    {
                        TaskId = task_id,
                        Type = TaskStatus.TaskType.Animate,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);

                    // Request animate Model
                    var animateTaskId = await this.tripoAIService.OldRequestAnimateModel(task_id);
                    //var animateTaskId = "0eeaa2ec-fbd6-4f8a-9d67-c56ccbab01f4";

                    TripoResponse tripoResponse = null;
                    // Waiting to task completed                
                    string status = string.Empty;
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        tripoResponse = await this.tripoAIService.GetTaskStatus(animateTaskId);

                        var data = tripoResponse.data;
                        status = data.status;
                        taskStatus.progress = data.progress;
                        taskStatus.msg = $"status:{status} progress:{data.progress}";
                    }

                    if (status == "success")
                    {
                        taskStatus.progress = 100;
                        taskStatus.msg = $"status:{status}";

                        this.modelCollectionManager.DownloadModel(tripoResponse, entityTag + "_animated");
                    }
                }
            });
        }

        public void RequestStylization(TripoAIService.Styles style)
        {
            Task.Run(async () =>
            {
                // Get task id
                string task_id = this.modelCollectionManager.FindTaskByCurrentSelectedEntity();                
                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;                

                if (!string.IsNullOrEmpty(task_id) && !string.IsNullOrEmpty(entityTag))
                {
                    TaskStatus.TaskType taskType = default;
                    switch (style)
                    {
                        case TripoAIService.Styles.Voxel:
                            taskType = TaskStatus.TaskType.Voxel;
                            break;
                        case TripoAIService.Styles.Voronoi:
                            taskType = TaskStatus.TaskType.Voronoi;
                            break;
                        case TripoAIService.Styles.Lego:
                            taskType = TaskStatus.TaskType.Lego;
                            break;
                        default:
                            break;
                    }

                    TaskStatus taskStatus = new TaskStatus()
                    {
                        TaskId = task_id,
                        Type = taskType,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);

                    // Request refine Model
                    var stylizationTaskId = await this.tripoAIService.RequestStylization(task_id, style);                    
                    if (string.IsNullOrEmpty(stylizationTaskId)) return;

                    TripoResponse tripoResponse = null;
                    // Waiting to task completed                
                    string status = string.Empty;
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        tripoResponse = await this.tripoAIService.GetTaskStatus(stylizationTaskId);

                        var data = tripoResponse.data;
                        status = data.status;
                        taskStatus.progress = data.progress;
                        taskStatus.msg = $"status:{status} progress:{data.progress}";
                    }

                    if (status == "success")
                    {
                        taskStatus.progress = 100;
                        taskStatus.msg = $"status:{status}";

                        this.modelCollectionManager.DownloadModel(tripoResponse, entityTag + $"_{style}");
                    }
                }
            });
        }
    }
}
