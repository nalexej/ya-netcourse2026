namespace EventMgtApi.Models.Dto;

/// <summary>
/// DTO для ответа с данными события.
/// </summary>
public class EventDtoResponse
{
    /// <summary>
    /// Уникальный идентификатор события.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Заголовок (название) события.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Описание события.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала события.
    /// </summary>
    public required DateTime? StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    public required DateTime? EndAt { get; set; }
}