using Amazon.DynamoDBv2;
using Amazon.Runtime;
using UrlShortener.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure AWS options
var awsOptions = builder.Configuration.GetAWSOptions();

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

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowAll");

app.MapControllers();

app.MapGet("/", () => "URL Shortener API Running");

app.MapGet("/{code}", async (string code, IUrlService urlService) =>
{
    var data = await urlService.GetAsync(code);
    if (data == null) return Results.NotFound();
    return Results.Redirect(data.OriginalUrl);
});


app.Run();
