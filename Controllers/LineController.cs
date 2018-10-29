using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using LineBot.Services;

namespace LineBot.Controllers
{
    [Produces("application/json")]
    [Route("api/line")]
    public class LineController : Controller
    {
        private readonly ILineService _lineService;

        public LineController(LineBotContext context, IOptions<AppSettings> settings, ILogger<LineService> logger)
        {
            _lineService = new LineService(context, settings.Value, logger);
        }

        [HttpPost]
        public async Task Post([FromBody] JObject args)
        {
            await _lineService.Post(args);
        }
    }
}