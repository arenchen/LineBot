using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LineBot.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LineBot.Services
{
    public class LineService : ILineService
    {

        private readonly LineBotContext _context;
        private readonly AppSettings _settings;
        private readonly ILogger<LineService> _logger;

        public LineService(LineBotContext context, AppSettings settings, ILogger<LineService> logger)
        {
            _context = context;
            _settings = settings;
            _logger = logger;
        }

        public async Task Post(JObject args)
        {
            var events = args["events"] as JArray;

            var learnCommand = _settings.LearnCommand + " ";
            var forgetCommand = _settings.ForgetCommand + " ";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                foreach (var item in events)
                {
                    if (string.IsNullOrWhiteSpace(item["type"].Value<string>()) || !item["source"].HasValues)
                        continue;

                    var replyToken = item["replyToken"].Value<string>();
                    var result = string.Empty;

                    switch (item["type"].Value<string>())
                    {
                        case "message":
                            var message = item["message"];

                            switch (message["type"].Value<string>())
                            {
                                case "text":
                                    var text = message["text"].Value<string>().Trim();

                                    if (text.StartsWith(learnCommand))
                                    {
                                        text = text.Substring(learnCommand.Length).Trim();

                                        var kv = ParserKeyValue(text);

                                        if (kv == null) continue;

                                        var ent = _context.KeyDictionaries.FirstOrDefault(t => kv.Key.ToLower() == t.Key);

                                        if (ent == null)
                                        {
                                            _context.KeyDictionaries.Add(kv);
                                        }
                                        else
                                        {
                                            ent.Value = kv.Value;

                                            _context.Update(ent);
                                        }

                                        _context.SaveChanges();

                                        result = await client.PostAsync(_settings.MessageApiUri + "/reply",
                                                                        new StringContent(JsonConvert.SerializeObject(new
                                                                        {
                                                                            replyToken = replyToken,
                                                                            messages = new[] { new { type = "text", text = "我知道了！" } }
                                                                        }), Encoding.UTF8, "application/json"))
                                                             .Result.Content.ReadAsStringAsync();
                                    }
                                    else if (text.StartsWith(forgetCommand))
                                    {
                                        text = text.Substring(forgetCommand.Length).Trim();

                                        if (string.IsNullOrWhiteSpace(text)) break;

                                        var ent = _context.KeyDictionaries.FirstOrDefault(t => text.Trim().ToLower() == t.Key.ToLower());

                                        if (ent == null) break;

                                        _context.KeyDictionaries.Remove(ent);

                                        _context.SaveChanges();

                                        result = await client.PostAsync(_settings.MessageApiUri + "/reply",
                                                                        new StringContent(JsonConvert.SerializeObject(new
                                                                        {
                                                                            replyToken = replyToken,
                                                                            messages = new[] { new { type = "text", text = "我忘記了！" } }
                                                                        }), Encoding.UTF8, "application/json"))
                                                             .Result.Content.ReadAsStringAsync();
                                    }
                                    else
                                    {
                                        var lstValue = _context.KeyDictionaries.Where(t => text.StartsWith(t.Key, StringComparison.CurrentCultureIgnoreCase)).ToArray().OrderByDescending(t => t.Key);

                                        var val = (lstValue.FirstOrDefault(t => text.ToLower() == t.Key.ToLower()) ?? lstValue.FirstOrDefault(t => text.StartsWith(t.Key, StringComparison.CurrentCultureIgnoreCase)))?.Value;

                                        if (val == null) break;

                                        result = await client.PostAsync(_settings.MessageApiUri + "/reply",
                                                                        new StringContent(JsonConvert.SerializeObject(new
                                                                        {
                                                                            replyToken = replyToken,
                                                                            messages = new[] { new { type = "text", text = val } }
                                                                        }), Encoding.UTF8, "application/json"))
                                                             .Result.Content.ReadAsStringAsync();
                                    }

                                    break;
                            }

                            break;
                        default:
                            continue;
                    }
                }
            }
        }

        private static KeyDictionary ParserKeyValue(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;

            var sp = 0;
            var ep = data.IndexOf(":");

            if (ep == -1)
                return null;

            try
            {
                var key = data.Substring(sp, ep).Trim();
                var val = data.Substring(ep + 1).Trim();

                if (key.StartsWith("\""))
                    key = key.Substring(1);
                if (key.EndsWith("\""))
                    key = key.Substring(0, key.Length - 1);

                if (val.StartsWith("\""))
                    val = val.Substring(1);
                if (val.EndsWith("\""))
                    val = val.Substring(0, val.Length - 1);

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val))
                    return null;

                return new KeyDictionary { Key = key.Trim(), Value = val.Trim() };
            }
            catch
            {
                return null;
            }
        }
    }
}
