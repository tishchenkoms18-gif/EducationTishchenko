using System.Reflection;
using Microsoft.EntityFrameworkCore;    
using WebAPI.Infrastructure.DependencyInjection;   
using WebAPI.Infrastructure.DataAccess;  
using WebAPI.DependencyInjection;                                

var builder = WebApplication.CreateBuilder(args);

// =============================================
// 1. Регистрация сервисов (Composition Root)
// =============================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Настройка Swagger с XML-комментариями
builder.Services.AddSwaggerGen(options => 
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// =============================================
// 2. Применение миграций
// =============================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// =============================================
// 3. Настройка HTTP pipeline
// =============================================

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();