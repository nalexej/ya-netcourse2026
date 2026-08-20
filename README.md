# Event Management API

Простое RESTful API для управления событиями (мероприятиями).  
Реализовано на **ASP.NET Core** с использованием **C# 12** (**.NET 10**) и современных практик разработки.

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

### События:
- ✅ Получить список всех событий (`GET /api/events`)
- ✅ Получить событие по ID (`GET /api/events/{id}`)
- ✅ Добавить новое событие (`POST /api/events`)
- ✅ Обновить существующее (`PUT /api/events/{id}`)
- ✅ Удалить событие (`DELETE /api/events/{id}`)

### Брони:
- ✅ Создать бронь на событие (`POST /api/events/{id}/book`)
- ✅ Проверить статус брони (`GET /api/bookings/{id}`)
- ✅ Отменить бронь (`DELETE /api/bookings/{id}`)

### Пользователи:
- ✅ Зарегистрировать нового пользователя (`POST /api/auth/register`)
- ✅ Аутентифицировать пользователя (`POST /api/auth/login`)

### С поддержкой:
- Валидации входных данных
- Понятных ошибок на русском языке
- Корректных HTTP-статусов (200, 201, 202, 400, 404, 409 и др.)
- Фоновой обработки броней
- **Защиты от овербукинга** (ограничение по TotalSeats и AvailableSeats)
- **Ролевого доступа** (User/Admin)
- **Асинхронной межсервисной коммуникации** через **Kafka** (pub/sub)

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
> Создайте данный файл в корневой папке проекта каждого сервиса, взяв за основу appsettings.json, и заполните нужными значениями.

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
- **Apache Kafka** — асинхронная межсервисная коммуникация (pub/sub)
- **Confluent.Kafka** — .NET-клиент для Kafka

---

## 🏗 Архитектура решения

Проект построен как набор **трёх независимых микросервисов** с **разделяемым проектом контрактов**.  
Каждый сервис реализован по принципам **чистой архитектуры (Clean Architecture)** с чётким разделением на слои.

### Общая структура

