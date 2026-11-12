using System.Text.Json;

namespace DWIMS.Service.Analysis;

public static class TemplateAnalysisJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    
    public static string Serialize(TemplateAnalysis templateAnalysis)
    {
        return JsonSerializer.Serialize(templateAnalysis, Options);
    }

    public static TemplateAnalysis? Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<TemplateAnalysis>(json, Options); }
        catch (JsonException) { return null; }
    }
}