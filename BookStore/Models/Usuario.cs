using System.ComponentModel.DataAnnotations;

namespace BookStore.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Rol { get; set; } = "Cliente";
    }
}