```
HomeWork/
├── EventMgtService.sln                    # Решение (3 сервиса + Contracts + тесты)
├── docker-compose.yml                     # PostgreSQL + Kafka
│
├── EventMgtApi.Contracts/                 # 📦 Разделяемый проект контрактов
│   ├── EventMgtApi.Contracts.csproj
│   ├── Enums/
│   │   ├── BookingStatus.cs               # Статусы бронирований
│   │   └── UserRole.cs                    # Роли пользователей
│   ├── Events/DTOs/
│   │   ├── EventDto.cs                    # DTO создания/обновления события
│   │   ├── EventDtoResponse.cs            # DTO ответа по событию
│   │   └── PaginatedResult.cs             # Пагинация
│   ├── Bookings/DTOs/
│   │   ├── BookingResponseDto.cs          # DTO ответа по бронированию
│   │   └── CreateBookingRequestDto.cs     # DTO создания брони
│   ├── Users/DTOs/
│   │   └── UserDtos.cs                    # DTO пользователей (регистрация, логин)
│   ├── Services/
│   │   ├── IUserService.cs                # Контраст сервиса пользователей
│   │   ├── IEventService.cs               # Контраст сервиса событий
│   │   └── IBookingService.cs             # Контраст сервиса броней
│   ├── Options/
│   │   └── JwtOptions.cs                  # Конфигурация JWT
│   ├── ServiceInteraction/
│   │   ├── ServiceInteractionConstants.cs # Константы топиков Kafka
│   │   └── ServiceEvents/
│   │       └── BookingConfirmed.cs        # Событие Kafka "Бронь подтверждена"
│   └── Middleware/                        # Общие middleware (используются всеми сервисами)
│
├── EventMgtApi.UsersService/              # 👤 Сервис пользователей (аутентификация)
│   ├── EventMgtApi.UsersService.Domain/
│   │   ├── Entities/
│   │   │   └── User.cs                    # Сущность пользователя
│   │   ├── Exceptions/
│   │   │   ├── InvalidCredentialsException.cs
│   │   │   └── ValidationException.cs
│   │   ├── Options/
│   │   │   └── SeedOptions.cs             # Seed-конфигурация администраторов
│   │   └── EventMgtApi.UsersService.Domain.csproj
│   ├── EventMgtApi.UsersService.Application/
│   │   ├── Services/
│   │   │   └── UserService.cs             # Бизнес-логика: регистрация, логин
│   │   ├── Abstractions/
│   │   │   ├── Persistence/
│   │   │   │   └── IUserRepository.cs     # Репо-интерфейс
│   │   │   └── Services/
│   │   │       ├── IJwtTokenService.cs    # Контраст генерации JWT
│   │   │       ├── IPasswordHasher.cs     # Контраст хеширования паролей
│   │   │       └── ISeedService.cs        # Контраст seed-заполнения
│   │   ├── DependencyInjection/
│   │   │   └── ApplicationServiceCollectionExtensions.cs
│   │   └── EventMgtApi.UsersService.Application.csproj
│   ├── EventMgtApi.UsersService.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── UserDbContext.cs           # DbContext для пользователей
│   │   │   ├── Configurations/
│   │   │   │   └── UserConfiguration.cs   # Fluent-конфигурация Entity
│   │   │   └── Repositories/
│   │   │       └── UserRepository.cs      # Реализация IUserRepository
│   │   ├── Services/
│   │   │   ├── JwtTokenService.cs         # Генерация JWT-токенов
│   │   │   ├── PasswordHasher.cs          # Хеширование паролей (PBKDF2)
│   │   │   └── SeedService.cs             # Seed-заполнение администраторов
│   │   ├── DependencyInjection/
│   │   │   └── InfrastructureServiceCollectionExtensions.cs
│   │   ├── Migrations/                    # Миграции EF Core
│   │   └── EventMgtApi.UsersService.Infrastructure.csproj
│   └── EventMgtApi.UsersService.Web/
│       ├── Controllers/
│       │   └── AuthController.cs          # POST /register, POST /login
│       ├── Middleware/
│       │   ├── GlobalExceptionHandlingMiddleware.cs
│       │   └── JwtBearerEvents.cs         # Обработка событий JWT Bearer
│       ├── Filters/
│       │   ├── RemoveAuthForAnonymousOperations.cs
│       │   └── ThrowValidationExceptionFilter.cs
│       ├── Extensions/
│       │   └── ApplicationBuilderExtensions.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── EventMgtApi.UsersService.Web.csproj
│
├── EventMgtApi.EventsService/             # 📅 Сервис событий (CRUD)
│   ├── EventMgtApi.EventsService.Domain/
│   │   ├── Entities/
│   │   │   └── Event.cs                   # Сущность события
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs
│   │   │   ├── ValidationException.cs
│   │   │   └── NoAvailableSeatsException.cs
│   │   ├── Options/
│   │   │   └── KafkaConsumerOptions.cs    # Конфигурация Kafka-консьюмера
│   │   └── EventMgtApi.EventsService.Domain.csproj
│   ├── EventMgtApi.EventsService.Application/
│   │   ├── Events/
│   │   │   ├── EventService.cs            # Бизнес-логика: CRUD событий
│   │   │   └── Extensions/
│   │   │       └── EventMappingExtensions.cs
│   │   ├── Persistence/
│   │   │   └── IEventRepository.cs        # Репо-интерфейс
│   │   ├── DependencyInjection/
│   │   │   └── ApplicationServiceCollectionExtensions.cs
│   │   └── EventMgtApi.EventsService.Application.csproj
│   ├── EventMgtApi.EventsService.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── EventDbContext.cs          # DbContext для событий
│   │   │   ├── Configurations/
│   │   │   │   └── EventConfiguration.cs  # Fluent-конфигурация Entity
│   │   │   └── Repositories/
│   │   │       └── EventRepository.cs     # Реализация IEventRepository
│   │   ├── ServiceInteractions/
│   │   │   ├── EventServiceMessagingConsumer.cs # Подписка на Kafka-события
│   │   │   └── KafkaTopicInitializer.cs # Инициализация топиков Kafka
│   │   ├── DependencyInjection/
│   │   │   └── InfrastructureServiceCollectionExtensions.cs
│   │   ├── Migrations/
│   │   └── EventMgtApi.EventsService.Infrastructure.csproj
│   └── EventMgtApi.EventsService.Web/
│       ├── Controllers/
│       │   └── EventsController.cs         # GET/POST/PUT/DELETE /events
│       ├── Middleware/
│       │   ├── GlobalExceptionHandlingMiddleware.cs
│       │   └── JwtBearerEvents.cs
│       ├── Filters/
│       │   ├── RemoveAuthForAnonymousOperations.cs
│       │   └── ThrowValidationExceptionFilter.cs
│       ├── Extensions/
│       │   └── ApplicationBuilderExtensions.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── EventMgtApi.EventsService.Web.csproj
│
├── EventMgtApi.BookingsService/           # 🎫 Сервис бронирований
│   ├── EventMgtApi.BookingsService.Domain/
│   │   ├── Entities/
│   │   │   └── Booking.cs                 # Сущность бронирования
│   │   ├── Exceptions/
│   │   │   ├── BookingPastEventException.cs
│   │   │   ├── ForbiddenException.cs
│   │   │   ├── TooManyActiveBookingsException.cs
│   │   │   ├── NoAvailableSeatsException.cs
│   │   │   ├── NotFoundException.cs
│   │   │   └── ValidationException.cs
│   │   ├── Options/
│   │   │   ├── BookingOptions.cs          # Конфиг бронирования (лимиты и т.д.)
│   │   │   └── KafkaOptions.cs            # Конфигурация Kafka-продюсера
│   │   └── EventMgtApi.BookingsService.Domain.csproj
│   ├── EventMgtApi.BookingsService.Application/
│   │   ├── Bookings/
│   │   │   ├── BookingService.cs          # Бизнес-логика: создание, отмена броней
│   │   │   └── Extensions/
│   │   │       └── BookingsMappingExtensions.cs
│   │   ├── Persistence/
│   │   │   └── IBookingRepository.cs      # Репо-интерфейс
│   │   ├── ServiceInteraction/
│   │   │   └── IEventPublisher.cs         # Контраст публикации Kafka-событий
│   │   ├── DependencyInjection/
│   │   │   └── ApplicationServiceCollectionExtensions.cs
│   │   └── EventMgtApi.BookingsService.Application.csproj
│   ├── EventMgtApi.BookingsService.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── BookingDbContext.cs        # DbContext для бронирований
│   │   │   ├── Configurations/
│   │   │   │   └── BookingConfiguration.cs # Fluent-конфигурация Entity
│   │   │   └── Repositories/
│   │   │       └── BookingRepository.cs   # Реализация IBookingRepository
│   │   ├── Persistence/Services/
│   │   │   └── BookingProcessingBackgroundService.cs # Фоновая обработка Pending-броней
│   │   ├── ServiceInteraction/
│   │   │   └── BookingServiceMessagingPublisher.cs     # Реализация публикации в Kafka
│   │   ├── DependencyInjection/
│   │   │   └── InfrastructureServiceCollectionExtensions.cs
│   │   ├── Migrations/
│   │   └── EventMgtApi.BookingsService.Infrastructure.csproj
│   └── EventMgtApi.BookingsService.Web/
│       ├── Controllers/
│       │   └── BookingsController.cs       # POST /events/{id}/book, GET/DELETE /bookings
│       ├── Middleware/
│       │   ├── GlobalExceptionHandlingMiddleware.cs
│       │   └── JwtBearerEvents.cs
│       ├── Filters/
│       │   ├── RemoveAuthForAnonymousOperations.cs
│       │   └── ThrowValidationExceptionFilter.cs
│       ├── Extensions/
│       │   └── ApplicationBuilderExtensions.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── EventMgtApi.BookingsService.Web.csproj
│
├── EventMgtApi.UnitTests/                 # 🧪 Unit-тесты
├── EventMgtApi.IntegrationTests/          # 🧪 Интеграционные тесты
│
└── docker-compose.yml                     # 🐳 PostgreSQL + Kafka
```

