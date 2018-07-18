using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagBot.Helpers;
using TagBot.Services;

namespace TagBot.Handlers
{
    public class HasteBinHandler
    {
        private readonly RequestsService _request;
        private readonly Regex _pattern = new Regex(@"https://pastebin.com/(?<pastecode>\w{8})");

        public HasteBinHandler(RequestsService request)
        {
            _request = request;
        }

        public async Task<string> CreateHasteOrGist(string code)
        {
            var createHaste = _request.CreateHaste(code);
            var createGist = _request.CreateGist(code);
            var result = await createHaste.TimeoutAndFallback(TimeSpan.FromSeconds(15), createGist);
            return result;
        }

        public async Task<string> GetCode(string content)
        {
            var match = _pattern.Match(content);
            if (match.Success)
            {
                return await GetPasteBinData(match.Groups["pastecode"].ToString());
            }

            var codes = content.GetCodes();

            if (!codes.Any()) return null;
            var joint = string.Join("\n", codes);
            return joint;
        }

        private async Task<string> GetPasteBinData(string pasteBin)
        {
            var content = await _request.GetPaste(pasteBin);
            return content.Contains("<!DOCTYPE HTML>") ? null : content;
        }
    }
}
