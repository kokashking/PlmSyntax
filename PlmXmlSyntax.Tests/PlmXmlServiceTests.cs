using PlmXmlSyntax.Models;
using PlmXmlSyntax.Services;
using System.IO;

namespace PlmXmlSyntax.Tests;

public class PlmXmlServiceTests
{
    private static string F(string name) => Path.Combine("TestData", name);

    private static PlmXmlService MakeService(RootSearchStrategy strategy = RootSearchStrategy.Auto)
    {
        var profile = new ParsingProfile { Name = "Default", RootStrategy = strategy, ResolveIndirectLinks = true, PartNumberUserDataTitle = "Mehrnummern" };
        return new PlmXmlService(new PlmXmlAuditService(), new StubProfileService(profile));
    }

    // happy path
    [Fact]
    public async Task ValidAssembly_ReturnsOneLeaf_WithCorrectNameAndFile()
    {
        var svc = MakeService();
        var plmXml = await svc.ParseAsync(F("valid_assembly.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml);

        Assert.Single(leaves);
        Assert.Equal("Geometry_A", leaves[0].Name);
        Assert.Equal("part_a.jt",  leaves[0].FileName);
        Assert.Equal(AuditSeverity.Info, leaves[0].Severity);
    }

    // indirect links: instance -> ProductRevision -> view
    [Fact]
    public async Task IndirectLinks_ResolvesViaProductRevision()
    {
        var svc = MakeService();
        var plmXml = await svc.ParseAsync(F("indirect_links.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml);

        Assert.Single(leaves);
        Assert.Equal("Final_View", leaves[0].Name);
        Assert.Equal("part.jt",   leaves[0].FileName);
    }

    // broken reference produces error entry instead of crashing
    [Fact]
    public async Task BrokenRef_ProducesErrorLeaf_NotCrash()
    {
        var svc = MakeService();
        var plmXml = await svc.ParseAsync(F("broken_refs.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml);

        Assert.Single(leaves);
        Assert.Equal(AuditSeverity.Error, leaves[0].Severity);
        Assert.Contains("could not be resolved", leaves[0].StatusMessage);
    }

    // no rootInstanceRef -> heuristic finds orphan root
    [Fact]
    public async Task NoRootRef_HeuristicFindsOrphanRoot()
    {
        var svc = MakeService(RootSearchStrategy.Auto);
        var plmXml = await svc.ParseAsync(F("no_root_ref.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml);

        Assert.Single(leaves);
        Assert.Equal("Heuristic_Leaf", leaves[0].Name);
    }

    // circular reference must not hang
    [Fact]
    public async Task CircularRef_Terminates_WithNoLeaves()
    {
        var svc = MakeService();
        var plmXml = await svc.ParseAsync(F("circular_ref.plmxml"));
        Assert.NotNull(plmXml);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var task = svc.TraverseTreeAsync(plmXml);
        var winner = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));

        Assert.Same(task, winner); // completed before timeout
        var leaves = await task;
        Assert.Empty(leaves);
    }

    // whitespace-only instanceRefs must not crash
    [Fact]
    public async Task WhitespaceInstanceRefs_DoesNotCrash()
    {
        var svc = MakeService();
        var plmXml = await svc.ParseAsync(F("whitespace_instance_refs.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml); // must not throw
        Assert.Empty(leaves);
    }

    // two disconnected trees -> heuristic only picks first orphan
    [Fact]
    public async Task MultiRoot_HeuristicPicksFirstOrphan_SecondTreeIgnored()
    {
        var svc = MakeService(RootSearchStrategy.Auto);
        var plmXml = await svc.ParseAsync(F("multi_root.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml);

        Assert.Single(leaves);
        Assert.Equal("Part1", leaves[0].Name);
    }

    // no instances at all -> flat fallback collects all views with geometry
    [Fact]
    public async Task FlatFallback_CollectsAllViewsWithGeometry()
    {
        var svc = MakeService(RootSearchStrategy.Auto);
        var plmXml = await svc.ParseAsync(F("flat_fallback.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml);

        Assert.Equal(2, leaves.Count); // v1 and v2 have geometry; empty assembly does not
        Assert.All(leaves, l => Assert.Equal("FLAT_FALLBACK", l.PartNumbers));
    }

    // part numbers in UserData propagate down to leaves
    [Fact]
    public async Task PartNumbers_PropagateFromAssemblyToLeaf()
    {
        var svc = MakeService();
        var plmXml = await svc.ParseAsync(F("part_numbers.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml);

        Assert.Single(leaves);
        Assert.Contains("PN-001", leaves[0].PartNumbers);
    }

    // refs without # prefix must resolve the same as with #
    [Fact]
    public async Task NoHashRefs_ResolvesCorrectly()
    {
        var svc = MakeService();
        var plmXml = await svc.ParseAsync(F("no_hash_refs.plmxml"));
        Assert.NotNull(plmXml);
        var leaves = await svc.TraverseTreeAsync(plmXml);

        Assert.Single(leaves);
        Assert.Equal("NoHash_Leaf", leaves[0].Name);
    }

    // missing file returns null, not an exception
    [Fact]
    public async Task ParseAsync_MissingFile_ReturnsNull()
    {
        var result = await MakeService().ParseAsync(F("does_not_exist.plmxml"));
        Assert.Null(result);
    }

    private class StubProfileService(ParsingProfile profile) : IProfileService
    {
        public IEnumerable<ParsingProfile> GetAllProfiles() => [profile];
        public ParsingProfile GetActiveProfile() => profile;
        public void SetActiveProfile(string name) { }
        public void SaveProfile(ParsingProfile p) { }
        public void DeleteProfile(string name) { }
    }
}
