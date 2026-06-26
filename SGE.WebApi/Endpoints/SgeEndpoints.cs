using Microsoft.AspNetCore.Mvc;
using SGE.Aplicacion.Interfaces;
using SGE.Aplicacion.Expedientes;
using SGE.Aplicacion.Tramites;
using SGE.Aplicacion.Autorizacion;
using SGE.Aplicacion.Usuarios;
using SGE.Dominio.Expedientes;
using SGE.Dominio.Tramites;
using SGE.Dominio.Usuarios;
using SGE.WebApi.Services;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace SGE.WebApi.Endpoints;

public static class SgeEndpoints
{
    public static void MapSgeEndpoints(this IEndpointRouteBuilder app)
    {
        // ==========================================
        // GRUPO: AUTENTICACIÓN
        // ==========================================
        var authGrupo = app.MapGroup("/api/auth").WithTags("Autenticación");

        // Login (sin token requerido)
        authGrupo.MapPost("/login", (
            [FromBody] LoginApiInput request,
            IUsuarioRepository repoUsuario,
            IPasswordHasher hasher,
            ITokenService tokenService) =>
        {
            var usuario = repoUsuario.ObtenerPorCorreo(request.CorreoElectronico);

            if (usuario == null || !hasher.VerificarHash(request.Contrasena, usuario.ContrasenaHash))
            {
                return Results.Problem(detail: "Credenciales inválidas.", statusCode: 401, title: "No Autorizado");
            }

            var token = tokenService.GenerarToken(usuario);
            return Results.Ok(new { Token = token, Usuario = usuario.Nombre });
        })
        .WithName("Login");

        // Registro abierto (sin token requerido)
        authGrupo.MapPost("/registro", (
            [FromBody] RegistrarUsuarioApiInput request,
            RegistrarUsuarioUseCase usoDeCaso) =>
        {
            var appRequest = new RegistrarUsuarioRequest(request.Nombre, request.CorreoElectronico, request.Contrasena);
            var response = usoDeCaso.Ejecutar(appRequest);
            return Results.Ok(response);
        })
        .WithName("RegistrarUsuario");


        // ==========================================
        // GRUPO: USUARIOS (RUTAS PROTEGIDAS)
        // ==========================================
        var usuariosGrupo = app.MapGroup("/api/usuarios").RequireAuthorization().WithTags("Usuarios");

        // Modificar mis propios datos (cualquier usuario autenticado)
        usuariosGrupo.MapPut("/mis-datos", (
            [FromBody] ModificarDatosApiInput request,
            ModificarMisDatosUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var usuarioLogueadoId = ObtenerUsuarioId(user);
            var appRequest = new ModificarDatosRequest(request.UsuarioId, request.Nombre, request.CorreoElectronico, request.NuevaContrasena);
            var response = usoDeCaso.Ejecutar(usuarioLogueadoId, appRequest);
            return Results.Ok(response);
        })
        .WithName("ModificarMisDatos");

        // Listar todos los usuarios (solo admin)
        usuariosGrupo.MapGet("/", (
            ListarUsuariosUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var ejecutorId = ObtenerUsuarioId(user);
            var response = usoDeCaso.Ejecutar(ejecutorId, new ListarUsuariosRequest());
            return Results.Ok(response);
        })
        .WithName("ListarUsuarios");

        // Eliminar usuario (solo admin)
        usuariosGrupo.MapDelete("/{id:guid}", (
            Guid id,
            EliminarUsuarioUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var ejecutorId = ObtenerUsuarioId(user);
            var appRequest = new EliminarUsuarioRequest(id);
            var response = usoDeCaso.Ejecutar(ejecutorId, appRequest);
            return Results.Ok(response);
        })
        .WithName("EliminarUsuario");

        // Modificar permisos de un usuario (solo admin)
        usuariosGrupo.MapPatch("/{id:guid}/permisos", (
            Guid id,
            [FromBody] ModificarPermisosApiInput request,
            ModificarPermisosUsuarioUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var ejecutorId = ObtenerUsuarioId(user);
            var appRequest = new ModificarPermisosRequest(id, request.Permiso, request.Asignar);
            var response = usoDeCaso.Ejecutar(ejecutorId, appRequest);
            return Results.Ok(response);
        })
        .WithName("ModificarPermisosUsuario");


        // ==========================================
        // GRUPO: EXPEDIENTES (RUTAS PROTEGIDAS)
        // ==========================================
        var expedientesGrupo = app.MapGroup("/api/expedientes").RequireAuthorization().WithTags("Expedientes");

        // Alta de Expediente
        expedientesGrupo.MapPost("/", (
            [FromBody] CrearExpedienteApiInput apiRequest,
            AltaExpedienteUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var usuarioId = ObtenerUsuarioId(user);
            var appRequest = new AltaExpedienteRequest(apiRequest.Caratula, usuarioId);
            var response = usoDeCaso.Ejecutar(appRequest);
            return Results.Created($"/api/expedientes/{response.Id}", response);
        });

        // Consulta de Todos los Expedientes
        expedientesGrupo.MapGet("/", (ListarExpedientesUseCase usoDeCaso) =>
        {
            var response = usoDeCaso.Ejecutar(new ListarExpedientesRequest());
            return Results.Ok(response);
        });

        // Modificación de Expediente
        expedientesGrupo.MapPut("/{id:guid}", (
            Guid id,
            [FromBody] ModificarExpedienteApiInput apiRequest,
            ModificarCaratulaExpedienteUseCase usoCaratula,
            CambiarEstadoExpedienteUseCase usoEstado,
            ClaimsPrincipal user) =>
        {
            var usuarioId = ObtenerUsuarioId(user);

            var requestCaratula = new ModificarCaratulaRequest(id, apiRequest.Caratula, usuarioId);
            usoCaratula.Ejecutar(requestCaratula);

            var requestEstado = new CambiarEstadoRequest(id, apiRequest.Estado, usuarioId);
            usoEstado.Ejecutar(requestEstado);

            return Results.NoContent();
        });

        // Baja de Expediente
        expedientesGrupo.MapDelete("/{id:guid}", (
            Guid id,
            BajaExpedienteUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var usuarioId = ObtenerUsuarioId(user);
            var appRequest = new BajaExpedienteRequest(id, usuarioId);
            usoDeCaso.Ejecutar(appRequest);
            return Results.NoContent();
        });


        // ==========================================
        // GRUPO: TRÁMITES (RUTAS PROTEGIDAS)
        // ==========================================
        var tramitesGrupo = app.MapGroup("/api/tramites").RequireAuthorization().WithTags("Trámites");

        // Alta de Trámite
        tramitesGrupo.MapPost("/", (
            [FromBody] CrearTramiteApiInput apiRequest,
            AltaTramiteUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var usuarioId = ObtenerUsuarioId(user);
            var appRequest = new AltaTramiteRequest(
                apiRequest.ExpedienteId,
                apiRequest.Etiqueta,
                apiRequest.Contenido,
                usuarioId
            );
            var response = usoDeCaso.Ejecutar(appRequest);
            return Results.Created(string.Empty, response);
        });

        // Consulta de Trámites por ID de Expediente
        tramitesGrupo.MapGet("/expediente/{expedienteId:guid}", (Guid expedienteId, ListarTramitesPorExpedienteUseCase usoDeCaso) =>
        {
            var appRequest = new ListarTramitesRequest(expedienteId);
            var response = usoDeCaso.Ejecutar(appRequest);
            return Results.Ok(response);
        });

        // Modificación de Trámite
        tramitesGrupo.MapPut("/{id:guid}", (
            Guid id,
            [FromBody] ModificarTramiteApiInput apiRequest,
            ModificarTramiteUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var usuarioId = ObtenerUsuarioId(user);
            var appRequest = new ModificarTramiteRequest(id, apiRequest.Contenido, usuarioId);
            usoDeCaso.Ejecutar(appRequest);
            return Results.NoContent();
        });

        // Baja de Trámite
        tramitesGrupo.MapDelete("/{id:guid}", (
            Guid id,
            BajaTramiteUseCase usoDeCaso,
            ClaimsPrincipal user) =>
        {
            var usuarioId = ObtenerUsuarioId(user);
            var appRequest = new BajaTramiteRequest(id, usuarioId);
            usoDeCaso.Ejecutar(appRequest);
            return Results.NoContent();
        });
    }

