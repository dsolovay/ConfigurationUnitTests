using System.IO;
using System.Linq;
using System.Xml;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Exceptions;
using Sitecore;
using Xunit;
using Sitecore.Configuration;
using Sitecore.IO;

namespace ConfigurationUnitTests
{
    public class LayerTests
    {
        private DefaultFactory _sut;

        public LayerTests()
        {
            Context.IsUnitTesting = true;
            _sut = new DefaultFactory(null, null);
        }

        [Fact]
        public void Layers_are_loaded()
        {
            string expectedName = "layer1outer";

            XmlNodeList list = _sut.GetConfigNodes("layerTests/testNodes");

            list.Count.Should().BeGreaterOrEqualTo(1);
            foreach (object node in list)
            {
                if (node.As<XmlElement>()?.Attributes["name"].Value == expectedName)
                    return;
            }

            Assert.Fail($"{expectedName} not found");
        }

        [Fact]
        public void Listed_before_outer()
        {
            XmlNodeList list = _sut.GetConfigNodes("layerTests/testNodes");
            int outerPos = GetNotePosition("layer1outer", list);
            int innerPos = GetNotePosition("layer1listed1", list);
            innerPos.Should().BeLessThan(outerPos);
        }

        [Fact]
        public void OuterListed_Before_NestedListed()
        {
            XmlNodeList list = _sut.GetConfigNodes("layerTests/testNodes");
            int outerListed = GetNotePosition("layer1listed1", list);
            int nestedListed = GetNotePosition("layer1listed11", list);
            outerListed.Should().BeLessThan(nestedListed);
        }

        [Fact]
        public void Within_folder_alpha()
        {
            XmlNodeList list = _sut.GetConfigNodes("layerTests/testNodes");
            int firstAlpha = GetNotePosition("layer1listed1", list);
            int secondAlpha = GetNotePosition("layer1listed1a", list);
            firstAlpha.Should().BeLessThan(secondAlpha);
        }

        [Fact]
        public void Layers_in_order()
        {
            XmlNodeList list = _sut.GetConfigNodes("layerTests/testNodes");
            int layer1 = GetNotePosition("layer1outer", list);
            int layer2 = GetNotePosition("layer2outer", list);
            layer1.Should().BeLessThan(layer2);
        }

        [Fact]
        public void Mode_off_skipped_at_layer_level()
        {
            XmlNodeList list = _sut.GetConfigNodes("layerTests/testNodes");

            Assert.Throws<ArgumentNotFoundException>(() => GetNotePosition("layer3outer", list));

        }


        [Fact]
        public void Mode_off_skipped_at_file_level()
        {
            XmlNodeList list = _sut.GetConfigNodes("layerTests/testNodes");

            Assert.Throws<ArgumentNotFoundException>(() => GetNotePosition("layer1excluded", list));

        }

        [Fact]
        public void Omitted_layer_skipped()
        {
            XmlNodeList list = _sut.GetConfigNodes("layerTests/testNodes");

            Assert.Throws<ArgumentNotFoundException>(() => GetNotePosition("layer4outer", list));

        }



        private int GetNotePosition(string name, XmlNodeList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Attributes["name"].Value == name) return i;
            }

            throw new ArgumentNotFoundException(name);
        }
    }
}
