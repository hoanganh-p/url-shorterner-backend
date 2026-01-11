using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using UrlShortener.Api.Options;
using UrlShortener.Api.Repositories;
using UrlShortener.Api.Services;
using UrlShortener.Api.Services.Interfaces;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

//JWT
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>();

if (jwtOptions is null)
{
    Console.WriteLine("CRITICAL ERROR: JwtOptions configuration section is missing.");
    throw new InvalidOperationException("JwtOptions configuration section is missing.");
}

if (string.IsNullOrEmpty(jwtOptions.Secret))
{
    Console.WriteLine("CRITICAL ERROR: JwtOptions.Secret is missing. The application cannot start.");
    throw new InvalidOperationException("JwtOptions.Secret is missing. Please ensure 'JwtSettings__Secret' environment variable is set correctly.");
}


// Configure AWS options
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<IDynamoDBContext>(provider =>
{
    var client = provider.GetRequiredService<IAmazonDynamoDB>();
    var config = new DynamoDBContextConfig 
    { 
        Conversion = DynamoDBEntryConversion.V2 
    };
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

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

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
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
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

// Health Checks (Important for AWS Load Balancer/Target Groups)
builder.Services.AddHealthChecks();

// Rate Limiting (Anti-Spam/DDoS protection)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // General limit: 100 requests/minute per IP
    options.AddFixedWindowLimiter("fixed", policy =>
    {
        policy.PermitLimit = 100;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 2;
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Global Error Handling (Production environment)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = exception?.Message ?? "An unexpected error occurred." });
        });
    });
    
    // HSTS: Enforce HTTPS in Production
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseCors("AllowCors");
app.UseHttpsRedirection();

// Enable Rate Limiter Middleware
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health Check Endpoint
app.MapHealthChecks("/health");

app.MapGet("/", () => "URL Shortener API Running");

app.MapGet("/{code}", async (string code, IUrlService urlService, ILogger<Program> logger) =>
{
    logger.LogInformation("Redirect request received for code: {Code}", code);

    var data = await urlService.GetAsync(code);
    if (data == null) return Results.NotFound();
    
    // Fire-and-forget increment for faster redirect (consider trade-offs)
    // Or keep await to ensure data consistency
    await urlService.IncrementClicksAsync(code);
    
    return Results.Redirect(data.OriginalUrl);
})
.RequireRateLimiting("fixed"); // Apply Rate Limit to the redirect endpoint

app.Run();
