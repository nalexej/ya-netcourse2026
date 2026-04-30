# Event Management API

Простой RESTful API для управления событиями (мероприятиями).  
Реализован на **ASP.NET Core 8** с использованием **C# 12** и современных практик разработки.

> 🎯 Подходит для обучения, демонстрации или прототипирования микросервисов.

---

## 📋 Функциональность

API предоставляет полный цикл операций **CRUD**:

- ✅ Получить список всех событий (`GET /api/events`)
- ✅ Получить событие по ID (`GET /api/events/{id}`)
- ✅ Добавить новое событие (`POST /api/events`)
- ✅ Обновить существующее (`PUT /api/events/{id}`)
- ✅ Удалить событие (`DELETE /api/events/{id}`)

С поддержкой:
- Валидации входных данных,
- Понятных ошибок на русском языке,
- Корректных HTTP-статусов (200, 201, 400, 404 и др.).

---

## 🛠 Технологии

- **.NET 8** / **C# 12**
- **ASP.NET Core Web API**
- **In-memory хранение** (данные теряются при перезапуске)
- **Dependency Injection (DI)**
- **DTO для запросов/ответов** — изоляция модели
- **Кастомная валидация** через `CustomValidationAttribute`
- **Потокобезопасность** с `lock`
- **Swagger UI** — документация API
- **XML-документация** — для IntelliSense и Swagger

---

## 🚀 Запуск проекта

### Предварительные требования
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Сборка и запуск
В корне репозитория выполните:

```bash
dotnet restore
dotnet build
dotnet run --project EventMgtApi/EventMgtApi.csproj --urls "https://localhost:7001"

🔐 API работает по HTTPS на порту 7001.

Адреса после запуска
Назначение	Адрес
API	https://localhost:7001
Swagger UI	https://localhost:7001/swagger

📥 Пример: Создание события
HTTP
POST /api/events
Content-Type: application/json
JSON
{
  "title": "Team Meeting",
  "description": "Обсуждение планов",
  "startAt": "2025-04-05T10:00:00Z",
  "endAt": "2025-04-05T11:00:00Z"
}

⚠️ Используйте UTC-время (Z в конце).


✅ Успешный ответ (201 Created)
HTTP
HTTP/1.1 201 Created
Location: https://localhost:7001/api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
JSON
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Team Meeting",
  "description": "Обсуждение планов",
  "startAt": "2025-04-05T10:00:00Z",
  "endAt": "2025-04-05T11:00:00Z"
}
❌ Ошибка валидации (400 Bad Request)
Если дата начала позже окончания:

JSON
{
  "errors": {
    "StartAt": [
      "Дата начала должна быть раньше даты окончания."
    ],
    "EndAt": [
      "Дата начала должна быть раньше даты окончания."
    ]
  },
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400
}
📦 Архитектура
EventMgtApi/
├── Models/
│   ├── Event.cs           # Внутренняя модель
│   └── Dto/
│       └── EventDto.cs    # DTO для входа с валидацией
├── Services/
│   ├── IEventService.cs
│   └── EventService.cs    # In-memory + потокобезопасность
├── Controllers/
│   └── EventsController.cs
├── Program.cs             # Настройка DI, маршрутов, Swagger
└── Properties/
    └── launchSettings.json

🔐 Валидация
Все поля в EventDto проходят валидацию:
[Required]
Кастомная проверка: StartAt < EndAt

Сообщения на русском языке.
Защита от null и логических ошибок.

🧱 Ограничения
Данные хранятся в памяти → теряются при перезапуске.
static List<Event> заменён на потокобезопасный доступ через lock.
Нет аутентификации или авторизации.
Часовые пояса не обрабатываются.

🚧 Будущие улучшения
 Перейти на EF Core + SQLite для постоянного хранения
 Добавить маппинг (AutoMapper или ручной)
 Реализовать тесты (xUnit)
 Сделать Docker-образ

🙌 Благодарности
Спасибо за использование!
