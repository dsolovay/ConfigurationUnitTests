using System.Collections.Generic;
using System.Xml;
using Sitecore.Reflection;

namespace ConfigurationUnitTests
{
    public class TestTypeInitializable: TestType, IInitializable 

    {
        public void Initialize(XmlNode configNode)
        {
            
        }

        public bool AssignProperties { get; } = true;
    }
}