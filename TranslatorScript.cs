using Feature_Translator.Dtos;
using Newtonsoft.Json;
using System.Text;

class TranslatorScript
{
    int successes = 0;
    int failures = 0;
    StringBuilder stringBuilder = new StringBuilder();
    const string French = "fr-BE";
    const string Dutch = "nl-BE";

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

            if (!response.IsSuccessStatusCode)
            {
                await LogFailure("Login", response);
                return;
            }

            // Process each level of children recursively
            await ProcessLevel(client, null);
        }
    }

    private async Task ProcessLevel(HttpClient client, int? parentId)
    {
        var response = await GetChildren(client, parentId);

        if (!response.IsSuccessStatusCode)
        {
            await LogFailure($"GetChildren for {parentId} failed", response);
            return;
        }

        var content = await ReadContent(response);
        var children = JsonConvert.DeserializeObject<List<ContentId>>(content);

        foreach (var child in children)
        {
            if (int.TryParse(child.Id, out var childId))
            {
                // GET, PROCESS AND PUT DATA 
                await ProcessContent(client, childId);

                // Recursively process next level
                await ProcessLevel(client, childId);
            }
        }
    }

    private async Task LogFailure(string action, HttpResponseMessage response)
    {
        Console.WriteLine($"{action} failed \n");
        Console.WriteLine($"{await ReadContent(response)} \n");
    }

    private async Task ProcessContent(HttpClient client, int id)
    {
        var response = await GetFrenchOverheadObject(client, id);

        if (response.IsSuccessStatusCode)
        {
            // READ CONTENT
            var content = await ReadContent(response);
            var frenchObject = JsonConvert.DeserializeObject<ContentDto>(content);
            var dutchFields = await GetDutchContentData(client, id);

            if (frenchObject != null)
            {
                if (frenchObject.Fields.Any())
                {
                    // TRANSLATE DUTCH FIELDVALUE IF FRENCH FIELDVALUE IS EMPTY
                    foreach (var field in frenchObject.Fields)
                    {
                        if (field.Value == null)
                        {
                            var dutchField = dutchFields.FirstOrDefault(f => f.TemplateFieldId == field.TemplateFieldId);

                            if (dutchField != null)
                            {
                                if (dutchField.Value != null 
                                    && dutchField.DataType == 0 
                                    && dutchField.TemplateFieldId != 79
                                    && dutchField.TemplateFieldId != 82)
                                {
                                    var toTranslate = dutchField.Value.Replace("\n", "TEXTE ")
                                                                        .Replace("&nbsp;", "ESPACE ");
                                    var translation = await DeepleTranslate(client, toTranslate);
                                    field.Value = translation.Replace("&amp;", "&")
                                                                .Replace("TEXTE ", "\n")
                                                                .Replace("ESPACE ", "&nbsp;");
                                }
                                else
                                {
                                    field.Value = dutchField.Value;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // ONLY TRANSLATE IF THERE IS DUTCH CONTENT
                    if (dutchFields.Any())
                    {
                        foreach (var field in dutchFields)
                        {
                            var frenchField = new FieldDto()
                            {
                                Id = -1,
                                TemplateFieldId = field.TemplateFieldId,
                            };

                            if (field.Value != null 
                                && field.DataType == 0 
                                && field.TemplateFieldId != 79)
                            {
                                var toTranslate = field.Value.Replace("\n", "BISOUS");
                                var translation = await DeepleTranslate(client, field.Value);
                                frenchField.Value = translation.Replace("&amp;", "&").Replace("BISOUS", "\n");
                            }
                            else
                            {
                                frenchField.Value = field.Value;
                            }

                            frenchObject.Fields.Add(frenchField);
                        }
                    }
                }

                // ADD VERSION, CHANNEL & LANGUAGE
                frenchObject.Version = 1;
                frenchObject.ChannelId = 3;
                frenchObject.Language = French;
                frenchObject.IsContentItemChannelActive = true;
            }

            // PUT CONTENT
            response = await PutContent(client, frenchObject);
        }

        // LOG ON CONSOLE
        if (response.IsSuccessStatusCode)
        {
            this.successes++;
        }
        else
        {
            this.failures++;
            this.stringBuilder.AppendLine($"{id} - Failed");
        }

        Console.Clear();
        Console.WriteLine($"Successes: {this.successes}");
        Console.WriteLine($"Failures:  {this.failures}");
        Console.WriteLine($"{this.stringBuilder}");
    }

    private async Task<HttpResponseMessage> PutContent(HttpClient client, ContentDto content)
    {
        string apiUrl = $"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item";
        var rawPayload = JsonConvert.SerializeObject(content);
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        var response = await client.PutAsync(apiUrl, payload);

        return response;
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

        data = data.TrimStart('"').TrimEnd('"');

        return data;
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

    private async Task<HttpResponseMessage> GetFrenchOverheadObject(HttpClient client, int id)
    {
        return await client.GetAsync($"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/{id}/Web/fr-BE/1");
    }

    private async Task<List<FieldDto>> GetDutchContentData(HttpClient client, int id)
    {
        string postApiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/inheritance";
        var rawPayload = $"{{contentItemId: {id}, channel: \"Internal\", languageCode: \"nl-BE\"}}";
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(postApiUrl, payload);

        if (response.IsSuccessStatusCode)
        {
            var content = await ReadContent(response);
            return JsonConvert.DeserializeObject<List<FieldDto>>(content);
        }

        return new List<FieldDto>();
    }

    private async Task<string> ReadContent(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }
}