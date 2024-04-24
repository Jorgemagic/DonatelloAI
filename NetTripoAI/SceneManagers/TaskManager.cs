using Evergine.Framework;
using Evergine.Framework.Managers;
using NetTripoAI.TripoAI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetTripoAI.SceneManagers
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
                ////task_id = "70984fdb-103d-4bb7-9e72-7d5d821d70a9";
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
                    ////var refineTaskId = "9381c697-cba8-4522-90cb-f83b23d88cbd";
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

                        this.modelCollectionManager.DownloadModel(tripoResponse, entityTag);
                    }
                }
            });
        }

        public void RequestAnimateModel()
        {
            Task.Run(async () =>
            {
                // Get task id
                string task_id = this.modelCollectionManager.FindTaskByCurrentSelectedEntity();
                ////task_id = "70984fdb-103d-4bb7-9e72-7d5d821d70a9";
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
                    var animateTaskId = await this.tripoAIService.RequestAnimateModel(task_id);
                    ////var animateTaskId = "63dd9653-45b3-4864-b18c-a20a80829431";

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

                        this.modelCollectionManager.DownloadModel(tripoResponse, entityTag);
                    }
                }
            });
        }
    }
}
