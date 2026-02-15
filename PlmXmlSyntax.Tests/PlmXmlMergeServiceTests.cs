using System.IO;
using System.Xml;
using PlmXmlSyntax.Services;

namespace PlmXmlSyntax.Tests;

public class PlmXmlMergeServiceTests : IDisposable
{
    private readonly string _tempDir;

    public PlmXmlMergeServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PlmXmlMergeTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTempFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static string MinimalPlmXml(string instanceGraphContent, string rootRef = "root1") =>
        $@"<?xml version=""1.0""?>
<PLMXML xmlns=""http://www.plmxml.org/Schemas/PLMXMLSchema"">
  <ProductDef>
    <InstanceGraph rootInstanceRef=""#{rootRef}"">
      {instanceGraphContent}
    </InstanceGraph>
  </ProductDef>
</PLMXML>";

    [Fact]
    public async Task MergeFilesAsync_ReturnsTrue_OnSuccess()
    {
        var svc = new PlmXmlMergeService();
        var file1 = CreateTempFile("a.xml", MinimalPlmXml(
            @"<ProductRevisionView id=""v1"" name=""View1"" type=""assembly"" instanceRefs=""inst1"" />
              <ProductInstance id=""inst1"" partRef=""#v1"" name=""Part1"" />",
            "inst1"));

        var output = Path.Combine(_tempDir, "merged.xml");
        var result = await svc.MergeFilesAsync(new[] { file1 }, output, "MasterAssembly");

        Assert.True(result);
        Assert.True(File.Exists(output));
    }

    [Fact]
    public async Task MergeFilesAsync_PrefixesIds()
    {
        var svc = new PlmXmlMergeService();
        var file1 = CreateTempFile("a.xml", MinimalPlmXml(
            @"<ProductRevisionView id=""v1"" name=""View1"" type=""part"" />
              <ProductInstance id=""inst1"" partRef=""#v1"" name=""Part1"" />",
            "inst1"));

        var output = Path.Combine(_tempDir, "merged.xml");
        await svc.MergeFilesAsync(new[] { file1 }, output, "Master");

        var content = File.ReadAllText(output);
        // IDs from file 1 should be prefixed with F1_
        Assert.Contains("F1_v1", content);
        Assert.Contains("F1_inst1", content);
    }

    [Fact]
    public async Task MergeFilesAsync_PrefixesReferences()
    {
        var svc = new PlmXmlMergeService();
        var file1 = CreateTempFile("a.xml", MinimalPlmXml(
            @"<ProductInstance id=""inst1"" partRef=""#v1"" name=""Part1"" />
              <ProductRevisionView id=""v1"" name=""View1"" type=""part"" />",
            "inst1"));

        var output = Path.Combine(_tempDir, "merged.xml");
        await svc.MergeFilesAsync(new[] { file1 }, output, "Master");

        var content = File.ReadAllText(output);
        // partRef should be prefixed: #v1 -> #F1_v1
        Assert.Contains("#F1_v1", content);
    }

    [Fact]
    public async Task MergeFilesAsync_CreatesMasterStructure()
    {
        var svc = new PlmXmlMergeService();
        var file1 = CreateTempFile("a.xml", MinimalPlmXml(
            @"<ProductInstance id=""inst1"" partRef=""#v1"" name=""Part1"" />",
            "inst1"));
        var file2 = CreateTempFile("b.xml", MinimalPlmXml(
            @"<ProductInstance id=""inst2"" partRef=""#v2"" name=""Part2"" />",
            "inst2"));

        var output = Path.Combine(_tempDir, "merged.xml");
        await svc.MergeFilesAsync(new[] { file1, file2 }, output, "TopLevel");

        var content = File.ReadAllText(output);
        Assert.Contains("MasterRootInst", content);
        Assert.Contains("MasterRootView", content);
        Assert.Contains("TopLevel", content);
    }

    [Fact]
    public async Task MergeFilesAsync_ReturnsFalse_OnInvalidPath()
    {
        var svc = new PlmXmlMergeService();
        var result = await svc.MergeFilesAsync(
            new[] { Path.Combine(_tempDir, "nonexistent.xml") },
            Path.Combine(_tempDir, "out.xml"),
            "Master");

        Assert.False(result);
    }

    [Fact]
    public async Task MergeFilesAsync_MultipleFiles_UseDifferentPrefixes()
    {
        var svc = new PlmXmlMergeService();
        var file1 = CreateTempFile("a.xml", MinimalPlmXml(
            @"<ProductInstance id=""inst1"" partRef=""#v1"" name=""P1"" />", "inst1"));
        var file2 = CreateTempFile("b.xml", MinimalPlmXml(
            @"<ProductInstance id=""inst1"" partRef=""#v1"" name=""P2"" />", "inst1"));

        var output = Path.Combine(_tempDir, "merged.xml");
        await svc.MergeFilesAsync(new[] { file1, file2 }, output, "Master");

        var content = File.ReadAllText(output);
        Assert.Contains("F1_inst1", content);
        Assert.Contains("F2_inst1", content);
    }
}
