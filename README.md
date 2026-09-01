# Event Management API

RESTful API для управления событиями на основе микросервисной архитектуры.
Создана на базе **ASP.NET Core** с использованием **C# 12** (**.NET 10**) и принципов **чистой архитектуры** (Clean Architecture).

> 🎯 Три независимых микросервиса, разделяющие проект контрактов, взаимодействующие асинхронно через **Apache Kafka**.

---

## 📋 Модель события

Каждое событие имеет следующие поля:

| Поле | Тип | Описание |
|------|------|----------|
| `Id` | `Guid` | Уникальный идентификатор |
| `Title` | `string` | Название события |
| `Description` | `string?` | Описание (необязательно) |
| `StartAt` | `DateTime` | Дата и время начала |
| `EndAt` | `DateTime` | Дата и время окончания |
| `TotalSeats` | `int` | **Общее количество мест** (обязательное, > 0) |
| `AvailableSeats` | `int` | **Текущее количество доступных мест** |

> 💡 При создании `AvailableSeats` автоматически устанавливается равным `TotalSeats`.
> При успешном бронировании `AvailableSeats` уменьшается на 1 (или на указанное значение).

---

## 📋 Функционал

API предоставляет полный цикл **CRUD**:

### События (EventsService):
- ✅ Получить все события (`GET /api/events`)
- ✅ Получить событие по ID (`GET /api/events/{id}`)
- ✅ Создать новое событие (`POST /api/events`) — **только для Администратора**
- ✅ Обновить существующее событие (`PUT /api/events/{id}`) — **только для Администратора**
- ✅ Удалить событие (`DELETE /api/events/{id}`) — **только для Администратора**

### Бронирования (BookingsService):
- ✅ Создать бронирование на событие (`POST /api/events/{id}/book`)
- ✅ Проверить статус бронирования (`GET /api/bookings/{id}`)
- ✅ Отменить бронирование (`DELETE /api/bookings/{id}`)

### Пользователи и авторизация (UsersService):
- ✅ Зарегистрировать нового пользователя (`POST /api/auth/register`)
- ✅ Аутентифицировать пользователя (`POST /api/auth/login`)

### Поддерживаемые возможности:
- Валидация входных данных с сообщениями об ошибках на русском языке
- Корректные HTTP-статусы (200, 201, 202, 400, 404, 409 и т.д.)
- Фоновая обработка бронирований
- **Защита от овербукинга** (ограничение по `TotalSeats` и `AvailableSeats`)
- **Доступ на основе ролей** (Пользователь/Администратор)
- **Асинхронное межсервисное взаимодействие** через **Apache Kafka** (pub/sub)

---

## 🎭 Ролевая модель

Система поддерживает два уровня доступа,Defined в перечислении `UserRole`:

### 1. Пользователь (User)

**Права:**
* **Просмотр событий:** Публичный список и детали (`GET /events`).
* **Бронирование событий:** Создание бронирований на доступные события (`POST /bookings`).
  * Подвергается ограничению на количество активных бронирований (по умолчанию: 10 на пользователя).
  * Нельзя бронировать прошедшие события.
* **Управление своими бронированиями:**
  * Просмотр своих бронирований (`GET /bookings`).
  * Отмена своего бронирования (`DELETE /bookings/{id}`), если событие еще не началось.
* **Регистрация и вход:** Использование эндпоинтов авторизации.

**Ограничения:**
* Нельзя редактировать или удалять чужие бронирования.
* Нельзя создавать, редактировать или удалять события.

### 2. Администратор (Admin)

**Права:**
* **Все права пользователя** (просмотр и бронирование событий).
* **Управление событиями:**
  * Создание событий (`POST /events`).
  * Редактирование событий (`PUT /events/{id}`).
  * Удаление событий (`DELETE /events/{id}`).
* **Управление бронированиями:**
  * Просмотр всех бронирований в системе.
  * Отмена любого бронирования (`DELETE /bookings/{id}`).

