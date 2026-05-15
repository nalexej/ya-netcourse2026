using System;
using System.Linq;
using EventMgtApi.Models;
using EventMgtApi.Models.Dto;
using EventMgtApi.Services;
using EventMgtApi.Repositories;
using EventMgtApi.Exceptions;
using Xunit;

namespace EventMgtApi.Tests;

public class EventServiceTests
{
    private readonly EventService _service;
    private readonly InMemoryEventRepository _repository;

    public EventServiceTests()
    {
        _repository = new InMemoryEventRepository();
        _service = new EventService(_repository);
    }

    // Тестовые данные: валидные DTO для создания/обновления событий
    public static IEnumerable<object[]> ValidEventDtos()
    {
        yield return new object[]
        {
            new EventDto
            {
                Title = "Встреча с командой (план)",
                Description = "Обсуждение плана",
                StartAt = new DateTime(2026, 5, 14, 10, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 5, 15, 23, 0, 0, DateTimeKind.Utc)
            },
            "Обсуждение плана"
        };

        yield return new object[]
        {
            new EventDto
            {
                Title = "План на квартал",
                Description = null,
                StartAt = new DateTime(2026, 5, 15, 9, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 5, 15, 10, 30, 0, DateTimeKind.Utc)
            },
            string.Empty
        };

        yield return new object[]
        {
            new EventDto
            {
                Title = "Встреча по обучению сотрудников",
                StartAt = new DateTime(2026, 5, 16, 14, 0, 0, DateTimeKind.Utc),
                EndAt = new DateTime(2026, 5, 16, 23, 0, 0, DateTimeKind.Utc)
            },
            string.Empty
        };
    }

    // ========================================================================
    // 1. Создание событий
    // ========================================================================
    // Проверяет, что валидное событие создаётся с корректным ID и полями

    [Theory]
    [MemberData(nameof(ValidEventDtos))]
    public void CreateEvent_ValidEvent_ReturnsCreatedEventWithId(EventDto inputEvent, string expectedDescription)
    {
        // Act
        var result = _service.AddEvent(inputEvent);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(inputEvent.Title, result.Title);
        Assert.Equal(expectedDescription, result.Description);
        Assert.Equal(inputEvent.StartAt, result.StartAt);
        Assert.Equal(inputEvent.EndAt, result.EndAt);
    }

