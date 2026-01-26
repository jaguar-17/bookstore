using System.ComponentModel.DataAnnotations;

namespace BookStore.DTOs
{
    public class LibroCreacionDto
    {
        [Required]
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int CategoriaId { get; set; }
        public IFormFile? Foto { get; set; }
    }
}
