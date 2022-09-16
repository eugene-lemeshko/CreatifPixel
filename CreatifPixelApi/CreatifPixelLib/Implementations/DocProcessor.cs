using CreatifPixelLib.Interfaces;
using CreatifPixelLib.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using SelectPdf;

namespace CreatifPixelLib.Implementations
{
    public class DocProcessor : IDocProcessor
    {
        private readonly ILogger<ImageProcessor> _logger;
        private readonly ImageTransformConfig _options;
        private readonly int RowsNumber = 2;
        private readonly int ColumnsNumber = 2;

        public DocProcessor(ILogger<ImageProcessor> logger,
            IOptions<ImageTransformConfig> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public string BuildHTMLScheme(int[,] pixels, string nameSuffix)
        {
            BuildTitle(pixels, nameSuffix, true);

            BuildSchema(pixels, nameSuffix, true);

            return nameSuffix;
        }

        public byte[] BuildPdfScheme(int[,] pixels, string nameSuffix)
        {
            var titleBody = BuildTitle(pixels, nameSuffix, false);

            var converter = new HtmlToPdf();
            converter.Options.MaxPageLoadTime = 120;

            var doc = converter.ConvertHtmlString(titleBody);

            byte[] bytes;
            using var mStream = new MemoryStream();

            doc.Save(mStream);
            mStream.Position = 0;

            bytes = new byte[mStream.Length];
            mStream.Read(bytes, 0, bytes.Length);

            doc.Close();

            return bytes;
        }


        protected string BuildTitle(int[,] pixels, string name, bool createFile)
        {
            var templateBody = File.ReadAllText(_options.SchemaTemplateFolder + "\\template_title.html");
            var outputDir = CreateOutputDir(name);
            var resultSchema = $"{outputDir}\\title_schema_{name}.html";

            var schemaString = new StringBuilder();
            var pixelsByColor = new int[] { 0, 0, 0, 0, 0 };
            for (var y = 0; y < pixels.GetLength(1); y++)
            {
                for (var x = 0; x < pixels.GetLength(0); x++)
                {
                    var pixelWeight = pixels[x, y];
                    var itemString = $"<div class=\"brick_{4 - pixelWeight}\"></div>";
                    schemaString.Append(itemString);
                    pixelsByColor[4 - pixelWeight] = pixelsByColor[4 - pixelWeight] + 1;
                }
            }

            // TEMP
            var schemaBody = templateBody;
            //var schemaBody = templateBody.Replace("{{schema-size-class}}", $"schema_{pixels.GetLength(0).ToString()}_{pixels.GetLength(1).ToString()}");
            //schemaBody = schemaBody.Replace("{{schema-data}}", schemaString.ToString());
            //for (var i = 0; i < pixelsByColor.Length; i++)
            //    schemaBody = schemaBody.Replace($"{{{{brick_amount_{i.ToString()}}}}}", pixelsByColor[i].ToString());

            if (createFile) File.WriteAllText(resultSchema, schemaBody, Encoding.UTF8);

            return schemaBody;
        }
        
        public (string fullFileName, string name) GetZipFolder(string name, bool removeAfter = true)
        {
            var filesFolder = CreateOutputDir(name);
            var zipFileName = $"{_options.OutputSchemaFolder}\\{name}.zip";

            ZipFile.CreateFromDirectory(filesFolder, zipFileName, CompressionLevel.Fastest, false);

            if (removeAfter) Directory.Delete(filesFolder, true);

            return (zipFileName, $"{name}.zip");
        }

        protected void BuildSchema(int[,] pixels, string nameSuffix, bool createFile)
        {
            var templateBody = File.ReadAllText(_options.SchemaTemplateFolder + "\\template.html");
            var outputDir = CreateOutputDir(nameSuffix);

            for (var pageY = 0; pageY < RowsNumber; pageY++)
            {
                for (var pageX = 0; pageX < ColumnsNumber; pageX++)
                {
                    var pageNumber = (pageY * ColumnsNumber) + (pageX + 1);
                    var resultSchema = $"{outputDir}\\schema_{nameSuffix}_{pageNumber.ToString()}.html";
                    var lengthX = pixels.GetLength(0) / ColumnsNumber;
                    var lengthY = pixels.GetLength(1) / RowsNumber;

                    var schemaBody = templateBody.Replace("{{schema-size-class}}", $"schema_{lengthX.ToString()}_{lengthY.ToString()}");

                    // page-number
                    schemaBody = schemaBody.Replace("{{page_number}}", $"{pageNumber.ToString()}/{(RowsNumber * ColumnsNumber).ToString()}");
                    var pageNumberSchema = "";
                    for (int pns = 0; pns < (ColumnsNumber * RowsNumber); pns++)
                    {
                        var selected = ((pageNumber - 1) == pns) ? "" : "selected";
                        pageNumberSchema += $"<div class=\"page-number-block {selected}\"></div>";
                    }
                    schemaBody = schemaBody.Replace("{{page_number_schema}}", pageNumberSchema);

                    //page-schema
                    var schemaString = new StringBuilder();
                    for (var y = 0; y < lengthY; y++)
                    {
                        var currentWeight = -1;
                        var weightCount = 0;

                        if (y == 0) schemaString.Append(BuildEmptyYLineNumber(lengthX));

                        for (var x = 0; x < lengthX; x++)
                        {
                            var coordX = (pageX * lengthX) + x;
                            var coordY = (pageY * lengthY) + y;
                            var pixelWeight = pixels[coordX, coordY];

                            if (pixelWeight != currentWeight)
                            {
                                currentWeight = pixelWeight;
                                weightCount = 1;
                            }
                            else 
                            {
                                weightCount++;
                            }

                            if (x == 0) schemaString.Append($"<div class=\"line_number\">{(y + 1).ToString()}</div>");

                            schemaString.Append($"<div class=\"block_{4 - pixelWeight}\"><span>{weightCount.ToString()}</span></div>");

                            if (x == (lengthX - 1)) schemaString.Append($"<div class=\"line_number\">{(y + 1).ToString()}</div>");
                        }

                        if (y == (lengthY - 1)) schemaString.Append(BuildEmptyYLineNumber(lengthX));
                    }

                    schemaBody = schemaBody.Replace("{{schema-data}}", schemaString.ToString());

                    if (createFile) File.WriteAllText(resultSchema, schemaBody, Encoding.UTF8);
                }
            }
        }

        protected string BuildEmptyYLineNumber(int lenghtX) 
        {
            var line = new StringBuilder();
            for (int x = 0; x < lenghtX; x++)
            {
                if (x == 0) line.Append($"<div class=\"line_number\"></div>");
                line.Append($"<div class=\"line_number\">{(x + 1).ToString()}</div>");
                if (x == (lenghtX - 1)) line.Append($"<div class=\"line_number\"></div>");
            }
            return line.ToString();
        }

        protected string CreateOutputDir(string nameSuffix)
        {
            var outputDir = $"{_options.OutputSchemaFolder}\\{nameSuffix}";
            Directory.CreateDirectory(outputDir);
            return outputDir;
        }
    }
}
