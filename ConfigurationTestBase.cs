using Sitecore;
using Sitecore.Configuration;

namespace ConfigurationUnitTests
{
  public abstract class ConfigurationTestBase
  {
    protected DefaultFactory _sut;

    public ConfigurationTestBase()
    {
      Context.IsUnitTesting = true;
      _sut = new DefaultFactory(null, null);
    }
  }
}