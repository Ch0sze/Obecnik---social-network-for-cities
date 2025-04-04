using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;

namespace CoatOfArmsDownloader.Services
{
    public class CoatOfArmsScraper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<byte[]?> GetCommunityCoatOfArms(string communityName)
        {
            var formattedName = RemoveDiacritics(communityName.ToLower());
            var url = $"https://rekos.psp.cz/vyhledani-symbolu?typ=0&obec={formattedName}&poverena_obec=&popis=&kraj=0&okres=0&od=&do=&hledat=";

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(responseContent);

                var rows = htmlDoc.DocumentNode.SelectNodes("//*[@id='main-content']/table/tbody/tr");

                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var nameNode = row.SelectSingleNode("./td[1]/a");
                        var name = nameNode?.InnerText.Trim().ToLower();
                        var link = nameNode?.GetAttributeValue("href", "");

                        var formattedNameWithoutDiacritics = RemoveDiacritics(formattedName);
                        if (name != null)
                        {
                            var nameWithoutDiacritics = RemoveDiacritics(name);

                            if (nameWithoutDiacritics != null && nameWithoutDiacritics == formattedNameWithoutDiacritics)
                            {
                                if (!string.IsNullOrEmpty(link))
                                {
                                    if (!link.StartsWith("http"))
                                    {
                                        link = "https://rekos.psp.cz" + link;
                                    }

                                    var requestDetail = new HttpRequestMessage(HttpMethod.Get, link);
                                    requestDetail.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                                    requestDetail.Headers.Add("Referer", "https://rekos.psp.cz/");
                                    requestDetail.Headers.Add("Accept-Language", "en-US,en;q=0.9");

                                    var detailResponse = await httpClient.SendAsync(requestDetail);
                                    detailResponse.EnsureSuccessStatusCode();

                                    var detailContent = await detailResponse.Content.ReadAsStringAsync();
                                    var detailDoc = new HtmlDocument();
                                    detailDoc.LoadHtml(detailContent);

                                    var imageNode = detailDoc.DocumentNode.SelectSingleNode("//*[@id='main-content']//img");

                                    if (imageNode != null)
                                    {
                                        var imageUrl = imageNode.GetAttributeValue("src", "");
                                        if (!imageUrl.StartsWith("http"))
                                        {
                                            imageUrl = "https://rekos.psp.cz" + imageUrl;
                                        }

                                        var imageRequest = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                                        imageRequest.Headers.Add("User-Agent", "Mozilla/5.0");
                                        var imageResponse = await httpClient.SendAsync(imageRequest);

                                        if (imageResponse.IsSuccessStatusCode)
                                        {
                                            return await imageResponse.Content.ReadAsByteArrayAsync();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return null;
        }

        private static string RemoveDiacritics(string input)
        {
            input = input.Replace(" - ", "-");
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var character in normalizedString)
            {
                var unicodeCategory = Char.GetUnicodeCategory(character);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(character);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
