namespace CreatifPixelLib.Models
{
    public class BrickImage
    {
        public string Base64DataString { get; set; }
        public string? LicenseKey { get; set; }
        public PixelizedImageSizes Size { get; set; }
        public int Contrast { get; set; }
        public int BuildByIndex { get; set; }
        public bool? GetPreviews { get; set; } = true;
        public bool? GetPixels { get; set; } = false;
        public int[]? ImageAsPixels { get; set; }
    }
}
