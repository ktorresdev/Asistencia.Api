using Asistencia.Data.DbContexts;
using Asistencia.Api.Jobs;
using Asistencia.Services;
using Asistencia.Services.Implements;
using Asistencia.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

// DbContext (SQL Server ejemplo, cambia la cadena seg�n tu entorno)
builder.Services.AddDbContext<MarcacionAsistenciaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RrhhConnection")));
builder.Services.AddScoped<IMarcacionAsistenciaService, MarcacionAsistenciaService>();
builder.Services.AddScoped<ISucursalCentroService, SucursalCentroService>();
builder.Services.AddScoped<ITrabajadorService, TrabajadorService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFichaTrabajadorService, FichaTrabajadorService>();
builder.Services.AddScoped<IReportesService, ReporteService>();
builder.Services.AddScoped<ITurnoService, TurnoService> ();
builder.Services.AddScoped<IHorarioTurnoService, HorarioTurnoService>();
builder.Services.AddScoped<ITipoTurnoService, TipoTurnoService>();
builder.Services.AddScoped<ICargaMasivaTrabajadoresService, CargaMasivaTrabajadoresService>();
// Add services to the container.
builder.Services.AddScoped<IAsignacionTurnoService, AsignacionTurnoService>();
builder.Services.AddScoped<IHorarioResolverService, HorarioResolverService>();
builder.Services.AddScoped<ICoberturaTurnoService, CoberturaTurnoService>();
builder.Services.AddScoped<IProgramacionDescansoService, ProgramacionDescansoService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICierreDiarioAsistenciaExecutor, CierreDiarioAsistenciaExecutor>();
builder.Services.AddHostedService<CierreDiarioAsistenciaJob>();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// CORS - pol�tica con or�genes permitidos (especificar las URLs cliente)
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins(
                "http://localhost:65344",
                "https://localhost:65344",
                "http://localhost:4200",
                "https://localhost:4200",
                "https://apirrhh.energigas.com",
                "https://rrhh.energigas.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


// Authentication JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

// Authentication: Policy scheme that routes to JwtBearer or ApiKey depending on token format
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "SmartScheme";
    options.DefaultChallengeScheme = "SmartScheme";
})
.AddPolicyScheme("SmartScheme", "JWT or ApiKey", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var auth = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(auth)) return JwtBearerDefaults.AuthenticationScheme;
        var token = auth.StartsWith("Bearer ") ? auth.Substring(7).Trim() : auth;
        // If token looks like JWT (has two dots) -> JWT handler
        if (token.Count(c => c == '.') == 2) return JwtBearerDefaults.AuthenticationScheme;
        // otherwise use ApiKey handler
        return "ApiKey";
    };
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
})
.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Asistencia.Api.AuthHandlers.ApiKeyAuthenticationHandler>("ApiKey", options => { });

// Controllers y Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserta el token en el formato: Bearer {token}",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme {
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }});
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
