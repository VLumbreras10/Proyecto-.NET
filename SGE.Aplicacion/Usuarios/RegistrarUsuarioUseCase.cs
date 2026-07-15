using SGE.Aplicacion.Interfaces;
using SGE.Dominio.Usuarios;
using System;

namespace SGE.Aplicacion.Usuarios;

public class RegistrarUsuarioUseCase(IUsuarioRepository repo, IPasswordHasher hasher, IUnidadDeTrabajo uow)
{
    public RegistrarUsuarioResponse Ejecutar(RegistrarUsuarioRequest request)
    {
        // Validación de correo ya registrado (Requisito 3.2)
        var usuarioExistente = repo.ObtenerPorCorreo(request.CorreoElectronico);
        if (usuarioExistente != null)
            throw new Exception("El correo electrónico ya se encuentra registrado.");
            //aca deberia ser AutorizacionException("El correo electrónico ya se encuentra registrado.");

        // Cifrado obligatorio de contraseña mediante hash
        string hash = hasher.CalcularHash(request.Contrasena);

        // Instancia del Dominio (Por defecto EsAdministrador es false y nace sin permisos)
        var nuevoUsuario = new Usuario(request.Nombre, request.CorreoElectronico, hash, esAdministrador: false);

        repo.Agregar(nuevoUsuario);
        uow.Guardar(); // Regla de Oro

        return new RegistrarUsuarioResponse(true, "Usuario registrado exitosamente.");
    }
}