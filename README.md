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

События:
- ✅ Получить список всех событий (`GET /api/events`)
- ✅ Получить событие по ID (`GET /api/events/{id}`)
- ✅ Добавить новое событие (`POST /api/events`)
- ✅ Обновить существующее (`PUT /api/events/{id}`)
- ✅ Удалить событие (`DELETE /api/events/{id}`)

Брони:
- ✅ Создать бронь на событие (`POST /api/events/{id}/book`)
- ✅ Проверить статус брони (`GET /api/bookings/{id}`)
- ✅ Отменить бронь (`DELETE /api/bookings/{id}`)

Пользователи:
- ✅ Зарегистрировать нового пользователя (`POST /api/auth/register`)
- ✅ Аутентифицировать пользователя (`POST /api/auth/login`)

С поддержкой:
- Валидации входных данных
- Понятных ошибок на русском языке
- Корректных HTTP-статусов (200, 201, 202, 400, 404, 409 и др.)
- Фоновой обработки броней
- **Защиты от овербукинга** (ограничение по TotalSeats и AvailableSeats)
- **Ролевого доступа** (User/Admin).

---

## 🎭 Ролевая модель и разграничение прав

Система поддерживает два уровня доступа (роли), определенные в перечислении `UserRole`:

### 1. User (Пользователь)
Базовая роль для всех зарегистрированных пользователей.

**Права:**
*   **Просмотр событий:** Доступ к публичному списку событий и деталям событий (`GET /events`).
*   **Бронирование:** Возможность создавать бронирования на доступные события (`POST /bookings`).
    *   Подвергается проверке на лимит активных броней (по умолчанию не более 10 активных броней на пользователя).
    *   Нельзя бронировать прошедшие события.
*   **Управление своими бронями:**
    *   Просмотр списка своих бронирований (`GET /bookings`).
    *   Отмена своей брони (`DELETE /bookings/{id}`), если событие еще не началось.
*   **Регистрация и Вход:** Использование endpoints аутентификации.

**Ограничения:**
*   Не может редактировать или удалять чужие бронирования.
*   Не может создавать, редактировать или удалять события.

### 2. Admin (Администратор)
Привилегированная роль для администраторов системы.

**Права:**
*   **Все права пользователя** (включая просмотр и бронирование событий).
*   **Управление событиями:**
    *   Создание событий (`POST /events`).
    *   Редактирование существующих событий (`PUT /events/{id}`).
    *   Удаление событий (`DELETE /events/{id}`).
*   **Управление бронированиями:**
    *   Просмотр всех бронирований в системе.
    *   Отмена любой брони (`DELETE /bookings/{id}`).

**Примечание по безопасности:**
Все административные endpoints помечены атрибутами авторизации, требующими роль `Admin`. Попытка выполнения действий от имени `User` на этих endpoint'ах приведет к ответу `403 Forbidden`.

### Инициализация администратора

По умолчанию при создании нового пользователя ему назначается роль `User`. 

В системе реализован механизм начального заполнения базы данных (seed). 
При запуске приложения автоматически создаются администраторы, указанные в конфигурации.

#### Настройка seed-пользователей

Логины и пароли администраторов хранятся в `appsettings.Development.json` (этот файл не попадает в git).

**`appsettings.Development.json` (не коммитится):**
```json
{
  "SeedOptions": {
    "Admins": [
      { "Login": "admin", "Password": "admin1" },
      { "Login": "superuser", "Password": "Super123!" }
    ]
  }
}
```
 
В `appsettings.json` массив **Admins** пустой, в целях безопасности.

**`appsettings.json` (коммитится):**
```json
{
  "SeedOptions": {
    "Admins": []
  }
}
```

> ⚠️ appsettings.Development.json уже добавлен в .gitignore.
> Если вы хотите изменить список администраторов или их пароли — редактируйте только appsettings.Development.json.
> Cоздайте данный файл в корневой папке проекта EventMgtApi.Web, взяв за основу appsettings.json, и заполните нужными значениями.

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
- **JWT-токены** — аутентификация пользователей и управление доступом к эндпоинтам API

---

## 🚀 Запуск проекта

