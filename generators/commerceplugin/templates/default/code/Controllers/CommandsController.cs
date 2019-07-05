namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Controllers
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Http.OData;

    using Microsoft.AspNetCore.Mvc;

    using Sitecore.Commerce.Core;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Commands;

    public class CommandsController : CommerceController
    {
        public CommandsController(IServiceProvider serviceProvider, CommerceEnvironment globalEnvironment)
            : base(serviceProvider, globalEnvironment)
        {
        }

        [HttpPut]
        [Route("SampleCommand()")]
        public async Task<IActionResult> SampleCommand([FromBody] ODataActionParameters value)
        {
            var id = value["Id"].ToString();
            var command = this.Command<SampleCommand>();
            var result = await command.Process(this.CurrentContext, id);

            return new ObjectResult(command);
        }
    }
}

