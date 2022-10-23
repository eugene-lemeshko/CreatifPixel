using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace CreatifPixelLib.Models
{
    public class PixelizedImageSet
    {
        public int[,] Pixels { get; set; }
        public Image Bitmap { get; set; }
        public string Base64ImageString { get; set; }
        public int Contrast { get; set; }
        public bool IsCombined { get; set; }
        public int[] PixelAmountsByColor { get; set; }
    }
}
