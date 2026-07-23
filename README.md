# Event Management API

Простой RESTful API для управления событиями (мероприятиями).  
Реализован на **ASP.NET Core** с использованием **C# 12** и современных практик разработки.

> 🎯 Подходит для обучения, демонстрации или прототипирования микросервисов.

---

## 📋 Модель события (Event)

Каждое событие имеет следующие поля:

| Поле | Тип | Описание |
|------|-----|----------|
| `Id` | `Guid` | Уникальный идентификатор |
| `Title` | `string` | Название события |
| `Description` | `string?` | Описание (опционально) |
| `StartAt` | `DateTime` | Дата и время начала |
| `EndAt` | `DateTime` | Дата и время окончания |
| `TotalSeats` | `int` | **Общее количество мест** (обязательное, > 0) |
| `AvailableSeats` | `int` | **Текущее количество доступных мест** |

> 💡 При создании события `AvailableSeats` автоматически устанавливается равным `TotalSeats`.  
> При успешном бронировании `AvailableSeats` уменьшается на 1 (или больше, если указано).

---

## 📋 Функциональность

API предоставляет полный цикл операций **CRUD**:

- ✅ Получить список всех событий (`GET /api/events`)
- ✅ Получить событие по ID (`GET /api/events/{id}`)
- ✅ Добавить новое событие (`POST /api/events`)
- ✅ Обновить существующее (`PUT /api/events/{id}`)
- ✅ Удалить событие (`DELETE /api/events/{id}`)

А также:
- ✅ Создать бронь на событие (`POST /api/events/{id}/book`)
- ✅ Проверить статус брони (`GET /api/bookings/{id}`)

С поддержкой:
- Валидации входных данных,
- Понятных ошибок на русском языке,
- Корректных HTTP-статусов (200, 201, 202, 400, 404, 409 и др.),
- Фоновой обработки броней,
- **Защиты от овербукинга** (ограничение по TotalSeats и AvailableSeats).

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
- **PostgreSQL** (продакшен) / **In-Memory** (тесты)
- **Entity Framework Core** (Npgsql провайдер)
- **Dependency Injection (DI)**
- **DTO для запросов/ответов** — изоляция модели
- **Кастомная валидация** через `IValidatableObject`
- **Потокобезопасность** с `SemaphoreSlim`
- **Swagger UI** — документация API
- **XML-документация** — для IntelliSense и Swagger
- **Фоновые службы** — обработка броней

---

## 🚀 Запуск проекта

### Предварительные требования
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **PostgreSQL 14+** (локально или в Docker)
> 💡 Для разработки и тестирования по умолчанию используется **In-Memory database** — данные хранятся только в памяти и теряются при перезапуске.  
> Для полноценной работы (включая фоновые сервисы) рекомендуется использовать PostgreSQL.

---

### Настройка PostgreSQL

Сервер PostgreSQL может быть запущен локально или в Docker.

#### Создайте базу данных:

```bash
PGPASSWORD=postgres psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE eventapi;"
```

