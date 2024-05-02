using System.IO;

namespace DonatelloAI.UI
{
    public struct ImageData
    {
        public string FilePath {  get; }
        public string Extension { get; }
        public string Base64String { get; }

        public ImageData(string filePath)
        {
            this.FilePath = filePath;
            this.Extension = Path.GetExtension(this.FilePath);
            var data = File.ReadAllBytes(filePath);
            this.Base64String = System.Convert.ToBase64String(data);
        }
    }
}
