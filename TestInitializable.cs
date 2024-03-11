using System.Xml;
using Sitecore.Reflection;

namespace ConfigurationUnitTests
{
    public class TestInitializable : IInitializable
    {
        public void Initialize(XmlNode configNode)
        {
            
        }
        public string Prop { get; set; }
        public bool AssignProperties { get; } = false;
    }
}