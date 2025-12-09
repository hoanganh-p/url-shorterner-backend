using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure AWS options from configuration (appsettings or environment)
var awsOptions = builder.Configuration.GetAWSOptions();

// If keys are present in configuration (not recommended for production), use them
var accessKey = builder.Configuration.GetValue<string>("AWS:AccessKeyId");
var secretKey = builder.Configuration.GetValue<string>("AWS:SecretAccessKey");
if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
{
    awsOptions.Credentials = new BasicAWSCredentials(accessKey, secretKey);
}

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonDynamoDB>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IUrlService, UrlService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "URL Shortener API Running");

app.Run();
