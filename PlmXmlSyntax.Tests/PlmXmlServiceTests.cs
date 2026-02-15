using Xunit;
using PlmXmlSyntax.Services;
using PlmXmlSyntax.Models;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace PlmXmlSyntax.Tests;

public class PlmXmlServiceTests
{
    private readonly PlmXmlService _service;

    public PlmXmlServiceTests()
    {
        // Mock profile service to return a default profile
        var mockProfileService = new ProfileService();
        var mockAuditService = new PlmXmlAuditService();
        _service = new PlmXmlService(mockAuditService, mockProfileService);
    }

    [Fact]
    public async Task ParseAndTraverse_ValidAssembly_ReturnsCorrectLeafCount()
    {
        // Arrange
        string path = Path.Combine("TestData", "valid_assembly.plmxml");

        // Act
        var plmXml = await _service.ParseAsync(path);
        Assert.NotNull(plmXml);
        var results = await _service.TraverseTreeAsync(plmXml);

        // Assert
        Assert.Single(results);
        Assert.Equal("Geometry_A", results[0].Name);
        Assert.Equal("part_a.jt", results[0].FileName);
    }

    [Fact]
    public async Task ParseAndTraverse_IndirectLinks_ResolvesCorrectly()
    {
        // Arrange
        string path = Path.Combine("TestData", "indirect_links.plmxml");

        // Act
        var plmXml = await _service.ParseAsync(path);
        Assert.NotNull(plmXml);
        var results = await _service.TraverseTreeAsync(plmXml);

        // Assert
        Assert.Single(results);
        Assert.Equal("Final_View", results[0].Name);
        Assert.Equal("part.jt", results[0].FileName);
    }

    [Fact]
    public async Task ParseAndTraverse_BrokenRefs_ReportsErrors()
    {
        // Arrange
        string path = Path.Combine("TestData", "broken_refs.plmxml");

        // Act
        var plmXml = await _service.ParseAsync(path);
        Assert.NotNull(plmXml);
        var results = await _service.TraverseTreeAsync(plmXml);

        // Assert
        Assert.Single(results);
        Assert.Equal(AuditSeverity.Error, results[0].Severity);
        Assert.Contains("could not be resolved", results[0].StatusMessage);
    }
}
