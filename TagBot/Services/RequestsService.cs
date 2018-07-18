using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TagBot.Services
{
    public class RequestsService
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly GitHubClient _gitClient;

        public RequestsService(GitHubClient gitClient)
        {
            _gitClient = gitClient;
        }

        public async Task<string> CreateHaste(string data)
        {
            try
            {
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using (var request = await _client.PostAsync("https://hastebin.com/documents", new StringContent(data)))
                {
                    var jObj = JObject.Parse(await request.Content.ReadAsStringAsync());
                    return $"I created a hastebin of your code ^_^ https://www.hastebin.com/{jObj["key"]}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<string> CreateGist(string content)
        {
            try
            {
                var newGist = new NewGist
                {
                    Description = "Code-snippet",
                    Public = false
                };
                newGist.Files.Add(DateTime.UtcNow.ToShortTimeString(), content);
                var created = await _gitClient.Gist.Create(newGist);
                return $"Hastebin didn't respond... So I created a gist ^_^ {created.HtmlUrl}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public async Task<string> GetPaste(string code)
        {
            try
            {
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using (var request = await _client.GetAsync($"https://pastebin.com/raw/{code}"))
                {
                    return await request.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}