### Предварительные требования
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **PostgreSQL 14+** (локально или в Docker)

---

### Настройка PostgreSQL

Сервер PostgreSQL может быть запущен локально или в Docker.

#### Создайте базу данных:

```bash
PGPASSWORD={YOUR_PASSWORD} psql -h localhost -p 5432 -U {YOUR_USER} -c "CREATE DATABASE eventapi;"
```

#### Добавьте строку подключения в appsettings.Development.json:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username={YOUR_USER};Password={YOUR_PASSWORD}"
  }
} 
```
На стенде разработки секреты хранятся в appsettings.Development.json - создайте данный файл в корневой папке проекта EventMgtApi.Web, взяв за основу appsettings.json, и заполните недостаюшими значениями.


#### 🧱 Миграции с Entity Framework Core

Схема базы данных управляется через **миграции EF Core**.

##### Создание миграции

После изменения модели (`AppDbContext`, сущностей и т.п.) создайте новую миграцию:

```bash
dotnet ef migrations add <Название_миграции> --project EventMgtApi.Infrastructure --startup-project EventMgtApi.Web
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

## 🔐 Получение JWT-токена через Swagger

Для работы с защищёнными эндпоинтами (создание событий, бронирование и т.д.) необходима аутентификация через JWT-токен.

### Шаг 1. Зарегистрируйте пользователя

В Swagger UI откройте эндпоинт `POST /api/auth/register`:

1. Нажмите кнопку **Try it out**
2. В поле **Request body** введите JSON:

   ```json
   {
     "login": "User1",
     "password": "User1234!"
   }
   ```

💡 Доступные роли: User. 
Для создания администратора - см. раздел **Инициализация администратора**

3. Нажмите Execute — ответ **201 Created** означает успешную регистрацию.

### Шаг 2. Войдите в систему

Откройте эндпоинт POST /api/auth/login:

1. Нажмите кнопку **Try it out**
2. Введите данные:

   ```json 
   {
     "login": "MyUserName",
     "password": "pass123"
   }
   ```

3. Нажмите Execute — вы получите ответ:

   ```json 
   {
     "token": "eyJhbGciOiJIUzI1NiIs...",
     "login": "MyUserName"
	 "role": "User"
   }
   ```
### Шаг 3. Скопируйте токен

В ответе скопируйте значение поля token.

### Шаг 4.  Авторизуйтесь через Swagger

1. В правом верхнем углу Swagger UI нажмите кнопку **🔒 Authorize**

2. В поле Value введите токен в формате:
 
   ```json 
   eyJhbGciOiJIUzI1NiIs...
   ```

3. Нажмите **Authorize**, затем Close.

Теперь все защищённые эндпоинты станут доступны.

### Шаг 5.  Проверка доступа

1. Откройте защищенный эндпоинт GET /api/Events/{id}.

    *  Рядом с методом должен появиться закрытый замок 🔒.
    *  Нажмите **Try it out**, введите любой ID события и нажмите **Execute**.
	*  Вы должны получить данные (200 OK), а не ошибку 401 Unauthorized.

2. Проверьте права администратора:

	*  Откройте метод POST /api/Events (Создание события).
	*  В теле запроса передайте объект:

   ```json 
   {
	  "title": "Тестовый концерт",
	  "startAt": "2026-08-10T19:00:00Z",
	  "endAt": "2026-08-10T22:00:00Z",
	  "totalSeats": 150
   }
   ```
Если токен имеет роль Admin, вернется ответ 201 Created. Если роль User — придет ошибка 403 Forbidden.

> 💡 **Важные нюансы работы с JWT в Swagger**
> 	*  Срок действия: Токены обычно живут недолго (например, 20–30 минут). Если запросы внезапно начали возвращать 401, просто повторите Шаг 2 (Login) и обновите токен в кнопке Authorize.
> 	*  Копирование токена: При копировании из поля ответа убедитесь, что захватили строку целиком, от eyJ... до последней точки включительно, но без кавычек JSON.
> 	*  Очистка прав: Чтобы выйти из аккаунта в Swagger, нажмите кнопку Authorize снова и нажмите Logout (или очистите поле Value). Замки на методах снова станут серыми.

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
> **HTTP 400 Bad Request** — некорректный формат идентификатора  
> **HTTP 401 Unauthorized** — требуется аутентификация  
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

