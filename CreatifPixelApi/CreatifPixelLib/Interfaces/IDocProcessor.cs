using CreatifPixelLib.Models;
using System.IO;

namespace CreatifPixelLib.Interfaces
{
    public interface IDocProcessor
    {
        string BuildHTMLScheme(int[,] pixels, string nameSuffix);
        byte[] BuildPdfScheme(int[,] pixels, string nameSuffix);
        (string fullFileName, string name) GetZipFolder(string name, bool removeAfter = true);
    }
}
