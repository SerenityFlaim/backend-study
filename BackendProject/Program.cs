using BackendProject.BLL.Services;
using BackendProject.DAL;
using BackendProject.DAL.Interfaces;
using BackendProject.DAL.Repositories;
using Dapper;


var builder = WebApplication.CreateBuilder(args); // создается билдер веб приложения
DefaultTypeMap.MatchNamesWithUnderscores = true; //сообщает Dapper-у, что при маппинге результатов sql-операций в объекты C#, надо использовать snake_case
builder.Services.AddScoped<UnitOfWork>(); // регистрирует зависимость UnitOfWork как scoped

builder.Services.Configure<DbSettings>(builder.Configuration.GetSection(nameof(DbSettings)));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddControllers(); // зависимость, которая автоматически подхватывает все контроллеры в проекте

builder.Services.AddSwaggerGen(); // добавляем swagger


var app = builder.Build(); // собираем билдер в приложение

// добавляем 2 миддлвари для обработки запросов в сваггер
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers(); // добавляем миддлварю для роутинга в нужный контроллер

Migrations.Program.Main([]); // по сути в этот момент будет происходить накатка миграций на базу

app.Run();