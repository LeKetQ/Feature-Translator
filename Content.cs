using System.Text.Json.Serialization;

namespace Feature_Translator;
internal class Content
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("parentId")]
    public int? ParentId { get; set; }

    [JsonPropertyName("catalogueId")]
    public int? CatalogueId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("locationId")]
    public int? LocationId { get; set; }

    [JsonPropertyName("productType")]
    public int? ProductType { get; set; }

    [JsonPropertyName("contentItemType")]
    public int? ContentItemType { get; set; }

    [JsonPropertyName("version")]
    public int? Version { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("channelId")]
    public int? ChannelId { get; set; }

    [JsonPropertyName("contentItemChannelId")]
    public int? ContentItemChannelId { get; set; }

    [JsonPropertyName("isContentItemChannelActive")]
    public bool IsContentItemChannelActive { get; set; }

    [JsonPropertyName("companyId")]
    public int? CompanyId { get; set; }

    [JsonPropertyName("position")]
    public int? Position { get; set; }

    [JsonPropertyName("templateId")]
    public int? TemplateId { get; set; }

    [JsonPropertyName("templateName")]
    public string? TemplateName { get; set; }

    [JsonPropertyName("templateFolderId")]
    public int? TemplateFolderId { get; set; }

    [JsonPropertyName("children")]
    public string? Children { get; set; }

    [JsonPropertyName("fields")]
    public List<Field>? Fields { get; set; }

    [JsonPropertyName("publishedBy")]
    public string? PublishedBy { get; set; }

    [JsonPropertyName("publishedDate")]
    public string? PublishedDate { get; set; }

    [JsonPropertyName("previewUrl")]
    public string? PreviewUrl { get; set; }

    [JsonPropertyName("styleSheets")]
    public string? StyleSheets { get; set; }
}