#### Добавьте строку подключения в appsettings.json:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  }
} 
```

#### 🧱 Миграции с Entity Framework Core

Схема базы данных управляется через **миграции EF Core**.

##### Создание миграции

После изменения модели (`AppDbContext`, сущностей и т.п.) создайте новую миграцию:

```bash
dotnet ef migrations add <Название_миграции>
```

##### Применение миграций
В момент запуска приложение автоматически применяет миграции к базе данных
Для ручного применения миграций используйте команду:

```bash
dotnet ef database update
```

---

### Сборка и запуск
В корне репозитория выполните:

```bash
dotnet restore
dotnet build
dotnet run --project EventMgtApi.Web/EventMgtApi.Web.csproj --urls "https://localhost:7001"
```

🔐 API работает по HTTPS на порту 7001.

---

### Адреса после запуска

| Назначение     | Адрес                          |
|----------------|--------------------------------|
| API            | https://localhost:7001         |
| Swagger UI     | https://localhost:7001/swagger |

---

> 📥 **Пример: Создание события**

```http
POST /api/events
Content-Type: application/json
```

```json
{
  "title": "Team Meeting",
  "description": "Обсуждение планов",
  "startAt": "2025-04-05T10:00:00Z",
  "endAt": "2025-04-05T11:00:00Z"
}
```

> ✅ **Успешный ответ (201 Created)**

```http
HTTP/1.1 201 Created
Location: https://localhost:7001/api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Team Meeting",
  "description": "Обсуждение планов",
  "startAt": "2025-04-05T10:00:00Z",
  "endAt": "2025-04-05T11:00:00Z"
}
```

---

> ❌ **Формат ответа при ошибках**

API возвращает ошибки в стандартизированном формате `ProblemDetails` (`application/problem+json`).

#### Валидация:

```json
{
  "title": "Ошибка валидации",
  "status": 400,
  "detail": "Дата окончания обязательна.",
  "instance": "/api/Events"
}
```

#### Ресурс не найден:

```json
{
  "title": "Ресурс не найден",
  "status": 404,
  "detail": "Событие с ID 4cbde443-... не найдено.",
  "instance": "/api/Events/4cbde443-..."
}
```

#### Внутренняя ошибка сервера:

```json
{
  "title": "Произошла ошибка",
  "status": 500,
  "detail": "[детали ошибки]",
  "instance": "/api/Events/..."
}
```

- Все сообщения — на русском языке.
- Используется централизованная обработка через middleware.
- Соответствует RFC 9110 (type опущен, если не требуется).

---

### 🆕 Новые эндпоинты

### 🛒 Создание брони: `POST /api/events/{eventId:guid}/book`

Создаёт новую бронь на указанное событие.  
Бронь изначально имеет статус `Pending`.

> **HTTP 202 Accepted** — бронь принята в обработку  
> **HTTP 409 Conflict** — отсутствуют свободные места (овербукинг)  
> **Location** — ссылка на `GET /api/bookings/{id}`

#### Пример запроса:
```http
POST /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book
Content-Type: application/json
```

```json
{}
```

> ⚠️ Тело запроса пока пустое (может быть расширено в будущем).

#### Успешный ответ:
```http
HTTP/1.1 202 Accepted
Location: https://localhost:7001/api/bookings/9e1b2f4d-8a3c-4e2a-9f1a-1b2c3d4e5f6a
```

```json
{
  "id": "9e1b2f4d-8a3c-4e2a-9f1a-1b2c3d4e5f6a",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "createdAt": "2025-04-05T14:30:00Z",
  "processedAt": null
}
```

#### Ошибка 409 Conflict (отсутствуют места):
```http
HTTP/1.1 409 Conflict
```

```json
{
  "title": "Недостаточно доступных мест",
  "status": 409,
  "detail": "Нет доступных мест для данного события.",
  "instance": "/api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book"
}
```

> 💡 Этот статус возвращается, когда `AvailableSeats == 0`, что предотвращает овербукинг.

---

### 🔍 Проверка статуса брони: `GET /api/bookings/{id:guid}`

Возвращает текущий статус брони по её идентификатору.

> **HTTP 200 OK** — бронь найдена  
> **HTTP 404 Not Found** — бронь не существует

#### Пример запроса:
```http
GET /api/bookings/9e1b2f4d-8a3c-4e2a-9f1a-1b2c3d4e5f6a
```

#### Ответ:
```json
{
  "id": "9e1b2f4d-8a3c-4e2a-9f1a-1b2c3d4e5f6a",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Confirmed",
  "createdAt": "2025-04-05T14:30:00Z",
  "processedAt": "2025-04-05T14:30:07Z"
}
```

---

## 🧩 Модель `Booking` и статусы

### `Booking` — модель бронирования
- `Id`: `Guid` — уникальный идентификатор брони
- `EventId`: `Guid` — ссылка на событие
- `Status`: `BookingStatus` — текущий статус (см. ниже)
- `CreatedAt`: `DateTime` — когда создана
- `ProcessedAt`: `DateTime?` — когда обработана (может быть `null`)

### `BookingStatus` — возможные статусы
```csharp
public enum BookingStatus
{
    /// <summary>Бронь создана, ожидает обработки.</summary>
    Pending,

    /// <summary>Бронь подтверждена.</summary>
    Confirmed,

