using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MyApi.Controllers
{
    [Authorize(Policy = "TestPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {

    }
}
