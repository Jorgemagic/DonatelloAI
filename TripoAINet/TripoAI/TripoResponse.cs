namespace TripoAINet.TripoAI
{
    public class TripoResponse
    {
        public int code { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public string task_id { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public Input input { get; set; }
        public Output output { get; set; }
        public int progress { get; set; }
        public int create_time { get; set; }
        public string prompt { get; set; }
        public string thumbnail { get; set; }
        public Result result { get; set; }
    }

    public class Input
    {
        public string prompt { get; set; }
    }

    public class Output
    {
        public string model { get; set; }
        public string rendered_image { get; set; }

        public string rendered_video { get; set; }
    }

    public class Result
    {
        public Model model { get; set; }
        public Rendered_Image rendered_image { get; set; }
    }

    public class Model
    {
        public string url { get; set; }
        public string type { get; set; }
    }

    public class Rendered_Image
    {
        public string type { get; set; }
        public string url { get; set; }
    }
}
