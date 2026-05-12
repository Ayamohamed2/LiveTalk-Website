using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Villa_API_Project.Controllers.V2
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("2.0")]
    public class TestExceptionController : ControllerBase
    {

        [HttpGet]
        public IActionResult Error()
        {

            throw new BadImageFormatException();
      
        }
    }
}
