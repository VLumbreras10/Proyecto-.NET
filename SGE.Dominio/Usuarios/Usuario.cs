using System;
using System.Collections.Generic;

namespace SGE.Dominio.Usuarios;

public class Usuario
{
    // Colección interna privada para evitar modificaciones directas desde afuera
    private readonly List<Permiso> _permisos = new();

    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public string CorreoElectronico { get; private set; }
    public string ContrasenaHash { get; private set; }
    public bool EsAdministrador { get; private set; }
    
    // Exponemos los permisos como de solo lectura para mantener el encapsulamiento
    public IReadOnlyCollection<Permiso> Permisos => _permisos.AsReadOnly();

    // Constructor de Negocio (Para cuando se registra un usuario nuevo)
    public Usuario(string nombre, string correoElectronico, string contrasenaHash, bool esAdministrador)
    {
        Validar(nombre, correoElectronico, contrasenaHash);

        Id = Guid.NewGuid();
        Nombre = nombre;
        CorreoElectronico = correoElectronico;
        ContrasenaHash = contrasenaHash;
        EsAdministrador = esAdministrador;
    }

    // Constructor privado exclusivo para el Factory Method de reconstrucción
    private Usuario(Guid id, string nombre, string correoElectronico, string contrasenaHash, bool esAdministrador, List<Permiso> permisos)
    {
        Id = id;
        Nombre = nombre;
        CorreoElectronico = correoElectronico;
        ContrasenaHash = contrasenaHash;
        EsAdministrador = esAdministrador;
        _permisos = permisos;
    }

    // FACTORY METHOD: Para cuando EF Core traiga al usuario desde SQLite
    public static Usuario Reconstruir(Guid id, string nombre, string correoElectronico, string contrasenaHash, bool esAdministrador, List<Permiso> permisos)
    {
        return new Usuario(id, nombre, correoElectronico, contrasenaHash, esAdministrador, permisos ?? new List<Permiso>());
    }

    // Métodos públicos obligatorios exigidos por la consigna para gestionar permisos de forma segura
    public void AsignarPermiso(Permiso permiso)
    {
        if (!_permisos.Contains(permiso))
        {
            _permisos.Add(permiso);
        }
    }

    public void RevocarPermiso(Permiso permiso)
    {
        if (_permisos.Contains(permiso))
        {
            _permisos.Remove(permiso);
        }
    }

    // Validación de Campos (Todos los datos son obligatorios)
    private static void Validar(string nombre, string correoElectronico, string contrasenaHash)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del usuario es obligatorio.");

        if (string.IsNullOrWhiteSpace(correoElectronico))
            throw new ArgumentException("El correo electrónico es obligatorio.");
            
        if (!correoElectronico.Contains("@"))
            throw new ArgumentException("El formato del correo electrónico es inválido.");

        if (string.IsNullOrWhiteSpace(contrasenaHash))
            throw new ArgumentException("La contraseña cifrada (hash) es obligatoria.");
    }
}