using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeal.core.Models
{
    internal class User
    {
        //Foundation Properties
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; //Avoid null ref
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string? ProfilePicture {  get; set; } //string? for optional picture upload...
        public DateTime CreatedAt { get; set; }
    }
}
