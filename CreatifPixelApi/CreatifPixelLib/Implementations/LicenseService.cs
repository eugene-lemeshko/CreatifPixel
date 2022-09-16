using CreatifPixelLib.Interfaces;
using CreatifPixelLib.Models;
using System;

namespace CreatifPixelLib.Implementations
{
    public class LicenseService: ILicenseService
    {
        private const string TEMP_SMALL_KEY = "Small_151187";
        private const string TEMP_MEDIUM_KEY = "Medium_190979";

        public ImageBrickLicense GetLicenseByKey(string? licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey)) return null;

            if (licenseKey == TEMP_SMALL_KEY) return new ImageBrickLicense { Expired = DateTime.MaxValue, Size = PixelizedImageSizes.Small };
            if (licenseKey == TEMP_MEDIUM_KEY) return new ImageBrickLicense { Expired = DateTime.MaxValue, Size = PixelizedImageSizes.Medium };

            return null;
        }
    }
}
