using API_Gestor_Minimal;
using API_Gestor_Minimal.Data;
using API_Gestor_Minimal.DTO;
using API_Gestor_Minimal.Model;
using API_Gestor_Minimal.Request;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DataContext>(op => op.UseSqlServer(builder.Configuration.GetConnectionString("conn")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/usuario", async (DataContext db) => await db.Usuarios.ToListAsync());

app.MapGet("/usuario/{id}", async (Guid id, DataContext db) =>
{
    var usuario = await db.Usuarios.FindAsync(id);
    if (usuario is Usuario)
    {
        UsuarioDTO usuarioDto = new UsuarioDTO
        {
            Nombre = usuario.Nombre,
            ApellidoP = usuario.ApellidoP,
            ApellidoM = usuario.ApellidoM,
            Email = usuario.Email,
            Rol = usuario.Rol,
            
            
        };
        return Results.Ok(usuarioDto);
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapPost("/usuario", async (UsuarioDTO usuarioDto, DataContext db) =>
{

    Utiles utiles = new Utiles();

    utiles.CrearPasswordHash(usuarioDto.Password, out byte[] passwordHash, out byte[] passwordSalt);

    Usuario usuarioDb = new Usuario();

    usuarioDb.Nombre = usuarioDto.Nombre;
    usuarioDb.ApellidoP = usuarioDto.ApellidoP;
    usuarioDb.ApellidoM = usuarioDto.ApellidoM;
    usuarioDb.Email = usuarioDto.Email;
    usuarioDb.PasswordHash = passwordHash;  
    usuarioDb.PasswordSalt = passwordSalt; 

    db.Usuarios.Add(usuarioDb);
    await db.SaveChangesAsync();

    return Results.Created($"/usuario/{usuarioDb.Id}", usuarioDb);
});

app.MapPut("/usuario/{id}", async (Guid id, UsuarioDTO usuarioDto, DataContext db) =>
{
    var usuario = await  db.Usuarios.FindAsync(id);

    if (usuario == null) return Results.NotFound();

    Utiles utiles = new Utiles();
    utiles.CrearPasswordHash(usuarioDto.Password, out byte[] passwordHash, out byte[] passwordSalt);
  
    usuario.Nombre = usuarioDto.Nombre.ToString();
    usuario.ApellidoP = usuarioDto.ApellidoP.ToString();
    usuario.ApellidoM = usuarioDto.ApellidoM.ToString();
    usuario.Email = usuarioDto.Email;
    usuario.PasswordHash = passwordHash;
    usuario.PasswordSalt = passwordSalt;
    usuario.Rol = usuarioDto.Rol;

    await db.SaveChangesAsync();

    return Results.NoContent();

});

app.MapDelete("/usuario/{id}", async (Guid id, DataContext db) =>
{
    if (await db.Usuarios.FindAsync(id) is Usuario usuario)
    {
        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.MapPost("/security/crearToken",
[AllowAnonymous] (LoginDTO login) =>
{
    if (login.Email == "login" && login.Password == "123")
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var key = Encoding.ASCII.GetBytes
        (builder.Configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, login.Email),
                new Claim(JwtRegisteredClaimNames.Email, login.Email),
                new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        var stringToken = tokenHandler.WriteToken(token);
        return Results.Ok(stringToken);
    }
    return Results.Unauthorized();
});

app.UseAuthentication();
app.UseAuthorization();

app.Run();

