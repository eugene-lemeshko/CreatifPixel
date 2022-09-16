using System;
using System.Collections.Generic;
using System.Text;

namespace CreatifPixelLib.Models
{
    public class ImageTransformConfig
    {
        public const string Name = "ImageTransform";
        public int MediumSizeBlocks { get; set; }
        public int SmallSizeBlocks { get; set; }
        public int MediumSizeCanvas { get; set; }
        public int SmallSizeCanvas { get; set; }
        public int[] ColorWeights { get; set; }
        public int[] ColorLimits { get; set; }
        public int[] ContrastLevels { get; set; }
        public string SchemaTemplateFolder { get; set; }
        public string OutputSchemaFolder { get; set; }
        public bool SaveSchemaImage { get; set; }
    }
}
