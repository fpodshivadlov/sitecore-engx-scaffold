namespace <%= solutionX %>.Plugin.<%= pluginNameX %>
{
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.OData.Builder;

    using Sitecore.Commerce.Core;
    using Sitecore.Commerce.Core.Commands;
    using Sitecore.Framework.Conditions;
    using Sitecore.Framework.Pipelines;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities;

    [PipelineDisplayName("SamplePluginConfigureServiceApiBlock")]
    public class ConfigureServiceApiBlock : PipelineBlock<ODataConventionModelBuilder, ODataConventionModelBuilder, CommercePipelineExecutionContext>
    {
        public override Task<ODataConventionModelBuilder> Run(ODataConventionModelBuilder modelBuilder, CommercePipelineExecutionContext context)
        {
            Condition.Requires(modelBuilder).IsNotNull($"{this.Name}: The argument cannot be null.");

            // Add the entities
            modelBuilder.AddEntityType(typeof(SampleEntity));

            // Add the entity sets
            modelBuilder.EntitySet<SampleEntity>("Sample");

            // Add complex types

            // Add unbound functions

            // Add unbound actions
            var configuration = modelBuilder.Action("SampleCommand");
            configuration.Parameter<string>("Id");
            configuration.ReturnsFromEntitySet<CommerceCommand>("Commands");

            return Task.FromResult(modelBuilder);
        }
    }
}
