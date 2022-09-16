using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CreatifPixelLib
{
    public static class Utils
    {
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
    }
}
