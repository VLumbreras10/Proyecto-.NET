using System;
using SGE.Aplicacion.Autorizacion;
using SGE.Aplicacion.Interfaces;
using SGE.Dominio.Usuarios;

namespace SGE.Aplicacion.Usuarios;

public class ModificarPermisosUsuarioUseCase(IUsuarioRepository repo, IUnidadDeTrabajo uow)
{
    public ModificarPermisosResponse Ejecutar(Guid ejecutorId, ModificarPermisosRequest request)
    {
        // Solo el administrador puede modificar permisos de otros usuarios
        var ejecutor = repo.ObtenerPorId(ejecutorId);
        if (ejecutor == null || !ejecutor.EsAdministrador)
            throw new AutorizacionException("Acción denegada: Se requieren privilegios de Administrador.");

        // Obtener el usuario objetivo
        var usuario = repo.ObtenerPorId(request.UsuarioObjetivoId)
            ?? throw new Exception("El usuario objetivo no existe.");

        // Asignar o revocar el permiso usando los métodos del Dominio
        if (request.Asignar)
            usuario.AsignarPermiso(request.Permiso);
        else
            usuario.RevocarPermiso(request.Permiso);

        repo.Modificar(usuario);
        uow.Guardar();

        return new ModificarPermisosResponse(true);
    }
}
