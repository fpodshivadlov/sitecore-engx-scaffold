namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Controllers
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;

    using Sitecore.Commerce.Core;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Commands;

    [Microsoft.AspNetCore.OData.EnableQuery]
    [Route("api/Sample")]
    public class SampleController : CommerceController
    {
        public SampleController(IServiceProvider serviceProvider, CommerceEnvironment globalEnvironment) : base(serviceProvider, globalEnvironment)
        {
        }

        [HttpGet]
        [Route("(Id={id})")]
        [Microsoft.AspNetCore.OData.EnableQuery]
        public async Task<IActionResult> Get(string id)
        {
            if (!this.ModelState.IsValid)
            {
                return new BadRequestObjectResult(this.ModelState);
            }

            var process = this.Command<SampleCommand>()?.Process(this.CurrentContext, id);
            if (process == null)
            {
                return new BadRequestObjectResult(this.ModelState);
            }

            var result = await process;
            if (result == null)
            {
                return this.NotFound();
            }

            return new ObjectResult(result);
        }
    }
}
