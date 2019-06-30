using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Services.AppAuthentication;
using webapp.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace webapp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;
        public HomeController(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _configuration = configuration;
            _clientFactory = clientFactory;
        }
        public IActionResult Index()
        {
            

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(HomeViewModel model) 
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string apiToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com");

            var subscriptionId = _configuration["Subscription:Id"];
            var domain = _configuration["Subscription:Domain"];
            var roleId = _configuration["Subscription:RoleId"];

            var oid =  User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var name =  User.FindFirst("name").Value;

            var resourceGroupName = $"Bootcamp_{model.ResourceGroupName}";
            var rgUrl = $"https://management.azure.com/subscriptions/{subscriptionId}" + 
                $"/resourcegroups/{resourceGroupName}?api-version=2019-05-01";

            var client = _clientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put, rgUrl);
            request.Headers.Add("Authorization", $"Bearer {apiToken}");
            request.Content = new System.Net.Http.StringContent("{location:'eastus'}", Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);

            var guid = Guid.NewGuid().ToString();
            var rbacUrl = "https://management.azure.com/subscriptions/" + 
                $"{subscriptionId}/resourceGroups/{resourceGroupName}/" + 
                $"providers/Microsoft.Authorization/roleAssignments/{guid}" + 
                "?api-version=2015-07-01";

            client = _clientFactory.CreateClient();
            request = new HttpRequestMessage(HttpMethod.Put, rbacUrl);
            request.Headers.Add("Authorization", $"Bearer {apiToken}");

            var json = $@"{{""properties"": {{" +
                $@"""roleDefinitionId"": ""/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleId}""," +
                $@"""principalId"": ""{oid}""}} }}";
            request.Content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

            response = await client.SendAsync(request);

            ViewBag.rgUrl = $"https://portal.azure.com/#@{domain}/resource/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/overview";

            return View("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
