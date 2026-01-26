using BookStore.Data;
using BookStore.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> ObtenerUsuarios()
        {
            var response = await _context.Usuarios.Select(u => new UsuarioResponseDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Email = u.Email,
                Rol = u.Rol
            }).ToListAsync();

            return Ok(response);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> ActualizarUsuario(int id, UsuarioUpdateDto updateDto)
        {
            var idUsuarioLogueado = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rolUsuarioLogueado = User.FindFirst(ClaimTypes.Role)?.Value;

            if (idUsuarioLogueado != id.ToString() && rolUsuarioLogueado != "Admin")
            {
                return Forbid(); // Acceso denegado
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            if (!string.IsNullOrEmpty(updateDto.Nombre))
                usuario.Nombre = updateDto.Nombre;

            if (!string.IsNullOrEmpty(updateDto.Email))
            {
                var emailOcupado = await _context.Usuarios.AnyAsync(u => u.Email == updateDto.Email && u.Id != id);
                if (emailOcupado) return BadRequest("Ese correo ya está en uso.");

                usuario.Email = updateDto.Email;
            }

            if (!string.IsNullOrEmpty(updateDto.Password))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateDto.Password);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var idUsuarioLogueado = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rolUsuarioLogueado = User.FindFirst(ClaimTypes.Role)?.Value;

            if (idUsuarioLogueado != id.ToString() && rolUsuarioLogueado != "Admin")
            {
                return Forbid();
            }

            var tienePedidos = await _context.Pedidos.AnyAsync(p => p.UsuarioId == id);
            if (tienePedidos)
            {
                return BadRequest("No puedes eliminar este usuario porque tiene historial de compras.");
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}