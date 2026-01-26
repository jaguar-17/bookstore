using BookStore.Data;
using BookStore.DTOs;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BookStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(ApplicationDbContext context, IConfiguration config) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IConfiguration _config = config;

        [HttpPost("registrar")]
        [AllowAnonymous]
        public async Task<ActionResult> RegistrarUsuario(RegisterDto registerDto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == registerDto.Email))
            {
                return BadRequest("El correo ya está registrado.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var nuevoUsuario = new Usuario
            {
                Nombre = registerDto.Nombre,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                Rol = "Cliente"
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // GENERAR TOKEN AUTOMÁTICAMENTE DE ESTA MANERA YA NO TIENE QUE HACER LOGIN
            var tokenString = GenerarTokenJwt(nuevoUsuario);

            return Ok(new
            {
                token = tokenString,
                usuarioId = nuevoUsuario.Id,
                nombre = nuevoUsuario.Nombre,
                rol = nuevoUsuario.Rol
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginDto loginDto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.PasswordHash))
            {
                return Unauthorized("Usuario o contraseña incorrectos");
            }

            var tokenString = GenerarTokenJwt(usuario);

            return Ok(new
            {
                token = tokenString,
                usuarioId = usuario.Id,
                nombre = usuario.Nombre,
                rol = usuario.Rol
            });
        }

        private string GenerarTokenJwt(Usuario usuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}