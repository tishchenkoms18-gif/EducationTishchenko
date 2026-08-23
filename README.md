# EducationTishchenko
# Event Management API

REST API для управления мероприятиями. Проект реализован на ASP.NET Core Web API с хранением данных в памяти.

## Технологии

- .NET 10
- ASP.NET Core Web API
- Swagger / Swashbuckle
- In-Memory хранилище (`List<Event>`)
- xUnit + Moq (юнит-тесты)


## Функциональность

| Метод | URL | Описание |
|-------|-----|----------|
| `GET` | `/api/events` | Получить список событий с фильтрацией и пагинацией |
| `GET` | `/api/events/{id}` | Получить событие по ID |
| `POST` | `/api/events` | Создать новое событие |
| `POST` | `/api/events/batch` | Создать несколько событий за один запрос |
| `PUT` | `/api/events/{id}` | Обновить событие целиком |
| `DELETE` | `/api/events/{id}` | Удалить событие |
| `POST` | `/api/events/{id}/book` | Создать бронирование на событие (202 Accepted) |
| `GET` | `/api/bookings/{id}` | Получить статус бронирования |


## Валидация

При создании или обновлении события проверяются следующие правила:

| Правило | Описание | Код ошибки |
|---------|----------|------------|
| `Title` не может быть пустым | Обязательное поле | 400 Bad Request |
| `Title` не более 300 символов | Ограничение длины | 400 Bad Request |
| `StartAt` раньше `EndAt` | Дата начала должна быть раньше даты окончания | 400 Bad Request |
| Событие не в прошлом | Дата начала не может быть раньше текущего времени (более чем на 1 час) | 400 Bad Request |
| `page` > 0 | Номер страницы должен быть положительным | 400 Bad Request |
| `pageSize` от 1 до 100 | Ограничение на размер страницы | 400 Bad Request |

 ### Логика фоновой обработки

- Фоновый сервис `BookingBackgroundService` запускается при старте приложения.
- Он периодически опрашивает хранилище броней в статусе `Pending`.
- Для каждой `Pending`-брони выполняется искусственная задержка 2 секунды (имитация внешнего вызова).
- После задержки бронь переводится в статус `Confirmed` и заполняется `ProcessedAt`.
- В будущем этот сервис может принимать решения о подтверждении/отклонении на основе внешних данных.


### Пример сценария использования

1. Создать событие: `POST /api/events`
2. Создать бронь: `POST /api/events/{eventId}/book`
   - Получить `202 Accepted` и заголовок `Location: /api/bookings/{bookingId}`
3. Сразу запросить статус: `GET /api/bookings/{bookingId}` → статус `Pending`
4. Подождать несколько секунд (пока отработает фоновый сервис)
5. Повторно запросить статус → статус изменится на `Confirmed`


## Параметры фильтрации GET /events

Эндпоинт `GET /api/events` поддерживает следующие параметры:

| Параметр | Тип | Обязательный | Описание | Пример |
|----------|-----|--------------|----------|--------|
| `title` | `string` | ❌ Нет | Поиск по названию (регистронезависимый, частичное совпадение) | `?title=C%23` |
| `from` | `DateTime` | ❌ Нет | События, начинающиеся не раньше указанной даты (UTC) | `?from=2026-07-01T00:00:00Z` |
| `to` | `DateTime` | ❌ Нет | События, заканчивающиеся не позже указанной даты (UTC) | `?to=2026-08-01T00:00:00Z` |
| `page` | `int` | ❌ Нет (по умолчанию 1) | Номер страницы для пагинации | `?page=2` |
| `pageSize` | `int` | ❌ Нет (по умолчанию 10) | Количество элементов на странице (макс. 100) | `?pageSize=20` |
    		
### 1. Клонируйте репозиторий

```bash
git clone https://github.com/tishchenkoms18-gif/EducationTishchenko.git
cd WebAPI/WebAPI

Восстановите зависимости
bash
dotnet restore


Запустите проект

bash
dotnet run 


### 2. Откройте Swagger 
https://localhost:5001/swagger


##  Запуск тестов
Проект покрыт юнит-тестами с использованием xUnit и Moq.
Запустить все тесты
dotnet test

Ожидаемый результат
Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16, Duration: 1.2s



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
### Примеры запросов с фильтрацией

# Поиск по названию
GET /api/events?title=C%23

# Фильтр по диапазону дат
GET /api/events?from=2026-07-01T00:00:00Z&to=2026-08-01T00:00:00Z

# Комбинированная фильтрация + пагинация
GET /api/events?title=конференция&from=2026-07-01T00:00:00Z&to=2026-08-01T00:00:00Z&page=2&pageSize=10

# Только пагинация
GET /api/events?page=3&pageSize=20

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

Формат ответа с пагинацией
json
{
    "totalCount": 25,
    "items": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "title": "Конференция .NET",
            "description": "Ежегодная конференция",
            "startAt": "2026-08-01T10:00:00Z",
            "endAt": "2026-08-01T18:00:00Z",
            "createdAt": "2026-07-15T12:00:00Z",
            "updatedAt": null
        }
    ],
    "pageNumber": 2,
    "pageSize": 10,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": true
}


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

# 6. Массовое создание событий (POST /batch)
POST https://localhost:5001/api/events/batch
Content-Type: application/json

{
    "events": [
        {
            "title": "Конференция .NET",
            "description": "Описание",
            "startAt": "2026-08-01T10:00:00Z",
            "endAt": "2026-08-01T18:00:00Z"
        },
        {
            "title": "Воркшоп по C#",
            "description": "Практический воркшоп",
            "startAt": "2026-08-10T09:00:00Z",
            "endAt": "2026-08-10T17:00:00Z"
        }
    ]
}
