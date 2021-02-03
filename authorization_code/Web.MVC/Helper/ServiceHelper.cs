using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace Web.MVC.Helper
{
    public class ServiceHelper : IServiceHelper
    {
        private readonly IConfiguration _configuration;

        public ServiceHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetProduct()
        {
            var consulClient = new ConsulClient(c =>
            {
                //consul地址
                c.Address = new Uri(_configuration["ConsulSetting:ConsulAddress"]);
            });

            var services = consulClient.Health.Service("OrderService", null, true, null).Result.Response;//健康的服务

            var serviceUrls = services.Select(p => $"http://{p.Service.Address + ":" + p.Service.Port}").ToArray();//订单服务地址列表

            if (!serviceUrls.Any())
            {
                return await Task.FromResult("【订单服务】服务列表为空");
            }

            //每次随机访问一个服务实例
            var client = new RestClient(serviceUrls[new Random().Next(0, serviceUrls.Length)]);
            var request = new RestRequest("/order", Method.GET);

            var response = await client.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetOrder()
        {
            var consulClient = new ConsulClient(c =>
            {
                //consul地址
                c.Address = new Uri(_configuration["ConsulSetting:ConsulAddress"]);
            });

            var services = consulClient.Health.Service("ProductService", null, true, null).Result.Response;//健康的服务

            var serviceUrls = services.Select(p => $"http://{p.Service.Address + ":" + p.Service.Port}").ToArray();//产品服务地址列表

            if (!serviceUrls.Any())
            {
                return await Task.FromResult("【产品服务】服务列表为空");
            }

            //每次随机访问一个服务实例
            var client = new RestClient(serviceUrls[new Random().Next(0, serviceUrls.Length)]);
            var request = new RestRequest("/product", Method.GET);

            var response = await client.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetProduct(string accessToken)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetOrder(string accessToken)
        {
            throw new NotImplementedException();
        }
    }
}
