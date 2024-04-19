namespace NetTripoAI.SceneManagers
{
    public class TaskStatus
    {
        public enum TaskType
        {
            Refine,
            Animate,
        }

        public string TaskId;
        public TaskType Type;
        public string ModelName;
        public int progress;
        public string msg;
    }
}
