using Blackbird.Filters.Transformations;
using System.Text;

namespace Blackbird.Filters.Tests.Heap;

[TestFixture]
public class FileHeapTest : TestBase
{
    private static readonly string FolderPath = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "Heap/Files"
    );

    [Test]
    public void Folder_ShouldExist()
    {
        string fullPath = Path.GetFullPath(FolderPath);
        TestContext.WriteLine($"Full path: {fullPath}");

        Assert.That(Directory.Exists(fullPath), Is.True, "Test folder not found");
    }

    private static IEnumerable<TestCaseData> FilePaths()
    {
        string fullPath = Path.GetFullPath(FolderPath);
        if (!Directory.Exists(fullPath))
            yield break;

        foreach (var file in Directory.GetFiles(fullPath))
        {
            var fileName = Path.GetFileName(file);
            yield return new TestCaseData(file).SetName($"FileParses({fileName})");
        }
    }

    [TestCaseSource(nameof(FilePaths))]
    public void FileParses(string filePath)
    {
        Assert.That(File.Exists(filePath), Is.True, $"Missing file: {filePath}");
        TestContext.WriteLine($"Testing file: {Path.GetFileName(filePath)}");

        var serialized = File.ReadAllText(filePath, Encoding.UTF8);

        var transformation = Transformation.Parse(serialized);

        var returned = transformation.Serialize();
        DisplayXml(returned);

        if (filePath.EndsWith(".html"))
        {
            var original = transformation.Source().Serialize();
            DisplayHtml(original);
        }
    }
}
