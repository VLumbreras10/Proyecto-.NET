using SGE.Aplicacion.Interfaces;
using SGE.Dominio.Usuarios;
using System;
using System.Linq;

namespace SGE.Aplicacion.Usuarios;

public class ModificarMisDatosUseCase(IUsuarioRepository repo, IPasswordHasher hasher, IUnidadDeTrabajo uow)
{
    public ModificarDatosResponse Ejecutar(Guid usuarioLogueadoId, ModificarDatosRequest request)
    {
        // REGLA DE CONTROL: El UserId extraído del token debe coincidir obligatoriamente (Requisito 3.2)
        if (usuarioLogueadoId != request.UsuarioId)
            throw new Exception("Acceso denegado: No tenés permisos para modificar los datos de otro usuario.");
            //Modificar a AutorizacionException("Acceso denegado: No tenés permisos para modificar los datos de otro usuario.");

        var usuario = repo.ObtenerPorId(request.UsuarioId)
            ?? throw new Exception("Usuario no encontrado.");

        // Si decide cambiar la contraseña se calcula un nuevo hash, de lo contrario se conserva el viejo
        string nuevoHash = string.IsNullOrWhiteSpace(request.NuevaContrasena)
            ? usuario.ContrasenaHash
            : hasher.CalcularHash(request.NuevaContrasena);

        // Reconstruimos el usuario modificado aplicando el Factory Method de Dominio
        var usuarioModificado = Usuario.Reconstruir(
            usuario.Id,
            request.Nombre,
            request.CorreoElectronico,
            nuevoHash,
            usuario.EsAdministrador,
            usuario.Permisos.ToList()
        );

        // Marcar actualización en repositorio y guardar cambios de forma transaccional
        repo.Modificar(usuarioModificado);
        uow.Guardar(); 

        return new ModificarDatosResponse(true);
    }
}