using System;

namespace EventMgtApi.Models.Dto;

/// <summary>
/// Представляет результат пагинированного запроса.
/// Содержит данные текущей страницы и метаинформацию о пагинации.
/// </summary>
/// <typeparam name="T">Тип элементов на странице.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Общее количество элементов.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Номер текущей страницы (начинается с 1).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Количество элементов на одной странице.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Общее количество страниц. Вычисляется как ceil(TotalCount / PageSize).
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Указывает, существует ли предыдущая страница.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Указывает, существует ли следующая страница.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Список элементов на текущей странице.
    /// </summary>
    public List<T> Items { get; set; } = new();
}