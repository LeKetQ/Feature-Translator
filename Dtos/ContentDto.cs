using System.Runtime.Serialization;

namespace Feature_Translator.Dtos;

[DataContract]
internal class ContentDto
{
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "parentId")]
    public int? ParentId { get; set; }

    [DataMember(Name = "catalogueId")]
    public int? CatalogueId { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "isActive")]
    public bool IsActive { get; set; }

    [DataMember(Name = "locationId")]
    public int? LocationId { get; set; }

    [DataMember(Name = "productType")]
    public int? ProductType { get; set; }

    [DataMember(Name = "contentItemType")]
    public int? ContentItemType { get; set; }

    [DataMember(Name = "version")]
    public int? Version { get; set; }

    [DataMember(Name = "language")]
    public string Language { get; set; }

    [DataMember(Name = "channelId")]
    public int? ChannelId { get; set; }

    [DataMember(Name = "contentItemChannelId")]
    public int? ContentItemChannelId { get; set; }

    [DataMember(Name = "isContentItemChannelActive")]
    public bool IsContentItemChannelActive { get; set; }

    [DataMember(Name = "companyId")]
    public int? CompanyId { get; set; }

    [DataMember(Name = "position")]
    public int? Position { get; set; }

    [DataMember(Name = "templateId")]
    public int? TemplateId { get; set; }

    [DataMember(Name = "templateName")]
    public string TemplateName { get; set; }

    [DataMember(Name = "templateFolderId")]
    public int? TemplateFolderId { get; set; }

    [DataMember(Name = "children")]
    public string Children { get; set; }

    [DataMember(Name = "publishedBy")]
    public string PublishedBy { get; set; }

    [DataMember(Name = "publishedDate")]
    public string PublishedDate { get; set; }

    [DataMember(Name = "previewUrl")]
    public string PreviewUrl { get; set; }

    [DataMember(Name = "styleSheets")]
    public string StyleSheets { get; set; }

    [DataMember(Name = "fields")]
    public List<FieldDto> Fields { get; set; }
}
