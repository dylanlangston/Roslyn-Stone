using RoslynStone.Core.Commands;
using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Core.Queries;
using RoslynStone.Infrastructure.CommandHandlers;
using RoslynStone.Infrastructure.QueryHandlers;
using RoslynStone.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register services
builder.Services.AddSingleton<RoslynScriptingService>();
builder.Services.AddSingleton<DocumentationService>();

// Register command handlers
builder.Services.AddScoped<ICommandHandler<ExecuteCodeCommand, ExecutionResult>, ExecuteCodeCommandHandler>();
builder.Services.AddScoped<ICommandHandler<LoadPackageCommand, PackageReference>, LoadPackageCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ExecuteFileCommand, string>, ExecuteFileCommandHandler>();

// Register query handlers
builder.Services.AddScoped<IQueryHandler<GetDocumentationQuery, DocumentationInfo?>, GetDocumentationQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ValidateCodeQuery, IReadOnlyList<CompilationError>>, ValidateCodeQueryHandler>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Add a health check endpoint
app.MapGet("/", () => new { 
    service = "RoslynStone API", 
    version = "1.0", 
    status = "running",
    endpoints = new[]
    {
        "/api/repl/execute",
        "/api/repl/validate",
        "/api/documentation/{symbolName}",
        "/api/mcp"
    }
});

app.Run();
