using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Product.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var result = $"【产品服务】{DateTime.Now:yyyy-MM-dd HH:mm:ss}——" +
                                $"{Request.HttpContext.Connection.LocalIpAddress}:{Request.HttpContext.Connection.LocalPort}";
            return Ok(result);
        }
    }
}
