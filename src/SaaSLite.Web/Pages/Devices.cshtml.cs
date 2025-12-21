using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SaaSLite.Contracts;

namespace SaaSLite.Web.Pages
{
    public class DevicesModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public DevicesModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IList<Device> Devices { get; private set; } = new List<Device>();

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("CloudApi");
            try
            {
                var res = await client.GetFromJsonAsync<Device[]>("/api/devices");
                if (res != null)
                {
                    Devices = res.ToList();
                }
            }
            catch
            {
                // For demo swallow exceptions; an empty list will be shown.
            }
        }
    }
}