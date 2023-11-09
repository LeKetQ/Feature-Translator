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
                            var result = await GetAlterAndPutContent(client, firstLevelId);

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
                                        result = await GetAlterAndPutContent(client, secondLevelId);

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
                                                    result = await GetAlterAndPutContent(client, secondLevelId);
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

    private async Task<HttpResponseMessage> GetAlterAndPutContent(HttpClient client, int id)
    {
        var response = await GetContentData(client, id);

        if (response.IsSuccessStatusCode)
        {
            var content = await ReadContent(response);
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
            return response;

            // TODO: Put Data
            return await PutContentData(client, contentData);
        }
        else
        {
            return response;
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
        var rawPayload = $"{{contentItemId: {id}, channel: \"Internal\", languageCode: \"fr-BE\"}}";
        var payload = new StringContent(rawPayload, Encoding.UTF8, "application/json");
        return await client.PostAsync(postApiUrl, payload);
    }

    private async Task<string> ReadContent(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }
}

#region - GET OVERVIEW OBJECT
// GET - FR Object without fields 
// https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/86/Internal/fr-BE/1

//{
//    "id": 86,
//    "parentId": 85,
//    "catalogueId": null,
//    "name": "Mauritius",
//    "isActive": true,
//    "locationId": null,
//    "productType": null,
//    "contentItemType": 0,
//    "version": 1,
//    "language": "fr-BE",
//    "channelId": 1,
//    "contentItemChannelId": 1825,
//    "isContentItemChannelActive": false, => SET TO TRUE BEFORE PUT
//    "companyId": 10,
//    "position": 0,
//    "templateId": 31,
//    "templateName": null,
//    "templateFolderId": null,
//    "children": null,
//    "fields": [    ],
//    "publishedBy": null,
//    "publishedDate": null,
//    "previewUrl": null,
//    "styleSheets": null
//}
#endregion

#region - GET FIELDS
// POST 
// https://tide-travelworld8527-staging.azurewebsites.net/api/content-item/inheritance
// PAYLOAD
//{
//    "contentItemId": 86,
//    "channel": "Internal",
//    "languageCode": "fr-BE"
//}
// => Get all FR object fields
#endregion

#region - RESPONSE
//[
//    {
//        "dataType": 0,
//        "templateFieldId": 122,
//        "value": "Mauritius",
//        "channel": "Internal",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 5,
//        "templateFieldId": 123,
//        "value": "{\"key\":\"142\",\"value\":\"Mauritius\",\"localizedValue\":null}",
//        "channel": "Internal",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 4,
//        "templateFieldId": 325,
//        "value": "{\"url\":\"https://tide-media-staging.azurewebsites.net/media/10/Headers/hero_homepage-1.jpg?crop=0,0,0,0&cropmode=percentage\",\"crop\":{\"left\":0,\"top\":0,\"right\":0,\"bottom\":0},\"id\":2,\"name\":\"hero_homepage.jpg\",\"title\":null,\"altText\":null}",
//        "channel": "Internal",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 4,
//        "templateFieldId": 242,
//        "value": "{\"url\":null,\"crop\":null,\"id\":null,\"aspectRatio\":1}",
//        "channel": "Web",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 7,
//        "templateFieldId": 252,
//        "value": "mauritius",
//        "channel": "Internal",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 0,
//        "templateFieldId": 302,
//        "value": "De ultieme strandbestemming in de Indische Oceaan",
//        "channel": "Web",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 0,
//        "templateFieldId": 124,
//        "value": "<p>Mauritius vormt samen met La Réunion en Rodrigues een eilandengroep, de Mascareignes archipel. Het is een klein vulkanisch en afwisselend eiland met unieke landschappen, zeldzame vegetatie en diersoorten, suikerriet- en theeplantages. de prachtige stranden en de lagunes van Mauritius behoren tot de mooiste ter wereld. De zee rond het eiland is een tuin van prachtige koraal. Een exclusieve bestemming met de meest luxueuze hotels op onze planeet, exclusieve winkels, watersportmogelijkheden en gastronomie van topnveau. Verwennerij in overvloed!</p>",
//        "channel": "Web",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 0,
//        "templateFieldId": 125,
//        "value": "<p>2 uur voorsprong tijden de zomer en 3 uur tijdens de winter</p><p>(GMT+4)</p>",
//        "channel": "Web",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 0,
//        "templateFieldId": 126,
//        "value": "<p>De lokale munt is de Roepie die verdeeld is in 100 cents.&nbsp;</p>",
//        "channel": "Web",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 0,
//        "templateFieldId": 127,
//        "value": "<p><em>​</em><span>Internationaal paspoort geldig tot minstens 6 maand na retour verplicht (zowel voor volwassenen als voor kinderen jonger dan 12 jaar).</span></p>",
//        "channel": "Internal",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 0,
//        "templateFieldId": 128,
//        "value": "<p>Mauritius heeft het hele jaar door een droogklimaat. In de zomertijd (november-mei) is de temperatuur 25°C à 30°C. In de wintertijd (juni-september) ligt de temperatuur 5 graden lager en waait er aan de noord en oostkust dikwijls een sterke wind. De zon schijnt er gemiddeld 7 uur per dag, maar gaat vroeg onder (18-19 uur)</p>",
//        "channel": "Web",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 0,
//        "templateFieldId": 129,
//        "value": "<p>Engels is de officiële taal. Frans en Creools worden vlot gesproken.</p>",
//        "channel": "Web",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 11,
//        "templateFieldId": 130,
//        "value": "{\"aspectRatio\":1.3333333333333333,\"images\":[{\"id\":13170,\"crop\":{\"left\":0,\"top\":0,\"right\":28,\"bottom\":0},\"url\":\"https://tide-media-staging.azurewebsites.net/media/10/Headers/Gozo-header-1.jpg?crop=0,0,0.28,0&cropmode=percentage\",\"aspectRatio\":1.3333333333333333,\"title\":\"Gozo\",\"altText\":\"Gozo\",\"name\":\"Gozo-header.jpg\",\"order\":0},{\"id\":1275,\"crop\":{\"left\":64,\"top\":76,\"right\":23,\"bottom\":9},\"url\":\"https://tide-media-staging.azurewebsites.net/media/10/Products/Indische Oceaan /Mauritius/Hotels/Constance Prince Maurice/my-constance-moment-constance-prince-maurice-feel-like-a-team-member-cooking-class-02_hd-1.jpg?crop=0.64,0.76,0.23,0.09&cropmode=percentage\",\"aspectRatio\":1.3333333333333333,\"title\":null,\"altText\":null,\"name\":\"my-constance-moment-constance-prince-maurice-feel-like-a-team-member-cooking-class-02_hd.jpg\",\"order\":1}]}",
//        "channel": "Internal",
//        "language": "nl-BE"
//    },
//    {
//    "dataType": 4,
//        "templateFieldId": 389,
//        "value": "{\"url\":\"https://tide-media-staging.azurewebsites.net/media/10/Headers/malediven_milaidhoo-1.jpg?crop=0.13,0,0.34,0&cropmode=percentage\",\"crop\":{\"left\":13,\"top\":0,\"right\":34,\"bottom\":0},\"id\":6558,\"aspectRatio\":0.7071135624381276,\"name\":\"malediven_milaidhoo.jpg\",\"title\":null,\"altText\":null}",
//        "channel": "Internal",
//        "language": "nl-BE"
//    }
//]
#endregion