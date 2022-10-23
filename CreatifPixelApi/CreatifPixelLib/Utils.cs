using CreatifPixelLib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Schema;

namespace CreatifPixelLib
{
    public static class Utils
    {
        private static int BRICK_SIZE = 67;
        private static int SMALL_BRICK_SIZE = 16;

        public static (string fileName, string env) GetEnvironmentFileName()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrWhiteSpace(env))
                env = "Development";
            else
            {
                var envs = env.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                env = (envs.Length > 0) ? envs[0] : "Development";
            }

            return ($"appsettings.{env}.json", env);
        }

        public static String GetBase64FromJavaScriptImage(String javaScriptBase64String)
        {
            return Regex.Match(javaScriptBase64String, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
        }

        public static String GetImageTypeFromJavaScriptImage(String javaScriptBase64String)
        {
            return Regex.Match(javaScriptBase64String, @"data:image/(?<type>.+?);(?<base64>.+?),(?<data>.+)").Groups["type"].Value;
        }

        public static Bitmap GetBitmapFromBase64(string base64)
        {
            using MemoryStream mstream = new MemoryStream(Convert.FromBase64String(GetBase64FromJavaScriptImage(base64)));
            return new Bitmap(mstream);
        }

        public static Bitmap Base64StringToBitmap(string base64String)
        {
            Bitmap bmp = null;
            byte[] byteBuffer = Convert.FromBase64String(base64String);
            using MemoryStream memoryStream = new MemoryStream(byteBuffer);
            memoryStream.Position = 0;
            bmp = (Bitmap)Image.FromStream(memoryStream);
            byteBuffer = null;
            return bmp;
        }

        public static string ImageToBase64(Image _image)
        {
            using var ms = new MemoryStream();

            if (ImageFormatGuidToString(_image.RawFormat) == null)
                _image.Save(ms, ImageFormat.Jpeg);
            else
                _image.Save(ms, _image.RawFormat);

            var bytes = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(bytes, 0, (int)ms.Length);

            return $"data:image/jpg;base64,{Convert.ToBase64String(bytes)}";
        }

        public static string ImageFormatGuidToString(ImageFormat _format)
        {
            if (_format.Guid == ImageFormat.Bmp.Guid)
            {
                return "bmp";
            }
            else if (_format.Guid == ImageFormat.Gif.Guid)
            {
                return "gif";
            }
            else if (_format.Guid == ImageFormat.Jpeg.Guid)
            {
                return "jpg";
            }
            else if (_format.Guid == ImageFormat.Png.Guid)
            {
                return "png";
            }
            else if (_format.Guid == ImageFormat.Icon.Guid)
            {
                return "ico";
            }
            else if (_format.Guid == ImageFormat.Emf.Guid)
            {
                return "emf";
            }
            else if (_format.Guid == ImageFormat.Exif.Guid)
            {
                return "exif";
            }
            else if (_format.Guid == ImageFormat.Tiff.Guid)
            {
                return "tiff";
            }
            else if (_format.Guid == ImageFormat.Wmf.Guid)
            {
                return "wmf";
            }
            else
            {
                return null;
            }
        }

        public static int[,] CombinePixels(int[,] pixels1, int[,] pixels2)
        {
            var pixels = new int[pixels1.GetLength(0), pixels1.GetLength(1)];
            for (int y = 0; y < pixels.GetLength(1); y++)
                for (int x = 0; x < pixels.GetLength(0); x++)
                    pixels[x, y] = (int)Math.Round((double)(pixels1[x, y] + pixels2[x, y]) / 2);
            return pixels;
        }

        public static int[,] GetPixels(Bitmap image)
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

        public static void ApplyColorMatrix(Bitmap image, int width, int height, ColorMatrix colorMatrix)
        {
            using var g = Graphics.FromImage(image);
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(image, new Rectangle(0, 0, width, height),
                        0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);

        }

        public static void ApplySaturation(Bitmap image, float saturation)
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

            Utils.ApplyColorMatrix(image, image.Width, image.Height, clrMatrix);
        }

        public static int[,] SetPixelized(Bitmap image, int blockSizeX, int blockSizeY, ImageTransformConfig options)
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
                    var pixelWeight = GetPixelWeight(image, x, y, blockSizeX, blockSizeY, options);
                    result[indX, indY] = pixelWeight.weight;
                    x = (x + blockSizeX);
                    indX++;
                }
                y = y + blockSizeY;
                indY++;
            }
            return result;
        }

        public static int[,] SetPixelized(int[,] pixels, int blockSizeX, int blockSizeY, ImageTransformConfig options)
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
                    var pixelWeight = GetPixelWeight(pixels, x, y, blockSizeX, blockSizeY, options);
                    result[indX, indY] = pixelWeight.weight;
                    x = (x + blockSizeX);
                    indX++;
                }
                y = y + blockSizeY;
                indY++;
            }
            return result;
        }

        public static (int color, int weight) GetPixelWeight(Bitmap? image, int x, int y, int blockSizeX, int blockSizeY, ImageTransformConfig? options)
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
            return GetPixelWeightUpdateByRange(pixelWeight, options);
        }

        public static (int color, int weight) GetPixelWeight(int[,] pixels, int x, int y, int blockSizeX, int blockSizeY, ImageTransformConfig options)
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
            return GetPixelWeightUpdateByRange(pixelWeight, options);
        }

        public static void SetPixelBlockColorOnImage(Bitmap image, int x, int y, int blockSizeX, int blockSizeY, int pixelWeight)
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

        public static (int color, int weight) GetPixelWeightUpdateByRange(int originalPixelWeight, ImageTransformConfig options)
        {
            if (originalPixelWeight < 0 || originalPixelWeight > 255) return (0, 0);

            var lowLimit = 0;
            var highLimit = options.ColorLimits[0];
            var idx = 0;
            while (lowLimit < highLimit)
            {
                if (originalPixelWeight == 255) return (options.ColorWeights[options.ColorWeights.Length - 1], options.ColorWeights.Length - 1);
                if (originalPixelWeight >= lowLimit && originalPixelWeight < highLimit) return (options.ColorWeights[idx], idx);

                lowLimit = highLimit;
                idx++;
                highLimit = (idx == (options.ColorWeights.Length - 1)) ? 255 : options.ColorLimits[idx];
            }

            return (0, 0);
        }

        public static Bitmap AdjustContrast(Bitmap image, float value)
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

        public static void SaveImagePreview(Bitmap image, string nameSuffix, ImageTransformConfig options)
        {
            var dir = $"{options.OutputSchemaFolder}\\{nameSuffix}";
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

        public static void SaveSchemaImage(List<PixelizedImageSet> pixelizedImageSets, string nameSuffix, ImageTransformConfig options)
        {
            var outputDir = $"{options.OutputSchemaFolder}\\{nameSuffix}";
            Directory.CreateDirectory(outputDir);

            DrawSchemaImage(pixelizedImageSets, BrickSizes.Big, true, options);

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

        public static void DrawSchemaImage(List<PixelizedImageSet> pixelizedImageSets, BrickSizes brickSize, bool saveBitmap, ImageTransformConfig options)
        {
            var templateDir = $"{options.SchemaTemplateFolder}\\bricks";

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

        public static int[] GetPixelAmountsByColor(int[,] pixels)
        {
            var pixelAmountsByColor = new int[] { 0, 0, 0, 0, 0 };
            for (var y = 0; y < pixels.GetLength(1); y++)
            {
                for (var x = 0; x < pixels.GetLength(0); x++)
                {
                    var pixelWeight = pixels[x, y];
                    pixelAmountsByColor[pixelWeight] = pixelAmountsByColor[pixelWeight] + 1;
                }
            }
            return pixelAmountsByColor;
        }

        public static void CorrectPixelColorByAmount(PixelizedImageSet pixelizedImageSet, int maxAmount)
        {
            var colorByPixelDescriptors = new ColorByPixelDescriptor[5];
            //
            for (var x = 0; x < pixelizedImageSet.PixelAmountsByColor.Length; x++)
            {
                var amount = pixelizedImageSet.PixelAmountsByColor[x];
                var colorByPixelDescriptor = new ColorByPixelDescriptor { Amount = amount, Index = x, FreeAmount = (maxAmount - amount) };
                colorByPixelDescriptors[x] = colorByPixelDescriptor;
            }

            //
            for (var currentIdx = 0; currentIdx < colorByPixelDescriptors.Length; currentIdx++)
            {
                var currentColorByPixelDescriptor = colorByPixelDescriptors[currentIdx];
                if (currentColorByPixelDescriptor.FreeAmount < 0)
                {
                    currentColorByPixelDescriptor.NearestFreeIndexes = FindNearestFreeIndexes(currentIdx, colorByPixelDescriptors);
                    for (var donorIdx = 0; donorIdx < currentColorByPixelDescriptor.NearestFreeIndexes.Length; donorIdx++)
                    {
                        var nearestFreeIndex = currentColorByPixelDescriptor.NearestFreeIndexes[donorIdx];
                        var donorColorByPixelDescriptor = colorByPixelDescriptors[nearestFreeIndex];

                        var donorAmountToGet = 0;
                        var currentAmountAbs = Math.Abs(currentColorByPixelDescriptor.FreeAmount);

                        if (donorColorByPixelDescriptor.FreeAmount < currentAmountAbs)
                        {
                            donorAmountToGet = donorColorByPixelDescriptor.FreeAmount;
                            donorColorByPixelDescriptor.FreeAmount = 0;
                            currentColorByPixelDescriptor.FreeAmount = currentColorByPixelDescriptor.FreeAmount + donorAmountToGet;
                            // 
                            UpdatePixelSetWithNewColor(pixelizedImageSet, currentIdx, nearestFreeIndex, donorAmountToGet);
                        }
                        else
                        {
                            donorAmountToGet = currentAmountAbs;
                            donorColorByPixelDescriptor.FreeAmount = donorColorByPixelDescriptor.FreeAmount - donorAmountToGet;
                            currentColorByPixelDescriptor.FreeAmount = 0;
                            //
                            UpdatePixelSetWithNewColor(pixelizedImageSet, currentIdx, nearestFreeIndex, donorAmountToGet);
                            break;
                        }

                    }
                }
            }
        }

        public static int[,] GetPixelsFromLine(int[] line, int size)
        {
            var result = new int[size, size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    result[x, y] = line[(y * size) + x];
            return result;
        }

        private static void UpdatePixelSetWithNewColor(PixelizedImageSet pixelizedImageSet, int toColorIdx, int fromColorIdx, int fromAmount)
        {
            var toColorIndexes = new List<ColorIdxCoord>();
            for (var y = 0; y < pixelizedImageSet.Pixels.GetLength(1); y++)
                for (var x = 0; x < pixelizedImageSet.Pixels.GetLength(0); x++)
                {
                    var pixelColor = pixelizedImageSet.Pixels[x, y];
                    if (pixelColor == toColorIdx) toColorIndexes.Add(new ColorIdxCoord { X = x, Y = y });
                }

            for (var randomIdx = 0; randomIdx < fromAmount; randomIdx++)
            {
                var randomCoordIdx = Random.Shared.Next(0, toColorIndexes.Count - 1);
                var coordToSet = toColorIndexes[randomCoordIdx];
                pixelizedImageSet.Pixels[coordToSet.X, coordToSet.Y] = fromColorIdx;
                toColorIndexes.RemoveAt(randomCoordIdx);
            }
        }

        private static int[] FindNearestFreeIndexes(int baseIndex, ColorByPixelDescriptor[] descriptors)
        {
            var result = new List<int>(4);
            for (var delta = 1; delta < 5; delta++)
            {
                var plusPosition = baseIndex + delta;
                var minusPosition = baseIndex - delta;

                if (plusPosition < 5 && descriptors[plusPosition].FreeAmount > 0) result.Add(plusPosition);
                if (minusPosition >= 0 && descriptors[minusPosition].FreeAmount > 0) result.Add(minusPosition);
            }
            return result.ToArray();
        }
    }

    class ColorIdxCoord
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    class ColorByPixelDescriptor
    {
        public int Amount { get; set; }
        public int FreeAmount { get; set; }
        public int Index { get; set; }
        public int[] NearestFreeIndexes { get; set; }

    }
}
