using System.Runtime.Serialization;

namespace Feature_Translator;
public class FieldPostDto
{
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "templateFieldId")]
    public int TemplateFieldId { get; set; }

    [DataMember(Name = "value")]
    public string Value { get; set; }
}