**Примечание по безопасности:**
Все административные эндпоинты помечены атрибутом `[Authorize(Roles = "Admin")]`. Попытка выполнения административных действий от имени `User` возвращает `403 Forbidden`.

### Инициализация администраторов

По умолчанию новые пользователи получают роль `User`.
Механизм seed создаёт администраторов, указанных в конфигурации, при запуске.

#### Настройка seed-администраторов

##### Если запускаете в докере

Для настройки seed-админа в докере (перед выполнением **docker compose up**), необходимо задать переменные окружения: SEED_ADMIN_LOGIN и SEED_ADMIN_PASSWORD.
Указанные переменные можно задать, например, в файле .env, который нужно поместить в одну папку с docker-compose.yml.
В продакшене эти переменные также могут устанавливаться в окружении хоста или в CI/CD pipeline.

Подробнее см. раздел **Запуск проекта**.

##### Если запускаете вручную

Логины и пароли администраторов хранятся в `appsettings.Development.json` (не коммитятся в git).

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

**`appsettings.json` (коммитится):**
```json
{
  "SeedOptions": {
    "Admins": []
  }
}
```

> ⚠️ `appsettings.Development.json` находится в `.gitignore`.
> Для изменения администраторов редактируйте только `appsettings.Development.json` для каждого сервиса.

---

## 🔍 Фильтрация событий — GET /events

Поддерживаемые параметры запроса:
- `title` — частичный поиск без учёта регистра
- `from` — события, начинающиеся не ранее этой даты
- `to` — события, заканчивающиеся не позже этой даты
- `page` — номер страницы (мин 1, по умолчанию 1)
- `pageSize` — размер страницы (1–100, по умолчанию 10)

> Пример:
> `GET /api/events?title=meeting&from=2026-05-14&to=2026-05-15&page=1&pageSize=5`

---

## 🛠 Технологии

- **.NET 10** / **C# 12**
- **ASP.NET Core Web API**
- **PostgreSQL 16** (по одной базе на сервис)
- **Entity Framework Core 10** (провайдер Npgsql)
- **Dependency Injection (DI)**
- **DTOs** — изоляция моделей
- **Кастомная валидация** через исключения домена
- **Потокобезопасные операции** с `SemaphoreSlim`
- **Swagger UI** — документация API
- **XML-документация** — IntelliSense и Swagger
- **Фоновые сервисы** — обработка бронирований
- **JWT-токены** — аутентификация и авторизация
- **Apache Kafka** — асинхронное межсервисное взаимодействие (pub/sub)
- **Confluent.Kafka** — .NET-клиент для Kafka

---

## 🏗 Архитектура

Решение состоит из **трёх независимых микросервисов** и **разделяемого проекта контрактов**.
Каждый сервис следует **чистой архитектуре** с чётким разделением слоёв.

### Структура решения

