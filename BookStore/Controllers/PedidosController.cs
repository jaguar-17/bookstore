using BookStore.Data;
using BookStore.DTOs;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<Pedido>> ObtenerPedido(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles) // Esto traer los detalles
                .ThenInclude(d => d.Libro) // Esto traer info del libro
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != pedido.UsuarioId.ToString() && userRole != "Admin")
            {
                return Forbid();
            }

            return Ok(pedido);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ObtenerTodosLosPedidos()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Libro)
                .OrderByDescending(p => p.FechaPedido)
                .Select(p => new
                {
                    Id = p.Id,
                    UsuarioId = p.UsuarioId,
                    UsuarioNombre = p.Usuario != null ? p.Usuario.Nombre : "Desconocido",
                    UsuarioEmail = p.Usuario != null ? p.Usuario.Email : "",
                    Fecha = p.FechaPedido,
                    Total = p.Total,
                    Estado = p.Estado,
                    Items = p.Detalles.Select(d => new
                    {
                        Libro = d.Libro != null ? d.Libro.Titulo : "Libro eliminado",
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.Cantidad * d.PrecioUnitario
                    })
                })
                .ToListAsync();

            return Ok(pedidos);
        }

        [HttpGet("mis-pedidos")]
        [Authorize]
        public async Task<ActionResult> ObtenerMisPedidos()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = int.Parse(userIdClaim);

            var misPedidos = await _context.Pedidos
                .Where(p => p.UsuarioId == userId)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Libro)
                .OrderByDescending(p => p.FechaPedido)
                .Select(p => new
                {
                    Id = p.Id,
                    Fecha = p.FechaPedido,
                    Total = p.Total,
                    Estado = p.Estado,
                    Items = p.Detalles.Select(d => new
                    {
                        Libro = d.Libro.Titulo,
                        Imagen = d.Libro.ImagenUrl,
                        Cantidad = d.Cantidad,
                        Precio = d.PrecioUnitario
                    })
                })
                .ToListAsync();

            return Ok(misPedidos);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CrearPedido(PedidoCreacionDto pedidoDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Token inválido");

            var usuarioIdReal = int.Parse(userIdClaim);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nuevoPedido = new Pedido
                {
                    UsuarioId = usuarioIdReal,
                    FechaPedido = DateTime.UtcNow,
                    Estado = "Pagado",
                    Total = 0
                };

                _context.Pedidos.Add(nuevoPedido);
                await _context.SaveChangesAsync();

                decimal totalCalculado = 0;

                foreach (var item in pedidoDto.Items)
                {
                    var libroEnBd = await _context.Libros.FindAsync(item.LibroId);

                    if (libroEnBd == null)
                        throw new Exception($"El libro con ID {item.LibroId} no existe.");

                    if (libroEnBd.Stock < item.Cantidad)
                        throw new Exception(
                            $"Stock insuficiente para '{libroEnBd.Titulo}'. Disponible: {libroEnBd.Stock}.");

                    var detalle = new DetallePedido
                    {
                        PedidoId = nuevoPedido.Id,
                        LibroId = item.LibroId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = libroEnBd.Precio
                    };

                    _context.DetallesPedido.Add(detalle);

                    libroEnBd.Stock -= item.Cantidad;

                    totalCalculado += (item.Cantidad * libroEnBd.Precio);
                }

                nuevoPedido.Total = totalCalculado;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction(nameof(ObtenerPedido), new { id = nuevoPedido.Id },
                    new { Mensaje = "Pedido procesado", PedidoId = nuevoPedido.Id, Total = totalCalculado });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { Mensaje = ex.Message });
            }
        }
    }
}