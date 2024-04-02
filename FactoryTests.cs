using System;
using System.Linq;
using System.ServiceModel.Channels;
using System.Web.UI.WebControls;
using System.Xml;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sitecore.Abstractions;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Exceptions;
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
            var element = MakeNode("type", "someValue");

            element.Attributes["type"].Value.Should().Be("someValue");
        }

        [Fact]
        public void CreateTestType()
        {
            TestType obj = _sut.CreateObject(MakeNode("type", "ConfigurationUnitTests.TestType"), true) as TestType;

            Assert.NotNull(obj);

        }

        [Fact]
        public void CreateTestTypeWithTextXml()
        {
            TestType obj = _sut.CreateObject(ToNode("<a type='ConfigurationUnitTests.TestType' />"), true) as TestType;

            Assert.NotNull(obj);

        }


        [Fact]
        public void AssertFailWorks()
        {
            XmlNode configNode = MakeNode("type", "ConfigurationUnitTests.NotReal");

            Assert.Throws<Exception>(() => _sut.CreateObject(configNode, true) as TestType);
        }


        [Fact]
        public void NoAssert()
        {
            XmlNode configNode = MakeNode("type", "ConfigurationUnitTests.NotReal");

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
            object result = _sut.CreateObject(ToNode("<a>some text</a>"), false);

            Assert.Equal("some text", (string)result);
        }


        /// <summary>
        /// Illustrates parameter assignment.
        /// </summary>
        /// <remarks>
        /// See <see cref="DefaultFactory.IsPropertyNode"/>
        /// <code>if (node.NodeType != XmlNodeType.Element || node.Name == "param" || this.GetAttribute("hint", node, parameters) == "skip")</code>
        /// <code>  return false;</code>
        /// <code>bool flag = this.GetAttribute("hint", node, parameters) == "defer";</code>
        /// <code>return deferred == flag;</code>
        /// </remarks>
        /// <param name="description"></param>
        /// <param name="xml"></param>
        /// <param name="matchExpected"></param>

        [Theory]
        [InlineData(
            "Attributes are not mapped to properties.",
            @"<a type='ConfigurationUnitTests.TestType' name='expected' />",
            false
            )]
        [InlineData(
            "Child elements are mapped to properties.",
            @"<a type='ConfigurationUnitTests.TestType'><name>expected</name></a>",
            true
        )]
        [InlineData(
            "Child elements are case insensitive.",
            @"<a type='ConfigurationUnitTests.TestType'><NaMe>expected</NaMe></a>",
            true
        )]
        [InlineData(
            @"hint='skip' ignored.",
            @"<a type='ConfigurationUnitTests.TestType'><name hint='skip'>expected</name></a>",
            false
        )]

        [InlineData(
            @"hint='skip' ignored, with parameter.",
            @"<a type='ConfigurationUnitTests.TestType' skip='skip'><name hint='$(skip)'>expected</name></a>",
            false
        )]
        [InlineData(
            @"hint='skip' not set, with parameter.",
            @"<a type='ConfigurationUnitTests.TestType' skip='no_skip'><name hint='$(skip)'>expected</name></a>",
            true
        )]
        [InlineData(
            @"hint='defer' ignored.",
            @"<a type='ConfigurationUnitTests.TestType'><name hint='defer'>expected</name></a>",
            false
        )]
        public void String_parameter_mapping(string description, string xml, bool matchExpected)
        {

            TestType t = (TestType)_sut.CreateObject(ToNode(xml), true);

            if (matchExpected)
            {
                t.Name.Should().Be("expected");
            }
            else
            {
                t.Name.Should().BeNull();
            }

        }

        /// <summary>
        /// See <see cref="DefaultFactory"/>,
        /// <code>
        /// if (IsObjectNode(paramNode))
        /// {
        ///    return CreateObject(paramNode, parameters, assert);
        /// }
        /// </code>
        /// </summary>
        [Fact]
        public void Child_element_mapping()
        {
            string xml = @"
<a type='ConfigurationUnitTests.TestType'>
    <childtype type='ConfigurationUnitTests.TestType' />
</a>";
            TestType t = _sut.CreateObject(ToNode(xml), true) as TestType;
            t.ChildType.Should().NotBeNull();
        }

        /// <summary>
        /// Illustrates use of <value>hint='call:'</value>.
        /// See <see cref="DefaultFactory"/>
        /// <code>
        ///  if (attribute1.StartsWith("call:", StringComparison.InvariantCulture))
        ///  {
        ///    ObjectList innerObject = new ObjectList(attribute1.Substring(5));
        ///    innerObject.Add(string.Empty, (object) paramNode);
        ///    return (object) innerObject;
        ///  }
        /// </code>
        /// </summary>
        [Fact]
        public void Can_pass_parent_node_to_call_method()
        {
            string xml = @"
<a type='ConfigurationUnitTests.TestType'>
    <b hint='call:ReadNode' specialParameter='special parameter value' />
</a>";
            TestType t = _sut.CreateObject(ToNode(xml), true) as TestType;
            t.Name.Should().Be("special parameter value");
        }


        /// <summary>
        /// Illustrates <value>hint='list'</value>
        /// See <see cref="DefaultFactory"/>
        /// <code>
        ///  bool flag1 = attribute1.StartsWith("list:", StringComparison.InvariantCulture) || attribute1 == "list" || attribute1 == "dictionary";
        /// </code>
        /// </summary>
        [Fact]
        public void Can_add_list()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                        <StringList hint='list'>
                          <e>foo</e>
                          <e>bar</e>
                          <e>baz</e>
                        </StringList>
                  </a>";
            TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;

            t.StringList.Count.Should().Be(3);
            t.StringList.Should().ContainInOrder("foo", "bar", "baz");


        }

        /// <summary>
        /// Although attributes are not directly used, they can be mapped to parameters.
        /// </summary>
        [Fact]
        public void Can_add_list_with_replacement()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType' 
                    a='alpha' b='beta' c='gamma'>
                        <StringList hint='list'>
                          <e>$(a)</e>
                          <e>$(b)</e>
                          <e>$(c)</e>
                        </StringList>
                  </a>";
            TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;

            t.StringList.Count.Should().Be(3);
            t.StringList.Should().ContainInOrder("alpha", "beta", "gamma");

        }

        /// <summary>
        /// Illustrates that presence of 'key' attribute causes IDictionary to be added.
        /// </summary>
        [Fact]
        public void If_key_attribute_adds_dictionary()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                        <StringDictionary hint='list'>
                          <e key='a'>foo</e>
                          <e key='b'>bar</e>
                          <e key='c'>baz</e>
                        </StringDictionary>
                  </a>";
            XmlNode node = ToNode(configNode);
            TestType t = _sut.CreateObject(node, true) as TestType;

            t.StringDictionary.Count.Should().Be(3);
            t.StringDictionary["a"].Should().Be("foo");
            t.StringDictionary["b"].Should().Be("bar");
            t.StringDictionary["c"].Should().Be("baz");
        }


        /// <summary>
        /// Illustrates recursive object definition.
        /// </summary>
        /// <remarks>
        /// Implemented by:
        /// <br /><br /> 
        /// <see cref="DefaultFactory.GetInnerObject"/>:
        /// <code> if (this.IsObjectNode(paramNode))</code>
        /// <code>   return this.CreateObject(paramNode, parameters, assert);</code>
        /// <br />
        /// <see cref="DefaultFactory.IsObjectNode"/>:
        /// <code> if (node.NodeType != XmlNodeType.Element)</code>
        /// <code>        return false;</code>
        /// <code>      return XmlUtil.HasAttribute("ref", node) || XmlUtil.HasAttribute("type", node) || XmlUtil.HasAttribute("path", node) || XmlUtil.HasAttribute("connectionStringName", node);</code>
        /// </remarks>
        [Fact]
        public void CanSetObjectProperty()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                         <ChildType type='ConfigurationUnitTests.TestType' />
                  </a>";
            TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;

            t.ChildType.Should().NotBeNull();
        }


        [Fact]
        public void KeySetForListThrows()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                         <StringList hint=""list"">
                            <child key=""someKey"">SomeValue</child>
                         </StringList>
                  </a>";
            try
            {
                TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;
                Xunit.Assert.Fail("Exception not throw.");
            }
            catch (Exception ex)
            {
                ex.Message.Should()
                    .StartWith("Could not assign values to the property: StringList (it is not an IDictionary)");
            }
        }

        [Fact]
        public void NoKeyOnDictionaryThrows()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                         <StringDictionary hint=""list"">
                            <child>SomeValue</child>
                         </StringDictionary>
                  </a>";
            try
            {
                TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;
                Assert.Fail("Exception not throw.");
            }
            catch (Exception ex)
            {
                ex.Message.Should()
                    .StartWith("Could not assign values to the property: StringDictionary (it is not an IList)");
            }
        }



        #region utility tests
        [Fact]
        public void StringUtilTests()
        {
            Sitecore.StringUtil.Mid("list", 4).Should().BeEmpty();
            Sitecore.StringUtil.Mid("dictionary", 4).Should().Be("ionary");
        }

        #endregion


        /// <summary>
        /// Documents the oddity that 'Dictionary' is a magic word, but not fully implemented
        /// <code>bool flag1 = attribute1.StartsWith("list:", StringComparison.InvariantCulture) || attribute1 == "list" || attribute1 == "dictionary";</code>
        /// <code>bool flag2 = attribute1.StartsWith("raw:", StringComparison.InvariantCulture);</code>
        /// <code>string addMethod = StringUtil.Mid(attribute1, flag2 ? 4 : 5);</code>
        /// <code>...</code>
        /// <code>string attribute2 = this.GetAttribute("key", xmlNode, parameters);</code>
        /// <code>object obj = flag2 ? (object) xmlNode : this.CreateObject(xmlNode, parameters, assert);</code>
        /// <code>if (obj != null)</code>
        /// <code>  innerObject.Add(attribute2, obj);</code>
        /// </summary>
        [Fact]
        public void DictionaryKeywordThrows()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                         <StringDictionary hint=""dictionary"">
                            <child key=""somekey"">SomeValue</child>
                         </StringDictionary>
                  </a>";
            try
            {
                TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;
                Assert.Fail("Exception not throw.");
            }
            catch (Exception ex)
            {
                ex.Message.Should()
                    .StartWith("Could not find add method: onary");
            }
        }

        /// <summary>
        /// Shows how <code>hint='raw:'</code> can be used to load an XmlElement to a collection. 
        /// </summary>
        /// <remarks>
        /// Implemented by
        /// <code>object obj = flag2 ? (object) xmlNode : this.CreateObject(xmlNode, parameters, assert);</code>
        /// </remarks>
        [Theory]
        [InlineData("raw:AddObject", typeof(XmlElement))]
        [InlineData("list:AddObject", typeof(TestType))]
        public void Raw_adds_xml_element(string attribute, Type returnedType)
        {
            string xml =
                $@"
<a type=""ConfigurationUnitTests.TestType"">      
      <ObjectList hint=""{attribute}"">
          <b type=""ConfigurationUnitTests.TestType"" />
      </ObjectList>
</a>";
            TestType t = _sut.CreateObject(ToNode(xml), false) as TestType;

            t.ObjectList.First().Should().BeOfType(returnedType);

            if (returnedType == typeof(XmlElement))
            {
                t.ObjectList.First().As<XmlElement>().OuterXml.Should()
                    .Be("<b type=\"ConfigurationUnitTests.TestType\" />");

                // Raw allows you to use the XML in a second call to the
                // factory _from_ the constructed class.
            }
        }

        /// <summary>
        /// Special logic to ignore 'setting' value.
        /// </summary>
        /// <remarks>
        /// Implemented here:
        /// <code>if (attribute1 == "setting")
        ///   return (object) null;</code>
        /// </remarks>
        [Fact]
        public void SettingSkipped()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                         <Name hint='setting'>
                            <Nested>SomeValue</Nested>
                         </Name>
                  </a>";

            TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;

            t.Name.Should().BeNull();
        }

        /// <summary>
        /// Illustrates fallback to nested text.
        /// <remarks>
        /// Implemented here:
        /// <br />
        /// <br />
        /// <see cref="DefaultFactory.GetInnerObject"></see>:
        /// <code>return flag1 | flag2 ? (object) new ObjectList(addMethod) : (object) this.GetStringValue(paramNode, parameters);</code>
        /// <see cref="DefaultFactory.GetStringValue"></see>:
        /// <code>return ConfigReader.ReplaceVariables(node.InnerText, node1, parameters);</code>
        /// </remarks>
        /// </summary>

        [Fact]
        public void NestedElement_falls_back_to_inner_text()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                         <Name>
                            <Nested>SomeValue</Nested>
                         </Name>
                  </a>";

            TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;

            t.Name.Should().Be("SomeValue");
        }


        /// <summary>
        /// Illustrates that token replace occurs.
        /// <remarks>
        /// Implemented here:
        /// <br />
        /// <br />
        /// <see cref="DefaultFactory.GetStringValue"></see>:
        /// <code>return ConfigReader.ReplaceVariables(node.InnerText, node1, parameters);</code> 
        /// </remarks>
        /// </summary>

        [Fact]
        public void NestedElement_replacement_applied()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType' someAttr='someAttrValue'>
                         <Name>
                            <Nested>Text with $(someAttr) in it.</Nested>
                         </Name>
                  </a>";

            TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;

            t.Name.Should().Be("Text with someAttrValue in it.");
        }

        /// <summary>
        /// Mapping out <see cref="DefaultFactory.AssignProperties(object, object[])"/> behavior.
        /// </summary>
        [Fact]
        public void Method_invoked_with_no_children()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                         <Name hint='list:AssignSetValue' /> 
                  </a>";

            TestType t = _sut.CreateObject(ToNode(configNode), true) as TestType;

            t.Name.Should().BeNull();
        }

        /// <summary>
        /// Illustrates alternative way of setting nested properties.
        /// </summary>
        /// <remarks>
        /// See <see cref="Sitecore.Reflection.ReflectionUtil.SetProperty(object, string, object)"/>
        /// <code>  string[] strArray = name.Split('.');</code>
        /// <code>  for (int index = 0; index &lt; strArray.Length - 1; ++index)</code>
        /// <code>  {</code>
        /// <code>    obj = ReflectionUtil.GetProperty(obj, strArray[index]);</code>
        /// <code>    if (obj == null)</code>
        /// <code>      return false;</code>
        /// <code>  }</code>
        /// <code>  name = strArray[strArray.Length - 1];</code>
        /// </remarks>
        [Fact]
        public void CanUseDotNotation()
        {
            string configNode =
                @"<a
                    type='ConfigurationUnitTests.TestType'>
                         <ChildType type='ConfigurationUnitTests.TestType'/>
                         <ChildType.Name>NestedName</ChildType.Name>
                  </a>";
            TestType t = (TestType)_sut.CreateObject(ToNode(configNode), true);

            t.ChildType.Name.Should().Be("NestedName");
        }

        /// <summary>
        /// Shows access to app.config configuration.
        /// </summary>
        [Fact]
        public void CanGetNode()
        {
            _sut.GetConfigNode("test/testobject").Should().NotBeNull();
        }

        [Fact]
        public void CreateObject_with_ref()
        {
            string configNode =
                @"<a ref='test/testobject' />";
            TestType t = (TestType)_sut.CreateObject(ToNode(configNode), true);

            t.Should().NotBeNull();
        }

        [Fact]
        public void CreateObject_with_path()
        {
            string configNode =
                @"<a path='test/testobject' />";
            TestType t = (TestType)_sut.CreateObject(ToNode(configNode), true);

            t.Should().NotBeNull();
        }

        [Fact]
        public void CreateObject_with_ref_not_singleton()
        {
            string configNode =
                @"<a path='test/testobject' />";
            TestType t1 = (TestType)_sut.CreateObject(ToNode(configNode), true);

            
            TestType t2 = (TestType)_sut.CreateObject(ToNode(configNode), true);

            t1.Should().NotBeNull();
            t2.Should().NotBeNull();
            t1.Should().NotBeSameAs(t2);
        }

        [Fact]
        public void CreateObject_with_a_static_factoryMethod()
        {
          string configNode =
            @"<a type='ConfigurationUnitTests.TestType' factoryMethod='FactoryMethod1' />";
          string s = (string)_sut.CreateObject(ToNode(configNode), true);

          s.Should().Be("Created from FactoryMethod1");
        }

        [Fact]
        public void CreateObject_with_a_static_factoryMethod_with_argument()
        {
          string configNode =
            @"<a type='ConfigurationUnitTests.TestType' factoryMethod='FactoryMethod2' arg0='myArg' />";
          string s = (string)_sut.CreateObject(ToNode(configNode), true);

          s.Should().Be("Created from FactoryMethod2 with arg 'myArg'");
        }
        
        [Fact]
        public void CreateObject_with_a_static_factoryMethod_and_wrong_arg_number_throws()
        {
          string configNode =
            @"<a type='ConfigurationUnitTests.TestType' factoryMethod='FactoryMethod2' arg1='myArg' />";


          ((Action)(() => _sut.CreateObject(ToNode(configNode), true)))
            .Should().Throw<System.Reflection.TargetParameterCountException>()
            .WithMessage("Parameter count mismatch."); 
        }

        [Fact]
        public void CreateObject_with_a_param1_in_same_object_does_not_get_replaced()
        {
          string configNode =
            @"<a type='ConfigurationUnitTests.TestType' param1='Value from parameter'>
                  <name>$(1)</name>
              </a>";

          TestType t = (TestType)_sut.CreateObject(ToNode(configNode), true);

          t.Name.Should().Be("$(1)");
        }

        /// <summary>
        /// Shows parameter passing from CreateObject. Sitecore uses this technique to
        /// apply logic to parameters before they are passed to configuration, for
        /// example in configuring the MediaLinkPrefix parameter when constructing
        /// <see cref="Sitecore.Links.UrlBuilders.MediaUrlBuilder"></see>.
        /// </summary>
        [Fact]
        public void CreateObject_with_a_param1_passed_in_call_does_get_replaced()
        {
          string configNode =
            @"<a type='ConfigurationUnitTests.TestType'>
                  <name>$(1)</name>
              </a>";

          TestType t = (TestType)_sut.CreateObject(ToNode(configNode),
            new string[] { null, "Value from CreateObject call" }, true);

          t.Name.Should().Be("Value from CreateObject call");
        }
        
        [Fact]
        public void CreateObject_with_ref_with_singleton()
        {
          string configNode =
            @"<a path='test/testobjectsingleton' />";
          TestType t1 = (TestType)_sut.CreateObject(ToNode(configNode), true);


          TestType t2 = (TestType)_sut.CreateObject(ToNode(configNode), true);

          t1.Should().NotBeNull();
          t2.Should().NotBeNull();
          t1.Should().BeSameAs(t2);
        }
     
        [Fact]
        public void CreateObject_with_ref_pulls_parameters_from_ref_location()
        {
            string configNode =
                $@"<a path='test/testconstructed' param0='TestValueFromRefNode' />";

            var t = (TestConstructed)_sut.CreateObject(ToNode(configNode), true);

            t.ConstructedValue.Should().Be("TestValueFromAppConfig");
        }

        /// <summary>
        /// Setting values with $(0) format.
        /// </summary>
        /// <remarks>
        /// Implemented here:
        /// <see cref="ConfigReader.ReplaceVariables(string, XmlNode, string[])"/>
        /// <code>
        /// if (parameters != null)
        /// {
        ///    for (int index = 0; index &lt; parameters.Length; ++index)
        ///        value = value.Replace("$(" + (object) index + ")", parameters[index]);
        /// }
        /// </code>
        /// </remarks>
        [Fact]
        public void CreateObject_with_ref_accepts_numbered_parms()
        {
            string configNode =
                $@"<a path='test/testcontstructedwithnumber' param0='TestValueFromRefNode' />";

            var t = (TestConstructed)_sut.CreateObject(ToNode(configNode), true);

            t.ConstructedValue.Should().Be("TestValueFromRefNode");
        }


        [Fact]
        public void CreateObject_with_ref_accepts_numbered_parms_multiple()
        {
            string configNode1 =
                $@"<a path='test/testcontstructedwithnumber' param0='TestValueFromRefNode1' />";
            string configNode2 =
                $@"<a path='test/testcontstructedwithnumber' param0='TestValueFromRefNode2' />";

            var t1 = (TestConstructed)_sut.CreateObject(ToNode(configNode1), true);
            var t2 = (TestConstructed)_sut.CreateObject(ToNode(configNode2), true);

            t1.ConstructedValue.Should().Be("TestValueFromRefNode1");
            t2.ConstructedValue.Should().Be("TestValueFromRefNode2");
        }


        [Fact]
        public void CreateObject_with_ref_set_property()
        {
            string configNode =
                $@" <a path='test/testconstructed'>
                        <PropertyValue>SetAtReferrer</PropertyValue>
                    </a>";

            var t = (TestConstructed)_sut.CreateObject(ToNode(configNode), true);

            t.ConstructedValue.Should().Be("TestValueFromAppConfig");
            t.PropertyValue.Should().Be("SetAtReferrer");
        }


        [Fact]
        public void CreateObject_with_ref_and_deferred_property()
        {
            string configNode =
                $@" <a path='test/testwithdefer' />";

            var t = (TestType)_sut.CreateObject(ToNode(configNode), true);

            t.Name.Should().Be("DeferredValue");
        }


        [Fact]
        public void CreateObject_with_ref_and_non_deferred_property()
        {
            string configNode =
                $@" <a path='test/testwithnondefer' />";

            var t = (TestType)_sut.CreateObject(ToNode(configNode), true);

            t.Name.Should().Be("NonDeferredValue");
        }


        [Fact]
        public void Deferred_applied_second()
        {
            string configNode =
                $@" <a path='test/testwithboth' />";

            var t = (TestType)_sut.CreateObject(ToNode(configNode), true);

            t.Name.Should().Be("DeferredValue");
        }

        /// <summary>
        /// Outer most setting always wins. Not sure why this parameter is there.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData("loc1A", "1")]
        [InlineData("loc2A", "1")]
        [InlineData("loc3A", "1")]
        [InlineData("loc4A", "1")]
        [InlineData("loc5A", "1")]
        [InlineData("loc6A", "1")]
        [InlineData("loc7A", "1")]
        
        public void Defer_does_not_impact_resolution(string element, string expected)
        {
            var t = (TestType)_sut.CreateObject($"deferTests/{element}", true);

            t.Name.Should().Be(expected);
        }

        /// <summary>
        /// Tip: Use defer to set a parameter on a class that doesn't allow it. 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="expected"></param>
        [Theory]
        [InlineData("initializableNonDefer", null)]
        [InlineData("initializableDefer", "1")] 

        public void Defer_does_impact_initializable(string element, string expected)
        {
            var t = (TestInitializable)_sut.CreateObject($"deferTests/{element}", true);

            t.Prop.Should().Be(expected);
        }




        private static XmlNode ToNode(string elementText)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(elementText);

            XmlNode docDocumentElement = doc.DocumentElement;
            return docDocumentElement;
        }

        private static XmlElement MakeNode(string qualifiedName, string attrValue)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("test");
            XmlAttribute attr = doc.CreateAttribute(qualifiedName);
            attr.Value = attrValue;
            element.Attributes.Append(attr);
            return element;
        }
    }
}