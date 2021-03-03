using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Pro
{
    public class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit 
        private const int KeySize = 32; // 256 bit
        private int Iterations { get; set; } = 10000;

        public PasswordHasher()
        { 
        }

        public string Hash(string password)
        {
            using var algorithm = new Rfc2898DeriveBytes(
              password,
              SaltSize,
              Iterations,
              HashAlgorithmName.SHA512);
            var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
            var salt = Convert.ToBase64String(algorithm.Salt);

            return salt+key;
        }

        public bool Check(string hash, string password)
        {
            var salt = Convert.FromBase64String(hash.Remove(24));
            var key = Convert.FromBase64String(hash.Substring(24));

            using var algorithm = new Rfc2898DeriveBytes(
              password,
              salt,
              Iterations,
              HashAlgorithmName.SHA512);
            var keyToCheck = algorithm.GetBytes(KeySize);
            var verified = keyToCheck.SequenceEqual(key);

            return (verified);
        }
    }
}
