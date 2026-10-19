using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using WorldCupTyper.ArchiveExporter;

namespace WorldCupTyper.Tests;

public class ArchiveEncryptionTests
{
    private const string Password = "Synthetic-archive-password-42!";

    [Fact]
    public void ExistingIdentityPasswordUnlocksAuthenticatedArchiveWithoutPublishingItsHash()
    {
        var identityHash = new PasswordHasher<object>().HashPassword(new object(), Password);
        var plaintext = Encoding.UTF8.GetBytes("{\"private\":\"player history\"}");
        var encrypted = ArchiveEncryption.Encrypt(identityHash, plaintext);
        Assert.Equal(plaintext, Decrypt(encrypted, Password));
        Assert.DoesNotContain(identityHash, JsonSerializer.Serialize(encrypted));
        Assert.DoesNotContain("player history", JsonSerializer.Serialize(encrypted));
        Assert.Equal("SHA-512", encrypted.Kdf.Hash);
    }

    [Fact]
    public void WrongPasswordAndModifiedCiphertextCannotUnlockTheArchive()
    {
        var identityHash = new PasswordHasher<object>().HashPassword(new object(), Password);
        var encrypted = ArchiveEncryption.Encrypt(identityHash, Encoding.UTF8.GetBytes("private"));
        Assert.ThrowsAny<CryptographicException>(() => Decrypt(encrypted, "wrong password"));
        var damaged = Convert.FromBase64String(encrypted.Ciphertext);
        damaged[0] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => Decrypt(encrypted with { Ciphertext = Convert.ToBase64String(damaged) }, Password));
    }

    [Fact]
    public void RepeatedExportsUseDifferentNonces()
    {
        var hash = new PasswordHasher<object>().HashPassword(new object(), Password);
        Assert.NotEqual(ArchiveEncryption.Encrypt(hash, [1]).Iv, ArchiveEncryption.Encrypt(hash, [1]).Iv);
    }

    private static byte[] Decrypt(PasswordEnvelope envelope, string password)
    {
        var hash = envelope.Kdf.Hash switch { "SHA-512" => HashAlgorithmName.SHA512, "SHA-256" => HashAlgorithmName.SHA256, _ => HashAlgorithmName.SHA1 };
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(envelope.Kdf.Salt), envelope.Kdf.Iterations, hash, envelope.Kdf.KeyBytes);
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("world-cup-typer.archive.v1\0").Concat(subkey).ToArray());
        var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
        var plaintext = new byte[ciphertext.Length - 16];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(Convert.FromBase64String(envelope.Iv), ciphertext.AsSpan(0, plaintext.Length), ciphertext.AsSpan(plaintext.Length), plaintext);
        return plaintext;
    }
}
