using Blackbird.Filters.Tests.CustomAssertions;
using Blackbird.Filters.Xliff.Xliff12;
using Blackbird.Filters.Xliff.Xliff2;
using System.Text;

namespace Blackbird.Filters.Tests.Xliff2;

[TestFixture]
public class Xliff2And12InteroperabilityTests : TestBase
{
    [Test]
    public void all_extensions()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/allExtensions.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void almost_empty()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/almostEmpty.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        //XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void empty_skeleton_with_href()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/emptySkeletonWithHref.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void everything_core()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/everything-core.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void non_empty_skeleton_without_href()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/nonEmptySkeletonWithoutHref.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void sample_1()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/sample1.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void source_only()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/sourceOnly.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void test_translate_with_target()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/testTranslateWithTarget.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void to_join()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/toJoin.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void to_segment()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/toSegment.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void to_segment_and_order()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/toSegmentAndOrder.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void type_subtype_values()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/typeSubTypeValues.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_cdata_sections()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withCDataSections.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_comment_annotations()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withCommentAnnotations.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_glossary()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withGlossary.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_matches()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withMatches.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_modules_attributes_in_ec()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withModulesAttributesInEc.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_notes()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withNotes.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_notes_complex()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withNotes_complex.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_references()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withReferences.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_reordered_codes()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withReorderedCodes.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_tbx_extension()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withTBXExtension.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_validation()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withValidation.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_xml_lang()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withXmlLang.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }

    [Test]
    public void with_xml_space()
    {
        var xliff = File.ReadAllText("Xliff2/XLIFF valid/withXmlSpace.xlf", Encoding.UTF8);
        var content = Xliff2Serializer.Deserialize(xliff);
        var xliff12 = Xliff12Serializer.Serialize(content);
        DisplayXml(xliff12);
        var back = Xliff12Serializer.Deserialize(xliff12);
        var returned = Xliff2Serializer.Serialize(back);
        DisplayXml(returned);
        XmlAssert.AreEqual(xliff, returned);
    }
}
