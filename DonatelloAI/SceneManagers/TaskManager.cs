using DonatelloAI.TripoAI;
using Evergine.Framework;
using Evergine.Framework.Managers;
using System.Collections.Generic;
using System.Threading.Tasks;
using static DonatelloAI.TripoAI.TripoAIService;

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

        public void RequestAnimateModel(Animations animation)
        {
            Task.Run(async () =>
            {
                // Get task id
                string task_id = this.modelCollectionManager.FindTaskByCurrentSelectedEntity();
                //string task_id = "bc6322ec-6466-48d1-9a6c-b4faff273898";
                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;

                if (!string.IsNullOrEmpty(task_id) && !string.IsNullOrEmpty(entityTag))
                {
                    TaskStatus taskStatus = new TaskStatus()
                    {
                        TaskId = task_id,
                        Type = TaskStatus.TaskType.PreRigCheck,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);

                    // Request PreRigCheck
                    var checkTaskId = await this.tripoAIService.RequestPreRigCheck(task_id);

                    TripoResponse tripoResponse = null;

                    // Waiting to task completed                
                    string status = string.Empty;
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        tripoResponse = await this.tripoAIService.GetTaskStatus(checkTaskId);

                        var data = tripoResponse.data;
                        status = data.status;
                        taskStatus.progress = data.progress / 3;
                        taskStatus.msg = $"status:{status} progress:{data.progress}";
                    }

                    if (status != "success" || !tripoResponse.data.output.riggable)
                    {
                        // Show dialog
                        return;
                    }

                    // Request Rig                    
                    taskStatus.Type = TaskStatus.TaskType.Rig;
                    taskStatus.msg = "starting";
                    status = string.Empty;
                    var rigTaskId = await this.tripoAIService.RequestRig(task_id);
                    //var rigTaskId = "c00c187d-f1b6-4157-8e17-63ece06df4cb";

                    // Waiting to task completed                                    
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        tripoResponse = await this.tripoAIService.GetTaskStatus(rigTaskId);

                        var data = tripoResponse.data;
                        status = data.status;
                        taskStatus.progress += data.progress / 3;
                        taskStatus.msg = $"status:{status} progress:{data.progress}";
                    }

                    if (status != "success") return;

                    // Request Retarget
                    taskStatus.Type = TaskStatus.TaskType.Retarget;
                    taskStatus.msg = "starting";
                    status = string.Empty;
                    var retargetTaskId = await this.tripoAIService.RequestRetarget(rigTaskId, animation);
                    //var retargetTaskId = "8f9a7f17-09fb-422c-8eec-a060664acf5f";

                    // Waiting to task completed                                    
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        tripoResponse = await this.tripoAIService.GetTaskStatus(retargetTaskId);

                        var data = tripoResponse.data;
                        status = data.status;
                        taskStatus.progress += data.progress / 3;
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