    // Проверяет валидацию: пустой заголовок → ошибка
    [Fact]
    public void AddEvent_TitleEmpty_ThrowsValidationException()
    {
        var dto = new EventDto
        {
            Title = string.Empty,
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        Assert.Throws<ValidationException>(() => _service.AddEvent(dto));
    }

    // Проверяет валидацию: дата окончания раньше начала → ошибка
    [Fact]
    public void AddEvent_StartAfterEnd_ThrowsValidationException()
    {
        var dto = new EventDto
        {
            Title = "Тест",
            StartAt = DateTime.UtcNow.AddDays(2),
            EndAt = DateTime.UtcNow.AddDays(1)
        };

        Assert.Throws<ValidationException>(() => _service.AddEvent(dto));
    }

    // ========================================================================
    // 2. Получение всех событий
    // ========================================================================
    // Проверяет, что все добавленные события возвращаются с правильной пагинацией

    [Fact]
    public void GetAllEvents_AfterAddingEvents_ReturnsAllAddedEvents()
    {
        // Arrange
        var eventsToAdd = ValidEventDtos().Select(data => (EventDto)data[0]).ToList();

        foreach (var evt in eventsToAdd)
        {
            _service.AddEvent(evt);
        }

        // Act
        var result = _service.GetEvents();

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.All(result.Items, item =>
        {
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.NotNull(item.Title);
            Assert.True(item.StartAt < item.EndAt);
        });
    }

    // ========================================================================
    // 3. Получение события по ID
    // ========================================================================
    // Проверяет, что существующее событие возвращается корректно

    [Fact]
    public void GetEventById_ExistingId_ReturnsEvent()
    {
        // Arrange
        var testEventDto = ValidEventDtos().First();
        var inputEvent = (EventDto)testEventDto[0];

        var addedEvent = _service.AddEvent(inputEvent);
        var id = addedEvent.Id;

        // Act
        var result = _service.GetEvent(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(inputEvent.Title, result.Title);
        Assert.Equal(inputEvent.Description ?? string.Empty, result.Description);
        Assert.Equal(inputEvent.StartAt, result.StartAt);
        Assert.Equal(inputEvent.EndAt, result.EndAt);
    }

    // Проверяет, что при отсутствии события выбрасывается NotFoundException
    [Fact]
    public void GetEvent_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<NotFoundException>(() =>
            _service.GetEvent(nonExistingId)
        );

        Assert.Contains($"Событие с ID {nonExistingId} не найдено", exception.Message);
    }

    // ========================================================================
    // 4. Обновление существующего события
    // ========================================================================
    // Проверяет, что событие обновляется с новыми валидными данными

    [Fact]
    public void UpdateEvent_ExistingIdAndValidData_UpdatesAndReturnsEvent()
    {
        // Arrange
        // Берём первое событие из ValidEventDtos для создания
        var addedEvent = _service.AddEvent((EventDto)ValidEventDtos().First()[0]!);
        var id = addedEvent.Id;

        // Событие для обновления (новые данные)
        var updateDto = new EventDto
        {
            Title = "Обновлённое название",
            Description = "Новое описание",
            StartAt = new DateTime(2026, 5, 14, 12, 0, 0, DateTimeKind.Utc),
            EndAt = new DateTime(2026, 5, 14, 13, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var result = _service.UpdateEvent(id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(updateDto.Title, result.Title);
        Assert.Equal(updateDto.Description, result.Description);
        Assert.Equal(updateDto.StartAt, result.StartAt);
        Assert.Equal(updateDto.EndAt, result.EndAt);
    }

    // Проверяет валидацию: пустой заголовок при обновлении → ошибка
    [Fact]
    public void UpdateEvent_EmptyTitle_ThrowsValidationException()
    {
        // Arrange
        var added = _service.AddEvent((EventDto)ValidEventDtos().First()[0]!);
        var updateDto = new EventDto { Title = string.Empty };

        // Act & Assert
        Assert.Throws<ValidationException>(() => _service.UpdateEvent(added.Id, updateDto));
    }

    // Проверяет валидацию: StartAfterEnd при обновлении → ошибка
    [Fact]
    public void UpdateEvent_StartAfterEnd_ThrowsValidationException()
    {
        // Arrange
        var added = _service.AddEvent((EventDto)ValidEventDtos().First()[0]!);
        var updateDto = new EventDto
        {
            Title = "Тест",
            StartAt = DateTime.UtcNow.AddDays(2),
            EndAt = DateTime.UtcNow.AddDays(1)
        };

        // Act & Assert
        Assert.Throws<ValidationException>(() => _service.UpdateEvent(added.Id, updateDto));
    }

    // Проверяет: обновление несуществующего ID → ошибка
    [Fact]
    public void UpdateEvent_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var updateDto = new EventDto { Title = "Тест", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1) };
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<NotFoundException>(() => _service.UpdateEvent(nonExistingId, updateDto));
        Assert.Contains("не найдено", ex.Message);
    }

    // ========================================================================
    // 5. Удаление события
    // ========================================================================
    // Проверяет, что событие удаляется и больше не доступно

    [Fact]
    public void DeleteEvent_ExistingId_DeletesEvent()
    {
        // Arrange
        // Добавляем событие, чтобы получить реальный ID
        var addedEvent = _service.AddEvent((EventDto)ValidEventDtos().First()[0]!);
        var id = addedEvent.Id;

        // Act
        _service.RemoveEvent(id);

        // Assert
        var exception = Assert.Throws<NotFoundException>(() => _service.GetEvent(id));
        Assert.Contains($"Событие с ID {id} не найдено", exception.Message);
    }

    // Проверяет: удаление несуществующего ID → ошибка
    [Fact]
    public void RemoveEvent_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<NotFoundException>(() =>
            _service.RemoveEvent(nonExistingId)
        );

        Assert.Contains("не найдено", exception.Message);
    }

    // ========================================================================
    // 6. Фильтрация по названию
    // ========================================================================
    // Проверяет частичное, регистронезависимое совпадение по заголовку

    [Fact]
    public void GetEvents_TitleFilter_MatchesPartialAndCaseInsensitive()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();

        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        var expectedCount = testEvents.Count(e => e.Title?.Contains("план", StringComparison.OrdinalIgnoreCase) == true);

        // Act
        var result = _service.GetEvents(title: "план");

        // Assert
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
        Assert.All(result.Items, item =>
            Assert.Contains("план", item.Title, StringComparison.OrdinalIgnoreCase));
    }

    // Проверяет: title = null → возвращает всё
    [Fact]
    public void GetEvents_TitleFilter_Null_ReturnsAllEvents()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        var expectedCount = testEvents.Count;

        // Act
        var result = _service.GetEvents(title: null);

