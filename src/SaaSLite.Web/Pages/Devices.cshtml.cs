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

        public List<DeviceDto> Devices { get; private set; } = new List<DeviceDto>();

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("CloudApi");
            var data = await client.GetFromJsonAsync<List<DeviceDto>>("api/devices");
            Devices = data ?? new List<DeviceDto>();
        }
    }
}