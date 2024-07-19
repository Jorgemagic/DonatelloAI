﻿using Evergine.Common.Graphics;
using System;
using System.Text.Json.Serialization;

namespace DonatelloAI.SceneManagers
{
    public class ModelData
    {
        public string TaskId { get; set; }

        public bool? IsRiggeable { get; set; }

        public string RigTaskId { get; set; }

        public string EntityName { get; set; }

        public string Thumbnail { get; set; }

        public string ModelFilePath { get; set; }

        public string Prompt { get; set; }

        [JsonIgnore]
        public Texture ThumbnailTexture { get; set; }

        [JsonIgnore]
        public IntPtr ThumbnailPointer { get; set; }
    }
}
