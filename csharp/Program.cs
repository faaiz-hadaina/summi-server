using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SummiServer.Models;
using System.Collections.Concurrent;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Serve the existing files/ directory at /files
var filesPath = System.IO.Path.Combine(app.Environment.ContentRootPath, "files");
// If project is run from the csharp folder but files are at repo root, check parent
if (!System.IO.Directory.Exists(filesPath))
{
    var alt = System.IO.Path.Combine(app.Environment.ContentRootPath, "..", "files");
    if (System.IO.Directory.Exists(alt)) filesPath = System.IO.Path.GetFullPath(alt);
    else System.IO.Directory.CreateDirectory(filesPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(filesPath),
    RequestPath = "/files"
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// In-memory store to keep this simple and runnable without MongoDB
var store = new ConcurrentDictionary<string, Contact>();

// Helper: parse simple CSV or lines "name,phone" or "name-phone"
IEnumerable<Contact> ParseContactsFromStream(System.IO.Stream stream)
{
    using var sr = new System.IO.StreamReader(stream, Encoding.UTF8);
    var list = new List<Contact>();
    while (!sr.EndOfStream)
    {
        var line = sr.ReadLine();
        if (string.IsNullOrWhiteSpace(line)) continue;
        var parts = line.Contains(',') ? line.Split(',') : line.Split('-');
        if (parts.Length < 2) continue;
        var name = parts[0].Trim();
        var phone = parts[1].Trim();
        list.Add(new Contact { Name = name, Phone = phone });
    }
    return list;
}

app.MapGet("/api", () => Results.Ok(store.Values));

app.MapGet("/api/search", (string? search) =>
{
    if (string.IsNullOrEmpty(search)) return Results.Ok(store.Values);
    var results = store.Values.Where(c => System.Text.RegularExpressions.Regex.IsMatch(c.Name, "\\b" + System.Text.RegularExpressions.Regex.Escape(search), System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    return Results.Ok(results);
});

app.MapGet("/api/{id}", (string id) =>
{
    if (store.TryGetValue(id, out var contact)) return Results.Ok(contact);
    return Results.NotFound(new { message = "Cannot find contact" });
});

app.MapPost("/api", (Contact contact) =>
{
    // simple uniqueness checks
    if (store.Values.Any(c => c.Name.Equals(contact.Name, StringComparison.OrdinalIgnoreCase)))
        return Results.BadRequest(new { message = "Name must be unique" });
    if (store.Values.Any(c => c.Phone == contact.Phone))
        return Results.BadRequest(new { message = "Phone must be unique" });
    contact.Id = System.Guid.NewGuid().ToString();
    store[contact.Id] = contact;
    return Results.Created($"/api/{contact.Id}", contact);
});

app.MapPost("/api/uploadBulkContacts", async (HttpRequest req) =>
{
    if (!req.HasFormContentType) return Results.BadRequest(new { message = "No file provided" });
    var form = await req.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null) return Results.BadRequest(new { message = "file field missing" });
    var savePath = System.IO.Path.Combine(filesPath, file.FileName);
    using (var fs = System.IO.File.Create(savePath))
    {
        await file.CopyToAsync(fs);
    }
    try
    {
        using var stream = file.OpenReadStream();
        var rows = ParseContactsFromStream(stream);
        var added = new List<Contact>();
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Phone)) continue;
            if (store.Values.Any(c => c.Phone == r.Phone)) continue;
            r.Id = System.Guid.NewGuid().ToString();
            store[r.Id] = r;
            added.Add(r);
        }
        return Results.Created("/api/uploadBulkContacts", new { message = "Contacts created", data = added });
    }
    catch (System.Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/updateBulkContacts", async (HttpRequest req) =>
{
    if (!req.HasFormContentType) return Results.BadRequest(new { message = "No file provided" });
    var form = await req.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null) return Results.BadRequest(new { message = "file field missing" });
    using var stream = file.OpenReadStream();
    var contacts = ParseContactsFromStream(stream);
    foreach (var x in contacts)
    {
        var existing = store.Values.FirstOrDefault(c => c.Phone == x.Phone);
        if (existing != null)
        {
            existing.Name = x.Name;
        }
    }
    return Results.Ok(new { message = "Contacts updated" });
});

app.MapPost("/api/deleteBulkContacts", async (HttpRequest req) =>
{
    if (!req.HasFormContentType) return Results.BadRequest(new { message = "No file provided" });
    var form = await req.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null) return Results.BadRequest(new { message = "file field missing" });
    using var stream = file.OpenReadStream();
    var contacts = ParseContactsFromStream(stream);
    var phones = contacts.Select(c => c.Phone).Where(p => !string.IsNullOrWhiteSpace(p)).ToHashSet();
    var toDelete = store.Values.Where(c => phones.Contains(c.Phone)).Select(c => c.Id).ToList();
    foreach (var id in toDelete) store.TryRemove(id, out _);
    return Results.Ok(new { message = "Contacts deleted" });
});

app.MapPatch("/api/{id}", async (string id, HttpRequest req) =>
{
    if (!store.TryGetValue(id, out var contact)) return Results.NotFound(new { message = "Cannot find contact" });
    var model = await req.ReadFromJsonAsync<Contact>();
    if (model == null) return Results.BadRequest();
    if (!string.IsNullOrWhiteSpace(model.Name)) contact.Name = model.Name;
    if (!string.IsNullOrWhiteSpace(model.Phone)) contact.Phone = model.Phone;
    store[id] = contact;
    return Results.Ok(contact);
});

app.MapDelete("/api/{id}", (string id) =>
{
    if (!store.TryRemove(id, out var _)) return Results.NotFound(new { message = "Cannot find contact" });
    return Results.Ok(new { message = "Deleted Contact" });
});

app.MapPost("/api/bulkdelete", async (HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<BulkDeleteBody>();
    if (body == null || body.SelectedIds == null) return Results.BadRequest();
    foreach (var id in body.SelectedIds)
    {
        store.TryRemove(id, out _);
    }
    return Results.Ok(new { message = "Deleted Contacts" });
});

app.Run();

public record BulkDeleteBody(string[]? SelectedIds);
