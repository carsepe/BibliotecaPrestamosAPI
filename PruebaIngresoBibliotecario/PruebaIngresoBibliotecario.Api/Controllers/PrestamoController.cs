using Microsoft.AspNetCore.Mvc;
using PruebaIngresoBibliotecario.Api.Models;
using PruebaIngresoBibliotecario.Infrastructure;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace PruebaIngresoBibliotecario.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrestamoController : ControllerBase
    {
        private readonly PersistenceContext _context;

        public PrestamoController(PersistenceContext context)
        {
            _context = context;
        }

        private bool ExistePrestamo(string identificacionUsuario)
        {
            return _context.Prestamos.Any(p => p.IdentificacionUsuario == identificacionUsuario);
        }

        public enum TipoUsuario
        {
            Afiliado = 1,
            Empleado = 2,
            Invitado = 3
        }

        [HttpPost]
        public async Task<IActionResult> CrearPrestamo(Prestamo prestamo)
        {

            bool usuarioInvitadoConPrestamo = ExistePrestamo(prestamo.IdentificacionUsuario);
            if (usuarioInvitadoConPrestamo)
            {
                // mensaje cuando el usuario tiene el libro prestado
                return BadRequest(new { message = $"El usuario con identificacion {prestamo.IdentificacionUsuario} ya tiene un libro prestado por lo cual no se le puede realizar otro préstamo" });

            }
            else
            {
                if ((TipoUsuario)prestamo.TipoUsuario == TipoUsuario.Invitado)
                {
                    prestamo.FechaMaximaDevolucion = CalcularFechaMaximaDevolucion(TipoUsuario.Invitado);
                }
                else if ((TipoUsuario)prestamo.TipoUsuario == TipoUsuario.Afiliado)
                {
                    prestamo.FechaMaximaDevolucion = CalcularFechaMaximaDevolucion(TipoUsuario.Afiliado);
                }
                else if ((TipoUsuario)prestamo.TipoUsuario == TipoUsuario.Empleado)
                {
                    prestamo.FechaMaximaDevolucion = CalcularFechaMaximaDevolucion(TipoUsuario.Empleado);
                }
                else
                {
                    return BadRequest(new { message = "Tipo de usuario no válido." });
                }
            }
            string fechaDevolucionFormateada = prestamo.FechaMaximaDevolucion.ToString("dd/MM/yyyy");

            try
            {
                // si no hay error se agrega el prestamo
                _context.Prestamos.Add(prestamo);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    prestamo.Id,
                    prestamo.Isbn,
                    prestamo.IdentificacionUsuario,
                    prestamo.TipoUsuario,
                    fechaMaximaDevolucion = fechaDevolucionFormateada
                });

            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = $"Error al guardar el préstamo en la base de datos. {ex}" });
            }
        }


        [HttpGet("{idPrestamo}")]
        public async Task<IActionResult> GetPrestamo(Guid idPrestamo)
        {
            // Buscar el prestamo por su identificador (idPrestamo) en el contexto
            var prestamo = await _context.Prestamos.FindAsync(idPrestamo);

            if (prestamo == null)
            {
                return NotFound(new { mensaje = $"El prestamo con id {idPrestamo} no existe" });
            }

            string fechaDevolucionFormateada = prestamo.FechaMaximaDevolucion.ToString("dd/MM/yyyy");

            return Ok(new
            {
                prestamo.Id,
                prestamo.Isbn,
                prestamo.IdentificacionUsuario,
                prestamo.TipoUsuario,
                fechaMaximaDevolucion = fechaDevolucionFormateada
            });
        }

        private DateTime CalcularFechaMaximaDevolucion(TipoUsuario tipoUsuario)
        {
            DateTime fechaActual = DateTime.Today;

            switch (tipoUsuario)
            {
                case TipoUsuario.Afiliado:
                    fechaActual = fechaActual.AddDays(10);
                    break;
                case TipoUsuario.Empleado:
                    fechaActual = fechaActual.AddDays(8);
                    break;
                case TipoUsuario.Invitado:
                    fechaActual = fechaActual.AddDays(7);
                    break;
                default:
                    break;
            }

            // Excluir sabados y domingos si la fecha cae en esos dias
            while (fechaActual.DayOfWeek == DayOfWeek.Saturday || fechaActual.DayOfWeek == DayOfWeek.Sunday)
            {
                fechaActual = fechaActual.AddDays(1);
            }

            return fechaActual;
        }
    }
}
