using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using UglyToad.PdfPig;

namespace NvidiaChatBot.Services;

public static class FileParserUtil
{
    static FileParserUtil()
    {
        // Required for ExcelDataReader in newer .NET Core
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    public static async Task<string> ParseFileAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        try
        {
            switch (extension)
            {
                case ".txt":
                case ".csv":
                case ".md":
                    using (var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true))
                    {
                        return await reader.ReadToEndAsync();
                    }
                case ".pdf":
                    return ParsePdf(fileStream);
                case ".docx":
                    return ParseDocx(fileStream);
                case ".xlsx":
                case ".xls":
                    return ParseExcel(fileStream);
                default:
                    // If we can't parse it as text, just return empty or a note. 
                    // Images will be handled separately as Base64 data URLs.
                    return $"[File type '{extension}' not supported for text extraction]";
            }
        }
        catch (Exception ex)
        {
            return $"[Error reading file '{fileName}': {ex.Message}]";
        }
    }

    private static string ParsePdf(Stream stream)
    {
        var sb = new StringBuilder();
        using (var document = PdfDocument.Open(stream))
        {
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }
        }
        return sb.ToString();
    }

    private static string ParseDocx(Stream stream)
    {
        var sb = new StringBuilder();
        using (var wordDoc = WordprocessingDocument.Open(stream, false))
        {
            var body = wordDoc.MainDocumentPart?.Document.Body;
            if (body != null)
            {
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    sb.AppendLine(paragraph.InnerText);
                }
            }
        }
        return sb.ToString();
    }

    private static string ParseExcel(Stream stream)
    {
        var sb = new StringBuilder();
        using (var reader = ExcelReaderFactory.CreateReader(stream))
        {
            var result = reader.AsDataSet();
            foreach (System.Data.DataTable table in result.Tables)
            {
                sb.AppendLine($"--- Sheet: {table.TableName} ---");
                foreach (System.Data.DataRow row in table.Rows)
                {
                    var rowData = row.ItemArray.Select(i => i?.ToString() ?? "");
                    sb.AppendLine(string.Join("\t", rowData));
                }
            }
        }
        return sb.ToString();
    }
}
