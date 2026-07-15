using System;
using SGE.Aplicacion.Interfaces;
using SGE.Aplicacion.ExceptionApp;

namespace SGE.Aplicacion.Usuarios;

public class EliminarUsuarioUseCase(IUsuarioRepository repo, IUnidadDeTrabajo uow)
{
    // Recibe el ID del ejecutor (desde el token JWT) y el Request corporativo
    public EliminarUsuarioResponse Ejecutar(Guid ejecutorId, EliminarUsuarioRequest request)
    {
        
        var ejecutor = repo.ObtenerPorId(ejecutorId);
        if (ejecutor == null || !ejecutor.EsAdministrador)
            throw new AutorizacionException("Acción denegada: Se requieren privilegios de Administrador.");

        // Lógica de Persistencia en Memoria
        var usuarioAEliminar = repo.ObtenerPorId(request.UsuarioAEliminarId);
        if (usuarioAEliminar == null)
            throw new Exception("El usuario a eliminar no existe.");
            //Modificar a AutorizacionException("El usuario a eliminar no existe.");

        repo.Eliminar(request.UsuarioAEliminarId);
        
        // Guardado atómico --> unit of work
        uow.Guardar(); 

        return new EliminarUsuarioResponse(true, "Usuario eliminado correctamente del sistema.");
    }
}