```
HomeWork/
├── EventMgtService.sln                    # Решение (3 сервиса + Contracts)
├── docker-compose.yml                     # PostgreSQL (×3) + Kafka + Zookeeper + Kafka UI
│
├── EventMgtApi.Contracts/                 # 📦 Разделяемый проект контрактов
│   ├── EventMgtApi.Contracts.csproj
│   ├── Enums/
│   │   ├── BookingStatus.cs               # Статусы бронирований
│   │   └── UserRole.cs                    # Роли пользователей
│   ├── Events/DTOs/
│   │   ├── EventDto.cs                    # DTO для создания/обновления события
│   │   ├── EventDtoResponse.cs            # DTO ответа события
│   │   └── PaginatedResult.cs             # Результат пагинации
│   ├── Bookings/DTOs/
│   │   ├── BookingResponseDto.cs          # DTO ответа бронирования
│   │   └── CreateBookingRequestDto.cs     # DTO запроса на создание бронирования
│   ├── Users/DTOs/
│   │   ├── RegisterRequestDto.cs          # Запрос на регистрацию
│   │   ├── RegisterResponseDto.cs         # Ответ регистрации
│   │   ├── LoginRequestDto.cs             # Запрос на вход
│   │   └── LoginResponseDto.cs            # Ответ входа (с JWT-токеном)
│   ├── Services/
│   │   ├── IUserService.cs                # Интерфейс сервиса пользователей
│   │   ├── IEventService.cs               # Интерфейс сервиса событий
│   │   └── IBookingService.cs             # Интерфейс сервиса бронирований
│   ├── ServiceInteraction/
│   │   ├── ServiceInteractionConstants.cs # Имена топиков Kafka
│   │   ├── IEventPublisher.cs             # Интерфейс издателя Kafka
│   │   └── ServiceEvents/
│   │       ├── BookingConfirmed.cs        # Событие «Бронирование подтверждено»
│   │       ├── BookingCancelled.cs        # Событие «Бронирование отменено»
│   │       └── BookingConfirmationFailed.cs # Событие «Ошибка подтверждения бронирования»
│   └── Options/
│       ├── JwtOptions.cs                  # Конфигурация JWT
│       └── KafkaOptions.cs                # Конфигурация Kafka
│
├── EventMgtApi.UsersService/              # 👤 Сервис пользователей (Авторизация)
│   ├── EventMgtApi.UsersService.Domain/
│   │   ├── Entities/
│   │   │   └── User.cs                    # Сущность пользователя
│   │   ├── Exceptions/
│   │   │   ├── InvalidCredentialsException.cs
│   │   │   └── ValidationException.cs
│   │   ├── Options/
│   │   │   └── SeedOptions.cs             # Конфигурация seed-администраторов
│   │   └── EventMgtApi.UsersService.Domain.csproj
│   ├── EventMgtApi.UsersService.Application/
│   │   ├── Services/
│   │   │   └── UserService.cs             # Логика регистрации и входа
│   │   ├── Abstractions/
│   │   │   ├── Persistence/
│   │   │   │   └── IUserRepository.cs     # Интерфейс репозитория
│   │   │   └── Services/
│   │   │       ├── IJwtTokenService.cs    # Интерфейс генерации JWT
│   │   │       ├── IPasswordHasher.cs     # Интерфейс хеширования паролей
│   │   │       └── ISeedService.cs        # Интерфейс seed-данных
│   │   ├── DependencyInjection/
│   │   │   └── ApplicationServiceCollectionExtensions.cs
│   │   └── EventMgtApi.UsersService.Application.csproj
│   ├── EventMgtApi.UsersService.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── UserDbContext.cs           # DbContext для пользователей
│   │   │   ├── Configurations/
│   │   │   │   └── UserConfiguration.cs   # Конфигурация Fluent API
│   │   │   └── Repositories/
│   │   │       └── UserRepository.cs      # Реализация IUserRepository
│   │   ├── Services/
│   │   │   ├── JwtTokenService.cs         # Генерация JWT-токенов
│   │   │   ├── PasswordHasher.cs          # Хеширование паролей (PBKDF2)
│   │   │   └── SeedService.cs             # Заполнение seed-администраторов
│   │   ├── DependencyInjection/
│   │   │   └── InfrastructureServiceCollectionExtensions.cs
│   │   ├── Migrations/                    # EF Core миграции
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
│   │   │   └── Event.cs                   # Сущность со��ытия
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs
│   │   │   ├── ValidationException.cs
│   │   │   └── NoAvailableSeatsException.cs
│   │   └── EventMgtApi.EventsService.Domain.csproj
│   ├── EventMgtApi.EventsService.Application/
│   │   ├── Events/
│   │   │   ├── EventService.cs            # Логика CRUD
│   │   │   └── Extensions/
│   │   │       └── EventMappingExtensions.cs
│   │   ├── Persistence/
│   │   │   └── IEventRepository.cs        # Интерфейс репозитория
│   │   ├── DependencyInjection/
│   │   │   └── ApplicationServiceCollectionExtensions.cs
│   │   └── EventMgtApi.EventsService.Application.csproj
│   ├── EventMgtApi.EventsService.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── EventDbContext.cs          # DbContext для событий
│   │   │   ├── Configurations/
│   │   │   │   └── EventConfiguration.cs  # Конфигурация Fluent API
│   │   │   └── Repositories/
│   │   │       └── EventRepository.cs     # Реализация IEventRepository
│   │   ├── ServiceInteractions/
│   │   │   ├── EventServiceMessagingConsumer.cs  # Потребитель Kafka
│   │   │   └── KafkaTopicInitializer.cs         # Инициализация топиков Kafka
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
│   │   │   └── BookingOptions.cs          # Конфигурация ограничений бронирований
│   │   └── EventMgtApi.BookingsService.Domain.csproj
│   ├── EventMgtApi.BookingsService.Application/
│   │   ├── Bookings/
│   │   │   ├── BookingService.cs          # Создание, отмена бронирований
│   │   │   └── Extensions/
│   │   │       └── BookingsMappingExtensions.cs
│   │   ├── Persistence/
│   │   │   └── IBookingRepository.cs      # Интерфейс репозитория
│   │   ├── ServiceInteraction/
│   │   │   └── IEventPublisher.cs         # Интерфейс издателя Kafka
│   │   ├── DependencyInjection/
│   │   │   └── ApplicationServiceCollectionExtensions.cs
│   │   └── EventMgtApi.BookingsService.Application.csproj
│   ├── EventMgtApi.BookingsService.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── BookingDbContext.cs        # DbContext для бронирований
│   │   │   ├── Configurations/
│   │   │   │   └── BookingConfiguration.cs # Конфигурация Fluent API
│   │   │   └── Repositories/
│   │   │       └── BookingRepository.cs   # Реализация IBookingRepository
│   │   ├── Persistence/Services/
│   │   │   └── BookingProcessingBackgroundService.cs  # Фоновая обработка Pending
│   │   ├── ServiceInteraction/
│   │   │   ├── BookingServiceMessagingPublisher.cs    # Издатель Kafka
│   │   │   └── BookingServiceMessagingConsumer.cs     # Потребитель Kafka
│   │   ├── DependencyInjection/
│   │   │   └── InfrastructureServiceCollectionExtensions.cs
│   │   ├── Migrations/
│   │   └── EventMgtApi.BookingsService.Infrastructure.csproj
│   └── EventMgtApi.BookingsService.Web/
│       ├── Controllers/
│       │   └── BookingsController.cs       # POST /book, GET/DELETE /bookings
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
└── docker-compose.yml                     # 🐳 PostgreSQL (×3) + Kafka + Zookeeper + Kafka UI
└── .env.example                     # пример задания переменных окружения (seed-администратор и др)
```

