using System;
using System.Security.Cryptography;
using System.Text;
using SGE.Aplicacion.Interfaces;

namespace SGE.Infraestructura.Servicios;

public class PasswordHasher : IPasswordHasher
{
    public string CalcularHash(string contrasenaEnTextoPlano)
    {
        if (string.IsNullOrWhiteSpace(contrasenaEnTextoPlano))
            throw new ArgumentException("La contraseña no puede estar vacía.");

        byte[] bytesInput = Encoding.UTF8.GetBytes(contrasenaEnTextoPlano);
        byte[] bytesHash = SHA256.HashData(bytesInput);

        return Convert.ToHexString(bytesHash); // Devuelve el hash alfanumérico de longitud fija
    }

public bool VerificarHash(string contrasenaEnTextoPlano, string hashAlmacenado)
{
    string hashInput = CalcularHash(contrasenaEnTextoPlano);
    
    /*
    // ESTO TE VA A MOSTRAR LA VERDAD EN LA CONSOLA
    //Por si lo quieren probar, esto lo usamos en consola para debugear el error que teniamos al verificar el hash de la contraseña
    Console.WriteLine($"INPUT: '{hashInput}'");
    Console.WriteLine($"STORE: '{hashAlmacenado}'");
    */
    
    return string.Equals(hashInput, hashAlmacenado, StringComparison.OrdinalIgnoreCase);
}   
}