using System.Security.Cryptography;
using System.Text;

namespace NexsusEcommerce.Utilities
{
	public class PasswordHasher
	{
		private readonly string _salt = "a_secure_salt"; 

		public string HashPassword(string password)
		{
			using (var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(_salt)))
			{
				var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(password));
				return Convert.ToBase64String(hash);
			}
		}

		public bool VerifyHashedPassword(string hashedPassword, string password)
		{
			var hashedPasswordToCheck = HashPassword(password);
			return hashedPassword == hashedPasswordToCheck;
		}
	}
}
