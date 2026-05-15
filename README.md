# Event Management API

Простой RESTful API для управления событиями (мероприятиями).  
Реализован на **ASP.NET Core** с использованием **C# 12** и современных практик разработки.

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

### 🔍 Фильтрация при GET /events

Поддерживаются параметры:
- `title` — частичный поиск по названию (регистронезависимо)
- `from` — события, начинающиеся не раньше указанной даты
- `to` — события, заканчивающиеся не позже указанной даты
- `page` — номер страницы (мин. 1, по умолчанию — 1)
- `pageSize` — размер страницы (от 1 до 100, по умолчанию — 10)

> Пример:  
> `GET /api/events?title=встреча&from=2026-05-14&to=2026-05-15&page=1&pageSize=5`

---

## 🛠 Технологии

- **.NET 10** / **C# 12**
- **ASP.NET Core Web API**
- **In-memory хранение** (данные теряются при перезапуске)
- **Dependency Injection (DI)**
- **DTO для запросов/ответов** — изоляция модели
- **Кастомная валидация** через `IValidatableObject`
- **Потокобезопасность** с `lock`
- **Swagger UI** — документация API
- **XML-документация** — для IntelliSense и Swagger

---

## 🚀 Запуск проекта

### Предварительные требования
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

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

❌ Формат ответа при ошибках
API возвращает ошибки в стандартизированном формате ProblemDetails (application/json).

Примеры:

Валидация:

JSON
{
  "title": "Ошибка валидации",
  "status": 400,
  "detail": "Дата окончания обязательна.",
  "instance": "/api/Events"
}

Ресурс не найден:

JSON
{
  "title": "Ресурс не найден",
  "status": 404,
  "detail": "Событие с ID 4cbde443-... не найдено.",
  "instance": "/api/Events/4cbde443-..."
}

Внутренняя ошибка сервера:

JSON
{
  "title": "Произошла ошибка",
  "status": 500,
  "detail": "[детали ошибки]",
  "instance": "/api/Events/..."
}

Все сообщения — на русском языке.
Используется централизованная обработка через middleware.
Соответствует RFC 9110 (type опущен, если не требуется).


📦 Архитектура
EventMgtApi/
├── Models/
│   ├── Event.cs           # Внутренняя модель события
│   └── Dto/
│       ├── EventDto.cs    # DTO для создания/обновления с валидацией
│       ├── EventDtoResponse.cs  # DTO ответа (с Id)
│       └── PaginatedResult.cs   # Обобщённый ответ с пагинацией: TotalCount, Page, PageSize, Items и т.д.
├── Services/
│   ├── IEventService.cs   # Интерфейс сервиса управления событиями
│   └── EventService.cs    # Реализация: in-memory + потокобезопасность
├── Controllers/
│   └── EventsController.cs # Обработка HTTP-запросов, маппинг, валидация
├── Exceptions/
│   ├── NotFoundException.cs     # Исключение "ресурс не найден" (404)
│   └── ValidationException.cs   # Исключение валидации (400), сообщения на русском
├── Middleware/
│   └── GlobalExceptionHandlingMiddleware.cs # Централизованная обработка исключений
├── Filters/
│   └── ThrowValidationExceptionFilter.cs # Преобразует ModelState в ValidationException
├── Extensions/
│   ├── EventMappingExtensions.cs        # Методы расширения: ToDtoResponse(), ToDtoList()
│   └── ApplicationBuilderExtensions.cs  # UseGlobalExceptionHandling() — подключение middleware
├── Repositories/
│   ├── IEventRepository.cs              # Интерфейс доступа к данным
│   └── InMemoryEventRepository.cs       # Потокобезопасная реализация хранилища в памяти
└── Program.cs             # Настройка DI, маршрутов, Swagger, middleware
    └── Properties/
        └── launchSettings.json

🔐 Валидация
Все поля в EventDto проходят валидацию:
[Required]
Кастомная проверка: StartAt < EndAt

Сообщения на русском языке.
Защита от null и логических ошибок.

🧱 Тесты
Реализованы unit-тесты для сервиса EventService

Запуск тестов (в корне репозитория)
```bash
dotnet test EventService.Tests/EventService.Tests.csproj

🧱 Ограничения
Данные хранятся в памяти → теряются при перезапуске.
static List<Event> заменён на потокобезопасный доступ через lock.
Нет аутентификации или авторизации.
Часовые пояса не обрабатываются.

🚧 Будущие улучшения
 Перейти на EF Core + SQLite для постоянного хранения
 Добавить маппинг (AutoMapper или ручной)
 Сделать Docker-образ

🙌 Благодарности
Спасибо за использование!
