using System.Text;
using Azure;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

var builder = WebApplication.CreateBuilder(args);

// Add appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure SQL Database context
var sqlConnectionString = builder.Configuration["SqlDBConnectionString"];
builder.Services.AddDbContext<ExampleDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// Configure Azure Blob Service Client
var blobServiceClient = new BlobServiceClient(builder.Configuration["StorageAccountConnectionString"]);
builder.Services.AddSingleton(blobServiceClient);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapPost("/demo-endpoint", async () =>
    {
        var containerClient = blobServiceClient.GetBlobContainerClient("demo-container");

        // Check if the container exists, if not create it
        try
        {
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerAlreadyExists)
        {
            // Container already exists, no need to do anything
        }

        // Create a blob client
        var blobClient = containerClient.GetBlobClient("demo-blob");

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