---

### Принципы разделения слоёв (в каждом сервисе)

| Слой | Зависит от | Ответственность |
|------|-----------|-----------------|
| **Domain** | Ничего | Бизнес-сущности, исключения домена, опции домена. Нет внешних зависимостей. |
| **Application** | Domain | Бизнес-логика, DTO, абстракции репозиториев, расширения DI. |
| **Infrastructure** | Domain + Application | Реализации репозиториев (EF Core), фоновые сервисы, клиенты Kafka, расширения DI. |
| **Presentation** | Application + Domain + Infrastructure | HTTP-контроллеры, middleware, фильтры, конфигурация приложения. |

> 💡 Эта структура позволяет:
> - Легко заменять хранилища (EF Core → Redis и т.д.)
> - Изолированное модульное тестирование
> - Добавление новых функций без нарушения существующего кода
> - Поддержка проекта при росте команды
> - Запуск каждого сервиса независимо

---

### 🔄 Межсервисное взаимодействие

Сервисы **не** взаимодействуют напрямую через HTTP. Вместо этого используется **Apache Kafka** для асинхронного pub/sub:

| Событие | Издатель | Подписчики | Описание |
|---------|----------|------------|----------|
| `BookingConfirmed` | `BookingsService` | `EventsService` | Уведомление о подтверждении бронирования для обновления `AvailableSeats` |
| `BookingCancelled` | `BookingsService` | `EventsService` | Уведомление об отмене бронирования для освобождения мест |
| `BookingConfirmationFailed` | `EventsService` | `BookingsService` | Ошибка подтверждения (нет мест / событие началось) |

