using System.ComponentModel.DataAnnotations;

namespace BookStore.DTOs
{
    public class CategoriaCreacionDto
    {
        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;
    }
}