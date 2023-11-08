using System.Runtime.Serialization;

[DataContract]
class ContentId
{
    [DataMember(Name = "id")]
    public string Id { get; set; }
}
