using Feature_Translator;
using Newtonsoft.Json;
using System.Text;

class TranslatorScript
{
    private int successes = 0;
    private int failures = 0;
    private StringBuilder stringBuilder = new StringBuilder();
    const string apiUrl = "https://tide-travelworld8527-staging.azurewebsites.net/api/";
    const string NL = "nl-BE";
    const string FR = "fr-BE";

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
                ShowConsoleFeedback("Login", response, null);
                return;
            }

            // Process each level of children recursively
            await ProcessLevel(client, null);
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

    private async Task ProcessLevel(HttpClient client, int? parentId)
    {
        var response = await GetChildren(client, parentId);

        if (!response.IsSuccessStatusCode)
        {
            ShowConsoleFeedback("GetChildren", response, parentId);
            return;
        }

        var content = await ReadContent(response);
        var children = JsonConvert.DeserializeObject<List<ContentId>>(content);

        foreach (var child in children)
        {
            if (int.TryParse(child.Id, out var childId))
            {
                // Process current child
                await ProcessContent(client, childId);

                // Recursively process next level
                await ProcessLevel(client, childId);
            }
        }
    }

    private async Task<HttpResponseMessage> ProcessContent(HttpClient client, int id)
    {
        var response = await GetFrenchContent(client, id);
        if (response.IsSuccessStatusCode)
        {
            var content = await ReadContent(response);
            var frenchContent = JsonConvert.DeserializeObject<Content>(content);

            if (frenchContent.Version == 0)
            {
                response = await GetFieldsContent(client, id);

                if (response.IsSuccessStatusCode)
                {
                    content = await ReadContent(response);
                    var fields = JsonConvert.DeserializeObject<List<Field>>(content);
                    if (fields != null && fields.Any())
                    {
                        frenchContent.Fields = new List<Field>();

                        foreach (var field in fields)
                        {
                            var frenchField = new Field()
                            {
                                Id = -1,
                                TemplateFieldId = field.TemplateFieldId,
                            };

                            if (field.DataType == 0)
                            {
                                frenchField.Value = await DeepleTranslate(client, field.Value);
                            }
                            else
                            {
                                frenchField.Value = field.Value;
                            }

                            frenchContent.Fields.Add(frenchField);
                        }
                    }
                }

                frenchContent.IsContentItemChannelActive = true;
                frenchContent.ChannelId = 3;
                frenchContent.Version = 1;
                response = await PutContent(client, frenchContent);
                ShowConsoleFeedback("PUT", response, id);
            }
        }

        return response;
    }

    private async Task<HttpResponseMessage> GetChildren(HttpClient client, int? parentId)
    {
        if (parentId == null)
        {
            // ROOT
            return await client.GetAsync($"{apiUrl}content-item-tree/children");
        }
        else
        {
            // CHILDREN FROM PARENT ID
            return await client.GetAsync($"{apiUrl}content-item-tree/children/{parentId}");
        }
    }

    private async Task<HttpResponseMessage> GetFrenchContent(HttpClient client, int id)
    {
        return await client.GetAsync($"{apiUrl}content-item/{id}/Web/fr-BE/1");
    }

    private async Task<HttpResponseMessage> GetFieldsContent(HttpClient client, int id)
    {
        string postApiUrl = $"{apiUrl}content-item/inheritance";
        var rawPayload = $"{{contentItemId: {id}, channel: \"Internal\", languageCode: \"nl-BE\"}}";
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        return await client.PostAsync(postApiUrl, payload);
    }

    private async Task<string> ReadContent(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    [Obsolete]
    private async Task<HttpResponseMessage> GetDutchContent(HttpClient client, int id)
    {
        return await client.GetAsync($"{apiUrl}content-item/{id}/Web/nl-BE/1");
    }

    private async Task<string> DeepleTranslate(HttpClient client, string data)
    {
        var deepleApiUrl = $"{apiUrl}deepl";
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

    [Obsolete]
    private async Task<int?> GetContentItemChannelId(HttpClient client, int id)
    {
        var response = await client.GetAsync($"{apiUrl}content-item/{id}/Web/fr-BE/1");
        var overheadObject = await ReadContent(response);
        var finalObject = JsonConvert.DeserializeObject<Content>(overheadObject);
        return finalObject.ContentItemChannelId;
    }

    private async Task<HttpResponseMessage> PutContent(HttpClient client, Content content)
    {
        string url = $"{apiUrl}content-item";
        var rawPayload = JsonConvert.SerializeObject(content);
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        var response = await client.PutAsync(url, payload);

        return response;
    }

    private void ShowConsoleFeedback(string action, HttpResponseMessage response, int? id)
    {
        if (response.IsSuccessStatusCode)
        {
            this.successes++;
            this.stringBuilder.AppendLine($"{id} - {action} SUCCESS \n");
        }
        else
        {
            this.failures++;
        }

        Console.Clear();
        Console.WriteLine($"Successes: {this.successes}");
        Console.WriteLine($"Failures:  {this.failures}");
        Console.WriteLine($"{this.stringBuilder}");
    }
}