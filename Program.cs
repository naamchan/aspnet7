using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<GachaContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.AddConsole();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/gacha", async (GachaContext db) =>
{
    var item = await db.Items
        .OrderBy(x => EF.Functions.Random())
        .FirstOrDefaultAsync();

    if (item == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(item);
})
.WithName("Get one item from database")
.WithOpenApi();

app.MapGet("/items", async (GachaContext db) =>
{
    var item = await db.Items.ToListAsync();
    return Results.Ok(item);
});

app.MapGet("/item/{id}", async (string id, GachaContext db) =>
{
    var item = await db.Items.FindAsync(id);
    return Results.Ok(item);
});

app.MapPost("/item", async (AddGacha addGacha, GachaContext db) =>
{
    if (addGacha.Name is null || addGacha.Data is null)
        throw new InvalidDataException("Invalid data");
    var item = new Item()
    {
        Data = addGacha.Data,
        Name = addGacha.Name
    };

    var newItem = await db.Items.AddAsync(item);
    await db.SaveChangesAsync();
    var savedItem = newItem.Entity;

    return Results.Created($"/gacha/{savedItem.Id}", savedItem);
});

app.Run("http://*:9000");

public record AddGacha
{
    public string? Name { get; init; }
    public string? Data { get; init; }

    public override string ToString() => $"{nameof(Name)}: {Name}\n{nameof(Data)}: Data";
}

class GachaContext : DbContext
{

    public DbSet<Item> Items { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source=gacha.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>()
            .Property(x => x.Id)
            .ValueGeneratedOnAdd();
    }
}

class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Data { get; set; }
}