        // Assert
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
    }

    // Проверяет: title = "" → возвращает всё
    [Fact]
    public void GetEvents_TitleFilter_EmptyString_ReturnsAllEvents()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        var expectedCount = testEvents.Count;

        // Act
        var result = _service.GetEvents(title: string.Empty);

        // Assert
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
    }

    // Проверяет: title = пробелы → возвращает всё
    [Fact]
    public void GetEvents_TitleFilter_WhitespaceOnly_ReturnsAllEvents()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        var expectedCount = testEvents.Count;

        // Act
        var result = _service.GetEvents(title: "   ");

        // Assert
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
    }

    // Проверяет: нет совпадений → пустой результат
    [Fact]
    public void GetEvents_TitleFilter_NoMatchingTitle_ReturnsEmptyList()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        // Act
        var result = _service.GetEvents(title: "несуществующее");

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    // ========================================================================
    // 7. Фильтрация по датам
    // ========================================================================
    // Проверяет фильтр "от" — события, начинающиеся не раньше указанной даты

    [Fact]
    public void GetEvents_FromFilter_ReturnsEventsStartingAfterOrAt()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        var from = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);

        var expectedCount = testEvents.Count(e => e.StartAt >= from);

        // Act
        var result = _service.GetEvents(from: from);

        // Assert
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.StartAt >= from));
    }

    // Проверяет фильтр "до" — события, заканчивающиеся не позже
    [Fact]
    public void GetEvents_ToFilter_ReturnsEventsEndingBeforeOrAt()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        var to = new DateTime(2026, 5, 15, 23, 59, 59, DateTimeKind.Utc);

        var expectedCount = testEvents.Count(e => e.EndAt <= to);

        // Act
        var result = _service.GetEvents(to: to);

        // Assert
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.EndAt <= to));
    }

    // Проверяет комбинацию from + to
    [Fact]
    public void GetEvents_FromAndToFilter_ReturnsEventsInDateRange()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        var from = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 5, 15, 23, 59, 59, DateTimeKind.Utc);

        var expectedCount = testEvents.Count(e => e.StartAt >= from && e.EndAt <= to);

        // Act
        var result = _service.GetEvents(from: from, to: to);

        // Assert
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
        Assert.All(result.Items, item =>
        {
            Assert.True(item.StartAt >= from);
            Assert.True(item.EndAt <= to);
        });
    }

    // Проверяет: from в будущем → пусто
    [Fact]
    public void GetEvents_FromFilter_FutureDate_ReturnsEmptyList()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        var from = DateTime.UtcNow.AddYears(10); // далеко в будущем

        // Act
        var result = _service.GetEvents(from: from);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    // ========================================================================
    // 8. Пагинация
    // ========================================================================
    // Проверяет вторую страницу при pageSize=2

    [Fact]
    public void GetEvents_Pagination_Page2_Size2_ReturnsCorrectSubset()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();

        // Убедимся, что все события добавлены
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        // Ожидаем: pageSize = 2 → страница 1: 2 события, страница 2: 1 событие
        const int page = 2;
        const int pageSize = 2;

        // Act
        var result = _service.GetEvents(page: page, pageSize: pageSize);

        // Assert
        Assert.Equal(3, result.TotalCount);       // Всего 3 события
        Assert.Equal(pageSize, result.PageSize); // Размер страницы = 2
        Assert.Equal(page, result.Page);         // Текущая страница = 2
        Assert.Single(result.Items);             // На второй странице — только 1
    }

    // Проверяет первую страницу
    [Fact]
    public void GetEvents_Pagination_Page1_Size2_ReturnsFirstTwoItems()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        // Act
        var result = _service.GetEvents(page: 1, pageSize: 2);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.Items.Count);
    }

    // Проверяет нормализацию: page=0 → page=1
    [Fact]
    public void GetEvents_Pagination_Page0_ReturnsFirstPage()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        // Act
        var result = _service.GetEvents(page: 0, pageSize: 2);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(testEvents.Count, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    // Проверяет нормализацию: pageSize=0 → pageSize=1
    [Fact]
    public void GetEvents_PageSize0_NormalizesTo1()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        foreach (var evt in testEvents)
        {
            _service.AddEvent(evt);
        }

        // Act
        var result = _service.GetEvents(page: 1, pageSize: 0);

        // Assert
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
    }

    // Проверяет ограничение: pageSize=150 → pageSize=100
    [Fact]
    public void GetEvents_PageSize150_NormalizesTo100()
    {
        // Arrange
        for (int i = 0; i < 50; i++)
        {
            _service.AddEvent(new EventDto
            {
                Title = $"Событие {i}",
                StartAt = DateTime.UtcNow.AddDays(i),
                EndAt = DateTime.UtcNow.AddDays(i + 1)
            });
        }

        // Act
        var result = _service.GetEvents(page: 1, pageSize: 150);

        // Assert
        Assert.Equal(100, result.PageSize);
        Assert.True(result.Items.Count <= 100);
    }

    // ========================================================================
    // 9. Комбинированная фильтрация (логическое И)
    // ========================================================================
    // Проверяет одновременное применение фильтров: title + from + to

    [Fact]
    public void GetEvents_CombinedFilters_ReturnsIntersection()
    {
        // Arrange
        var testEvents = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();

        foreach (var testEvent in testEvents)
        {
            _service.AddEvent(testEvent);
        }

        var from = new DateTime(2026, 5, 14, 9, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 5, 15, 23, 59, 59, DateTimeKind.Utc);
        var title = "встреча";

        // Act
        var result = _service.GetEvents(title: title, from: from, to: to);

        // Assert
        Assert.Single(result.Items);

        var evt = result.Items.First();

        Assert.Contains(title, evt.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(evt.StartAt >= from, "Событие должно начинаться не раньше 'from'");
        Assert.True(evt.EndAt <= to, "Событие должно заканчиваться не позже 'to'");
    }
}