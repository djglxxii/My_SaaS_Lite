using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SaaSLite.Contracts;

namespace SaaSLite.Web.Pages
{
    public class ResultsModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public ResultsModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public List<ResultSummaryDto> Results { get; private set; } = new List<ResultSummaryDto>();

        [BindProperty(SupportsGet = true)]
        public string? DeviceId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromUtc { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToUtc { get; set; }

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("CloudApi");

            var qs = new List<string>();
            if (FromUtc.HasValue) qs.Add($"fromUtc={Uri.EscapeDataString(FromUtc.Value.ToString("O"))}");
            if (ToUtc.HasValue) qs.Add($"toUtc={Uri.EscapeDataString(ToUtc.Value.ToString("O"))}");
            if (!string.IsNullOrWhiteSpace(DeviceId)) qs.Add($"deviceId={Uri.EscapeDataString(DeviceId.Trim())}");

            var url = "api/results" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

            var data = await client.GetFromJsonAsync<List<ResultSummaryDto>>(url);
            Results = data ?? new List<ResultSummaryDto>();
        }
    }
}