- **BookingsService** публикует `BookingConfirmed` и `BookingCancelled` в Kafka при подтверждении или отмене бронирования.
- **EventsService** подписывается на эти топики и обновляет `AvailableSeats` для соответствующего события.
- **EventsService** публикует `BookingConfirmationFailed`, когда недостаточно мест или событие началось.
- **BookingsService** подписывается на `BookingConfirmationFailed` и может отклонить соответствующее бронирование.

Все разделяемые контракты событий (сериализуемые DTO) хранятся в `EventMgtApi.Contracts/ServiceInteraction/ServiceEvents/`.

---

### 🛡 Идемпотентность обработки сообщений

Повторная или дублирующаяся доставка Kafka-сообщений **не приводит** к некорректному изменению количества мест.

**Механизм:**

1. В `EventsService` создана сущность `ProcessedBooking` с составным ключом `(EventId, BookingId, EventType)`.
2. Перед обработкой сообщения `BookingConfirmed` или `BookingCancelled` потребитель проверяет наличие записи через `IProcessedBookingRepository.ExistsAsync`.
3. Если запись уже существует — сообщение игнорируется (дубликат).
4. Если записи нет — сообщение обрабатывается (уменьшается/увеличивается `AvailableSeats`), а запись в `ProcessedBooking` добавляется.

> 💡 Это гарантирует, что даже при повторной доставке одного и того же сообщения (at-least-once delivery) места будут учтены только один раз.

---

## 🚀 Запуск проекта

### Вариант 1: Полный запуск через Docker Compose (рекомендуется)

Все сервисы, базы данных и Kafka запускаются в едином Docker Compose-стеке. Сборка и запуск одной командой:

```bash
docker compose up -d --build
```
>ВАЖНО: перед запуском установите переменные окружения SEED_ADMIN_LOGIN, SEED_ADMIN_PASSWORD и JWT_SECRET_KEY в файле .env.
(также cм. раздел **Настройка seed-администраторов**).

**`.env` (не коммитится):**
```bash
SEED_ADMIN_LOGIN=admin
SEED_ADMIN_PASSWORD=admin1
JWT_SECRET_KEY=super-secret-key-for-jwt-token-generation
```
Файл .env нужно поместить в одну папку с docker-compose.yml. В качестве примера в репозиторий помещен файл .env.example.

Команда *docker compose up -d --build* соберет и запустит в докере весь стек решения - сервисы и инфраструктуру:

Сервисы:

| Сервис | Контейнер | Порт (хост) | Swagger UI |
|--------|-----------|-------------|------------|
| **UsersService** | users-service | 7001 | **http**://localhost:7001/swagger |
| **EventsService** | events-service | 7002 | **http**://localhost:7002/swagger |
| **BookingsService** | bookings-service | 7003 | **http**://localhost:7003/swagger |

Инфраструктура:

| Сервис | Контейнер | Порт (хост) |
|--------|-----------|-------------|
| Zookeeper | eventapi-zookeeper | 2181 |
| Kafka | eventapi-kafka | 9092 |
| Kafka UI | kafka-ui | 8080 |
| PostgreSQL (Users) | users-postgres | 5433 |
| PostgreSQL (Events) | events-postgres | 5434 |
| PostgreSQL (Bookings) | bookings-postgres | 5435 |

> 💡 Все три сервиса используют одинаковые конфигурации JWT и подключаются к базам по внутреннему Docker-имени хоста (например, `Host=postgres-users;Port=5432`). `appsettings.Development.json` не нужен.

### Вариант 2: Локальный запуск сервисов (.NET CLI) + Docker-инфраструктура

Если вы хотите разрабатывать сервисы локально, сначала запустите инфраструктуру:

