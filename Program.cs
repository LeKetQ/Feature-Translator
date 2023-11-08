using Newtonsoft.Json;
using System.Dynamic;
using System.Runtime.Serialization;
using System.Text;

[DataContract]
class ContentData
{
    [DataMember(Name = "dataType")]
    public int DataType { get; set; }

    [DataMember(Name = "templateFieldId")]
    public int TemplateFieldId { get; set; }

    [DataMember(Name = "value")]
    public string Value { get; set; }

    // Where => DataType == 0 => SET language == "fr-BE"
    [DataMember(Name = "channel")]
    public string Channel { get; set; }

    [DataMember(Name = "language")]
    public string Language { get; set; }
}

[DataContract]
class ContentId
{
    [DataMember(Name = "id")]
    public string Id { get; set; }
}

class Program
{
    static async Task Main()
    {
        var launch = new Program();
        using (HttpClient client = new HttpClient())
        {
            // LOGIN
            var response = await launch.Login(client);

            if (response.IsSuccessStatusCode)
            {
                // GET ALL CHILDREN FROM ROOT-CONTENT-ITEM
                response = await launch.GetChildren(client, null);

                if (response.IsSuccessStatusCode)
                {
                    // CREATE LIST OF ALL ID's OF CHILDREN OF ROOT-CONTENT-ITEM
                    var content = await launch.ReadContent(response);
                    var rootChildren = JsonConvert.DeserializeObject<List<ContentId>>(content);

                    #region - FIRST LEVEL - CHILDREN OF ROOT CONTENT
                    foreach (var child in rootChildren)
                    {
                        var success = int.TryParse(child.Id, out var id);

                        if (success)
                        {
                            // GET ALL DATA FROM ALL CHILDREN OF ROOT-CONTENT-ITEM
                            response = await launch.GetContentData(client, id);

                            if (response.IsSuccessStatusCode)
                            {
                                content = await launch.ReadContent(response);
                                var deserializedContent = JsonConvert.DeserializeObject<List<ContentData>>(content);

                                foreach (var item in deserializedContent)
                                {
                                    // ALTER DATA ONLY FOR DATATYPE 0
                                    if (item.DataType == 0)
                                    {
                                        // DEEPLE TRANSLATE 
                                        item.Value = await launch.DeepleTranslate(client, item.Value);
                                    }

                                    item.Language = "fr-BE";
                                }

                                // PUT ALTERED DATA
                                // TODO: PUT CONTENT DATA BACK THROUGH API
                                // TODO: Check that data is altered... check api url?
                                // TODO: ITEM PER ITEM OR ALL AT ONCE?
                                //string putApiUrl = $"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/{id}";
                                //var requestData = JsonConvert.SerializeObject(deserializedChildContent);
                                //var request = new StringContent(requestData, Encoding.UTF8, "application/json");
                                //response = await client.PutAsync(putApiUrl, request);
                            }
                            else
                            {
                                Console.WriteLine("Get Data from Root-Content-Item failed \n");
                                Console.WriteLine($"{await launch.ReadContent(response)} \n");
                            }
                        }
                    }
                    #endregion

                    #region - SECOND LEVEL - CHILDREN OF CHILDREN OF ROOT CONTENT
                    // GET CHILDREN OF CHILDREN OF ROOT-CONTENT-ITEM AND GO AROUND AGAIN
                    foreach (var child in rootChildren)
                    {
                        var success = int.TryParse(child.Id, out int childId);

                        if (success)
                        {
                            // GET THE CHILDREN OF CHILDREN
                            response = await launch.GetChildren(client, childId);

                            if (response.IsSuccessStatusCode)
                            {
                                // CREATE LIST OF CHILDREN OF CHILDREN ID's
                                content = await launch.ReadContent(response);
                                var secondLevelChildren = JsonConvert.DeserializeObject<List<ContentId>>(content);

                                foreach (var secondLevelChild in secondLevelChildren)
                                {
                                    success = int.TryParse(secondLevelChild.Id, out int id);

                                    if (success)
                                    {
                                        response = await launch.GetContentData(client, id);

                                        if (response.IsSuccessStatusCode)
                                        {
                                            content = await launch.ReadContent(response);
                                            var contentData = JsonConvert.DeserializeObject<List<ContentData>>(content);

                                            foreach (var item in contentData)
                                            {
                                                // ALTER DATA ONLY FOR DATATYPE 0
                                                if (item.DataType == 0)
                                                {
                                                    // DEEPLE TRANSLATE 
                                                    item.Value = await launch.DeepleTranslate(client, item.Value);
                                                }

                                                item.Language = "fr-BE";
                                            }

                                            // PUT ALTERED DATA
                                            // TODO: PUT CONTENT DATA BACK THROUGH API
                                            // TODO: Check that data is altered... check api url?
                                            // TODO: ITEM PER ITEM OR ALL AT ONCE?
                                            //string putApiUrl = $"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/{id}";
                                            //var requestData = JsonConvert.SerializeObject(deserializedChildContent);
                                            //var request = new StringContent(requestData, Encoding.UTF8, "application/json");
                                            //response = await client.PutAsync(putApiUrl, request);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Get Data from content {secondLevelChild.Id} failed \n");
                                            Console.WriteLine($"{await launch.ReadContent(response)} \n");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Get Data from Child-Content-Item failed \n");
                                Console.WriteLine($"{await launch.ReadContent(response)} \n");
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    Console.WriteLine("Get Root Children failed \n");
                    Console.WriteLine($"{await launch.ReadContent(response)} \n");
                }
            }
            else
            {
                Console.WriteLine("Login failed \n");
                Console.WriteLine($"{await launch.ReadContent(response)} \n");
            }





            #region - OLD CODE
            if (response.IsSuccessStatusCode)
            {
                List<int> parentList = new List<int>() { 84, 1097, 1624 };
                // TODO: GO DEEPER INTO THE RABBIT HOLE
                // LOOP OVER THE PARENT LIST ID's TO GET CHILDREN AND ALTER THE CHILDREN DATA
                foreach (var parent in parentList)
                {
                    string parentTreeApiUrl = $"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item-tree/children/{parent}";
                    response = await client.GetAsync(parentTreeApiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var parentContent = await response.Content.ReadAsStringAsync();
                        var deserializedParentContent = JsonConvert.DeserializeObject<List<ContentId>>(parentContent);

                        if (deserializedParentContent != null)
                        {
                            // LOOP OVER EACH 1ST LEVEL CHILD, GET ALL DATA AND 2ND LEVEL DATA, ALTER DATA IN FR AND PUT BACK
                            foreach (var child in deserializedParentContent)
                            {
                                string postApiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/inheritance";
                                var rawPayload = $"{{contentItemId: {child.Id}, channel: \"Internal\", languageCode: \"nl-BE\"}}";
                                var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
                                response = await client.PostAsync(postApiUrl, payload);

                                if (response.IsSuccessStatusCode)
                                {
                                    var childContent = await response.Content.ReadAsStringAsync();
                                    var deserializedChildContent = JsonConvert.DeserializeObject<List<ContentData>>(childContent);

                                    if (deserializedChildContent != null)
                                    {
                                        // ALTER DATA
                                        foreach (var item in deserializedChildContent)
                                        {
                                            if (item.DataType == 0)
                                            {
                                                var deepleApiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/deepl";
                                                var deepleRawPayload = $"{{text: \"{item.Value}\", from: \"NL\", to: \"FR\"}}";
                                                var deeplePayload = new StringContent(deepleRawPayload, Encoding.UTF8, "application/json");
                                                var deepleResponse = await client.PostAsync(deepleApiUrl, deeplePayload);
                                                item.Value = await deepleResponse.Content.ReadAsStringAsync();
                                            }

                                            item.Language = "fr-BE";
                                        }

                                        // PUT ALTERED DATA
                                        // TODO: Check that data is altered... check api url?
                                        string putApiUrl = $"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/{child.Id}";
                                        var requestData = JsonConvert.SerializeObject(deserializedChildContent);
                                        var request = new StringContent(requestData, Encoding.UTF8, "application/json");
                                        response = await client.PutAsync(putApiUrl, request);

                                        if (response.IsSuccessStatusCode)
                                        {
                                            Console.WriteLine($"{child.Id} - FR Data transfer successful \n");
                                        }
                                        else
                                        {
                                            string errorContent = await response.Content.ReadAsStringAsync();
                                            Console.WriteLine($"{child.Id} - {errorContent} \n");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"deserializedContent == null \n");
                                    }
                                }
                                else
                                {
                                    string errorContent = await response.Content.ReadAsStringAsync();
                                    Console.WriteLine($"{errorContent} \n");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"List of Children == null \n");
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"{errorContent} \n");
                    }
                }
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{errorContent} \n");
            }
            #endregion
        }
    }

    private async Task<string> DeepleTranslate(HttpClient client, string data)
    {
        var deepleApiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/deepl";
        var deepleRawPayload = $"{{text: \"{data}\", from: \"NL\", to: \"FR\"}}";
        var deeplePayload = new StringContent(deepleRawPayload, Encoding.UTF8, "application/json");
        var deepleResponse = await client.PostAsync(deepleApiUrl, deeplePayload);

        if (deepleResponse.IsSuccessStatusCode)
        {
            return await deepleResponse.Content.ReadAsStringAsync();
        }
        else
        {
            return data;
        }
    }

    private async Task<HttpResponseMessage> Login(HttpClient client)
    {
        var loginData = new
        {
            Email = "travelworld@qite.be",
            Password = "XdYZJ7QFxttAqjKRSEW!",
        };

        string signinApiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/account/signin";
        string jsonData = JsonConvert.SerializeObject(loginData);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        return await client.PostAsync(signinApiUrl, content);
    }

    private async Task<HttpResponseMessage> GetChildren(HttpClient client, int? parentId)
    {
        if (parentId == null)
        {
            // ROOT
            return await client.GetAsync("https://tide-travelworld8527-staging.azurewebsites.net/api/content-item-tree/children");
        }
        else
        {
            // CHILDREN FROM PARENT ID
            return await client.GetAsync($"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item-tree/children/{parentId}");
        }
    }

    private async Task<HttpResponseMessage> GetContentData(HttpClient client, int id)
    {
        string postApiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/inheritance";
        var rawPayload = $"{{contentItemId: {id}, channel: \"Internal\", languageCode: \"nl-BE\"}}";
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        return await client.PostAsync(postApiUrl, payload);
    }

    private async Task<string> ReadContent(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }
}