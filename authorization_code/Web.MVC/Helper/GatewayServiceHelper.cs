using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;

namespace Web.MVC.Helper
{
    /// <summary>
    /// 通过gateway调用服务
    /// </summary>
    public class GatewayServiceHelper : IServiceHelper
    {
        public async Task<string> GetOrder(string accessToken)
        {
            var Client = new RestClient("http://localhost:9070");
            var request = new RestRequest("/order", Method.GET);
            request.AddHeader("Authorization", "Bearer " + accessToken);

            var response = await Client.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetProduct(string accessToken)
        {
            var Client = new RestClient("http://localhost:9070");
            var request = new RestRequest("/product", Method.GET);
            request.AddHeader("Authorization", "Bearer " + accessToken);

            var response = await Client.ExecuteAsync(request);
            return response.Content;
        }

        public void GetServices()
        {
            throw new NotImplementedException();
        }
    }
}
