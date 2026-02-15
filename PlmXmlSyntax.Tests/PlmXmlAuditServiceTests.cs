using PlmXmlSyntax.Models;
using PlmXmlSyntax.Services;

namespace PlmXmlSyntax.Tests;

public class PlmXmlAuditServiceTests
{
    private static PlmXml CreatePlmXml(
        List<ProductInstance>? instances = null,
        List<ProductRevisionView>? views = null,
        List<ProductRevision>? revisions = null,
        List<Product>? products = null)
    {
        return new PlmXml
        {
            ProductDef = new ProductDef
            {
                InstanceGraph = new InstanceGraph
                {
                    ProductInstances = instances ?? new(),
                    ProductRevisionViews = views ?? new()
                }
            },
            ProductRevisions = revisions ?? new(),
            Products = products ?? new()
        };
    }

    [Fact]
    public async Task AuditAsync_WhenDisabled_ReturnsEmpty()
    {
        var svc = new PlmXmlAuditService { IsEnabled = false };
        var plmXml = CreatePlmXml(
            instances: new() { new ProductInstance { Id = "i1", PartRef = "#missing" } }
        );

        var issues = await svc.AuditAsync(plmXml, "test.xml");
        Assert.Empty(issues);
    }

    [Fact]
    public async Task AuditAsync_DetectsMissingReference()
    {
        var svc = new PlmXmlAuditService();
        var plmXml = CreatePlmXml(
            instances: new() { new ProductInstance { Id = "i1", PartRef = "#nonexistent" } },
            views: new() { new ProductRevisionView { Id = "v1" } }
        );

        var issues = await svc.AuditAsync(plmXml, "test.xml");

        Assert.Single(issues);
        Assert.Equal(AuditCategory.MissingReference, issues[0].Category);
        Assert.Equal(AuditSeverity.Error, issues[0].Severity);
        Assert.Equal("i1", issues[0].NodeId);
    }

    [Fact]
    public async Task AuditAsync_NoIssue_WhenReferenceExists()
    {
        var svc = new PlmXmlAuditService();
        var plmXml = CreatePlmXml(
            instances: new() { new ProductInstance { Id = "i1", PartRef = "#v1" } },
            views: new() { new ProductRevisionView { Id = "v1" } }
        );

        var issues = await svc.AuditAsync(plmXml, "test.xml");
        Assert.Empty(issues);
    }

    [Fact]
    public async Task AuditAsync_DetectsEmptyAssembly()
    {
        var svc = new PlmXmlAuditService();
        var plmXml = CreatePlmXml(
            views: new()
            {
                new ProductRevisionView { Id = "v1", Type = "assembly", InstanceRefs = "", Representation = null }
            }
        );

        var issues = await svc.AuditAsync(plmXml, "test.xml");

        Assert.Single(issues);
        Assert.Equal(AuditCategory.Performance, issues[0].Category);
        Assert.Equal(AuditSeverity.Warning, issues[0].Severity);
    }

    [Fact]
    public async Task AuditAsync_NoWarning_WhenAssemblyHasChildren()
    {
        var svc = new PlmXmlAuditService();
        var plmXml = CreatePlmXml(
            views: new()
            {
                new ProductRevisionView { Id = "v1", Type = "assembly", InstanceRefs = "i1 i2" }
            }
        );

        var issues = await svc.AuditAsync(plmXml, "test.xml");
        Assert.Empty(issues);
    }

    [Fact]
    public async Task AnalyzeCrossFileCollisions_DetectsCollision()
    {
        var svc = new PlmXmlAuditService();
        var file1 = CreatePlmXml(instances: new() { new ProductInstance { Id = "shared_id" } });
        var file2 = CreatePlmXml(views: new() { new ProductRevisionView { Id = "shared_id" } });

        var datasets = new List<(PlmXml Data, string Path)>
        {
            (file1, "file1.xml"),
            (file2, "file2.xml")
        };

        var issues = await svc.AnalyzeCrossFileCollisionsAsync(datasets);

        Assert.Single(issues);
        Assert.Equal(AuditCategory.Collision, issues[0].Category);
        Assert.Equal("shared_id", issues[0].NodeId);
    }

    [Fact]
    public async Task AnalyzeCrossFileCollisions_NoIssue_WhenUniqueIds()
    {
        var svc = new PlmXmlAuditService();
        var file1 = CreatePlmXml(instances: new() { new ProductInstance { Id = "id_a" } });
        var file2 = CreatePlmXml(instances: new() { new ProductInstance { Id = "id_b" } });

        var datasets = new List<(PlmXml Data, string Path)>
        {
            (file1, "file1.xml"),
            (file2, "file2.xml")
        };

        var issues = await svc.AnalyzeCrossFileCollisionsAsync(datasets);
        Assert.Empty(issues);
    }
}
