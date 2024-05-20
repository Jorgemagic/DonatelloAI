using DonatelloAI.TripoAI;
using Evergine.Framework;
using Evergine.Framework.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static DonatelloAI.TripoAI.TripoAIService;

namespace DonatelloAI.SceneManagers
{
    public class TaskManager : SceneManager
    {
        public event EventHandler<string> InfoEvent;

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
                var modelData = this.modelCollectionManager.FindModelDataByCurrentSelectedEntity();
                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;

                if (modelData != null && !string.IsNullOrEmpty(entityTag))
                {
                    TaskStatus taskStatus = new TaskStatus()
                    {
                        TaskId = modelData.TaskId,
                        Type = TaskStatus.TaskType.Refine,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);

                    // Request refine Model
                    var refineTaskId = await this.tripoAIService.RequestRefineModel(modelData.TaskId);

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

                        var modelURL = tripoResponse.data.output.model;
                        var taskID = refineTaskId;
                        this.modelCollectionManager.DownloadModel(modelURL, refineTaskId, entityTag + "_refined");
                    }
                }
            });
        }

        public void RequestPreRigCheckModel()
        {
            Task.Run(async () =>
            {
                // Get task id
                var modelData = this.modelCollectionManager.FindModelDataByCurrentSelectedEntity();                
                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;

                if (modelData != null && !string.IsNullOrEmpty(entityTag))
                {
                    TaskStatus taskStatus = new TaskStatus()
                    {
                        TaskId = modelData.TaskId,
                        Type = TaskStatus.TaskType.PreRigCheck,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);

                    // Request PreRigCheck
                    var checkTaskId = await this.tripoAIService.RequestPreRigCheck(modelData.TaskId);

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
                        taskStatus.progress = data.progress;
                        taskStatus.msg = $"status:{status} progress:{data.progress}";
                    }

                    if (status == "success")
                    {
                        modelData.IsRiggeable = tripoResponse.data.output.riggable;
                        taskStatus.progress = 100;
                        taskStatus.msg = $"status:{status}";
                        this.InfoEvent?.Invoke(this, $"Success: The model {entityTag} can be rigged");
                    }
                    else
                    {
                        this.InfoEvent?.Invoke(this, $"Failed: The model {entityTag} cannot be rigged");
                    }
                }
            });
        }

        public void RequestRigModel()
        {
            Task.Run(async () =>
            {
                // Get task id
                var modelData = this.modelCollectionManager.FindModelDataByCurrentSelectedEntity();

                if (!modelData.IsRiggeable.HasValue ||
                    !modelData.IsRiggeable.Value)
                {
                    throw new System.Exception("Model is not riggeable");                    
                }

                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;

                if (modelData != null && !string.IsNullOrEmpty(entityTag))
                {
                    TaskStatus taskStatus = new TaskStatus()
                    {
                        TaskId = modelData.TaskId,
                        Type = TaskStatus.TaskType.Rig,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);

                    // Request Rig                                   
                    var rigTaskId = await this.tripoAIService.RequestRig(modelData.TaskId);

                    TripoResponse tripoResponse = null;

                    // Waiting to task completed
                    string status = string.Empty;
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        tripoResponse = await this.tripoAIService.GetTaskStatus(rigTaskId);

                        var data = tripoResponse.data;
                        status = data.status;
                        taskStatus.progress += data.progress;
                        taskStatus.msg = $"status:{status} progress:{data.progress}";
                    }

                    if (status == "success")
                    {
                        modelData.RigTaskId = rigTaskId;

                        taskStatus.progress = 100;
                        taskStatus.msg = $"status:{status}";
                        this.InfoEvent?.Invoke(this, $"Success: The model {entityTag} has been rigged");
                    }
                    else
                    {
                        this.InfoEvent?.Invoke(this, $"Failed: The model {entityTag} cannot be rigged");
                    }
                }
            });
        }


        public void RequestAnimateModel(Animations animation)
        {
            Task.Run(async () =>
            {
                // Get task id
                var modelData = this.modelCollectionManager.FindModelDataByCurrentSelectedEntity();

                if (string.IsNullOrEmpty(modelData.RigTaskId))
                {
                    throw new System.Exception("The model needs to be rigged before to be animated");
                }
                
                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;

                if (modelData != null && !string.IsNullOrEmpty(entityTag))
                {
                    TaskStatus taskStatus = new TaskStatus()
                    {
                        TaskId = modelData.TaskId,
                        Type = TaskStatus.TaskType.Retarget,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);                    

                    // Request Retarget                                        
                    var retargetTaskId = await this.tripoAIService.RequestRetarget(modelData.RigTaskId, animation);

                    TripoResponse tripoResponse = null;

                    // Waiting to task completed
                    string status = string.Empty;
                    while (status == string.Empty ||
                           status == "queued" ||
                           status == "running")
                    {
                        await Task.Delay(100);
                        tripoResponse = await this.tripoAIService.GetTaskStatus(retargetTaskId);

                        var data = tripoResponse.data;
                        status = data.status;
                        taskStatus.progress += data.progress;
                        taskStatus.msg = $"status:{status} progress:{data.progress}";
                    }

                    if (status == "success")
                    {
                        taskStatus.progress = 100;
                        taskStatus.msg = $"status:{status}";

                        var modelURL = tripoResponse.data.output.model;
                        this.modelCollectionManager.DownloadModel(modelURL, string.Empty, entityTag + "_animated");
                    }
                }
            });
        }

        public void RequestStylization(TripoAIService.Styles style)
        {
            Task.Run(async () =>
            {
                // Get task id
                var modelData = this.modelCollectionManager.FindModelDataByCurrentSelectedEntity();                
                string entityTag = this.modelCollectionManager.CurrentSelectedEntity?.Tag;                

                if (modelData != null && !string.IsNullOrEmpty(entityTag))
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
                        TaskId = modelData.TaskId,
                        Type = taskType,
                        ModelName = entityTag,
                        progress = 0,
                        msg = "starting",
                    };
                    this.TaskCollection.Add(taskStatus);

                    // Request refine Model
                    var stylizationTaskId = await this.tripoAIService.RequestStylization(modelData.TaskId, style);                    
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

                        var modelURL = tripoResponse.data.output.model;
                        this.modelCollectionManager.DownloadModel(modelURL, string.Empty, entityTag + $"_{style}");
                    }
                }
            });
        }
    }
}
