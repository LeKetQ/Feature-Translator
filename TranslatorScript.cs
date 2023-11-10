using Feature_Translator;
using Newtonsoft.Json;
using System.Text;

class TranslatorScript
{
    int successes = 0;
    int failures = 0;
    StringBuilder stringBuilder = new StringBuilder();

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
                    foreach (var finalContentField in finalContent.Fields)
                    {
                        if (finalContentField.TemplateFieldId == field.TemplateFieldId && string.IsNullOrEmpty(finalContentField.Value))
                        {
                            finalContentField.Value = await DeepleTranslate(client, field.Value);
                        }
                    }
                }

                if (finalContent.ContentItemChannelId == -1)
                {
                    finalContent.ContentItemChannelId = await GetContentItemId(client, id);
                }
                finalContent.IsContentItemChannelActive = true;
                finalContent.Language = "fr-BE";

                response = await PutContent(client, finalContent);

                if (response.IsSuccessStatusCode)
                {
                    this.successes++;
                }
                else
                {
                    this.failures++;

                    var failedId = $"{finalContent.Id}";
                    this.stringBuilder.AppendLine($"{failedId.PadRight(5)} - Failed");
                }
                Console.Clear();
                Console.WriteLine($"Successes: {this.successes}");
                Console.WriteLine($"Failures:  {this.failures}");
                Console.WriteLine($"{this.stringBuilder}");
            }
        }

        return response;
    }

    private async Task<HttpResponseMessage> GetOverheadObject(HttpClient client, int id)
    {
        return await client.GetAsync($"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/{id}/Internal/fr-BE/1");
    }

    private async Task<int?> GetContentItemId(HttpClient client, int id)
    {
        var response = await client.GetAsync($"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/{id}/Internal/nl-BE/1");
        var dutchOverheadObject = await ReadContent(response);
        var finalObject = JsonConvert.DeserializeObject<Content>(dutchOverheadObject);

        if (finalObject.ContentItemChannelId == -1)
        {
            response = await client.GetAsync($"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/{id}/Web/nl-BE/1");
            dutchOverheadObject = await ReadContent(response);
            finalObject = JsonConvert.DeserializeObject<Content>(dutchOverheadObject);
        }

        return finalObject.ContentItemChannelId;
    }

    private async Task<HttpResponseMessage> PutContent(HttpClient client, Content content)
    {
        string apiUrl = $"https://tide-travelworld8527-staging.azurewebsites.net/api/content-item";

        // PUT TO WEB
        content.ChannelId = 3; // CHANNEL 3 = WEB
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

        data.TrimStart('"').TrimEnd('"');

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

    private async Task<HttpResponseMessage> GetContentData(HttpClient client, int id)
    {
        string postApiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/inheritance";
        var rawPayload = $"{{contentItemId: {id}, channel: \"Web\", languageCode: \"nl-BE\"}}";
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        return await client.PostAsync(postApiUrl, payload);
    }

    private async Task<string> ReadContent(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }
}