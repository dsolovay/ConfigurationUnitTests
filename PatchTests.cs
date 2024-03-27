using NSubstitute.Exceptions;

using Sitecore;
using Sitecore.Configuration;

using System.Xml;

using Xunit;

namespace ConfigurationUnitTests
{ 
	public class PatchTests
	{
		private DefaultFactory _sut;
		public PatchTests()
		{
			Context.IsUnitTesting = true;
			_sut = new DefaultFactory(null, null);
		}

		[Fact]
		public void PatchDefaultsToLast()
    {
      var nodes = _sut.GetConfigNodes("patchTests/PatchDefaultToLast/child::node()");
      Assert.Equal(nodes.Count - 1, GetNodePosition("patched", nodes));
    }


    [Fact]
    public void PatchBefore()
    {
      var nodes = _sut.GetConfigNodes("patchTests/PatchBefore/child::node()");

      Assert.Equal(4, nodes.Count);
      Assert.Equal(0, GetNodePosition("patched", nodes));
    }

    [Fact]
    public void PatchAfter()
    {
      var nodes = _sut.GetConfigNodes("patchTests/PatchAfter/child::node()");
      Assert.Equal(4, nodes.Count);
			Assert.Equal(1, GetNodePosition("patched", nodes));
    }

    [Fact]
    public void PatchInstead()
    {
      var nodes = _sut.GetConfigNodes("patchTests/PatchInstead/child::node()");
      Assert.Equal(3, nodes.Count);
			Assert.Equal(0, GetNodePosition("patched", nodes));
    }

    [Fact]
    public void PatchDelete()
    {
      var nodes = _sut.GetConfigNodes("patchTests/PatchDelete/child::node()");
      Assert.Equal(2, nodes.Count);
      Assert.Equal(0, GetNodePosition("node2", nodes));
      Assert.Equal(1, GetNodePosition("node3", nodes));
		}


    [Fact]
    public void PatchAttributeNew()
    {
      XmlNode node = _sut.GetConfigNode("patchTests/PatchAttributeNew");
      Assert.Equal("newValue", node.Attributes["toAdd"].Value);

    }

    [Fact]
    public void PatchAttributeReplace()
    {
      XmlNode node = _sut.GetConfigNode("patchTests/PatchAttributeReplace");
      Assert.Equal("newValue", node.Attributes["toReplace"].Value);

    }

    [Fact]
    public void PatchDeleteAttribute()
    {
      XmlNode node = _sut.GetConfigNode("patchTests/PatchDeleteAttribute");
      Assert.Null(node.Attributes["toDelete"]);

    }


    [Fact]
    public void SetAttribute()
    {
      XmlNode node = _sut.GetConfigNode("patchTests/SetAttribute");
			Assert.Equal("newValue", node.Attributes["toSet"].Value);

		}


		private int GetNodePosition(string name, XmlNodeList list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Name == name) return i;
			}

			throw new ArgumentNotFoundException(name);
		}

	}
}