using System.Text;
using Microsoft.EntityFrameworkCore;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration.AzureKeyVault;

var builder = WebApplication.CreateBuilder(args);

// Configuration for Azure Key Vault
var azureServiceTokenProvider = new AzureServiceTokenProvider();
var keyVaultClient =
    new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
builder.Configuration.AddAzureKeyVault($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/",
    keyVaultClient, new DefaultKeyVaultSecretManager());

// Configure SQL Database context
var sqlConnectionString = builder.Configuration["SqlConnectionString"];
builder.Services.AddDbContext<ExampleDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// Configure Azure Blob Service Client
var blobServiceClient =
    new BlobServiceClient(new Uri(builder.Configuration["BlobServiceEndpoint"]), new DefaultAzureCredential());
builder.Services.AddSingleton(blobServiceClient);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/demo-endpoint", async () =>
    {
        // Create a blob client
        var blobClient = blobServiceClient
            .GetBlobContainerClient("demo-container")
            .GetBlobClient("demo-blob");

        // Upload content to the blob
        var content = "This is a demo content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, true);

        // Create a new entity
        var entity = new FileEntity
        {
            FileName = "demo-blob",
            BlobUrl = blobClient.Uri.ToString()
        };

        // Save the entity in the database
        var dbContext = builder.Services.BuildServiceProvider().GetRequiredService<ExampleDbContext>();
        
        await dbContext.FileEntities.AddAsync(entity);
        await dbContext.SaveChangesAsync();

        return Results.Ok("File created and entity saved successfully.");
    })
    .WithName("CreateFileAndSaveEntity")
    .WithOpenApi();

app.Run();

// DbContext for SQL Database
public class ExampleDbContext : DbContext
{
    public ExampleDbContext(DbContextOptions<ExampleDbContext> options) : base(options)
    {
    }

    public DbSet<FileEntity> FileEntities { get; set; }
}

public class FileEntity
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string BlobUrl { get; set; }
}