    /// <summary>Бронь отклонена.</summary>
    Rejected
}
```

---

## ⏳ Логика фоновой обработки

Брони со статусом `Pending` обрабатываются фоновым сервисом **в автоматическом режиме**.

### Как это работает:
- Сервис `BookingProcessingBackgroundService` запускается при старте приложения.
- Каждые **5 секунд** он:
  - Ищет все брони со статусом `Pending`
  - Для каждой:
    - Ждёт **2 секунды** (имитация внешней системы)
    - Меняет статус на `Confirmed`
    - Устанавливает `ProcessedAt = DateTime.UtcNow`
    - Сохраняет изменения

> 💡 Это имитирует интеграцию с платёжной системой или внешним API.

### Особенности:
- Потокобезопасен (через `ConcurrentDictionary`)
- Работает асинхронно
- Логирует каждый шаг
- Может быть прерван при остановке приложения

---

## 🔐 Примитивы синхронизации

Для обеспечения потокобезопасности и предотвращения овербукинга используется **SemaphoreSlim**:

### `SemaphoreSlim` в `BookingService`

**Зачем нужен:**  
При создании брони необходимо атомарно проверить наличие свободных мест и уменьшить счётчик `AvailableSeats`. Без синхронизации два одновременных запроса могут оба увидеть `AvailableSeats > 0`, создать брони, и в итоге продать больше мест, чем доступно.

---

## 🎭 Пример сценария с овербукингом

Представим ситуацию, когда на событие осталось **только 2 места**, но одновременно поступают **3 запроса** на бронирование.

### Шаг 1: Событие с 2 местами

```http
POST /api/events
Content-Type: application/json
```

```json
{
  "title": "Концерт",
  "startAt": "2026-06-20T19:00:00Z",
  "endAt": "2026-06-20T23:00:00Z",
  "totalSeats": 2,
  "availableSeats": 2
}
```

→ Ответ: `201 Created`, ID события — `event-id`

---

### Шаг 2: Три пользователя одновременно пытаются забронировать

**Пользователь 1** отправляет запрос:
```http
POST /api/events/event-id/book
```

**Пользователь 2** отправляет запрос (одновременно):
```http
POST /api/events/event-id/book
```

**Пользователь 3** отправляет запрос (одновременно):
```http
POST /api/events/event-id/book
```

---

### Шаг 3: Обработка запросов

Благодаря `SemaphoreSlim` в `BookingService` запросы обрабатываются **последовательно**:

1. **Запрос 1** заходит в критическую секцию:
   - Проверяет: `AvailableSeats == 2` → OK
   - Уменьшает: `AvailableSeats = 1`
   - Создаёт бронь со статусом `Pending`
   - Возвращает `202 Accepted`

2. **Запрос 2** ждёт, затем заходит в критическую секцию:
   - Проверяет: `AvailableSeats == 1` → OK
   - Уменьшает: `AvailableSeats = 0`
   - Создаёт бронь со статусом `Pending`
   - Возвращает `202 Accepted`

3. **Запрос 3** ждёт, затем заходит в критическую секцию:
   - Проверяет: `AvailableSeats == 0` → ❌
   - Выбрасывает `NoAvailableSeatsException`
   - Возвращается `409 Conflict` с сообщением:  
     `"Нет доступных мест для данного события."`

---

### Итог:

- ✅ **2 брони** созданы и обработаны (статус `Confirmed`)
- ❌ **1 запрос** отклонён с HTTP 409

> 💡 Таким образом, система гарантирует, что количество проданных мест никогда не превысит `TotalSeats`.

---

## 📦 Архитектура (обновлённая)

Проект построен по принципам **чистой архитектуры (Clean Architecture)** с чётким разделением ответственностей.  
Структура папок отражает слои приложения, что упрощает масштабирование, тестирование и поддержку.

```
EventMgtService.sln
├── EventMgtApi.Domain/           # Бизнес-ядро: сущности, интерфейсы, исключения
│   ├── Entities/
│   │   ├── Event.cs             # Модель события (с TotalSeats, AvailableSeats)
│   │   └── Booking.cs           # Модель бронирования
│   ├── Enums/
│   │   └── BookingStatus.cs     # Статусы брони: Pending, Confirmed, Rejected
│   ├── Exceptions/
│   │   ├── NotFoundException.cs # Ошибка "не найдено" (404)
│   │   ├── ValidationException.cs # Ошибка валидации (400)
│   │   └── NoAvailableSeatsException.cs # Ошибка овербукинга (409)
│   └── Interfaces/
│       ├── IEventRepository.cs  # Абстракция доступа к событиям
│       └── IBookingRepository.cs # Абстракция доступа к броням
│
├── EventMgtApi.Application/      # Логика приложения: сервисы, DTO, маппинг
│   ├── Abstractions/
│   │   ├── Persistence/
│   │   │   ├── IEventRepository.cs  # Абстракция доступа к событиям
│   │   │   └── IBookingRepository.cs # Абстракция доступа к броням
│   │   └── Services/
│   │       ├── IEventService.cs     # Интерфейс управления событиями
│   │       └── IBookingService.cs   # Интерфейс управления бронями
│   ├── Events/
│   │   ├── EventService.cs      # Реализация бизнес-логики событий
│   │   ├── DTOs/
│   │   │   ├── EventDto.cs          # DTO для создания/обновления события
│   │   │   ├── EventDtoResponse.cs  # DTO ответа с Id
│   │   │   └── PaginatedResult.cs   # Обобщённый ответ с пагинацией
│   │   └── Extensions/
│   │       └── EventMappingExtensions.cs # Методы ToDtoResponse(), ToDtoList()
│   ├── Bookings/
│   │   ├── BookingService.cs    # Логика создания и получения броней (с SemaphoreSlim)
│   │   ├── DTOs/
│   │   │   ├── BookingResponseDto.cs # DTO статуса брони
│   │   │   └── CreateBookingRequestDto.cs # Пустой DTO для бронирования
│   │   └── Extensions/
│   │       └── BookingsMappingExtensions.cs # Метод ToDtoResponse()
│   └── Extensions/
│
├── EventMgtApi.Infrastructure/   # Внешние реализации
│   ├── Persistence/
│   │   ├── AppDbContext.cs      # Контекст базы данных
│   │   ├── Configurations/      # Конфигурации объектов в базе данных
│   │   │   ├── BookingConfiguration.cs # Бронирования
│   │   │   └── EventConfiguration.cs # События
│   │   ├── Migrations/          # Миграции EF Core
│   │   └── Repositories/
│   │       ├── EventRepository.cs  # Реализация репозитория для событий
│   │       └── BookingRepository.cs # Реализация репозитория для броней
│   └── Services/
│       └── BookingProcessingBackgroundService.cs # Обработка Pending → Confirmed (с SemaphoreSlim)
│
├── EventMgtApi.Web/              # Входная точка API (Presentation Layer)
│   ├── Controllers/
│   │   ├── EventsController.cs  # Обработка /api/events
│   │   └── BookingsController.cs # Обработка /api/bookings
│   ├── Middleware/
│   │   └── GlobalExceptionHandlingMiddleware.cs # Централизованная обработка ошибок
│   ├── Filters/
│   │   └── ThrowValidationExceptionFilter.cs # Преобразует ModelState в исключение
│   └── Extensions/
│       ├── ApplicationBuilderExtensions.cs # Метод UseGlobalExceptionHandling()
│       └── ServiceCollectionExtensions.cs # Методы регистрации сервисов
│
├── EventMgtApi.UnitTests/        # Юнит-тесты
│   ├── EventServiceTests.cs
│   ├── BookingServiceTests.cs
│   ├── EventTests.cs
│   ├── BookingTests.cs
│   └── TestDataFactory.cs
│
├── EventMgtApi.IntegrationTests/ # Интеграционные тесты
│   ├── DatabaseFixture.cs
│   ├── EventRepositoryTests.cs
│   ├── BookingRepositoryTests.cs
│   ├── CommonTests.cs
│   └── ConcurrentTests.cs
│
├── EventMgtApi/                  # Основной проект (стартовый)
│   ├── Program.cs                # Настройка DI, слоёв, маршрутов, Swagger
│   ├── appsettings.json          # Конфигурация
│   └── Properties/
│       └── launchSettings.json   # Конфигурация запуска (HTTPS, порт 7001)
│
└── docker-compose.yml            # Конфигурация Docker Compose
```

---

#### 🔁 Принципы разделения:

- **Domain** — не зависит ни от чего. Содержит только бизнес-сущности и контракты.
- **Application** — зависит от `Domain`. Содержит логику, DTO и сервисы.
- **Infrastructure** — зависит от `Domain` и `Application`. Реализует абстракции (например, репозитории).
- **Presentation** — зависит от `Application` и `Domain`. Отвечает за HTTP, контроллеры, middleware.

> 💡 Такая структура позволяет:
> - Легко заменить in-memory хранилище на EF Core или Redis.
> - Писать изолированные unit-тесты.
> - Добавлять новые функции без нарушения существующего кода.
> - Поддерживать проект при росте числа разработчиков.

---

### 🔐 Валидация

Все поля в `EventDto` проходят валидацию:
- `[Required]`
- Кастомная проверка: `StartAt < EndAt`
- Сообщения на русском языке.
- Защита от `null` и логических ошибок.

---

## 🧱 Тесты

### Unit-тесты

Реализованы unit-тесты для:
- сервисов `EventService`, `BookingService`, `BookingProcessingBackgroundService`.
- сущностей `Event` и `Booking`

Запуск unit-тестов (в корне репозитория):

```bash
dotnet test EventMgtApi.UnitTests/EventMgtApi.UnitTests.csproj
```

### Интеграционные тесты

В проект включены интеграционные тесты (EventMgtApi.IntegrationTests), которые проверяют взаимодействие с базой данных через реальный AppDbContext.
К интеграционным тестам также добавлены тесты на конкурентность.

Для запуска интеграционных тестов требуется Docker — тестовый контейнер с PostgreSQL запускается автоматически через testcontainers.

Требования
Установленный Docker
.NET 10+ SDK (используется EF Core 10 — актуально на текущий момент)

Запуск интеграционных тестов (в корне репозитория):

```bash
dotnet test EventMgtApi.IntegrationTests/EventMgtApi.IntegrationTests.csproj
```
> 💡 При первом запуске Docker скачает образ postgres:16-alpine

Запуск всех тестов сразу (в корне репозитория):
```bash
dotnet test
```

---

### 🧱 Ограничения

- Нет аутентификации или авторизации.
- Часовые пояса не обрабатываются.

---

### 🚧 Будущие улучшения

- Сделать Docker-образ
- Интеграция с email и платежами

---

> 🙌 Спасибо за использование!
