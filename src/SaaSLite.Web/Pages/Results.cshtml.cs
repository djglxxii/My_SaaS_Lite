using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Threading.Tasks;
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

        public IList<Result> Results { get; private set; } = new List<Result>();

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("CloudApi");
            try
            {
                var res = await client.GetFromJsonAsync<Result[]>("/api/results");
                if (res != null)
                {
                    Results = res.ToList();
                }
            }
            catch
            {
                // In demo mode swallow exceptions; an empty list will be shown.
            }
        }
    }
}