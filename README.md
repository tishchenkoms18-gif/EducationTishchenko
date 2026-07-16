# EducationTishchenko
# Event Management API

REST API для управления мероприятиями. Проект реализован на ASP.NET Core Web API с хранением данных в памяти.

## Технологии

- .NET 10
- ASP.NET Core Web API
- Swagger / Swashbuckle
- In-Memory хранилище (`List<Event>`)


##  Функциональность

| Метод | URL | Описание |
|-------|-----|----------|
| `GET` | `/api/events` | Получить список всех событий |
| `GET` | `/api/events/{id}` | Получить событие по ID |
| `POST` | `/api/events` | Создать новое событие |
| `PUT` | `/api/events/{id}` | Обновить событие целиком |
| `DELETE` | `/api/events/{id}` | Удалить событие |


### 1. Клонируйте репозиторий

```bash
git clone https://github.com/tishchenkoms18-gif/EducationTishchenko.git
cd WebAPI

Восстановите зависимости
bash
dotnet restore


Запустите проект

bash
dotnet run 


### 2. Откройте Swagger 
https://localhost:5001/swagger


# 1. Создание события (POST)
POST https://localhost:5001/api/events
Content-Type: application/json

{
    "title": "Конференция .NET",
    "description": "Ежегодная конференция для разработчиков .NET",
    "startAt": "2026-08-15T10:00:00Z",
    "endAt": "2026-08-15T18:00:00Z"
}
Ответ (201 Created):

json
{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Конференция .NET",
    "description": "Ежегодная конференция для разработчиков .NET",
    "startAt": "2026-08-15T10:00:00Z",
    "endAt": "2026-08-15T18:00:00Z"
}

# 2. Получение всех событий (GET)
Запрос:

http
GET https://localhost:5001/api/events
Ответ (200 OK):

json
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "title": "Конференция .NET",
        "description": "Ежегодная конференция для разработчиков .NET",
        "startAt": "2026-08-15T10:00:00Z",
        "endAt": "2026-08-15T18:00:00Z"
       
    }
]

# 3. Получение события по ID (GET)
Запрос:

http
GET https://localhost:5001/api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
Ответ (200 OK):

json
{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Конференция .NET",
    "description": "Ежегодная конференция для разработчиков .NET",
    "startAt": "2026-08-15T10:00:00Z",
    "endAt": "2026-08-15T18:00:00Z"
}
Ответ (404 Not Found):

json
{
    "message": "Событие с ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 не найдено"
}

# 4. Обновление события (PUT)
Запрос:

http
PUT https://localhost:5001/api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
    "title": "Конференция .NET 2026",
    "description": "Обновлённое описание конференции",
    "startAt": "2026-08-15T10:00:00Z",
    "endAt": "2026-08-15T18:00:00Z"
}
Ответ (200 OK):

json
{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Конференция .NET 2026",
    "description": "Обновлённое описание конференции",
    "startAt": "2026-08-15T10:00:00Z",
    "endAt": "2026-08-15T18:00:00Z"
}

# 5. Удаление события (DELETE)
Запрос:

http
DELETE https://localhost:5001/api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
Ответ (204 No Content) — без тела.
