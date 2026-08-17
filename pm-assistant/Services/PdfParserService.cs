using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace PmAssistant.Services;

public interface IPdfParserService
{
    Task<string> ExtractTextAsync(string filePath);
}

public class PdfParserService : IPdfParserService
{
    public async Task<string> ExtractTextAsync(string filePath)
    {
        using var pdf = PdfDocument.Open(filePath);
        var sb = new System.Text.StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }
}
