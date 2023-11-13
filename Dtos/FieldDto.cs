using System.Runtime.Serialization;

[DataContract]
class FieldDto
{
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "contentItemId")]
    public int? ContentItemId { get; set; }

    [DataMember(Name = "name")]
    public string? Name { get; set; }

    [DataMember(Name = "dataType")]
    public int? DataType { get; set; }

    [DataMember(Name = "templateFieldId")]
    public int TemplateFieldId { get; set; }

    [DataMember(Name = "value")]
    public string? Value { get; set; }

    [DataMember(Name = "channel")]
    public string? Channel { get; set; }

    [DataMember(Name = "language")]
    public string? Language { get; set; }
}
