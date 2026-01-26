using BookStore.Data;
using BookStore.DTOs;
using BookStore.Interfaces;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibrosController(ApplicationDbContext context, IPhotoService photoService) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IPhotoService _photoService = photoService;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LibroDto>>> ObtenerLibros()
        {
            var librosDto = await _context.Libros.Select(l => new LibroDto
            {
                Id = l.Id,
                Titulo = l.Titulo,
                Autor = l.Autor,
                Precio = l.Precio,
                Stock = l.Stock,
                ImagenUrl = l.ImagenUrl,
                CategoriaNombre = l.Categoria != null ? l.Categoria.Nombre : "Sin Categoría"
            }).ToListAsync();

            return Ok(librosDto);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LibroDto>> ObtenerLibro(int id)
        {
            var libroDto = await _context.Libros.Where(l => l.Id == id).Select(l => new LibroDto
            {
                Id = l.Id,
                Titulo = l.Titulo,
                Autor = l.Autor,
                Descripcion = l.Descripcion,
                Precio = l.Precio,
                Stock = l.Stock,
                ImagenUrl = l.ImagenUrl,
                CategoriaId = l.CategoriaId,
                CategoriaNombre = l.Categoria != null ? l.Categoria.Nombre : "Sin Categoría"
            }).FirstOrDefaultAsync();
            if (libroDto == null) return NotFound("Libro no encontrado");
            return Ok(libroDto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<LibroDto>> CrearLibro([FromForm] LibroCreacionDto libroDto)
        {
            var categoria = await _context.Categorias.FindAsync(libroDto.CategoriaId);
            if (categoria == null) return BadRequest("La categoría no existe");
            var nuevoLibro = new Libro
            {
                Titulo = libroDto.Titulo,
                Descripcion = libroDto.Descripcion,
                Autor = libroDto.Autor,
                Precio = libroDto.Precio,
                Stock = libroDto.Stock,
                CategoriaId = libroDto.CategoriaId
            };
            if (libroDto.Foto != null)
            {
                var resultadoFoto = await _photoService.AddPhotoAsync(libroDto.Foto);

                if (resultadoFoto.Error != null)
                    return BadRequest(resultadoFoto.Error.Message);

                nuevoLibro.ImagenUrl = resultadoFoto.SecureUrl.AbsoluteUri;
                nuevoLibro.ImagenPublicId = resultadoFoto.PublicId;
            }

            _context.Libros.Add(nuevoLibro);
            await _context.SaveChangesAsync();
            var libroRespuesta = new LibroDto
            {
                Id = nuevoLibro.Id,
                Titulo = nuevoLibro.Titulo,
                Autor = nuevoLibro.Autor,
                Descripcion = nuevoLibro.Descripcion,
                Precio = nuevoLibro.Precio,
                Stock = nuevoLibro.Stock,
                ImagenUrl = nuevoLibro.ImagenUrl,
                CategoriaNombre = categoria.Nombre
            };

            return CreatedAtAction(nameof(ObtenerLibro), new { id = nuevoLibro.Id }, libroRespuesta);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActualizarLibro(int id, [FromForm] LibroCreacionDto libroDto)
        {
            var libroExistente = await _context.Libros.FindAsync(id);
            if (libroExistente == null) return NotFound("Libro no encontrado");
            var categoria = await _context.Categorias.FindAsync(libroDto.CategoriaId);
            if (categoria == null) return BadRequest("La categoría no existe");
            libroExistente.Titulo = libroDto.Titulo;
            libroExistente.Descripcion = libroDto.Descripcion;
            libroExistente.Autor = libroDto.Autor;
            libroExistente.Precio = libroDto.Precio;
            libroExistente.Stock = libroDto.Stock;
            libroExistente.CategoriaId = libroDto.CategoriaId;
            if (libroDto.Foto != null)
            {
                if (!string.IsNullOrEmpty(libroExistente.ImagenPublicId))
                {
                    await _photoService.DeletePhotoAsync(libroExistente.ImagenPublicId);
                }

                var resultadoFoto = await _photoService.AddPhotoAsync(libroDto.Foto);
                if (resultadoFoto.Error != null)
                    return BadRequest(resultadoFoto.Error.Message);
                libroExistente.ImagenUrl = resultadoFoto.SecureUrl.AbsoluteUri;
                libroExistente.ImagenPublicId = resultadoFoto.PublicId;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BorrarLibro(int id)
        {
            var libroExistente = await _context.Libros.FindAsync(id);
            if (libroExistente == null) return NotFound("Libro no encontrado");
            var haSidoVendido = await _context.DetallesPedido.AnyAsync(d => d.LibroId == id);
            if (haSidoVendido)
            {
                return BadRequest("No puedes eliminar este libro porque forma parte de pedidos históricos.");
            }

            if (!string.IsNullOrEmpty(libroExistente.ImagenPublicId))
            {
                await _photoService.DeletePhotoAsync(libroExistente.ImagenPublicId);
            }

            _context.Libros.Remove(libroExistente);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}