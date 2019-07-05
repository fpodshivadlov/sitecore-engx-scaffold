namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Pipelines.Arguments
{
    using Sitecore.Commerce.Core;
    using Sitecore.Framework.Conditions;

    public class SampleArgument : PipelineArgument
    {
        public SampleArgument(object parameter)
        {
            Condition.Requires(parameter).IsNotNull("The parameter can not be null");

            this.Parameter = parameter;
        }

        public object Parameter { get; set; }
    }
}
