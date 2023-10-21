using Asp.Versioning;
using Baltaio.Location.Api.Application.Addresses.Commons;
using Baltaio.Location.Api.Application.Data.Import.Commons;
using Baltaio.Location.Api.Application.Data.Import.ImportData;
using Baltaio.Location.Api.Application.Users.Abstractions;
using Baltaio.Location.Api.Application.Users.Login;
using Baltaio.Location.Api.Application.Users.Login.Abstractions;
using Baltaio.Location.Api.Application.Users.Register;
using Baltaio.Location.Api.Application.Users.Register.Abstraction;
using Baltaio.Location.Api.Contracts.Users;
using Baltaio.Location.Api.Infrastructure;
using Baltaio.Location.Api.Infrastructure.Addresses;
using Baltaio.Location.Api.Infrastructure.Users.Authentication;
using Baltaio.Location.Api.Infrastructure.Users.Persistance;
using Baltaio.Location.Api.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();
    builder.Services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddScoped<ICityRepository, CityRepository>();
    builder.Services.AddScoped<IStateRepository, StateRepository>();
    builder.Services.AddScoped<IFileRepository, FileRepository>();
    builder.Services.AddScoped<IAddressRepository, AddressRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            JwtSettings? jwtSettings = builder.Configuration.GetSection(JwtSettings.SECTION_NAME).Get<JwtSettings>();
            ArgumentNullException.ThrowIfNull(jwtSettings, nameof(jwtSettings));

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
        });
    builder.Services.AddScoped<IJwtGenerator, JwtGenerator>();
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SECTION_NAME));

    builder.Services.AddScoped<ILoginAppService, LoginAppService>();
    builder.Services.AddScoped<IRegisterUserAppService, RegisterUserAppService>();
    builder.Services.Configure<SaltSettings>(builder.Configuration.GetSection(SaltSettings.SECTION_NAME));

    builder.Services.AddScoped<IImportDataAppService, ImportDataAppService>();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen(option => option.OperationFilter<SwaggerDefaultValues>());
}

var app = builder.Build();
{
    var versionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0))
        .HasApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 1))
        .HasApiVersion(new ApiVersion(majorVersion: 2, minorVersion: 0))
        .ReportApiVersions()
        .Build();

    #region Routes

    app.MapGet("is-alive", () => Results.Ok());

    app.MapPost("api/v{version:apiVersion}/auth/register", ([FromBody] RegisterUserRequest request) => RegisterAsync(request))
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0));
    app.MapPost("api/v{version:apiVersion}/auth/login", ([FromBody] LoginRequest request) => LoginAsync(request))
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0));

    app.MapPost("api/v{version:apiVersion}/locations", () => Results.Ok())
        .RequireAuthorization()
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0));
    app.MapGet("api/v{version:apiVersion}/locations/{id}", () => Results.Ok())
        .RequireAuthorization()
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0));
    app.MapPut("api/v{version:apiVersion}/locations/{id}", () => Results.Ok())
        .RequireAuthorization()
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0));
    app.MapDelete("api/v{version:apiVersion}/locations/{id}", () => Results.Ok())
        .RequireAuthorization()
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0));
    app.MapGet("api/v{version:apiVersion}/locations", () => Results.Ok())
        .RequireAuthorization()
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0));
    app.MapPost("api/v{version:apiVersion}/locations/import-data", (IFormFile file) => ImportData(file))
        .RequireAuthorization()
        .WithApiVersionSet(versionSet)
        .MapToApiVersion(new ApiVersion(majorVersion: 1, minorVersion: 0));

    #endregion

    // Configure the HTTP request pipeline.
    //if (app.Environment.IsDevelopment())
    //{
    app.UseSwagger();
    app.UseSwaggerUI(
        options =>
        {
            var descriptions = app.DescribeApiVersions();

            // build a swagger endpoint for each discovered API version
            foreach (var description in descriptions)
            {
                var url = $"/swagger/{description.GroupName}/swagger.json";
                var name = description.GroupName.ToUpperInvariant();
                options.SwaggerEndpoint(url, name);
            }
        });
    //}

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}

async Task<IResult> RegisterAsync(RegisterUserRequest request)
{
    RegisterUserInput input = new(request.Email, request.Password);
    var registerUserAppService = app.Services.CreateScope().ServiceProvider.GetRequiredService<IRegisterUserAppService>();

    RegisterUserOutput output = await registerUserAppService.ExecuteAsync(input);

    if (!output.IsValid)
    {
        return Results.BadRequest(output.Errors);
    }

    return Results.Ok();
}
async Task<IResult> LoginAsync(LoginRequest request)
{
    LoginInput input = new(request.Email, request.Password);
    var loginAppService = app.Services.CreateScope().ServiceProvider.GetRequiredService<ILoginAppService>();

    LoginOutput output = await loginAppService.ExecuteAsync(input);

    if (!output.IsValid)
    {
        return Results.BadRequest(output.Errors);
    }

    return Results.Ok(output.Token);
}
 
async Task<IResult> ImportData(IFormFile file)
{
    var allowedContentTypes = new string[]
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };

    if (!allowedContentTypes.Contains(file.ContentType))
        return Results.BadRequest("Tipo de arquivo inv�lido.");

    var importDataAppService = app.Services.CreateScope().ServiceProvider.GetRequiredService<IImportDataAppService>();
    var importedDataOutput = await importDataAppService.Execute(file.OpenReadStream());

    return Results.Accepted(null, importedDataOutput);
}