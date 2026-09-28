using System.Text.Json.Serialization;

namespace FieldFlow.Application.DTOs.OData;

/// <summary>Wrapper usado apenas em <c>[ProducesResponseType]</c> para o Swagger documentar respostas OData.</summary>
public class ODataResponse<T>
{
    [JsonPropertyName("@odata.context")]
    public string? Context { get; set; }

    [JsonPropertyName("@odata.count")]
    public long? Count { get; set; }

    [JsonPropertyName("value")]
    public IEnumerable<T> Value { get; set; } = new List<T>();
}