### 🔍 Отмена брони: `DELETE /api/bookings/{id:guid}`

Переводит бронь в статус **Cancelled**

> **HTTP 204 No Content** — бронь отменена  
> **HTTP 400 Bad Request** — ошибка отмены брони  
> **HTTP 401 Unauthorized** — требуется аутентификация  
> **HTTP 403 Forbidden** — недостаточно прав для отмены брони  
> **HTTP 404 Not Found** — бронь не существует


### 🔍 Регистрация нового пользователя: `POST /api/auth/register`

Регистрирует нового пользователя

> **HTTP 201 Created** — пользователь зарегистрирован  
> **HTTP 400 Bad Request** — ошибка регистрации пользователя  

#### Пример запроса:
```http
POST /api/auth/register

{
  "login": "MyUserLogin",
  "password": "Mypwd123!"
}
```

#### Пример тела ответа при успешной регистрации:
```json
{
  "userId": "ad971e06-e7cb-4c28-a4cc-1edd34586614",
  "login": "string88",
  "role": "User"
}
```

#### Пример тела ответа при ошибке регистрации:
```json
{
  "title": "Ошибка валидации",
  "status": 400,
  "detail": "Обнаружены ошибки валидации входных данных.",
  "instance": "/api/Auth/register",
  "errors": {
    "Login": [
      "Пользователь с таким логином уже существует."
    ]
  }
}
```

### 🔍 Аутентификация пользователя: `POST /api/auth/login`

Аутентифицирует пользователя

> **HTTP 200 OK** — пользователь аутентифицирован  
> **HTTP 400 Bad Request** — ошибка аутентификации  
> **HTTP 404 Not Found** — пользователь не найден

#### Пример запроса:
```http
POST /api/auth/login

{
  "login": "MyUserName",
  "password": "pass123"
}
```

#### Ответ:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cC...",
  "login": "MyUserName",
  "role": "User"
}
```

---

## 🧩 Модель `Booking` и статусы

### `Booking` — модель бронирования
- `Id`: `Guid` — уникальный идентификатор брони
- `EventId`: `Guid` — ссылка на событие
- `UserId`: `Guid` — ссылка на пользователя
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
    Rejected,
	
    /// <summary>Бронь отменена.</summary>
    Cancelled
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
│   │   └── User.cs           # Модель пользователя
│   ├── Enums/
│   │   └── BookingStatus.cs     # Статусы брони: Pending, Confirmed, Rejected
│   ├── Exceptions/
│       ├── BookingPastEventException.cs # Ошибка отмены бронирования прошедшего события (400)
│       ├── ForbiddenException.cs # Ошибка недостаточности прав доступа (403)
│       ├── NoAvailableSeatsException # Ошибка овербукинга (409)
│       ├── NotFoundException.cs # Ошибка "не найдено" (404)
│       ├── TooManyActiveBookingsException.cs # Превышен лимит активных броней пользователя (409)
│       └── ValidationException.cs # Ошибка валидации (400)
│
├── EventMgtApi.Application/      # Логика приложения: сервисы, DTO, маппинг
│   ├── Abstractions/
│   │   ├── Persistence/
│   │   │   ├── Repositories/
│   │   │       ├── IEventRepository.cs  # Абстракция доступа к событиям
│   │   │       └── IBookingRepository.cs # Абстракция доступа к броням
│   │   │       └── IUserRepository.cs # Абстракция доступа к пользователям
│   │   └── Services/
│   │       ├── IEventService.cs     # Интерфейс управления событиями
│   │       ├── IBookingService.cs   # Интерфейс управления бронями
│   │       ├── IUserService.cs   # Интерфейс управления пользователям
│   │       └── ISeedService.cs   # Интерфейс начальным заполнением БД
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
│   ├── Users/
│   │   ├── UserService.cs      # Реализация бизнес-логики управления пользователями
│   │   ├── SeedService.cs      # Начальное заполнение БД
│   │   ├── DTOs/
│   │       ├── UserDtos.cs          # DTO для регистрации/аутентификации пользователя
│   │       
│   └── DependencyInjection/
│       ├── ApplicationServiceCollectionExtensions.cs    # Регистрация сервисов уровня приложения
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
│   │   └── BookingProcessingBackgroundService.cs # Обработка Pending → Confirmed (с SemaphoreSlim)
│   └── DependencyInjection/
│       ├── InfrastructureServiceCollectionExtensions.cs    # Регистрация сервисов уровня инфраструктуры
│
├── EventMgtApi.Web/              # Входная точка API (Presentation Layer)
│   ├── Controllers/
│   │   ├── EventsController.cs  # Обработка /api/events
│   │   ├── BookingsController.cs # Обработка /api/bookings
│   │   └── AuthController.cs # Обработка /api/auth
│   ├── Middleware/
│   │   └── GlobalExceptionHandlingMiddleware.cs # Централизованная обработка ошибок
│   ├── Filters/
│   │   └── ThrowValidationExceptionFilter.cs # Преобразует ModelState в исключение
│   │   └── RemoveAuthForAnonymousOperations.cs # Фильтр операций Swagger
│   └── Extensions/
│   │    ├── ApplicationBuilderExtensions.cs # Метод UseGlobalExceptionHandling()
│   │    └── ServiceCollectionExtensions.cs # Методы регистрации сервисов
│   │
│   ├── Program.cs                # Настройка DI, слоёв, маршрутов, Swagger
│
├── EventMgtApi.UnitTests/        # Юнит-тесты
│   ├── EventServiceTests.cs
│   ├── BookingServiceTests.cs
│   ├── UserServiceTests.cs
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
└── docker-compose.yml            # Конфигурация Docker Compose
```

