using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Models
{
    public class Libro
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Descripcion { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Autor { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        public int Stock { get; set; }

        // --- Propiedades para Cloudinary ---
        [MaxLength(500)]
        public string ImagenUrl { get; set; } = string.Empty; // La URL pública

        [MaxLength(500)]
        public string? ImagenPublicId { get; set; } // El ID para borrarla después si hace falta

        // --- Relaciones ---
        public int CategoriaId { get; set; }

        public Categoria? Categoria { get; set; }
    }
}
