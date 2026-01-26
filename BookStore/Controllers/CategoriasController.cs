using BookStore.Data;
using BookStore.DTOs;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> ObtenerCategorias()
        {
            var categoriasDto = await _context.Categorias.Select(c => new CategoriaDto
            {
                Id = c.Id,
                Nombre = c.Nombre
            }).ToListAsync();
            return Ok(categoriasDto);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoriaDto>> ObtenerCategoria(int id)
        {
            var categoriaDto = await _context.Categorias.Where(c => c.Id == id).Select(c => new CategoriaDto
            {
                Id = c.Id,
                Nombre = c.Nombre
            }).FirstOrDefaultAsync();
            if (categoriaDto == null) return NotFound("Categoria no encontrada");
            return Ok(categoriaDto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoriaDto>> CrearCategoria([FromBody] CategoriaCreacionDto categoriaDto)
        {
            var existe = await _context.Categorias.AnyAsync(c => c.Nombre == categoriaDto.Nombre);
            if (existe) return BadRequest("Ya existe una categoría con ese nombre.");
            var nuevaCategoria = new Categoria { Nombre = categoriaDto.Nombre };
            _context.Categorias.Add(nuevaCategoria);
            await _context.SaveChangesAsync();
            var categoriaRespuesta = new CategoriaDto
            {
                Id = nuevaCategoria.Id,
                Nombre = nuevaCategoria.Nombre
            };
            return CreatedAtAction(nameof(ObtenerCategoria), new { id = nuevaCategoria.Id }, categoriaRespuesta);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActualizarCategoria(int id, [FromBody] CategoriaCreacionDto categoriaDto)
        {
            var categoriaExistente = await _context.Categorias.FindAsync(id);
            if (categoriaExistente == null) return NotFound("Categoría no encontrada");
            var existeDuplicado =
                await _context.Categorias.AnyAsync(c => c.Nombre == categoriaDto.Nombre && c.Id != id);
            if (existeDuplicado) return BadRequest("Ya existe otra categoría con ese nombre.");
            categoriaExistente.Nombre = categoriaDto.Nombre;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound("Categoría no encontrada.");
            var tieneLibros = await _context.Libros.AnyAsync(l => l.CategoriaId == id);
            if (tieneLibros) return BadRequest("No puedes borrar esta categoría porque tiene libros asociados.");
            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}