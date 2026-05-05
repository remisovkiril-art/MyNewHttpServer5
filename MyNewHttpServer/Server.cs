using System.Net;
using System.Text;
using System.Web;
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
        new Student { Id = 5, Name = "Olena", Surname = "Melnyk", Group = "A3" }
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
            if (req.HttpMethod == "GET")
            {
                if (url == "/student")
                {
                    await ShowStudents(res);
                }
                else
                {
                    await ReturnPage(res, url);
                }
            }
            else if (req.HttpMethod == "POST")
            {
                if (url == "/register")
                {
                    await Register(req, res);
                }
            }
            res.Close();
        }
    }
    private async Task Register(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream);
        string body = await reader.ReadToEndAsync();
        var data = HttpUtility.ParseQueryString(body);
        string login = data["login"] ?? "";
        string password = data["password"] ?? "";
        string repeat = data["repeat"] ?? "";
        string email = data["email"] ?? "";
        string agree = data["agree"] ?? "";
        List<string> errors = new();
        if (login.Length < 5)
            errors.Add("Login должен быть больше 5 символов");
        if (password != repeat)
            errors.Add("Пароли не совпадают");
        if (string.IsNullOrEmpty(email))
            errors.Add("Email обязательный");
        if (agree != "on")
            errors.Add("Нужно согласиться");
        string html;
        if (errors.Count > 0)
        {
            html = "<h2>Ошибки:</h2><ul>";
            foreach (var error in errors)
                html += $"<li>{error}</li>";
            html += "</ul><a href='/'>Назад</a>";
        }
        else
        {
            Console.WriteLine($"Email sent to {email}");
            html = $"""
            <h2>Регистрация успешна</h2>
            <p>Пользователь {login} зарегистрирован.</p>
            <p>Письмо отправлено на {email}</p>
            <a href="/">На главную</a>
            """;
        }
        await WriteResponse(res, html);
    }
    private async Task ShowStudents(HttpListenerResponse res)
    {
        string html = "<h1>Students</h1><ul>";
        foreach (var s in students)
        {
            html += $"<li>{s.Id} {s.Name} {s.Surname} ({s.Group})</li>";
        }
        html += "</ul><a href='/'>Назад</a>";
        await WriteResponse(res, html);
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
        await WriteResponse(res, html);
    }
    private async Task WriteResponse(HttpListenerResponse res, string content)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        res.ContentLength64 = buffer.Length;
        res.ContentType = "text/html; charset=utf-8";
        await res.OutputStream.WriteAsync(buffer);
    }
}
