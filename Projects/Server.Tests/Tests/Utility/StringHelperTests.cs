using Xunit;

namespace Server.Tests
{
    public class TestStringHelpers
    {
        [Theory, InlineData(null, "default value", "default value"), InlineData("", "default value", "default value"),
         InlineData("this is a valid string", "default value", "this is a valid string")]
        public void TestIsNullOrDefault(string value, string defaultValue, string expected)
        {
            var actual = value.DefaultIfNullOrEmpty(defaultValue);

            Assert.Equal(expected, actual);
        }
    }
}
