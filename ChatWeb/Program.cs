using ChatWeb.Hubs;
using ChatWeb.Models;
using ChatWeb.Models.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(cfg => cfg.EnableDetailedErrors = true);
builder.Services.AddDbContext<ChatContext>(o => o.UseSqlite(builder.Configuration["ConnectionStrings:Default"]));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
//RPC -> Endpoints
app.UseEndpoints(e =>
{
    e.MapHub<ChatWebHub>("ZapWebHub");
});
app.Run();
