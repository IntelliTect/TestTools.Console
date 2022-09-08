namespace IntelliTect.TestTools.Console.Tests;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    [DataRow("ThisIsATest", "ThisIsATest")]
    [DataRow("ThisIsATest", "This*Test")]
    [DataRow("ThisIsTestNumber 3", "ThisIsTestNumber* 3")]
    [DataRow("ThisIsATest", "ThisIsTestNumber* 3")]
    public void IsLike_GivenLikeString_ReturnsTrue(string @string, string isLike)
    {
        Assert.IsTrue(@string.IsLike(isLike));
    }

    [TestMethod]
    [DataRow("*3")]
    [DataRow("* 3")]
    public void IsLike_GivenLikeStringWithSpaces_ReturnsTrue(string isLike)
    {
        const string output = @"*  3";

        Assert.IsTrue(output.IsLike(isLike));
    }

    [TestMethod]
    public void IsLike_GivenLikeStringWithEscape_ReturnsTrue()
    {
        const string output = @"*3";

        Assert.IsTrue(output.IsLike("\\*3", '\\'));
    }

    [TestMethod]
    public void IsLike_GivenLikeStringWithOverrideEscape_ReturnsTrue()
    {
        const string output = @"*3";

        Assert.IsTrue(output.IsLike("`*3", '`'));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void IsLike_GivenInvalideEscapeCharacter_Throws()
    {
        const string output = @"*3";

        Assert.IsTrue(output.IsLike(@"\3", '\\'));
    }
}