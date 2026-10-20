using RaceDay.Api.Helpers;

namespace RaceDay.Api.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_DoesNotContainTheOriginalPassword()
    {
        string hash = PasswordHasher.Hash("Password123!");

        Assert.NotEqual("Password123!", hash);
        Assert.DoesNotContain("Password123!", hash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        string first = PasswordHasher.Hash("Password123!");
        string second = PasswordHasher.Hash("Password123!");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        string hash = PasswordHasher.Hash("Password123!");

        Assert.True(PasswordHasher.Verify("Password123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        string hash = PasswordHasher.Hash("Password123!");

        Assert.False(PasswordHasher.Verify("password123!", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("HASH_Organiser01")]
    [InlineData("a.b.c")]
    [InlineData("0.AAAA.AAAA")]
    public void Verify_BadlyFormattedHash_ReturnsFalse(string storedHash)
    {
        Assert.False(PasswordHasher.Verify("Password123!", storedHash));
    }
}