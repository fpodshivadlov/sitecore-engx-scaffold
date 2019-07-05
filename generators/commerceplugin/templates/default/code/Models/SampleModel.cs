namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Models
{
    using Sitecore.Commerce.Core;

    public class SampleModel : Model
    {
        public SampleModel()
        {
            this.Id = string.Empty;
        }

        public string Id { get; private set; }
    }
}
