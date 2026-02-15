using System.Numerics;
using PlmXmlSyntax.Helpers;

namespace PlmXmlSyntax.Tests;

public class PlmXmlHelpersTests
{
    [Fact]
    public void ParseTransform_NullOrEmpty_ReturnsIdentity()
    {
        Assert.Equal(Matrix4x4.Identity, ((string)null!).ParseTransform());
        Assert.Equal(Matrix4x4.Identity, "".ParseTransform());
        Assert.Equal(Matrix4x4.Identity, "   ".ParseTransform());
    }

    [Fact]
    public void ParseTransform_WrongElementCount_ReturnsIdentity()
    {
        Assert.Equal(Matrix4x4.Identity, "1 2 3".ParseTransform());
        Assert.Equal(Matrix4x4.Identity, "1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17".ParseTransform());
    }

    [Fact]
    public void ParseTransform_IdentityString_ReturnsIdentityMatrix()
    {
        var input = "1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1";
        var result = input.ParseTransform();
        Assert.Equal(Matrix4x4.Identity, result);
    }

    [Fact]
    public void ParseTransform_ValidValues_MapsCorrectly()
    {
        var input = "1.5 2.5 3.5 4.5 5.5 6.5 7.5 8.5 9.5 10.5 11.5 12.5 13.5 14.5 15.5 16.5";
        var result = input.ParseTransform();

        Assert.Equal(1.5f, result.M11);
        Assert.Equal(2.5f, result.M12);
        Assert.Equal(13.5f, result.M41);
        Assert.Equal(16.5f, result.M44);
    }

    [Fact]
    public void ParseTransform_HandlesWhitespaceVariants()
    {
        var input = "1\t0\n0\r\n0  0  1  0  0  0  0  1  0  0  0  0  1";
        var result = input.ParseTransform();
        Assert.Equal(Matrix4x4.Identity, result);
    }

    [Fact]
    public void ToExplicitDecimalString_Identity_RoundTrips()
    {
        var str = Matrix4x4.Identity.ToExplicitDecimalString();
        var roundTripped = str.ParseTransform();
        Assert.Equal(Matrix4x4.Identity, roundTripped);
    }

    [Fact]
    public void ToExplicitDecimalString_FormatsAllElements()
    {
        var str = Matrix4x4.Identity.ToExplicitDecimalString();
        var parts = str.Split(' ');
        Assert.Equal(16, parts.Length);
    }
}