```bash
docker compose up -d zookeeper kafka postgres-users postgres-events postgres-bookings
```

Затем настройте конфигурацию для каждого сервиса (см. ниже) и запустите:

```bash
dotnet restore
dotnet build
```

```bash
# Сервис пользователей
dotnet run --project EventMgtApi.UsersService/EventMgtApi.UsersService.Web/EventMgtApi.UsersService.Web.csproj

# Сервис событий
dotnet run --project EventMgtApi.EventsService/EventMgtApi.EventsService.Web/EventMgtApi.EventsService.Web.csproj

# Сервис бронирований
dotnet run --project EventMgtApi.BookingsService/EventMgtApi.BookingsService.Web/EventMgtApi.BookingsService.Web.csproj
```

🔐 При таком варианте запуска каждый сервис работает по HTTP**S** на своём порту (см. `Properties/launchSettings.json`):

| Сервис | Swagger UI |
|--------|-----------|
| **UsersService** (auth) | **https**://localhost:7001/swagger |
| **EventsService** (events) | **https**://localhost:7002/swagger |
| **BookingsService** (bookings) | **https**://localhost:7003/swagger |

---

### Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — для локального запуска (Вариант 2)
- **[Docker](https://www.docker.com/)** — для запуска инфраструктуры и сборки Docker-образов

---

### Конфигурация (только для Варианта 2)

Каждому сервису нужен свой `appsettings.Development.json`, в папке `Web` проекта.

Примеры:

**UsersService** (`EventMgtApi.UsersService.Web/appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=users_db;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-secret-key-here-must-be-at-least-32-chars-long",
    "Issuer": "EventMgtApi.UsersService",
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
    "DefaultConnection": "Host=localhost;Port=5434;Database=events_db;Username=postgres;Password=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ConsumerGroup": "events-service-group",
    "Topics": [
      "booking-confirmed",
      "booking-cancelled"
    ]
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "EventCacheTtlSeconds": 300,
    "TopEventsCacheTtlSeconds": 300	
  },    
  "Jwt": {
    "Secret": "your-secret-key-here-must-be-at-least-32-chars-long",
    "Issuer": "EventMgtApi.UsersService",
    "Audience": "EventMgtApi",
    "ExpiryMinutes": 60
  }
}
```

**BookingsService** (`EventMgtApi.BookingsService.Web/appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5435;Database=bookings_db;Username=postgres;Password=postgres"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ConsumerGroup": "bookings-service-group",
    "Topics": [
      "booking-confirmation-failed"
    ]
  },
  "BookingOptions": {
    "MaxActiveBookings": 10
  },
  "Jwt": {
    "Secret": "your-secret-key-here-must-be-at-least-32-chars-long",
    "Issuer": "EventMgtApi.UsersService",
    "Audience": "EventMgtApi",
    "ExpiryMinutes": 60
  },
  "SeedOptions": {
    "Admins": []
  }
}
```

> 🔒 Секреты (JWT Secret, пароли) хранятся в `appsettings.Development.json` — не коммитятся в git.
> Создайте `appsettings.Development.json` в папке `Web` каждого сервиса на основе `appsettings.json` и примеров выше.

---

## 🔐 Получение JWT-токена через Swagger

### Шаг 1. Регистрация пользователя

В **UsersService** Swagger, откройте `POST /api/auth/register`:

1. Нажмите **Try it out**
2. Введите JSON:
   ```json
   {
     "login": "User1",
     "password": "User1234!"
   }
   ```
3. Нажмите **Execute** — ответ **201 Created** означает успех.

### Шаг 2. Вход

В **UsersService** Swagger, откройте `POST /api/auth/login`:

1. Нажмите **Try it out**
2. Введите:
   ```json
   {
     "login": "User1",
     "password": "User1234!"
   }
   ```
3. Нажмите **Execute** — вы получите:
   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIs...",
     "login": "User1",
     "role": "User"
   }
   ```

### Шаг 3. Авторизация

1. В правом верхнем углу любого Swagger UI нажмите **🔒 Authorize**
2. Вставьте токен (без кавычек)
3. Нажмите **Authorize**, затем **Close**

Защищённые эндпоинты теперь доступны.

### Шаг 4. Тестирование доступа

1. Откройте `POST /api/events` в **EventsService** с ролью администратора:
   ```json
   {
     "title": "Test Concert",
     "startAt": "2026-10-10T19:00:00Z",
     "endAt": "2026-11-10T22:00:00Z",
     "totalSeats": 150
   }
   ```
   → **201 Created** для Admin, **403 Forbidden** для User.

---

## 🆕 Бронирование: `POST /api/events/{eventId:guid}/book`

Создаёт ��овое бронирование для указанного события.
Начальный статус — `Pending`.

> **HTTP 201 Created** — бронирование создано и поставлено в очередь на обработку
> **HTTP 409 Conflict** — нет доступных мест
> **Location** — ссылка на `GET /api/bookings/{id}`

#### Пример:
```http
POST /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book
Content-Type: application/json
```

```json
{}
```

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

---

## 🔍 Статус бронирования: `GET /api/bookings/{id:guid}`

Возвращает текущий статус бронирования.

> **HTTP 200 OK** — найдено
> **HTTP 401 Unauthorized** — требуется авторизация
> **HTTP 403 Forbidden** — это не ваше бронирование
> **HTTP 404 Not Found** — не существует

#### Пример:
```http
GET /api/bookings/9e1b2f4d-8a3c-4e2a-9f1a-1b2c3d4e5f6a
```

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

## 🔍 Отмена бронирования: `DELETE /api/bookings/{id:guid}`

Переводит бронирование в статус `Cancelled`.

> **HTTP 204 No Content** — отменено
> **HTTP 401 Unauthorized** — требуется авторизация
> **HTTP 403 Forbidden** — недостаточно прав
> **HTTP 404 Not Found** — не существует

---

## 🧩 Модель бронирования и статусы

### Сущность `Booking`

| Поле | Тип | Описание |
|------|------|----------|
| `Id` | `Guid` | Уникальный идентификатор |
| `EventId` | `Guid` | Ссылка на событие |
| `UserId` | `Guid` | Ссылка на пользователя |
| `Status` | `BookingStatus` | Текущий статус |
| `CreatedAt` | `DateTime` | Время создания |
| `ProcessedAt` | `DateTime?` | Время обработки (nullable) |

### Перечисление `BookingStatus`
```csharp
public enum BookingStatus
{
    Pending,    // Создано, ожидает обработки
    Confirmed,  // Подтверждено
    Rejected,   // Отклонено
    Cancelled   // Отменено пользователем
}
```

---

## ⏳ Фоновая обработка

### Обработка бронирований (BookingsService)

`BookingProcessingBackgroundService` запускается при старте:

1. Каждые **5 секунд** сканирует бронирования со статусом `Pending`
2. Для каждого:
   - Ждёт **2 секунды** (имитация внешней системы)
   - Меняет статус на `Confirmed`
   - Устанавливает `ProcessedAt = DateTime.UtcNow`
   - Публикует `BookingConfirmed` в **Kafka**
   - Сохраняет изменения

### Обработка событий (EventsService)

`EventServiceMessagingConsumer` подписывается на топики Kafka:

| Топик | Действие |
|-------|----------|
| `booking-confirmed` | Уменьшает `AvailableSeats` на количество забронированных мест |
| `booking-cancelled` | Увеличивает `AvailableSeats` на количество отменённых мест |
| `booking-confirmation-failed` | Публикуется **EventsService** при нехватке мест, подписчик — **BookingsService** |

### Возможности фоновых сервисов:
- Потокобезопасность
- Асинхронные операции
- Логирование на каждом шаге
- Корректное завершение работы

---

## 🔐 Валидация

Все поля `EventDto` проходят валидацию:
- Атрибуты `[Required]`
- Кастомная проверка: `StartAt < EndAt`
- Сообщения об ошибках на русском языке
- Защита от `null` и логических ошибок

---

## ⚙️ Конфигурация JWT

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

> 🔒 **Безопасность:**
> - Используйте случайное значение (минимум 32 символа) для `Jwt.Secret`
> - В продакшене используйте переменные окружения или менеджер секретов (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
> - Все три сервиса должны использовать **одинаковые** `Secret`, `Issuer` и `Audience`

---

## ⚙️ Конфигурация Seed

Начальное заполнение базы данных настраивается через `SeedOptions`.
Администраторы создаются автоматически, при запуске сервиса **UsersService**.

Пример:

**`appsettings.Development.json`:**
```json
{
  "SeedOptions": {
    "Admins": [
      { "Login": "admin", "Password": "Admin123!" }
    ]
  }
}
```

Если пароль не указан, выбрасывается исключение.
Администраторы с одинаковым логином не пересоздаются.

---

## ❌ Формат ответа об ошибках

API возвращает ошибки в стандартизированном формате `ProblemDetails` (`application/problem+json`).

### Валидация:
```json
{
  "title": "Ошибка валидации",
  "status": 400,
  "detail": "Обнаружены ошибки валидации входных данных.",
  "instance": "/api/Events",
  "errors": {
    "Title": ["Заголовок обязателен."]
  }
}
```

### Не найдено:
```json
{
  "title": "Ресурс не найден",
  "status": 404,
  "detail": "Событие с ID 4cbde443-... не найдено.",
  "instance": "/api/Events/4cbde443-..."
}
```

### Внутренняя ошибка сервера:
```json
{
  "title": "Внутренняя ошибка сервера",
  "status": 500,
  "detail": "Произошла ошибка при обработке запроса.",
  "instance": "/api/Events/..."
}
```

- Все сообщения на русском языке
- Централизованная обработка через middleware
- Следует RFC 9110

---

## ⚡ Кеширование (Redis Cache-Aside)

### Что кешируется

| Ключ | Значение | TTL | Стратегия инвалидации |
|------|----------|-----|----------------------|
| `event:{id}` | JSON-представление `EventDtoResponse` одного события | 5 минут | Явная инвалидация при `UPDATE`/`DELETE` события и при изменении `AvailableSeats` через Kafka |
| `events:top10` | JSON-список топ-10 событий по проценту продаж | 5 минут | Только по TTL — рейтинговый агрегат, небольшое устаревание некритично |

### Почему так

- **Отдельное событие (`event:{id}`)** — данные меняются часто (бронирования), устаревание заметно пользователю → **инвалидация при записи**.
- **Топ-10 (`events:top10`)** — рейтинговый агрегат, меняется редко → **только TTL**, явная инвалидация избыточна.

### Устойчивость к отказам

Если Redis недоступен:
- Ошибка логируется на уровне `RedisCacheClient`
- Возвращается `null` / операция пропускается
- Запрос идёт напрямую в базу данных
- Клиент **не получает ошибок** — кеш просто деградирует

### Порядок операций (безопасность)

При изменении данных:
1. Сначала `SaveChangesAsync()` в базу
2. Затем `RemoveAsync()` в кэш

Если операция прервётся между шагами — база останется актуальной, кеш обновится при следующем чтении.

### Где инвалидируется кэш

| Событие | Ключи инвалидации |
|---------|-------------------|
| `PUT /events/{id}` | `event:{id}` |
| `DELETE /events/{id}` | `event:{id}` |
| Kafka: `BookingConfirmed` | `event:{eventId}`, `events:top10` |
| Kafka: `BookingCancelled` | `event:{eventId}`, `events:top10` |

---

## ⚠️ Ограничения

- Часовые пояса не обрабатываются (все даты в UTC)
- Нет поддержки refresh-токенов

---

## 🚧 Будущие улучшения

- Интеграция с email и платежами
- Распределённое трассирование (OpenTelemetry)
- API Gateway (Ocelot / YARP)
---

> 🙌 Спасибо за использование!