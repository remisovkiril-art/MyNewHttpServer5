using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.Json;
namespace MyNewHttpServer;
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}
internal class Server
{
    readonly string _HOST = "http://127.0.0.1:8080/";
    List<Student> students = new List<Student>
    {
        new Student { Id = 1, Name = "Ivan", Surname = "Ivanov", Group = "A1" },
        new Student { Id = 2, Name = "Petro", Surname = "Petrov", Group = "A1" },
        new Student { Id = 3, Name = "Oleg", Surname = "Sydorenko", Group = "A2" },
        new Student { Id = 4, Name = "Anna", Surname = "Koval", Group = "A2" },
        new Student { Id = 5, Name = "Olena", Surname = "Melnyk", Group = "A3" },
        new Student { Id = 6, Name = "Dmytro", Surname = "Tkachenko", Group = "A3" },
        new Student { Id = 7, Name = "Serhii", Surname = "Bondar", Group = "A1" },
        new Student { Id = 8, Name = "Nazar", Surname = "Kravets", Group = "A2" },
        new Student { Id = 9, Name = "Ira", Surname = "Shevchenko", Group = "A3" },
        new Student { Id = 10, Name = "Maksym", Surname = "Boyko", Group = "A1" }
    };
    public async Task RunServer()
    {
        HttpListener server = new HttpListener();
        server.Prefixes.Add(_HOST);
        server.Start();
        Console.WriteLine($"Server started at {_HOST}");
        while (true)
        {
            var ctx = await server.GetContextAsync();
            var req = ctx.Request;
            var res = ctx.Response;
            string url = req.Url?.AbsolutePath ?? "/";
            try
            {
                if (req.HttpMethod == "GET")
                {
                    if (url.StartsWith("/student"))
                    {
                        await HandleGetStudents(req, res, url);
                        continue;
                    }
                    await ReturnPage(res, url);
                }
                else if (req.HttpMethod == "POST")
                {
                    if (url == "/student")
                    {
                        using var reader = new StreamReader(req.InputStream);
                        string body = await reader.ReadToEndAsync();
                        var student = JsonSerializer.Deserialize<Student>(body);
                        if (student != null)
                        {
                            student.Id = students.Max(s => s.Id) + 1;
                            students.Add(student);
                            string json = JsonSerializer.Serialize(student);
                            await WriteResponse(res, json, "application/json");
                        }
                    }
                }
                else if (req.HttpMethod == "PUT")
                {
                    if (url.StartsWith("/student/"))
                    {
                        var parts = url.Split('/');
                        int id = int.Parse(parts[2]);
                        using var reader = new StreamReader(req.InputStream);
                        string body = await reader.ReadToEndAsync();
                        var updated = JsonSerializer.Deserialize<Student>(body);
                        var student = students.FirstOrDefault(s => s.Id == id);
                        if (student != null && updated != null)
                        {
                            student.Name = updated.Name;
                            student.Surname = updated.Surname;
                            student.Group = updated.Group;
                            string json = JsonSerializer.Serialize(student);
                            await WriteResponse(res, json, "application/json");
                        }
                    }
                }
                res.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                res.StatusCode = 500;
                res.Close();
            }
        }
    }
    private async Task HandleGetStudents(HttpListenerRequest req, HttpListenerResponse res, string url)
    {
        if (url == "/student")
        {
            var query = req.QueryString;
            IEnumerable<Student> result = students;
            if (!string.IsNullOrEmpty(query["Name"]))
                result = result.Where(s => s.Name == query["Name"]);
            if (!string.IsNullOrEmpty(query["Group"]))
                result = result.Where(s => s.Group == query["Group"]);
            string html = "<h1>Students</h1><ul>";
            foreach (var s in result)
            {
                html += $"<li>{s.Id} {s.Name} {s.Surname} ({s.Group})</li>";
            }
            html += "</ul>";
            await WriteResponse(res, html, "text/html");
        }
        else
        {
            var parts = url.Split('/');
            if (parts.Length == 3 && int.TryParse(parts[2], out int id))
            {
                var student = students.FirstOrDefault(s => s.Id == id);
                string html = student != null
                    ? $"<p>{student.Id} {student.Name} {student.Surname} Group: {student.Group}</p>"
                    : "<p>Student not found</p>";
                await WriteResponse(res, html, "text/html");
            }
        }
    }
    private async Task ReturnPage(HttpListenerResponse res, string url)
    {
        string file = url switch
        {
            "/" => "index.html",
            "/about" => "about.html",
            "/contacts" => "contacts.html",
            _ => "notfound.html"
        };
        string path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "pages", file);
        string html = await File.ReadAllTextAsync(path);
        await WriteResponse(res, html, "text/html");
    }
    private async Task WriteResponse(HttpListenerResponse res, string content, string type)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        res.ContentLength64 = buffer.Length;
        res.ContentType = type;
        res.StatusCode = 200;
        await res.OutputStream.WriteAsync(buffer);
    }
}