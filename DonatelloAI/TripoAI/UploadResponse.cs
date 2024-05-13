namespace DonatelloAI.TripoAI
{
    public class UploadResponse
    {
        public int code { get; set; }
        public UploadData data { get; set; }        
    }

    public class UploadData
    {
        public string image_token { get; set; }
    }
}