---

#### 🔁 Принципы разделения слоёв (в каждом сервисе):

| Слой | Зависит от | Ответственность |
|------|-----------|-----------------|
| **Domain** | Ничего | Бизнес-сущности, исключения, доменные опции. Не зависит ни от чего внешнего. |
| **Application** | Domain | Бизнес-логика, DTO, контракты репозиториев (Abstractions), DI-расширения. |
| **Infrastructure** | Domain + Application | Реализации репозиториев (EF Core), фоновые службы, Kafka-клиенты, DI-расширения. |
| **Presentation** | Application + Domain + Infrastructure | HTTP-контроллеры, middleware, фильтры, конфигурация приложения. |

> 💡 Такая структура позволяет:
> - Легко заменить хранилище (EF Core → Redis и т.д.).
> - Писать изолированные unit-тесты.
> - Добавлять новые функции без нарушения существующего кода.
> - Поддерживать проект при росте числа разработчиков.
> - Запускать каждый сервис независимо.

### 🔄 Межсервисная коммуникация

Сервисы **не общаются** напрямую через HTTP-вызовы друг к другу. Вместо этого используется **Apache Kafka** для асинхронной pub/sub коммуникации:

| Событие | Издатель | Подписчики | Описание |
|---------|----------|------------|----------|
| `BookingConfirmed` | `BookingsService` | `EventsService` | Уведомление о подтверждённой брони для обновления количества доступных мест |

