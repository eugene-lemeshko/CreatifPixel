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
            var htmlTitle = BuildTitle(pixels, nameSuffix, _options.SaveSchemaImage);

            var htmlBodies = BuildSchema(pixels, nameSuffix, _options.SaveSchemaImage);

            var converter = new HtmlToPdf();
            converter.Options.MaxPageLoadTime = 120;
            converter.Options.MarginTop = 16;
            converter.Options.MarginBottom = 16;

            var pdfDoc = converter.ConvertHtmlString(htmlTitle);
            for (int i = 0; i < htmlBodies.Length; i++)
            {
                var page = converter.ConvertHtmlString(htmlBodies[i]);
                pdfDoc.Append(page);
            }

            byte[] bytes;
            using var mStream = new MemoryStream();

            pdfDoc.Save(mStream);
            mStream.Position = 0;

            bytes = new byte[mStream.Length];
            mStream.Read(bytes, 0, bytes.Length);

            pdfDoc.Close();

            return bytes;
        }


        protected string BuildTitle(int[,] pixels, string name, bool createFile)
        {
            var templateBody = File.ReadAllText(_options.SchemaTemplateFolder + "\\template_title2.html");

            var schemaString = new StringBuilder();

            var xCenter = pixels.GetLength(0) / 2;
            var yCenter = pixels.GetLength(1) / 2;
            for (var y = 0; y < pixels.GetLength(1); y++)
            {
                schemaString.Append("<tr>");
                var ySeparator = (y + 1) == yCenter ? "y_separator" : "";
                for (var x = 0; x < pixels.GetLength(0); x++)
                {
                    var pixelWeight = pixels[x, y];
                    var xSeparator = (x + 1) == xCenter ? "x_separator" : "";
                    var itemString = $"<td class=\"{xSeparator} {ySeparator}\"><div class=\"brick_{4 - pixelWeight}\"></div></td>";
                    schemaString.Append(itemString);
                }
                schemaString.Append("</tr>");
            }

            //
            var schemaBody = templateBody.Replace("{{schema-size-class}}", $"schema_{pixels.GetLength(0).ToString()}_{pixels.GetLength(1).ToString()}");
            var pixelAmountsByColor = Utils.GetPixelAmountsByColor(pixels);
            schemaBody = schemaBody.Replace("{{schema-data}}", schemaString.ToString());
            for (var i = 0; i < pixelAmountsByColor.Length; i++)
                schemaBody = schemaBody.Replace($"{{{{brick_amount_{i.ToString()}}}}}", pixelAmountsByColor[i].ToString());

            if (createFile)
            {
                var outputDir = CreateOutputDir(name);
                var resultSchema = $"{outputDir}\\title_schema_{name}.html";
                File.WriteAllText(resultSchema, schemaBody, Encoding.UTF8);
            }

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

        protected string[] BuildSchema(int[,] pixels, string nameSuffix, bool createFile)
        {
            var templateBody = File.ReadAllText(_options.SchemaTemplateFolder + "\\template2.html");
            var results = new string[4];
            var idx = 0;

            for (var pageY = 0; pageY < RowsNumber; pageY++)
            {
                for (var pageX = 0; pageX < ColumnsNumber; pageX++)
                {
                    var pageNumber = (pageY * ColumnsNumber) + (pageX + 1);

                    var lengthX = pixels.GetLength(0) / ColumnsNumber;
                    var lengthY = pixels.GetLength(1) / RowsNumber;

                    var schemaBody = templateBody.Replace("{{schema-size-class}}", $"schema_{lengthX.ToString()}_{lengthY.ToString()}");

                    // page-number
                    schemaBody = schemaBody.Replace("{{page_number}}", $"{pageNumber.ToString()}/{(RowsNumber * ColumnsNumber).ToString()}");

                    var pageNumberSchema = "";
                    for (int pnsY = 0; pnsY < RowsNumber; pnsY++)
                    {
                        pageNumberSchema += "<tr>";
                        for (int pnsX = 0; pnsX < ColumnsNumber; pnsX++)
                        {
                            var pns = (pnsY * ColumnsNumber) + pnsX;
                            var selected = ((pageNumber - 1) == pns) ? "selected" : "";
                            pageNumberSchema += $"<td><div class=\"page-number-block {selected}\"></div></td>";
                        }
                        pageNumberSchema += "</tr>";
                    }
                    schemaBody = schemaBody.Replace("{{page_number_schema}}", pageNumberSchema);

                    //page-schema
                    var schemaString = new StringBuilder();
                    for (var y = 0; y < lengthY; y++)
                    {
                        var currentWeight = -1;
                        var weightCount = 0;

                        if (y == 0) schemaString.Append(BuildEmptyYLineNumber(lengthX));

                        schemaString.Append("<tr>");
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

                            if (x == 0) schemaString.Append($"<td><div class=\"line_number\">{(y + 1).ToString()}</div></td>");

                            schemaString.Append($"<td><div class=\"block_{4 - pixelWeight}\"><span>{weightCount.ToString()}</span></div></td>");

                            if (x == (lengthX - 1)) schemaString.Append($"<td><div class=\"line_number\">{(y + 1).ToString()}</div></td>");
                        }
                        schemaString.Append("</tr>");

                        if (y == (lengthY - 1)) schemaString.Append(BuildEmptyYLineNumber(lengthX));
                    }

                    schemaBody = schemaBody.Replace("{{schema-data}}", schemaString.ToString());
                    results[idx++] = schemaBody;

                    if (createFile)
                    {
                        var outputDir = CreateOutputDir(nameSuffix);
                        var resultSchema = $"{outputDir}\\schema_{nameSuffix}_{pageNumber.ToString()}.html";
                        File.WriteAllText(resultSchema, schemaBody, Encoding.UTF8);
                    }
                }
            }

            return results;
        }

        protected string BuildEmptyYLineNumber(int lenghtX) 
        {
            var line = new StringBuilder();
            line.Append("<tr>");
            for (int x = 0; x < lenghtX; x++)
            {
                if (x == 0) line.Append($"<td><div class=\"line_number\"></div></td>");
                line.Append($"<td><div class=\"line_number\">{(x + 1).ToString()}</div></td>");
                if (x == (lenghtX - 1)) line.Append($"<td><div class=\"line_number\"></div></td>");
            }
            line.Append("</tr>");
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
