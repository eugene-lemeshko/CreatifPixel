using CreatifPixelLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CreatifPixelLib.Interfaces
{
    public interface ILicenseService
    {
        ImageBrickLicense GetLicenseByKey(string? licenseKey);
    }
}