---

#### 🔁 Принципы разделения:

- **Domain** — не зависит ни от чего. Содержит только бизнес-сущности и контракты.
- **Application** — зависит от `Domain`. Содержит логику, DTO и сервисы.
- **Infrastructure** — зависит от `Domain` и `Application`. Реализует абстракции (например, репозитории).
- **Presentation** — зависит от `Application`, `Domain` и `Infrastructure`. Отвечает за HTTP, контроллеры, middleware.

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

## ⚙️ JWT-конфигурация

Аутентификация основана на JWT-токенах (Bearer). Настройки читаются из конфигурации:

```json
{
  "Jwt": {
    "Secret": "your-secret-key-here",
    "Issuer": "EventMgtApi",
    "Audience": "EventMgtApi",
    "ExpiryMinutes": 60
  }
}
```

> 🔒 Безопасность секрета:

- На стенде разработки секреты хранятся в appsettings.Development.json - создайте данный файл в корневой папке проекта EventMgtApi.Web, взяв за основу appsettings.json, и заполните недостаюшими значениями.
- Для Jwt:Secret используйте любое случайное значение (минимум 32 символа).
- В продакшене секрет должен задаваться через защищённый источник:
	- Переменные окружения (не через appsettings.json);
	- Azure Key Vault, AWS Secrets Manager, HashiCorp Vault;

## ⚙️ Seed-конфигурация

Начальное заполнение базы данных (seed) настраивается через `SeedOptions`. 
Администраторы создаются автоматически при запуске приложения, после применения миграций БД.

```json
{
  "SeedOptions": {
    "Admins": [
      { "Login": "admin", "Password": "Admin123!" }
    ]
  }
}
```

Если пароль не указан, будет выброшено исключение.
Администраторы с таким логином не создаются повторно.

---

## 🧱 Тесты

### Unit-тесты

Реализованы unit-тесты для:
- сервисов `EventService`, `BookingService`, `UserService`, `BookingProcessingBackgroundService`.
- сущностей `Event`, `Booking` и `User`

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

### ⚠️ Ограничения

- Часовые пояса не обрабатываются (все даты в UTC).
- Нет поддержки refresh-токенов.

---

### 🚧 Будущие улучшения

- Сделать Docker-образ
- Интеграция с email и платежами

---

> 🙌 Спасибо за использование!
