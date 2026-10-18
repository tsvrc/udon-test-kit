using NUnit.Framework;

namespace Tsvrc.UdonTestKit.Editor.Tests
{
    public class AssemblyDefinitionTextTests
    {
        [Test]
        public void Items_ArrayOfStrings_ReturnsEachValue()
        {
            Assert.That(AssemblyDefinitionText.Items("{ \"references\": [\"Mine\", \"GUID:abc\"], \"name\": \"A\" }", "references"),
                Is.EqualTo(new[] { "Mine", "GUID:abc" }));
        }
    }
}
