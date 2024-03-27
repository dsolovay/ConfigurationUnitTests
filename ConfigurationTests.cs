using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Sitecore.Abstractions;
using Sitecore.Configuration;
using Sitecore.Data.Comparers;
using Sitecore.Diagnostics;
using Xunit;
using Assert = Xunit.Assert;

namespace ConfigurationUnitTests
{
    public class ConfigurationTests
    {
        [Fact]
        public void GetSetting_with_default()
        {
            Assert.Equal("default", Sitecore.Configuration.Settings.GetSetting("test", "default"));
        }


        [Fact]
        public void GetSetting_without_default()
        {
            Assert.Equal("", Sitecore.Configuration.Settings.GetSetting("test"));
        }


        [Fact]
        public void GetBoolSetting()
        {
            Assert.False(Sitecore.Configuration.Settings.GetBoolSetting("test", false));
        }


        /// <summary>
        /// Useful for feature toggle. You have new feature default to false, and in unit tests, you can override this setting.
        /// </summary>
        [Fact]
        public void GetBoolSetting_with_wrapper()
        {
            using (new SettingsSwitcher("test", "true"))
            {
                Assert.True(Sitecore.Configuration.Settings.GetBoolSetting("test", false));
            }
        }


        [Fact]
        public void ConfigReader()
        {
            ConfigReader configReader = new ConfigReader();
            Assert.Equal("", configReader.ToString());
        }

        [Fact]
        public void ConfigurationManagerTest()
        {
            Assert.Empty(ConfigurationManager.AppSettings.AllKeys);
        }

        [Fact]
        public void ConfigurationManagerTest2()
        {
            Assert.Null(ConfigurationManager.GetSection("test"));
        }
    }
}
