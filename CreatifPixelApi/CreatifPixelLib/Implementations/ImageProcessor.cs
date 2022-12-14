using CreatifPixelLib.Interfaces;
using CreatifPixelLib.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace CreatifPixelLib.Implementations
{
    public class ImageProcessor : IImageProcessor
    {
        private readonly ILogger<ImageProcessor> _logger;
        private readonly ImageTransformConfig _options;

        private readonly ColorMatrix grayscaleColorMatrix = new ColorMatrix(new[] {
            new[] {.3f, .3f, .3f, 0, 0},
            new[] {.59f, .59f, .59f, 0, 0},
            new[] {.11f, .11f, .11f, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 1}
        });

        public ImageProcessor(ILogger<ImageProcessor> logger,
            IOptions<ImageTransformConfig> options) 
        {
            _logger = logger;
            _options = options.Value;
        }

        public (List<PixelizedImageSet>? pixelizedImageSets, string? name, string? errorCode) BuildNewImage(string imageBase64, PixelizedImageSizes size, int contrast, int buildByIndex, bool saveSchemaImage)
        {
            if (imageBase64 == null) return (null, null, "NO_IMAGE_BODY");

            using var image = Utils.GetBitmapFromBase64(imageBase64);

            if (image.Width != image.Height) return (null, null, "WRONG_IMAGE_SIZE");

            _logger.LogInformation("Image Width/Height: {Width}/{Height}", image.Width, image.Height);

            var pixelizedImages = BuildPixelizedImage(image, size, contrast, buildByIndex);

            Utils.DrawSchemaImage(pixelizedImages.pixelizedImageSets, BrickSizes.Small, false, _options);

            var g = Guid.NewGuid().ToString("N");

            //////            
            if (_options.SaveSchemaImage && saveSchemaImage)
            {
                Utils.SaveSchemaImage(pixelizedImages.pixelizedImageSets, g, _options);
            }
            //////

            foreach (var pixelizedImageSet in pixelizedImages.pixelizedImageSets)
            {
                try
                {
                    if (pixelizedImageSet.Bitmap != null) pixelizedImageSet.Bitmap.Dispose();
                }
                catch { }
            }

            return (pixelizedImages.pixelizedImageSets, g, null);
        }

        protected (Bitmap image, List<PixelizedImageSet> pixelizedImageSets) BuildPixelizedImage(Bitmap image, PixelizedImageSizes size, int contrast, int buildByIndex)
        {
            int imageSize, blockSize;
            if (size == PixelizedImageSizes.Small) {
                imageSize = _options.SmallSizeCanvas * _options.SmallSizeBlocks;
                blockSize = _options.SmallSizeBlocks;
            }
            else if (size == PixelizedImageSizes.Medium)
            {
                imageSize = _options.MediumSizeBlocks * _options.MediumSizeCanvas;
                blockSize = _options.MediumSizeBlocks;
            }
            else
                throw new ArgumentException("PixelizedImageSizes is incorrect");

            int width, height;
            if (image.Width > image.Height)
            {
                width = imageSize;
                height = Convert.ToInt32(image.Height * imageSize / (double)image.Width);
            }
            else
            {
                width = Convert.ToInt32(image.Width * imageSize / (double)image.Height);
                height = imageSize;
            }

            using var imageOriginal = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(imageOriginal);

            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.Low;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImage(image, 0, 0, width, height);

            // 
            Utils.ApplyColorMatrix(imageOriginal, width, height, grayscaleColorMatrix);
            var pixelsConOriginal = Utils.GetPixels(imageOriginal);

            if (_options.ContrastLevels.Length == 0 || buildByIndex == 0)
                return (null, new List<PixelizedImageSet>(1)
                {
                    new PixelizedImageSet { Pixels = Utils.SetPixelized(pixelsConOriginal, blockSize, blockSize, _options), Contrast = 0 }
                });

            if (buildByIndex > ((_options.ContrastLevels.Length * 2) + 1))
                throw new ArgumentException("Build index is incorrect");

            int[] contrastLevels;
            bool isCombined = false;
            if (buildByIndex == -1)
                contrastLevels = _options.ContrastLevels;
            else
            {
                int contrastLevelIdx;
                if (buildByIndex > _options.ContrastLevels.Length) 
                {
                    contrastLevelIdx = buildByIndex - (_options.ContrastLevels.Length + 1);
                    isCombined = true;
                } 
                else
                    contrastLevelIdx = buildByIndex - 1;
                contrastLevels = new int[] { _options.ContrastLevels[contrastLevelIdx] };
            }

            //
            var results = new List<PixelizedImageSet>((contrastLevels.Length * 2) + 1);
            results.Add(new PixelizedImageSet { Pixels = pixelsConOriginal, Contrast = 0, IsCombined = false });

            foreach (var level in contrastLevels)
            {
                using var pixelizedImageConLevel = Utils.AdjustContrast(imageOriginal, level);
                Utils.ApplyColorMatrix(pixelizedImageConLevel, width, height, grayscaleColorMatrix);
                var pixelizedImageSet = new PixelizedImageSet
                {
                    Pixels = Utils.GetPixels(pixelizedImageConLevel),
                    Contrast = level,
                    IsCombined = false
                };
                results.Add(pixelizedImageSet);
            }

            //
            if (buildByIndex == -1 || isCombined)
            {
                for (var idx = 0; idx < contrastLevels.Length; idx++)
                {
                    var pixelizedImageSet = new PixelizedImageSet
                    {
                        Pixels = Utils.CombinePixels(pixelsConOriginal, results[idx + 1].Pixels),
                        Contrast = results[idx + 1].Contrast,
                        IsCombined = true
                    };
                    results.Add(pixelizedImageSet);
                }
            }

            // 
            if (buildByIndex != -1)
            { 
                var last = results[results.Count - 1];
                results.Clear();
                results.Add(last);
            }

            foreach (var result in results)
            {
                result.Pixels = Utils.SetPixelized(result.Pixels, blockSize, blockSize, _options);
            }

            return (null, results);
        }
    }
}
