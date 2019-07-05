namespace <%= solutionX %>.Plugin.<%= pluginNameX %>.Entities
{
    using System;
    using System.Collections.Generic;

    using Microsoft.AspNetCore.OData.Builder;

    using Sitecore.Commerce.Core;

    using <%= solutionX %>.Plugin.<%= pluginNameX %>.Components;

    public class SampleEntity : CommerceEntity
    {
        public SampleEntity()
        {
            this.Components = new List<Component>();
            this.DateCreated = DateTime.UtcNow;
            this.DateUpdated = this.DateCreated;
        }

        public SampleEntity(string id) : this()
        {
            this.Id = id;
        }

        [Contained]
        public IEnumerable<SampleComponent> ChildComponents { get; set; }
    }
}
