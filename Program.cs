using Newtonsoft.Json;
using System.Text;

class Program
{
    static async Task Main()
    {
        var launch = new Program();
        await launch.Run();
    }

    private async Task Run()
    {
        using (HttpClient client = new HttpClient())
        {
            // LOGIN
            var response = await Login(client);

            if (response.IsSuccessStatusCode)
            {
                // GET ALL CHILDREN FROM ROOT-CONTENT-ITEM
                response = await GetChildren(client, null);

                if (response.IsSuccessStatusCode)
                {
                    // CREATE LIST OF ALL ID's OF CHILDREN OF ROOT-CONTENT-ITEM
                    var content = await ReadContent(response);
                    var rootChildren = JsonConvert.DeserializeObject<List<ContentId>>(content);

                    #region - FIRST LEVEL - CHILDREN OF ROOT CONTENT
                    foreach (var child in rootChildren)
                    {
                        var success = int.TryParse(child.Id, out var firstLevelId);

                        if (success)
                        {
                            // GET ALL DATA FROM ALL CHILDREN OF ROOT-CONTENT-ITEM
                            response = await GetContentData(client, firstLevelId);

                            if (response.IsSuccessStatusCode)
                            {
                                content = await ReadContent(response);
                                var contentData = JsonConvert.DeserializeObject<List<ContentData>>(content);

                                foreach (var item in contentData)
                                {
                                    // ALTER DATA ONLY FOR DATATYPE 0
                                    if (item.DataType == 0)
                                    {
                                        // DEEPLE TRANSLATE 
                                        item.Value = await DeepleTranslate(client, item.Value);
                                    }

                                    item.Language = "fr-BE";
                                }

                                // PUT ALTERED DATA
                                response = await PutContentData(client, contentData);
                            }
                            else
                            {
                                Console.WriteLine("Get Data from Root-Content-Item failed \n");
                                Console.WriteLine($"{await ReadContent(response)} \n");
                            }
                        }
                    }
                    #endregion

                    #region - SECOND LEVEL - CHILDREN OF CHILDREN OF ROOT CONTENT
                    // GET CHILDREN OF CHILDREN OF ROOT-CONTENT-ITEM AND GO AROUND AGAIN
                    foreach (var child in rootChildren)
                    {
                        var success = int.TryParse(child.Id, out int firstLevelId);

                        if (success)
                        {
                            // GET THE CHILDREN OF CHILDREN
                            response = await GetChildren(client, firstLevelId);

                            if (response.IsSuccessStatusCode)
                            {
                                // CREATE LIST OF CHILDREN OF CHILDREN ID's
                                content = await ReadContent(response);
                                var secondLevelChildren = JsonConvert.DeserializeObject<List<ContentId>>(content);

                                foreach (var secondLevelChild in secondLevelChildren)
                                {
                                    // TRANSLATE SECOND LEVEL
                                    success = int.TryParse(secondLevelChild.Id, out int secondLevelId);

                                    if (success)
                                    {
                                        response = await GetContentData(client, secondLevelId);

                                        if (response.IsSuccessStatusCode)
                                        {
                                            content = await ReadContent(response);
                                            var contentData = JsonConvert.DeserializeObject<List<ContentData>>(content);

                                            foreach (var item in contentData)
                                            {
                                                // ALTER DATA ONLY FOR DATATYPE 0
                                                if (item.DataType == 0)
                                                {
                                                    // DEEPLE TRANSLATE 
                                                    item.Value = await DeepleTranslate(client, item.Value);
                                                }

                                                item.Language = "fr-BE";
                                            }

                                            // PUT ALTERED DATA
                                            response = await PutContentData(client, contentData);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Get Data from content {secondLevelChild.Id} failed \n");
                                            Console.WriteLine($"{await ReadContent(response)} \n");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Get Data from Child-Content-Item failed \n");
                                Console.WriteLine($"{await ReadContent(response)} \n");
                            }
                        }
                    }
                    #endregion

                    #region - THIRD LEVEL - CHILDREN OF CHILDREN OF ROOT CONTENT
                    // GET CHILDREN OF CHILDREN OF CHILDREN OF ROOT-CONTENT-ITEM AND GO AROUND AGAIN
                    foreach (var child in rootChildren)
                    {
                        var success = int.TryParse(child.Id, out int firstLevelId);

                        if (success)
                        {
                            // GET THE CHILDREN OF CHILDREN
                            response = await GetChildren(client, firstLevelId);

                            if (response.IsSuccessStatusCode)
                            {
                                // CREATE LIST OF CHILDREN OF CHILDREN ID's
                                content = await ReadContent(response);
                                var secondLevelChildren = JsonConvert.DeserializeObject<List<ContentId>>(content);

                                foreach (var secondLevelChild in secondLevelChildren)
                                {
                                    success = int.TryParse(secondLevelChild.Id, out int secondLevelId);

                                    // GET THE CHILDREN OF CHILDREN OF CHILDREN
                                    response = await GetChildren(client, secondLevelId);

                                    if (response.IsSuccessStatusCode)
                                    {
                                        // CREATE LIST OF CHILDREN OF CHILDREN OF CHILDREN ID's
                                        content = await ReadContent(response);
                                        var thirdLevelChildren = JsonConvert.DeserializeObject<List<ContentId>>(content);

                                        foreach ( var thirdLevelChild in thirdLevelChildren )
                                        {
                                            // TRANSLATE THIRD LEVEL
                                            success = int.TryParse(thirdLevelChild.Id, out int thirdLevelId);

                                            if (success)
                                            {
                                                response = await GetContentData(client, thirdLevelId);

                                                if (response.IsSuccessStatusCode)
                                                {
                                                    content = await ReadContent(response);
                                                    var contentData = JsonConvert.DeserializeObject<List<ContentData>>(content);

                                                    foreach (var item in contentData)
                                                    {
                                                        // ALTER DATA ONLY FOR DATATYPE 0
                                                        if (item.DataType == 0)
                                                        {
                                                            // DEEPLE TRANSLATE 
                                                            item.Value = await DeepleTranslate(client, item.Value);
                                                        }

                                                        item.Language = "fr-BE";
                                                    }

                                                    // PUT ALTERED DATA
                                                    response = await PutContentData(client, contentData);

                                                }
                                                else
                                                {
                                                    Console.WriteLine($"Get Data from content {thirdLevelChild.Id} failed \n");
                                                    Console.WriteLine($"{await ReadContent(response)} \n");
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Get Data from Child-Content-Item failed \n");
                                Console.WriteLine($"{await ReadContent(response)} \n");
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    Console.WriteLine("Get Root Children failed \n");
                    Console.WriteLine($"{await ReadContent(response)} \n");
                }
            }
            else
            {
                Console.WriteLine("Login failed \n");
                Console.WriteLine($"{await ReadContent(response)} \n");
            }
        }
    }

    private async Task<HttpResponseMessage> PutContentData(HttpClient client, List<ContentData> data)
    {
        // TODO: PUT CONTENT DATA BACK THROUGH API
        // TODO: Check that data is altered... check api url?
        // TODO: ITEM PER ITEM OR ALL AT ONCE?
        string putApiUrl = $"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item";
        var requestData = JsonConvert.SerializeObject(data);
        var request = new StringContent(requestData, Encoding.UTF8, "application/json");
        return await client.PutAsync(putApiUrl, request);
    }

    private async Task<string> DeepleTranslate(HttpClient client, string data)
    {
        var deepleApiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/deepl";
        var deepleRawPayload = $"{{text: \"{data}\", from: \"NL\", to: \"FR\"}}";
        var deeplePayload = new StringContent(deepleRawPayload, Encoding.UTF8, "application/json");
        var deepleResponse = await client.PostAsync(deepleApiUrl, deeplePayload);

        if (deepleResponse.IsSuccessStatusCode)
        {
            data = await deepleResponse.Content.ReadAsStringAsync();
        }

        return data.TrimStart('"').TrimEnd('"');
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