    private static Guid ObtenerUsuarioId(ClaimsPrincipal user)
    {
        var claimId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claimId))
            throw new AutorizacionException("Token inválido: Identidad ausente.");

        return Guid.Parse(claimId);
    }
}

// ==========================================
// DTOs de entrada de la capa HTTP
// ==========================================
public record LoginApiInput(
    [property: JsonPropertyName("correoElectronico")] string CorreoElectronico,
    [property: JsonPropertyName("contrasena")] string Contrasena
);

public record RegistrarUsuarioApiInput(
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("correoElectronico")] string CorreoElectronico,
    [property: JsonPropertyName("contrasena")] string Contrasena
);

public record ModificarDatosApiInput(
    [property: JsonPropertyName("usuarioId")] Guid UsuarioId,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("correoElectronico")] string CorreoElectronico,
    [property: JsonPropertyName("nuevaContrasena")] string NuevaContrasena
);

public record ModificarPermisosApiInput(
    [property: JsonPropertyName("permiso")] Permiso Permiso,
    [property: JsonPropertyName("asignar")] bool Asignar
);

public record CrearExpedienteApiInput(string Caratula);
public record ModificarExpedienteApiInput(string Caratula, EstadoExpediente Estado);
public record CrearTramiteApiInput(Guid ExpedienteId, EtiquetaTramite Etiqueta, string Contenido);
public record ModificarTramiteApiInput(EtiquetaTramite Etiqueta, string Contenido);