- **BookingsService** — публикует событие `BookingConfirmed` в Kafka при подтверждении брони.
- **EventsService** — подписывается на топик и обновляет `AvailableSeats` у события.

Общие контракты событий (сериализуемые DTO) хранятся в `EventMgtApi.Contracts/ServiceInteraction/ServiceEvents/`.

---

## 🚀 Запуск проекта

### Предварительные требования
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **PostgreSQL 14+** (локально или в Docker)
- **Apache Kafka** (локально или в Docker)
- **[Docker](https://www.docker.com/)** (для быстрого запуска PostgreSQL и Kafka через `docker compose up -d`)

---

### Настройка PostgreSQL и Kafka

Серверы PostgreSQL и Kafka могут быть запущены локально или в Docker.

#### Запуск через Docker (рекомендуется)

Проще всего запустить инфраструктуру с помощью `docker compose`:

```bash
docker compose up -d
```

Это запустит контейнеры с PostgreSQL и Kafka по умолчанию:

| Сервис | Хост | Порт |
|--------|------|------|
| PostgreSQL | `localhost` | `5432` |
| Database | `eventapi` | — |
| Username | `postgres` | — |
| Password | `postgres` | — |
| Kafka | `localhost` | `9092` |

#### Создайте базу данных (ручной способ):

```bash
PGPASSWORD=postgres psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE eventapi;"
```

#### Добавьте строки подключения в `appsettings.Development.json` **каждого сервиса**:

**UsersService** (`EventMgtApi.UsersService.Web/appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi_users;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-secret-key-here-must-be-at-least-32-chars-long",
    "Issuer": "EventMgtApi",
    "Audience": "EventMgtApi",
    "ExpiryMinutes": 60
  },
  "SeedOptions": {
    "Admins": [
      { "Login": "admin", "Password": "admin1" },
      { "Login": "superuser", "Password": "Super123!" }
    ]
  }
}
```

**EventsService** (`EventMgtApi.EventsService.Web/appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi_events;Username=postgres;Password=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "events-service-group"
  }
}
```

**BookingsService** (`EventMgtApi.BookingsService.Web/appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi_bookings;Username=postgres;Password=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "BookingOptions": {
    "MaxActiveBookingsPerUser": 10
  }
}
```

> 🔒 Секреты (JWT Secret, пароли) хранятся в `appsettings.Development.json` — этот файл не попадает в git.
> Создайте файл `appsettings.Development.json` в корневой папке каждого сервиса, взяв за основу `appsettings.json`.

---

### Сборка и запуск

В корне репозитория выполните:

```bash
dotnet restore
dotnet build
```

Запуск каждого сервиса:

```bash
# Сервис пользователей
dotnet run --project EventMgtApi.UsersService/EventMgtApi.UsersService.Web/EventMgtApi.UsersService.Web.csproj

# Сервис событий
dotnet run --project EventMgtApi.EventsService/EventMgtApi.EventsService.Web/EventMgtApi.EventsService.Web.csproj

# Сервис бронирований
dotnet run --project EventMgtApi.BookingsService/EventMgtApi.BookingsService.Web/EventMgtApi.BookingsService.Web.csproj
```

🔐 Каждый сервис работает по HTTPS на своём порту (указан в `Properties/launchSettings.json`).

---

### Адреса после запуска

| Сервис | Swagger UI |
|--------|-----------|
| **UsersService** (аутентификация) | https://localhost:7001/swagger |
| **EventsService** (события) | https://localhost:7002/swagger |
| **BookingsService** (брони) | https://localhost:7003/swagger |

> 💡 Порты могут отличаться — смотрите `Properties/launchSettings.json` каждого сервиса.

---

## 🔐 Получение JWT-токена через Swagger

Для работы с защищёнными эндпоинтами (создание событий, бронирование и т.д.) необходима аутентификация через JWT-токен.

### Шаг 1. Зарегистрируйте пользователя

В Swagger UI сервиса **UsersService** откройте эндпоинт `POST /api/auth/register`:

1. Нажмите кнопку **Try it out**
2. В поле **Request body** введите JSON:

   ```json
   {
     "login": "User1",
     "password": "User1234!"
   }
   ```

💡 Доступные роли: User. Для создания администратора — см. раздел **Инициализация администратора**.

3. Нажмите Execute — ответ **201 Created** означает успешную регистрацию.

### Шаг 2. Войдите в систему

Откройте эндпоинт `POST /api/auth/login` в **UsersService**:

1. Нажмите кнопку **Try it out**
2. Введите данные:

   ```json
   {
     "login": "User1",
     "password": "User1234!"
   }
   ```

3. Нажмите Execute — вы получите ответ:

   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIs...",
     "login": "User1",
     "role": "User"
   }
   ```

### Шаг 3. Скопируйте токен

В ответе скопируйте значение поля `token`.

### Шаг 4. Авторизуйтесь через Swagger

1. В правом верхнем углу Swagger UI соответствующего сервиса нажмите кнопку **🔒 Authorize**
2. В поле Value введите токен (без кавычек JSON)
3. Нажмите **Authorize**, затем **Close**

Теперь все защищённые endpoints станут доступны.

### Шаг 5. Проверка доступа

1. Откройте защищенный эндпоинт в сервисе **EventsService**, например `GET /api/events/{id}`.
   - Рядом с методом должен появиться закрытый замок 🔒.
   - Нажмите **Try it out**, введите любой ID события и нажмите **Execute**.
   - Вы должны получить данные (200 OK), а не ошибку 401 Unauthorized.

2. Проверьте права администратора:
   - Откройте метод `POST /api/events` в **EventsService**.
   - В теле запроса передайте объект:

   ```json
   {
     "title": "Тестовый концерт",
     "startAt": "2026-10-10T19:00:00Z",
     "endAt": "2026-11-10T22:00:00Z",
     "totalSeats": 150
   }
   ```

   Если токен имеет роль `Admin`, вернётся ответ `201 Created`. Если роль `User` — придёт ошибка `403 Forbidden`.

> 💡 **Важные нюансы работы с JWT в Swagger**
> - **Срок действия:** Токены обычно живут недолго (например, 20–30 минут). Если запросы внезапно начали возвращать 401, просто повторите Шаг 2 (Login) и обновите токен.
> - **Копирование токена:** При копировании из поля ответа убедитесь, что захватили строку целиком, от `eyJ...` до последней точки включительно, но без кавычек JSON.
> - **Очистка прав:** Чтобы выйти из аккаунта в Swagger, нажмите кнопку Authorize снова и нажмите Logout (или очистите поле Value). Замки на методах снова станут серыми.

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
  "startAt": "2027-04-05T10:00:00Z",
  "endAt": "2027-04-05T11:00:00Z"
}
```

> ✅ **Успешный ответ (201 Created)**

```http
HTTP/1.1 201 Created
Location: https://localhost:7002/api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Team Meeting",
  "description": "Обсуждение планов",
  "startAt": "2027-04-05T10:00:00Z",
  "endAt": "2027-04-05T11:00:00Z"
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

### 🆕 Создание брони: `POST /api/events/{eventId:guid}/book`

Сервис **BookingsService** cоздаёт новую бронь на указанное событие.  
Бронь изначально имеет статус `Pending`.

> **HTTP 201 Created** — бронь создана и принята в обработку  
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
HTTP/1.1 201 Created
Location: https://localhost:7003/api/bookings/9e1b2f4d-8a3c-4e2a-9f1a-1b2c3d4e5f6a
```

```json
{
  "id": "9e1b2f4d-8a3c-4e2a-9f1a-1b2c3d4e5f6a",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "createdAt": "2027-04-05T14:30:00Z",
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

Сервис **BookingsService** возвращает текущий статус брони по её идентификатору.

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

Сервис **BookingsService** переводит бронь в статус **Cancelled**.

> **HTTP 204 No Content** — бронь отменена  
> **HTTP 400 Bad Request** — ошибка отмены брони  
> **HTTP 401 Unauthorized** — требуется аутентификация  
> **HTTP 403 Forbidden** — недостаточно прав для отмены брони  
> **HTTP 404 Not Found** — бронь не существует

### 🔍 Регистрация нового пользователя: `POST /api/auth/register`

Сервис **UsersService** регистрирует нового пользователя.

> **HTTP 201 Created** — пользователь зарегистрирован  
> **HTTP 400 Bad Request** — ошибка регистрации пользователя

#### Пример запроса:
```http
POST /api/auth/register
```

```json
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

ервис **UsersService** аутентифицирует пользователя.

> **HTTP 200 OK** — пользователь аутентифицирован  
> **HTTP 400 Bad Request** — ошибка аутентификации  
> **HTTP 404 Not Found** — пользователь не найден

#### Пример запроса:
```http
POST /api/auth/login
```

```json
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

### Фоновая обработка броней (BookingsService)

Брони со статусом `Pending` обрабатываются фоновым сервисом **в автоматическом режиме**.

- Сервис `BookingProcessingBackgroundService` запускается при старте сервиса **BookingsService** .
- Каждые **5 секунд** он:
  - Ищет все брони со статусом `Pending`
  - Для каждой:
    - Ждёт **2 секунды** (имитация внешней системы)
    - Меняет статус на `Confirmed`
    - Устанавливает `ProcessedAt = DateTime.UtcNow`
    - Публикует событие `BookingConfirmed` в **Kafka**
    - Сохраняет изменения

> 💡 Это имитирует интеграцию с платёжной системой или внешним API.

### Обработка события `BookingConfirmed` (EventsService)

Сервис **EventsService** подписан на Kafka-топик и при получении события `BookingConfirmed`:
- Находит соответствующее событие по `EventId`
- Уменьшает `AvailableSeats` на 1

> 💡 Это обеспечивает согласованность данных между сервисами без синхронных HTTP-вызовов.

### Особенности фоновых служб:
- Потокобезопасны
- Работают асинхронно
- Логируют каждый шаг
- Корректно останавливаются при завершении приложения

---

## 🔐 Валидация

Все поля в `EventDto` проходят валидацию:
- `[Required]`
- Кастомная проверка: `StartAt < EndAt`
- Сообщения на русском языке.
- Защита от `null` и логических ошибок.

---

## ⚙️ JWT-конфигурация

Аутентификация основана на JWT-токенах (Bearer). Настройки читаются из конфигурации каждого сервиса:

```json
{
  "Jwt": {
    "Secret": "your-secret-key-here",
    "Issuer": "EventMgtApi.UsersService",
    "Audience": "EventMgtApi",
    "ExpiryMinutes": 60
  }
}
```

> 🔒 Безопасность секрета:
> - На стенде разработки секреты хранятся в `appsettings.Development.json` — создайте данный файл в корневой папке каждого сервиса, взяв за основу `appsettings.json`.
> - Для `Jwt.Secret` используйте любое случайное значение (минимум 32 символа).
> - В продакшене секрет должен задаваться через защищённый источник:
>   - Переменные окружения (не через `appsettings.json`);
>   - Azure Key Vault, AWS Secrets Manager, HashiCorp Vault.

## ⚙️ Seed-конфигурация

Начальное заполнение базы данных (seed) настраивается через `SeedOptions`. 
Администраторы и системный пользователь (**anonymous**) создаются автоматически при запуске приложения, после применения миграций БД.

Пример настройки в `appsettings.Development.json`
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
- сущностей `Event`, `Booking` и `User`.

Запуск unit-тестов (в корне репозитория):

```bash
dotnet test EventMgtApi.UnitTests/EventMgtApi.UnitTests.csproj
```

### Интеграционные тесты

В проект включены интеграционные тесты (`EventMgtApi.IntegrationTests`), которые проверяют взаимодействие с базой данных через реальный `AppDbContext`.
К интеграционным тестам также добавлены тесты на конкурентность.

Для запуска интеграционных тестов требуется Docker — тестовый контейнер с PostgreSQL запускается автоматически через testcontainers.

**Требования:**
- Установленный Docker
- .NET 10+ SDK (используется EF Core 10 — актуально на текущий момент)

Запуск интеграционных тестов (в корне репозитория):

```bash
dotnet test EventMgtApi.IntegrationTests/EventMgtApi.IntegrationTests.csproj
```

> 💡 При первом запуске Docker скачает образ `postgres:16-alpine`

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

- Сделать Docker-образ для каждого сервиса
- Интеграция с email и платежами
- Distributed Tracing (OpenTelemetry)
- API Gateway (Ocelot / YARP)

---

> 🙌 Спасибо за использование!
