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

        private readonly int BRICK_SIZE = 67;
        private readonly int SMALL_BRICK_SIZE = 16;

        public ImageProcessor(ILogger<ImageProcessor> logger,
            IOptions<ImageTransformConfig> options) 
        {
            _logger = logger;
            _options = options.Value;
        }

        public (List<PixelizedImageSet> pixelizedImageSets, string name) BuildNewImage(string imageBase64, PixelizedImageSizes size, int buildByIndex, int contrast, bool saveSchemaImage = false)
        {
            if (imageBase64 == null) return (null, null);

            using var image = Utils.GetBitmapFromBase64(imageBase64);

            _logger.LogInformation("Image Width/Height: {Width}/{Height}", image.Width, image.Height);

            var pixelizedImages = BuildPixelizedImage(image, size, buildByIndex, contrast);

            DrawSchemaImage(pixelizedImages.pixelizedImageSets, BrickSizes.Small, false);

            var g = Guid.NewGuid().ToString("N");

            //////            
            if (_options.SaveSchemaImage && saveSchemaImage)
            {
                SaveSchemaImage(pixelizedImages.pixelizedImageSets, g);
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

            return (pixelizedImages.pixelizedImageSets, g);
        }

        protected (Bitmap image, List<PixelizedImageSet> pixelizedImageSets) BuildPixelizedImage(Bitmap image, PixelizedImageSizes size, int buildByIndex, int contrast = 0)
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

            using var pixelizedImageOriginal = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(pixelizedImageOriginal);

            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.Low;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImage(image, 0, 0, width, height);

            // 
            ApplyColorMatrix(pixelizedImageOriginal, width, height, grayscaleColorMatrix);
            var pixelsConOriginal = GetPixels(pixelizedImageOriginal);

            if (_options.ContrastLevels.Length == 0 || buildByIndex == 0)
                return (null, new List<PixelizedImageSet>(1) { new PixelizedImageSet { Pixels = SetPixelized(pixelsConOriginal, blockSize, blockSize), Contrast = 0 } });

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
                using var pixelizedImageConLevel = AdjustContrast(pixelizedImageOriginal, level);
                ApplyColorMatrix(pixelizedImageConLevel, width, height, grayscaleColorMatrix);
                var pixelizedImageSet = new PixelizedImageSet
                {
                    Pixels = GetPixels(pixelizedImageConLevel),
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
                        Pixels = CombinePixels(pixelsConOriginal, results[idx + 1].Pixels),
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
                result.Pixels = SetPixelized(result.Pixels, blockSize, blockSize);
            }

            //ApplySaturation(pixelizedImage, 1.0f);

            //using var pixelizedImageConFirst = AdjustContrast(pixelizedImageOriginal, 0);
            //using var pixelizedImageConSecond = AdjustContrast(pixelizedImageOriginal, contrast);


            //ApplyColorMatrix(pixelizedImageConSecond, width, height, grayscaleColorMatrix);


            //var pixelsCon50 = GetPixels(pixelizedImageConSecond);
            //var pixelsCombined = CombinePixels(pixelsCon0, pixelsCon50);

            //var pixels = SetPixelized(pixelizedImageCon50, blockSize, blockSize);
            //var pixels = SetPixelized(pixelsConOriginal, blockSize, blockSize);

            return (null, results);
        }

        protected int[,] SetPixelized(Bitmap image, int blockSizeX, int blockSizeY)
        {
            var y = 0;
            var arraySizeX = (int)Math.Ceiling((double)image.Width / blockSizeX);
            var arraySizeY = (int)Math.Ceiling((double)image.Height / blockSizeY);
            var result = new int[arraySizeX, arraySizeY];
            var indX = 0;
            var indY = 0;
            while (y < image.Height)
            {
                var x = 0;
                indX = 0;
                while (x < image.Width)
                {
                    var pixelWeight = GetPixelWeight(image, x, y, blockSizeX, blockSizeY);
                    //SetPixelBlockColorOnImage(image, x, y, blockSizeX, blockSizeY, pixelWeight.color);
                    result[indX, indY] = pixelWeight.weight;
                    x = (x + blockSizeX);
                    indX++;
                }
                y = y + blockSizeY;
                indY++;
            }
            return result;
        }

        protected int[,] SetPixelized(int[,] pixels, int blockSizeX, int blockSizeY)
        {
            var y = 0;
            var arraySizeX = (int)Math.Ceiling((double)pixels.GetLength(0) / blockSizeX);
            var arraySizeY = (int)Math.Ceiling((double)pixels.GetLength(1) / blockSizeY);
            var result = new int[arraySizeX, arraySizeY];
            var indX = 0;
            var indY = 0;
            while (y < pixels.GetLength(1))
            {
                var x = 0;
                indX = 0;
                while (x < pixels.GetLength(0))
                {
                    var pixelWeight = GetPixelWeight(pixels, x, y, blockSizeX, blockSizeY);
                    result[indX, indY] = pixelWeight.weight;
                    x = (x + blockSizeX);
                    indX++;
                }
                y = y + blockSizeY;
                indY++;
            }
            return result;
        }

        protected (int color, int weight) GetPixelWeight(Bitmap image, int x, int y, int blockSizeX, int blockSizeY)
        {
            int pixelWeight = 0;
            for (var blockX = 0; blockX < blockSizeX; blockX++)
            {
                for (var blockY = 0; blockY < blockSizeY; blockY++)
                {
                    if ((x + blockX) >= image.Width || (y + blockY) >= image.Height) break;
                    var pixel = image.GetPixel(x + blockX, y + blockY);
                    int avg = (pixel.R + pixel.G + pixel.B) / 3;
                    pixelWeight += avg;
                }
            }
            pixelWeight = Convert.ToInt32(pixelWeight / (blockSizeX * blockSizeY));
            return GetPixelWeightUpdateByRange(pixelWeight);
        }

        protected (int color, int weight) GetPixelWeight(int[,] pixels, int x, int y, int blockSizeX, int blockSizeY)
        {
            int pixelWeight = 0;
            for (var blockX = 0; blockX < blockSizeX; blockX++)
            {
                for (var blockY = 0; blockY < blockSizeY; blockY++)
                {
                    if ((x + blockX) >= pixels.GetLength(0) || (y + blockY) >= pixels.GetLength(1)) break;
                    var pixel = pixels[x + blockX, y + blockY];
                    pixelWeight += pixel;
                }
            }
            pixelWeight = Convert.ToInt32(pixelWeight / (blockSizeX * blockSizeY));
            return GetPixelWeightUpdateByRange(pixelWeight);
        }

        protected (int color, int weight) GetPixelWeightUpdateByRange(int originalPixelWeight)
        {
            if (originalPixelWeight < 0 || originalPixelWeight > 255) return (0, 0);

            var lowLimit = 0;
            var highLimit = _options.ColorLimits[0];
            var idx = 0;
            while (lowLimit < highLimit)
            {
                if (originalPixelWeight == 255) return (_options.ColorWeights[_options.ColorWeights.Length - 1], _options.ColorWeights.Length - 1);
                if (originalPixelWeight >= lowLimit && originalPixelWeight < highLimit) return (_options.ColorWeights[idx], idx);

                lowLimit = highLimit;
                idx++;
                highLimit = (idx == (_options.ColorWeights.Length - 1)) ? 255 : _options.ColorLimits[idx];
            }

            return (0, 0);
        }

        protected void SetPixelBlockColorOnImage(Bitmap image, int x, int y, int blockSizeX, int blockSizeY, int pixelWeight)
        {
            for (var blockX = 0; blockX < blockSizeX; blockX++)
            {
                for (var blockY = 0; blockY < blockSizeY; blockY++)
                {
                    if ((x + blockX) >= image.Width || (y + blockY) >= image.Height) break;
                    image.SetPixel(x + blockX, y + blockY, Color.FromArgb(255, pixelWeight, pixelWeight, pixelWeight));
                }
            }
        }

        protected Bitmap AdjustContrast(Bitmap image, float value)
        {
            value = (100.0f + value) / 100.0f;
            value *= value;
            Bitmap newBitmap = (Bitmap)image.Clone();
            BitmapData data = newBitmap.LockBits(
                new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                ImageLockMode.ReadWrite,
                newBitmap.PixelFormat);
            int height = newBitmap.Height;
            int width = newBitmap.Width;

            unsafe
            {
                for (int y = 0; y < height; ++y)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int columnOffset = 0;
                    for (int x = 0; x < width; ++x)
                    {
                        byte B = row[columnOffset];
                        byte G = row[columnOffset + 1];
                        byte R = row[columnOffset + 2];

                        float Red = R / 255.0f;
                        float Green = G / 255.0f;
                        float Blue = B / 255.0f;
                        Red = (((Red - 0.5f) * value) + 0.5f) * 255.0f;
                        Green = (((Green - 0.5f) * value) + 0.5f) * 255.0f;
                        Blue = (((Blue - 0.5f) * value) + 0.5f) * 255.0f;

                        int iR = (int)Red;
                        iR = iR > 255 ? 255 : iR;
                        iR = iR < 0 ? 0 : iR;
                        int iG = (int)Green;
                        iG = iG > 255 ? 255 : iG;
                        iG = iG < 0 ? 0 : iG;
                        int iB = (int)Blue;
                        iB = iB > 255 ? 255 : iB;
                        iB = iB < 0 ? 0 : iB;

                        row[columnOffset] = (byte)iB;
                        row[columnOffset + 1] = (byte)iG;
                        row[columnOffset + 2] = (byte)iR;

                        columnOffset += 4;
                    }
                }
            }

            newBitmap.UnlockBits(data);

            return newBitmap;
        }

        protected void SaveImagePreview(Bitmap image, string nameSuffix)
        {
            var dir = $"{_options.OutputSchemaFolder}\\{nameSuffix}";
            Directory.CreateDirectory(dir);

            using (var output = File.Open($"{dir}\\preview_image_{nameSuffix}.jpg", FileMode.Create))
            {
                var myEncoder = System.Drawing.Imaging.Encoder.Quality;
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(myEncoder, 75L);
                var codec = ImageCodecInfo.GetImageDecoders()
                    .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                image.Save(output, codec, encoderParameters);
            }
        }

        protected void SaveSchemaImage(List<PixelizedImageSet> pixelizedImageSets, string nameSuffix)
        {
            var outputDir = $"{_options.OutputSchemaFolder}\\{nameSuffix}";
            Directory.CreateDirectory(outputDir);

            DrawSchemaImage(pixelizedImageSets, BrickSizes.Big, true);

            using (var output = File.Open($"{outputDir}\\schema_image_{nameSuffix}.jpg", FileMode.Create))
            {
                var myEncoder = System.Drawing.Imaging.Encoder.Quality;
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(myEncoder, 100L);
                var codec = ImageCodecInfo.GetImageDecoders()
                    .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                foreach (var pixelizedImageSet in pixelizedImageSets)
                {
                    if (pixelizedImageSet.Bitmap != null) pixelizedImageSet.Bitmap.Save(output, codec, encoderParameters);
                }
            }
        }

        protected void DrawSchemaImage(List<PixelizedImageSet> pixelizedImageSets, BrickSizes brickSize, bool saveBitmap)
        {
            var templateDir = $"{_options.SchemaTemplateFolder}\\bricks";

            var _brickSize = (brickSize == BrickSizes.Small ? SMALL_BRICK_SIZE : BRICK_SIZE);
            var _brickSizeSuffix = (brickSize == BrickSizes.Small ? "_s" : "");

            var sizeX = _brickSize * pixelizedImageSets[0].Pixels.GetLength(0);
            var sizeY = _brickSize * pixelizedImageSets[0].Pixels.GetLength(1);

            using var brick1 = Image.FromFile($"{templateDir}\\c1{_brickSizeSuffix}.png");
            using var brick2 = Image.FromFile($"{templateDir}\\c2{_brickSizeSuffix}.png");
            using var brick3 = Image.FromFile($"{templateDir}\\c3{_brickSizeSuffix}.png");
            using var brick4 = Image.FromFile($"{templateDir}\\c4{_brickSizeSuffix}.png");
            using var brick5 = Image.FromFile($"{templateDir}\\c5{_brickSizeSuffix}.png");

            var bricks = new Image[] { brick5, brick4, brick3, brick2, brick1 };
            using var bitmap = new Bitmap(sizeX, sizeY);
            using var graphics = Graphics.FromImage(bitmap);

            foreach (var pixelizedImageSet in pixelizedImageSets)
            {
                for (int y = 0; y < pixelizedImageSets[0].Pixels.GetLength(0); y++)
                {
                    var coordY = y * _brickSize;
                    for (int x = 0; x < pixelizedImageSets[0].Pixels.GetLength(1); x++)
                    {
                        var pixelWeight = pixelizedImageSet.Pixels[x, y];
                        var coordX = x * _brickSize;
                        graphics.DrawImage(bricks[pixelWeight], coordX, coordY, _brickSize, _brickSize);
                    }
                }
                pixelizedImageSet.Base64ImageString = Utils.ImageToBase64(bitmap);
                if (saveBitmap) pixelizedImageSet.Bitmap = bitmap;
            }
        }

        protected void ApplySaturation(Bitmap image, float saturation)
        {
            float rWeight = 0.3086f;
            float gWeight = 0.6094f;
            float bWeight = 0.0820f;

            float a = (1.0f - saturation) * rWeight + saturation;
            float b = (1.0f - saturation) * rWeight;
            float c = (1.0f - saturation) * rWeight;
            float d = (1.0f - saturation) * gWeight;
            float e = (1.0f - saturation) * gWeight + saturation;
            float f = (1.0f - saturation) * gWeight;
            float g = (1.0f - saturation) * bWeight;
            float h = (1.0f - saturation) * bWeight;
            float i = (1.0f - saturation) * bWeight + saturation;

            var clrMatrix = new ColorMatrix(new[] {
                                new float[] {a,  b,  c,  0, 0},
                                new float[] {d,  e,  f,  0, 0},
                                new float[] {g,  h,  i,  0, 0},
                                new float[] {0,  0,  0,  1, 0},
                                new float[] {0, 0, 0, 0, 1}
                            });

            ApplyColorMatrix(image, image.Width, image.Height, clrMatrix);
        }

        protected void ApplyColorMatrix(Bitmap image, int width, int height, ColorMatrix colorMatrix)
        {
            using var g = Graphics.FromImage(image);
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(image, new Rectangle(0, 0, width, height),
                        0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);

        }

        protected int[,] GetPixels(Bitmap image) 
        {
            var pixels = new int[image.Width, image.Height];
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    int avg = (pixel.R + pixel.G + pixel.B) / 3;
                    pixels[x, y] = avg;
                }
            return pixels;
        }

        protected int[,] CombinePixels(int[,] pixels1, int[,] pixels2)
        {
            var pixels = new int[pixels1.GetLength(0), pixels1.GetLength(1)];
            for (int y = 0; y < pixels.GetLength(1); y++)
                for (int x = 0; x < pixels.GetLength(0); x++)
                    pixels[x, y] = (int)Math.Round((double)(pixels1[x, y] + pixels2[x, y]) / 2);
            return pixels;
        }
    }
}
