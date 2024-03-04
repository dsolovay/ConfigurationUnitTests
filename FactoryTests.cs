using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Web.UI.WebControls;
using System.Xml;
using FluentAssertions;
using NSubstitute;
using Sitecore.Abstractions;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Reflection;
using Sitecore.Web.UI.WebControls;
using Xunit;

namespace ConfigurationUnitTests
{
    public class FactoryTests
    {
        private IServiceProvider _serviceProvider;
        private BaseComparerFactory _baseFactory;
        private DefaultFactory _sut;

        public FactoryTests()
        {
            _serviceProvider = Substitute.For<IServiceProvider>();
            _baseFactory = Substitute.For<BaseComparerFactory>();
            _sut = new DefaultFactory(_baseFactory, _serviceProvider);
        }

        [Fact]
        public void Instantiate()
        {
            Assert.NotNull(_sut);
        }

        [Fact]
        public void EmptyNodeThrows()
        {
            XmlNode configNode = new XmlDocument();
            Assert.NotNull(configNode);

            Assert.Throws<ArgumentNullException>(() => _sut.CreateObject(configNode, false));

        }

      

        [Fact]
        public void CreateAttr()
        {
            var element = MakeXmlNode("type", "someValue");

            element.Attributes["type"].Value.Should().Be("someValue");
        }

        private static XmlElement MakeXmlNode(string qualifiedName, string attrValue)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("test");
            XmlAttribute attr = doc.CreateAttribute(qualifiedName);
            attr.Value = attrValue;
            element.Attributes.Append(attr);
            return element;
        }

        [Fact]
        public void CreateTestType()
        {
            XmlNode configNode = MakeXmlNode("type", "ConfigurationUnitTests.TestType");

            TestType obj = _sut.CreateObject(configNode, true) as TestType;

            Assert.NotNull(obj);

        }

        [Fact]
        public void CreateTestTypeWithTextXml()
        {
            var docDocumentElement = GetXmlNode("<a type='ConfigurationUnitTests.TestType' />");
            TestType obj = _sut.CreateObject(docDocumentElement, true) as TestType;

            Assert.NotNull(obj);

        }


        [Fact]
        public void AssertFailWorks()
        {
            XmlNode configNode = MakeXmlNode("type", "ConfigurationUnitTests.NotReal");

            Assert.Throws<Exception>(() => _sut.CreateObject(configNode, true) as TestType);

             

        }


        [Fact]
        public void NoAssert()
        {
            XmlNode configNode = MakeXmlNode("type", "ConfigurationUnitTests.NotReal");

            _sut.CreateObject(configNode, false);
        }

        [Fact]
        public void GenericTypeInferred()
        {
            TestType type = _sut.CreateObject<TestType>(new XmlDocument().CreateElement("a"));
            Assert.NotNull(type);
        }

        [Fact]
        public void StringFallback()
        {
            object result = _sut.CreateObject(GetXmlNode("<a>some text</a>"), false);

            Assert.Equal("some text", (string)result);
        }

        [Fact]
        public void CannotDirectlySetParameters()
        {
            XmlNode node = GetXmlNode("<a type='ConfigurationUnitTests.TestType' name='someName' />");
            TestType t = _sut.CreateObject(node, false) as TestType;

            t.Name.Should().BeEmpty();  // Why isn't this null?
        }

        [Fact]
        public void AssignProperties()
        {
            string configNode =  
                @"<a
                    type='ConfigurationUnitTests.TestTypeInitializable'>
                        <name>SomeName</name>
                        <Index>3</Index>
                        <BoolTest>true</BoolTest>
                  </a>";
            XmlNode node = GetXmlNode(configNode);
            TestTypeInitializable t = _sut.CreateObject(node, false) as TestTypeInitializable;

            t.Name.Should().Be("SomeName");
            t.Index.Should().Be(3);
            t.BoolTest.Should().BeTrue();
        }

        [Fact]
        public void AssignPropertiesCaseInsensitive()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestTypeInitializable'>
                        <nAme>SomeName</nAme>
                        <iNdex>3</iNdex>
                        <bOoltest>true</bOoltest>                        
                  </a>";
            XmlNode node = GetXmlNode(configNode);
            TestTypeInitializable t = _sut.CreateObject(node, true) as TestTypeInitializable;

            t.Name.Should().Be("SomeName");
            t.Index.Should().Be(3);
            t.BoolTest.Should().BeTrue();
        }


        [Fact]
        public void AddList()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestTypeInitializable'>
                        <StringList hint='list'>
                          <e>foo</e>
                          <e>bar</e>
                          <e>baz</e>
                        </StringList>
                  </a>";
            XmlNode node = GetXmlNode(configNode);
            TestTypeInitializable t = _sut.CreateObject(node, true) as TestTypeInitializable;

            t.StringList.Count.Should().Be(3);
            t.StringList.Should().ContainInOrder("foo", "bar", "baz");


        }

        [Fact]
        public void AddDictionary()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestTypeInitializable'>
                        <StringDictionary hint='list'>
                          <e key='a'>foo</e>
                          <e key='b'>bar</e>
                          <e key='c'>baz</e>
                        </StringDictionary>
                  </a>";
            XmlNode node = GetXmlNode(configNode);
            TestTypeInitializable t = _sut.CreateObject(node, true) as TestTypeInitializable;

            t.StringDictionary.Count.Should().Be(3);
            t.StringDictionary["a"].Should().Be("foo");
            t.StringDictionary["b"].Should().Be("bar");
            t.StringDictionary["c"].Should().Be("baz");


        }

        [Fact]
        public void CanSetObjectProperty()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestTypeInitializable'>
                         <ChildType type='ConfigurationUnitTests.TestType' />
                  </a>";
            XmlNode node = GetXmlNode(configNode);
            TestTypeInitializable t = _sut.CreateObject(node, true) as TestTypeInitializable;

            t.ChildType.Should().NotBeNull();
        }






        private static XmlNode GetXmlNode(string elementText)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(elementText);

            XmlNode docDocumentElement = doc.DocumentElement;
            return docDocumentElement;
        }
    }

    public class TestType
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public bool BoolTest { get; set; }

       

    }

    public class TestTypeInitializable:IInitializable 

    {
        public string Name { get; set; }
        public int Index { get; set; }
        public bool BoolTest { get; set; }

        public List<string> StringList { get; set; }  = new List<string>();
        public void Initialize(XmlNode configNode)
        {
            
        }

        public bool AssignProperties { get; } = true;
        public Dictionary<string, string> StringDictionary { get; set; } = new Dictionary<string, string>();
        public TestType ChildType { get; set; }
    }
}