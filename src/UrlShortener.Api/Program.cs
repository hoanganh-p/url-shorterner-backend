using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UrlShortener.Api.Models;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;
using UrlShortener.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

//JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() 
    ?? throw new InvalidOperationException("JwtSettings is not configured properly");
builder.Services.AddSingleton(jwtSettings);


// Configure AWS options
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<IDynamoDBContext>(provider =>
{
    var client = provider.GetRequiredService<IAmazonDynamoDB>();
    var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
#pragma warning disable CS0618 // Type or member is obsolete
    return new DynamoDBContext(client, config);
#pragma warning restore CS0618 // Type or member is obsolete
});
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddScoped<IUrlService, UrlService>();

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCors",
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins("https://d2wnt86s7rdbuv.cloudfront.net")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowCors");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "URL Shortener API Running");

app.MapGet("/{code}", async (string code, IUrlService urlService) =>
{
    var data = await urlService.GetAsync(code);
    if (data == null) return Results.NotFound();
    return Results.Redirect(data.OriginalUrl);
});

app.Run();
