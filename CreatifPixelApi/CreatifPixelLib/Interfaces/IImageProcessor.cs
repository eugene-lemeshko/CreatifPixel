using CreatifPixelLib.Models;
using System.Collections.Generic;

namespace CreatifPixelLib.Interfaces
{
    public interface IImageProcessor
    {
        (List<PixelizedImageSet>? pixelizedImageSets, string? name, string? errorCode) BuildNewImage(string imageBase64, PixelizedImageSizes size, int contrast, int buildByIndex, bool saveSchemaImage);
    }    
}
