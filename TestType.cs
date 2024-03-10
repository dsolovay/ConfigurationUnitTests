using System.Collections.Generic;
using System.Xml;

namespace ConfigurationUnitTests
{
    public class TestType
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public bool BoolTest { get; set; }
        public Dictionary<string, string> StringDictionary { get; set; } = new Dictionary<string, string>();
        public TestType ChildType { get; set; }
        public List<string> StringList { get; set; }  = new List<string>();
        public List<object> ObjectList { get; set; } = new List<object>();

        public void AddObject(object obj) => ObjectList.Add(obj);

        public void ReadNode(XmlNode node)
        {
            Name = node.Attributes.GetNamedItem("specialParameter").Value;
        }

        public void AssignSetValue()
        {
            Name = "set value";
        }
    }
}