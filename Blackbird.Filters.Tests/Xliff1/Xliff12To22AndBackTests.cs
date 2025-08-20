using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff1;
using Blackbird.Filters.Xliff.Xliff2;
using System.Text;

namespace Blackbird.Filters.Tests.Xliff1;

[TestFixture]
public class Xliff12To22AndBackTests : TestBase
{
    [TestCase("basic.xliff")]
    [TestCase("segmented.xliff")]
    [TestCase("approved.xliff")]
    [TestCase("complex.xliff")]
    [TestCase("differentStatesWithSegmentation.xliff")]
    [TestCase("multifile.xliff")]
    [TestCase("translate.xliff")]
    [TestCase("state.xliff")]
    [TestCase("stateWithSegmentation.xliff")]
    [TestCase("emptyTarget.xliff")]
    [TestCase("everythingCore.xliff")]
    public void RoundTripTest(string fileName)
    {
        // Arrange
        var originalXliff12 = File.ReadAllText($"Xliff1/Files/{fileName}", Encoding.UTF8);

        // Act - Convert XLIFF 1.2 → XLIFF 2.2 → XLIFF 1.2
        var content = Xliff1Serializer.Deserialize(originalXliff12);
        var xliff22 = Xliff2Serializer.Serialize(content);
        DisplayXml(xliff22);
        
        var contentFromXliff22 = Xliff2Serializer.Deserialize(xliff22);
        var finalXliff12 = Xliff1Serializer.Serialize(contentFromXliff22);
        DisplayXml(finalXliff12);

        // Assert
        XmlAssert.AreEqual(originalXliff12, finalXliff12);
    }
}