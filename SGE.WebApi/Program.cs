using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SGE.Aplicacion.Autorizacion;
using SGE.Aplicacion.Interfaces;
using SGE.Aplicacion.Expedientes;
using SGE.Aplicacion.Tramites;
using SGE.Aplicacion.Usuarios;
using SGE.Infraestructura.Persistencia.Sqlite;
using SGE.Infraestructura.Servicios;
using SGE.WebApi.Endpoints;
using SGE.WebApi.Middlewares;
using SGE.WebApi.Services;
using Scalar.AspNetCore;
using System.Text;
using SGE.Aplicacion.Fecha;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
//  1. REGISTRO DE SERVICIOS (INYECCIÓN DE DEPENDENCIAS)
// ==========================================

// Base de Datos (Contexto único por Request)
builder.Services.AddScoped<SgeContext>();
builder.Services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();

// Repositorios de Infraestructura
builder.Services.AddScoped<IExpedienteRepository, ExpedientesSqliteRepository>();
builder.Services.AddScoped<ITramiteRepository, TramitesSqliteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioSqliteRepository>();

// Servicios Técnicos
builder.Services.AddSingleton<IDateTimeProvider, MachineDateTimeProvider>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAutorizacionService, AutorizacionService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// Servicio interno requerido por los Casos de Uso de Trámites
builder.Services.AddScoped<ActualizacionEstadoExpedienteService>();

// Casos de Uso de Trámites
builder.Services.AddScoped<AltaTramiteUseCase>();
builder.Services.AddScoped<BajaTramiteUseCase>();
builder.Services.AddScoped<ModificarTramiteUseCase>();
builder.Services.AddScoped<ListarTramitesPorExpedienteUseCase>();

// Casos de Uso de Expedientes
builder.Services.AddScoped<AltaExpedienteUseCase>();
builder.Services.AddScoped<BajaExpedienteUseCase>();
builder.Services.AddScoped<ModificarCaratulaExpedienteUseCase>();
builder.Services.AddScoped<CambiarEstadoExpedienteUseCase>();
builder.Services.AddScoped<ListarExpedientesUseCase>();

// Casos de Uso de Usuarios
builder.Services.AddScoped<RegistrarUsuarioUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<ModificarMisDatosUseCase>();
builder.Services.AddScoped<ListarUsuariosUseCase>();
builder.Services.AddScoped<EliminarUsuarioUseCase>();
builder.Services.AddScoped<ModificarPermisosUsuarioUseCase>();

// Middleware del Manejador de Excepciones Global (ProblemDetails)
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddProblemDetails();

// ==========================================
// 2. CONFIGURACIÓN DE AUTENTICACIÓN JWT ESTRICTA
// ==========================================
var key = Encoding.UTF8.GetBytes(TokenService.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true, // Validación estricta del tiempo de expiración
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// OpenAPI básica para el renderizador
builder.Services.AddOpenApi();

var app = builder.Build();

// ==========================================
// 3. INICIALIZACIÓN AUTOMÁTICA DE SQLITE
// ==========================================
using (var scope = app.Services.CreateScope())
{
    // Provocamos la instanciación inicial para que corra el constructor con EnsureCreated y journal_mode=DELETE
    var context = scope.ServiceProvider.GetRequiredService<SgeContext>();
}

// ==========================================
// 4. PIPELINE DE MIDDLEWARES (ORDEN DE CÁTEDRA)
// ==========================================

app.UseExceptionHandler(); // Captura los errores globales primero de todo

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Interfaz interactiva de Scalar
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Sistema de Gestión de Expedientes (SGE)")
               .WithTheme(ScalarTheme.DeepSpace);
    });
}

app.UseAuthentication(); // 1° ¿Quién sos?
app.UseAuthorization();  // 2° ¿Tenés permiso?

// Mapeamos los Endpoints Minimales de SgeEndpoints.cs
app.MapSgeEndpoints();

app.Run();
