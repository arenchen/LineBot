using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LineBot.Services
{
    public interface ILineService
    {
        Task Post(JObject args);
    }
}
