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

А также:
- ✅ Создать бронь на событие (`POST /api/events/{id}/book`)
- ✅ Проверить статус брони (`GET /api/bookings/{id}`)

С поддержкой:
- Валидации входных данных,
- Понятных ошибок на русском языке,
- Корректных HTTP-статусов (200, 201, 202, 400, 404 и др.),
- Фоновой обработки броней.

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
- **Фоновые службы** — обработка броней

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

## 🎭 Пример сценария использования

### Шаг 1: Создать событие
```http
POST /api/events
Content-Type: application/json
```
```json
{
  "title": "Конференция .NET",
  "description": "Ежегодная встреча разработчиков",
  "startAt": "2026-06-10T09:00:00Z",
  "endAt": "2026-06-10T18:00:00Z"
}
```
→ Ответ: `201 Created`, ID события — `event-id`

---

### Шаг 2: Забронировать место
```http
POST /api/events/event-id/book
```
→ Ответ: `202 Accepted`, ID брони — `booking-id`, статус `Pending`

---

### Шаг 3: Проверить статус брони
```http
GET /api/bookings/booking-id
```
→ Ответ: `200 OK`, статус `Pending`, `processedAt: null`

---

### Шаг 4: Подождать ~7 секунд

Фоновый сервис:
- Найдёт бронь
- Обработает её
- Изменит статус на `Confirmed`

---

### Шаг 5: Проверить статус снова
```http
GET /api/bookings/booking-id
```
```json
{
  "id": "booking-id",
  "eventId": "event-id",
  "status": "Confirmed",
  "createdAt": "2025-04-05T14:30:00Z",
  "processedAt": "2025-04-05T14:30:07Z"
}
```

✅ Бронь подтверждена!

---

### 📦 Архитектура (обновлённая)

Проект построен по принципам **чистой архитектуры (Clean Architecture)** с чётким разделением ответственностей.  
Структура папок отражает слои приложения, что упрощает масштабирование, тестирование и поддержку.

```
EventMgtApi/
├── Domain/                       # Бизнес-ядро: сущности, интерфейсы, исключения
│   ├── Entities/
│   │   ├── Event.cs             # Модель события
│   │   └── Booking.cs           # Модель бронирования
│   ├── Enums/
│   │   └── BookingStatus.cs     # Статусы брони: Pending, Confirmed, Rejected
│   ├── Exceptions/
│   │   ├── NotFoundException.cs # Ошибка "не найдено" (404)
│   │   └── ValidationException.cs # Ошибка валидации (400)
│   └── Interfaces/
│       ├── IEventRepository.cs  # Абстракция доступа к событиям
│       └── IBookingRepository.cs # Абстракция доступа к броням
│
├── Application/                  # Логика приложения: сервисы, DTO, маппинг
│   ├── Services/
│   │   ├── IEventService.cs     # Интерфейс управления событиями
│   │   ├── EventService.cs      # Реализация бизнес-логики событий
│   │   ├── IBookingService.cs   # Интерфейс управления бронями
│   │   └── BookingService.cs    # Логика создания и получения броней
│   ├── DTOs/
│   │   ├── EventDto.cs          # DTO для создания/обновления события
│   │   ├── EventDtoResponse.cs  # DTO ответа с Id
│   │   ├── BookingResponseDto.cs # DTO статуса брони
│   │   ├── CreateBookingRequestDto.cs # Пустой DTO для бронирования
│   │   └── PaginatedResult.cs   # Обобщённый ответ с пагинацией
│   └── Extensions/
│       └── EventMappingExtensions.cs # Методы ToDtoResponse(), ToDtoList()
│
├── Infrastructure/               # Внешние реализации
│   ├── Repositories/
│   │   └── InMemoryEventRepository.cs # In-memory реализация репозитория
│   └── BackgroundServices/
│       └── BookingProcessingBackgroundService.cs # Обработка Pending → Confirmed
│
├── Presentation/                 # Входная точка API
│   ├── Controllers/
│   │   ├── EventsController.cs  # Обработка /api/events
│   │   └── BookingsController.cs # Обработка /api/bookings
│   ├── Middleware/
│   │   └── GlobalExceptionHandlingMiddleware.cs # Централизованная обработка ошибок
│   ├── Filters/
│   │   └── ThrowValidationExceptionFilter.cs # Преобразует ModelState в исключение
│   └── Extensions/
│       └── ApplicationBuilderExtensions.cs # Метод UseGlobalExceptionHandling()
│
├── Program.cs                    # Настройка DI, слоёв, маршрутов, Swagger
└── Properties/
└── launchSettings.json       # Конфигурация запуска (HTTPS, порт 7001)
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

### 🧱 Тесты
Реализованы unit-тесты для сервисов `EventService` и `BookingService`.

Запуск тестов (в корне репозитория):

```bash
dotnet test
```

---

### 🧱 Ограничения

- Данные хранятся в памяти → теряются при перезапуске.
- Нет аутентификации или авторизации.
- Часовые пояса не обрабатываются.
- Все брони автоматически подтверждаются — нет отказов.

---

### 🚧 Будущие улучшения

- Перейти на EF Core + SQLite для постоянного хранения
- Добавить маппинг (AutoMapper или ручной)
- Сделать Docker-образ
- Реализовать механизм отклонения броней
- Добавить `PATCH /bookings/{id}/cancel`
- Интеграция с email и платежами

---

> 🙌 Спасибо за использование!