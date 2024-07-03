using blogs.Data;
using blogs.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
     .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();


//added
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

//list
app.MapGet("/blog", (ApplicationDbContext context) =>
{
    return new { blogs = context.Blogs.ToList()};
}
);


//list one item
app.MapGet("/blog/{id}", async (Guid id, ApplicationDbContext context) =>
{
    var blog = await context.Blogs.FindAsync(id);
    if (blog == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(new { blogs = blog });
});


//add
app.MapPost("/blog/add", async ([FromBody]Blog blog, ApplicationDbContext context) =>
{
    context.Blogs.Add(blog);
    await context.SaveChangesAsync();

    return Results.Ok(new {message = "ok"});
});

//delete
app.MapDelete("/blog/delete/{id}", async (Guid id, ApplicationDbContext context) =>
{
    var blog = await context.Blogs.FindAsync(id);
    if (blog == null)
    {
        return Results.NotFound();
    }

    context.Blogs.Remove(blog);
    await context.SaveChangesAsync();

    return Results.NoContent();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
