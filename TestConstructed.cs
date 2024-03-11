namespace ConfigurationUnitTests
{
    public class TestConstructed
    {
        public string ConstructedValue { get; }
        public string PropertyValue { get; set; }
        public TestConstructed(string value)
        {
            ConstructedValue = value;
        }
         
    }
}
