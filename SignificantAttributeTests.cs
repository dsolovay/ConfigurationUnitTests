using System.Xml;
using FluentAssertions;
using Xunit;

namespace ConfigurationUnitTests
{
  public class SignificantAttributeTests:ConfigurationTestBase
  {
    /// <summary>
    /// Shows that merging only occurs when a patch file is applied, not within a file.
    /// </summary>
    /// <code>
    /// <NoSignificantAttributesTogether>
    ///    <childNode attr1='a' />
    ///    <childNode attr1='b' />
    /// </NoSignificantAttributesTogether>
    /// </code> 
    [Fact]
    public void Two_nodes_with_no_significant_attributes_in_same_file_stay_separate()
    {
      XmlNode parentNode = _sut.GetConfigNode("SignificantAttributeTests/NoSignificantAttributesTogether");
      parentNode.ChildNodes.Count.Should().Be(2);
      parentNode.ChildNodes[0].Attributes["attr1"].Value.Should().Be("a");
      parentNode.ChildNodes[1].Attributes["attr1"].Value.Should().Be("b");

    }

    /// <summary>
    /// Shows that merging only occurs when a patch file is applied, not within a file.
    /// </summary>
    /// <code>
    /// app.config
    /// <NoSignificantAttributesSeparate>
    ///    <childNode attr1='a' />
    /// </NoSignificantAttributesSeparate>
    /// 
    /// SignificantAttributes.config
    /// <NoSignificantAttributesSeparate>
    ///    <childNode attr1='b' />
    /// </NoSignificantAttributesSeparate>
    /// </code> 
    [Fact]
    public void Two_nodes_with_no_significant_attributes_in_different_files_merged()
    {
      XmlNode parentNode = _sut.GetConfigNode("SignificantAttributeTests/NoSignificantAttributesSeparate");
      parentNode.ChildNodes.Count.Should().Be(1);
      XmlNode childNode = parentNode.ChildNodes[0];
      childNode.Attributes["attr1"].Value.Should().Be("b");
      childNode.Attributes["source", "http://www.sitecore.net/xmlconfig/"].Value.Should()
        .Be("SignificantAttributes.config");
      childNode.Attributes.Count.Should().Be(2);
    }


    /// <summary>
    /// Shows that merging only occurs when a patch file is applied, not within a file.
    /// </summary>
    /// <code>
    /// app.config
    /// <HasSignificantAttributesSeparate>
    ///    <childNode attr1='a' />
    /// </HasSignificantAttributesSeparate>
    /// 
    /// SignificantAttributes.config
    /// <HasSignificantAttributesSeparate>
    ///    <childNode attr1='b'  desc='test'/>
    /// </HasSignificantAttributesSeparate>
    /// </code> 
    [Fact]
    public void Two_nodes_with_significant_attributes_in_different_files_not_merged()
    {
      XmlNode parentNode = _sut.GetConfigNode("SignificantAttributeTests/HasSignificantAttributesSeparate");
      parentNode.ChildNodes.Count.Should().Be(2);
      parentNode.ChildNodes[0].Attributes["attr1"].Value.Should().Be("a");
      parentNode.ChildNodes[1].Attributes["attr1"].Value.Should().Be("b"); 
    }


    /// <summary>
    /// Shows undocumented behavior that 'name' (or 'uid') force a merge, overwriting other significant attributes.
    /// </summary>
    /// <code>
    /// app.config
    /// <HasName>
    ///    <childNode name='test_name' attr1='a' desc='test_desc1' />
    /// </HasName>
    /// 
    /// SignificantAttributes.config
    /// <HasName>
    ///    <childNode name='test_name' attr1='b' desc='test_desc2' />
    /// </HasName>
    /// </code> 
    [Fact]
    public void Attribute_name_overrides_match_behavior()
    {
      XmlNode parentNode = _sut.GetConfigNode("SignificantAttributeTests/HasName");
      parentNode.ChildNodes.Count.Should().Be(1);
      parentNode.ChildNodes[0].Attributes["attr1"].Value.Should().Be("b");
      parentNode.ChildNodes[0].Attributes["desc"].Value.Should().Be("test_desc2");
    }

  }
}