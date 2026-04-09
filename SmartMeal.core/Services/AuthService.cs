using SmartMeal.core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace SmartMeal.core.Services
{

    public class AuthService
    {
        private static readonly List<User> users = new();
        public (bool Success, string Message) RegisterUser(string name, string email, string password,string confirmPassword)
        {
            if(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                  return (false, "Please fill all the fields");
            }
            if(!email.Contains("@") || !email.Contains("."))
            {
                return (false, "Invalid Format!!");
            }
            if(password != confirmPassword)
            {
                return (false, "Passwords do not match");
            }
            foreach(var i in users)
            {
                if(i.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Email already exists");
                }
            }
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = "User",
                CreatedAt = DateTime.Now
            };
            users.Add(user);
            return (true, "Registration successful");
        }
        public(bool Success, string Message,User? User) LoginUser(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Please fill all the requirements", null);
            }
            User? user = null;
            foreach(var i in users)
            {
                if(i.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    user = i;
                    break;
                }
            }
            if(user == null)
            {
                return (false, "User not found", null);
            }
            var hashedPassword = HashPassword(password);
            if(user.PasswordHash != hashedPassword)
            {
                return (false, "Incorrect password", null);
            }
            return (true, "Login successful", user);
        }
        private string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
