using Blackbird.Filters.Coders;
using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blackbird.Filters.Tests.Xliff2;

[TestFixture]
public class Xliff2ValidTestSuiteTests : TestBase
{
    [TestCase("allExtensions")]
    [TestCase("withMetaData")]
    [TestCase("almostEmpty")]
    [TestCase("emptySkeletonWithHref")]
    [TestCase("everything-core")]
    [TestCase("nonEmptySkeletonWithoutHref")]
    [TestCase("sample1")]
    [TestCase("sourceOnly")]
    [TestCase("testTranslateWithTarget")]
    [TestCase("toJoin")]
    [TestCase("toSegment")]
    [TestCase("toSegmentAndOrder")]
    [TestCase("typeSubTypeValues")]
    [TestCase("withCDataSections")]
    [TestCase("withCommentAnnotations")]
    [TestCase("withGlossary")]
    [TestCase("withMatches")]
    [TestCase("withModulesAttributesInEc")]
    [TestCase("withNotes")]
    [TestCase("withNotes_complex")]
    [TestCase("withReferences")]
    [TestCase("withReorderedCodes")]
    [TestCase("withTBXExtension")]
    [TestCase("withValidation")]
    [TestCase("withXmlLang")]
    [TestCase("withXmlSpace")]
    public void Valid(string fileName)
    {
        var xliff = File.ReadAllText($"Xliff2/XLIFF valid/{fileName}.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var returned = Xliff2Serializer.Serialize(content);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }
}