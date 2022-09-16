using CreatifPixelLib.Models;
using System.Collections.Generic;

namespace CreatifPixelLib.Interfaces
{
    public interface IImageProcessor
    {
        (List<PixelizedImageSet> pixelizedImageSets, string name) BuildNewImage(string imageBase64, PixelizedImageSizes size, int contrast = 0, int buildByIndex = 0, bool saveSchemaImage = false);
    }    
}
