using System;
using System.ComponentModel.DataAnnotations;

namespace EventMgtApi.Models;

/// <summary>
/// DTO для создания новой брони.
/// </summary>
public class CreateBookingRequestDto
{
    /// <summary>
    /// Идентификатор события, которое бронируется.
    /// </summary>
    [Required(ErrorMessage = "Идентификатор события обязателен.")]
    public Guid? EventId { get; set; }
}
