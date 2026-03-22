using System.Text;
using UglyToad.PdfPig;

namespace AssignmentManagement.Api.Services;

public interface IDocumentExtractService
{
    Task<string?> ExtractTextAsync(Stream fileStream, string fileName);
}

public class DocumentExtractService : IDocumentExtractService
{
    public async Task<string?> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == ".txt")
            return await ExtractFromTxtAsync(fileStream);
        if (ext == ".pdf")
            return await ExtractFromPdfAsync(fileStream);
        return null;
    }

    private static async Task<string?> ExtractFromTxtAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    /// <summary>.</summary>
    private static async Task<string?> ExtractFromPdfAsync(Stream stream)
    {
        try
        {
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            using var document = PdfDocument.Open(ms);
            var sb = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                var words = page.GetWords();
                var line = string.Join(" ", words.Select(w => w.Text));
                if (!string.IsNullOrWhiteSpace(line)) sb.AppendLine(line);
            }
            return sb.Length > 0 ? sb.ToString().Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
