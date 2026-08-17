using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LocalCodeAgent.Core;
using LocalCodeAgent.Models;

namespace PmAssistant.Tools;

/// <summary>
/// Tool per creare documenti Word (.docx).
/// </summary>
public class WordDocTools(WorkspaceContext workspace)
{
    public List<ToolDefinition> Definitions =>
    [
        new() { Function = new() {
            Name = "create_word_doc",
            Description = "Crea un documento Word (.docx) con testo, titoli e paragrafi. Il file viene salvato nel workspace.",
            Parameters = new { type = "object", properties = new {
                filename = new { type = "string", description = "Nome del file (es. 'report.docx'). Se non ha estensione .docx viene aggiunta automaticamente." },
                title = new { type = "string", description = "Titolo principale del documento" },
                paragraphs_json = new { type = "string", description = "JSON array di paragrafi: [{\"text\":\"...\",\"heading_level\":1}]. heading_level opzionale (0=normale, 1-6=titolo)." },
                save_path = new { type = "string", description = "Percorso di salvataggio relativo al workspace (default: root)" }
            }, required = new[] { "filename", "title" } }
        }}
    ];

    public string Execute(string toolName, string argumentsJson)
    {
        return toolName switch
        {
            "create_word_doc" => CreateWordDoc(argumentsJson),
            _ => $"Tool '{toolName}' non trovato."
        };
    }

    private string CreateWordDoc(string argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<JsonElement>(argsJson);

            if (!args.TryGetProperty("filename", out var fnProp))
                return "ERRORE: parametro 'filename' obbligatorio mancante.";

            string filename = fnProp.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(filename))
                return "ERRORE: 'filename' non può essere vuoto.";

            // Aggiungi estensione .docx se mancante
            if (!filename.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                filename += ".docx";

            string title = args.TryGetProperty("title", out var tProp) ? (tProp.GetString() ?? "") : "";
            if (string.IsNullOrWhiteSpace(title))
                return "ERRORE: parametro 'title' obbligatorio mancante o vuoto.";

            // Percorso di salvataggio
            string savePath = ".";
            if (args.TryGetProperty("save_path", out var spProp) && !string.IsNullOrEmpty(spProp.GetString()))
                savePath = spProp.GetString()!;

            var absDir = workspace.Resolve(savePath);
            Directory.CreateDirectory(absDir);
            var fullPath = Path.Combine(absDir, filename);

            // Leggi i paragrafi da paragraphs_json (stringa JSON) o paragraphs (array)
            List<(string Text, int HeadingLevel)> paragraphs = [];
            
            if (args.TryGetProperty("paragraphs_json", out var pjProp))
            {
                var jsonStr = pjProp.GetString() ?? "[]";
                try
                {
                    using var doc = JsonDocument.Parse(jsonStr);
                    foreach (var para in doc.RootElement.EnumerateArray())
                    {
                        string text = "";
                        int heading = 0;

                        if (para.TryGetProperty("text", out var txt)) text = txt.GetString() ?? "";
                        if (para.TryGetProperty("heading_level", out var hl) && hl.ValueKind == JsonValueKind.Number) heading = hl.GetInt32();

                        paragraphs.Add((text, heading));
                    }
                }
                catch { /* Ignora paragrafi non validi */ }
            }

            // Crea il documento Word usando OpenXML
            using (var wordDocument = WordprocessingDocument.Create(fullPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();

                // Titolo principale (Heading 1)
                if (!string.IsNullOrWhiteSpace(title))
                    body.Append(CreateParagraph(title, headingLevel: 1));

                // Paragrafi aggiuntivi
                foreach (var para in paragraphs)
                {
                    if (string.IsNullOrWhiteSpace(para.Text)) continue;
                    var level = para.HeadingLevel > 0 ? para.HeadingLevel : 0;
                    body.Append(CreateParagraph(para.Text, headingLevel: level));
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return $"✓ Documento Word creato con successo: {fullPath} ({paragraphs.Count + 1} paragrafi totali)";
        }
        catch (Exception ex)
        {
            return $"ERRORE nella creazione del documento: {ex.Message}";
        }
    }

    private Paragraph CreateParagraph(string text, int headingLevel = 0)
    {
        var runProperties = new RunProperties();

        // Applica stile di intestazione se specificato
        if (headingLevel > 0 && headingLevel <= 6)
        {
            var paragraphStyleId = new ParagraphStyleId() { Val = $"Heading{headingLevel}" };
            runProperties.Append(paragraphStyleId);
        }

        var run = new Run(
            new RunProperties(runProperties),
            new Text(text)
        );

        return new Paragraph(run);
    }
}
