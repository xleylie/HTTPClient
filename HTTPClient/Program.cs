using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using HTTPClient.Models;

HttpClient client = new HttpClient()
{
    BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
};

JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
{
    PropertyNameCaseInsensitive = true
};

while (true)
{
    ShowMenu();
    Console.Write("Выберите пункт: ");
    string choice = Console.ReadLine();

    switch (choice)
    {
        case "1": await GetAllUsersAsync(); break;
        case "2": await GetUserByIdAsync(); break;
        case "3": await CreateUserAsync(); break;
        case "4": await DeleteUserAsync(); break;
        case "5": await CheckHttpStatusesAsync(); break;
        case "6": Console.WriteLine("Программа завершена"); return;
        default: Console.WriteLine("Неверный пункт"); break;
    }
}
void ShowMenu()
{
    Console.WriteLine("1.Получить список пользователей");
    Console.WriteLine("2.Получить пользователя по Id");
    Console.WriteLine("3.Создать нового пользователя");
    Console.WriteLine("4.Удалить пользователя по Id");
    Console.WriteLine("5.Проверить HTTP статусы");
    Console.WriteLine("6.Выход");
}

async Task<HttpResponseMessage?> SendRequestAsync(HttpMethod method, string url, HttpContent? content = null)
{
    using var request = new HttpRequestMessage(method, url);
    if (content != null) {  request.Content = content; }

    try
    {
        return await client.SendAsync(request);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Сетевая ошибка: {ex.Message}");
        return null;
    }
    catch (TaskCanceledException ex)
    {
        Console.WriteLine($"Время ожидания истекло: {ex.Message}");
        return null;
    }
}

async Task GetAllUsersAsync()
{
    try
    {
        using var response = await SendRequestAsync(HttpMethod.Get, "users");

        if (response == null) { return; }
        Console.WriteLine($"HTTP status: {(int)response.StatusCode} {response.StatusCode}");

        string json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Ошибка: {json}");
            return;
        }

        var users = JsonSerializer.Deserialize<List<User>>(json, jsonOptions);
        if (users == null)
        {
            Console.WriteLine("Не удалось обработать JSON");
            return;
        }

        foreach (var user in users)
        {
            Console.WriteLine($"ID: {user.Id}");
            Console.WriteLine($"Name: {user.Name}");
            Console.WriteLine($"Email: {user.Email}");
        }
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Ошибка JSON: {ex.Message}");
    }
}

async Task GetUserByIdAsync()
{
    Console.Write("Введите Id пользователя: ");
    if (!int.TryParse(Console.ReadLine(), out int id) || id <= 0)
    {
        Console.WriteLine("Некорректный Id");
        return;
    }

    try
    {
        using var response = await SendRequestAsync(HttpMethod.Get, $"users/{id}");

        if (response == null) { return; }
        Console.WriteLine($"HTTP status: {(int)response.StatusCode} {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine("Пользователь не найден");
            return;
        }

        string json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Ошибка: {json}");
            return;
        }

        var user = JsonSerializer.Deserialize<User>(json, jsonOptions);
        if (user != null)
        {
            Console.WriteLine("Информация о пользователе:");
            Console.WriteLine($"Id: {user.Id}");
            Console.WriteLine($"Name: {user.Name}");
            Console.WriteLine($"Username: {user.Username}");
            Console.WriteLine($"Email: {user.Email}");
        }
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Ошибка JSON: {ex.Message}");
    }
}

async Task CreateUserAsync()
{
    Console.Write("Введите имя: ");
    string name = Console.ReadLine();
    Console.Write("Введите username: ");
    string username = Console.ReadLine();
    Console.Write("Введите email: ");
    string email = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
    {
        Console.WriteLine("Некорректные данные");
        return;
    }

    UserDTO newUser = new UserDTO()
    {
        Name = name,
        Username = username,
        Email = email
    };

    try
    {
        using var content = JsonContent.Create(newUser);

        using var response = await SendRequestAsync(HttpMethod.Post, "users", content);

        if (response == null) { return; }
        Console.WriteLine($"HTTP status: {(int)response.StatusCode} {response.StatusCode}");

        string json = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Ответ сервера:");
        Console.WriteLine(json);
        if (response.IsSuccessStatusCode)
            Console.WriteLine("Пользователь успешно созда");
        else
            Console.WriteLine("Ошибка создания пользователя");
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Ошибка JSON: {ex.Message}");
    }
}

async Task DeleteUserAsync()
{
    Console.Write("Введите Id пользователя для удаления: ");
    if (!int.TryParse(Console.ReadLine(), out int id) || id <= 0)
    {
        Console.WriteLine("Некорректный Id");
        return;
    }
    using var response = await SendRequestAsync(HttpMethod.Delete, $"users/{id}");

    if (response == null) { return; }
    Console.WriteLine($"HTTP status: {(int)response.StatusCode} {response.StatusCode}");

    if (response.IsSuccessStatusCode)
        Console.WriteLine("Запрос на удаление выполнен успешно");
    else
        Console.WriteLine($"Ошибка удаления пользователя: {(int)response.StatusCode}");
}
async Task CheckHttpStatusesAsync()
{
    string[] urls =
    {
        "users/1",
        "users/9999",
        "invalid-resource"
    };

    foreach (string url in urls)
    {
        Console.WriteLine($"URL: {client.BaseAddress}{url}");

        using var response = await SendRequestAsync(HttpMethod.Get, url);

        if (response == null) { return; }
        Console.WriteLine($"HTTP status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine($"Успешен ли запрос: {response.IsSuccessStatusCode}");

        if (response.IsSuccessStatusCode)
            Console.WriteLine("Запрос выполнен успешно");
        else if (response.StatusCode == HttpStatusCode.NotFound)
            Console.WriteLine("Ресурс не найден (404)");
        else
            Console.WriteLine($"HTTP ошибка ({(int)response.StatusCode})");
    }
}