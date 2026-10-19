using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace WorldCupTyper.ArchiveExporter;

public sealed record IdentityKdf(string Salt, int Iterations, string Hash, int KeyBytes);
public sealed record PasswordEnvelope(IdentityKdf Kdf, string Iv, string Ciphertext);

public static class ArchiveEncryption
{
    public static PasswordEnvelope Encrypt(string identityHash, byte[] plaintext)
    {
        var bytes = Convert.FromBase64String(identityHash);
        int iterations, saltLength, offset;
        string hash;
        if (bytes.Length == 49 && bytes[0] == 0)
        {
            (iterations, saltLength, offset, hash) = (1000, 16, 1, "SHA-1");
        }
        else if (bytes.Length >= 45 && bytes[0] == 1)
        {
            hash = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1, 4)) switch
            {
                0 => "SHA-1", 1 => "SHA-256", 2 => "SHA-512", _ => throw new FormatException("Unsupported Identity PRF.")
            };
            iterations = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4)));
            saltLength = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(9, 4)));
            offset = 13;
        }
        else throw new FormatException("Unsupported Identity hash format.");

        if (iterations is < 1000 or > 1000000 || saltLength is < 16 or > 64 || bytes.Length - offset - saltLength is < 16 or > 64)
            throw new FormatException("Invalid Identity KDF parameters.");

        var subkey = bytes.AsSpan(offset + saltLength).ToArray();
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("world-cup-typer.archive.v1\0").Concat(subkey).ToArray());
        try
        {
            var iv = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(iv, plaintext, ciphertext, tag);
            return new PasswordEnvelope(new IdentityKdf(Convert.ToBase64String(bytes, offset, saltLength), iterations, hash, subkey.Length),
                Convert.ToBase64String(iv), Convert.ToBase64String(ciphertext.Concat(tag).ToArray()));
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(subkey); CryptographicOperations.ZeroMemory(bytes); }
    }
}
