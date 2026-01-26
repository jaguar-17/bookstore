using System.ComponentModel.DataAnnotations;

namespace BookStore.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        public List<Libro>? Libros { get; set; }
    }
}
