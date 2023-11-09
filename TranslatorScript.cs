using Feature_Translator;
using Newtonsoft.Json;
using System.Text;

class TranslatorScript
{
    static async Task Main()
    {
        var launch = new TranslatorScript();
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
                #region - First Level
                // GET ALL DATA FROM ROOT-CONTENT-ITEM
                response = await GetChildren(client, null);

                if (response.IsSuccessStatusCode)
                {
                    var content = await ReadContent(response);
                    var firstLevelChildren = JsonConvert.DeserializeObject<List<ContentId>>(content);

                    foreach (var firstLevelChild in firstLevelChildren)
                    {
                        var success = int.TryParse(firstLevelChild.Id, out var firstLevelId);

                        if (success)
                        {
                            // GET, PROCESS AND PUT FIRST LEVEL DATA 
                            var result = await ProcessContent(client, firstLevelId);

                            #region - Second Level
                            // GET SECOND LEVEL CHILDREN
                            var secondLevelChildren = await GetChildren(client, firstLevelId);

                            if (secondLevelChildren.IsSuccessStatusCode)
                            {
                                var secondLevelContent = await ReadContent(secondLevelChildren);
                                var secondLevelChildrenIds = JsonConvert.DeserializeObject<List<ContentId>>(secondLevelContent);

                                foreach (var secondLevelChildId in secondLevelChildrenIds)
                                {
                                    success = int.TryParse(secondLevelChildId.Id, out int secondLevelId);

                                    if (success)
                                    {
                                        // GET, PROCESS AND PUT SECOND LEVEL DATA 
                                        result = await ProcessContent(client, secondLevelId);

                                        #region - Third Level
                                        // GET THIRD LEVEL CHILDREN
                                        var thirdLevelChildren = await GetChildren(client, secondLevelId);

                                        if (thirdLevelChildren.IsSuccessStatusCode)
                                        {
                                            var thirdLevelContent = await ReadContent(thirdLevelChildren);
                                            var thirdLevelChildrenIds = JsonConvert.DeserializeObject<List<ContentId>>(thirdLevelContent);

                                            foreach (var thirdLevelChildId in thirdLevelChildrenIds)
                                            {
                                                success = int.TryParse(thirdLevelChildId.Id, out int thirdLevelId);

                                                if (success)
                                                {
                                                    // GET, PROCESS AND PUT THIRD LEVEL DATA 
                                                    result = await ProcessContent(client, thirdLevelId);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Get Third Level Children failed \n");
                                            Console.WriteLine($"{await ReadContent(response)} \n");
                                        }
                                        #endregion
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Get Second Level Children failed \n");
                                Console.WriteLine($"{await ReadContent(response)} \n");
                            }
                            #endregion
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Get First Level Children failed \n");
                    Console.WriteLine($"{await ReadContent(response)} \n");
                }
                #endregion
            }
            else
            {
                Console.WriteLine("Login failed \n");
                Console.WriteLine($"{await ReadContent(response)} \n");
            }
        }
    }

    private async Task<HttpResponseMessage> ProcessContent(HttpClient client, int id)
    {
        var response = await GetContentData(client, id);

        if (response.IsSuccessStatusCode)
        {
            var content = await ReadContent(response);
            var fields = JsonConvert.DeserializeObject<List<Field>>(content);

            // GET OVERHEAD OBJECT
            response = await GetOverheadObject(client, id);

            if (response.IsSuccessStatusCode)
            {
                content = await ReadContent(response);
                var finalContent = JsonConvert.DeserializeObject<Content>(content);

                // MAP THE TRANSLATED VALUE
                foreach (var field in fields.Where(f => f.DataType == 0))
                {
                    field.Value = await DeepleTranslate(client, field.Value);

                    finalContent.Fields
                        .Where(fc => fc.TemplateFieldId == field.TemplateFieldId)
                        .ToList()
                        .ForEach(fc => fc.Value = field.Value);

                    field.Language = "fr-BE";
                }

                finalContent.IsContentItemChannelActive = true;
                finalContent.Language = "fr-BE";

                response = await PutContent(client, finalContent);

                Console.WriteLine(response.IsSuccessStatusCode
                    ? $"Content ID: {finalContent.Id} - SUCCESS"
                    : $"Content ID: {finalContent.Id} - FAILED");
            }
        }

        return response;
    }

    private async Task<HttpResponseMessage> GetOverheadObject(HttpClient client, int id)
    {
        return await client.GetAsync($"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/{id}/Internal/fr-BE/1");
    }

    private async Task<HttpResponseMessage> PutContent(HttpClient client, Content content)
    {
        string putApiUrl = $"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item";
        var rawPayload = JsonConvert.SerializeObject(content);
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        var test = await client.PutAsync(putApiUrl, payload);
        return test;
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
        var rawPayload = $"{{contentItemId: {id}, channel: \"Internal\", languageCode: \"fr-BE\"}}";
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        return await client.PostAsync(postApiUrl, payload);
    }

    private async Task<string> ReadContent(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }
}