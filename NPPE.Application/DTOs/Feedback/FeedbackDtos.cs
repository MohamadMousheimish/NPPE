namespace NPPE.Application.DTOs.Feedback;

/// <summary>A published testimonial shown on the public landing page.</summary>
public record FeedbackDto(string AuthorName, string? AuthorTitle, int Rating, string Comment);

/// <summary>A student's own review, for their edit form.</summary>
public record MyFeedbackDto(int Rating, string Comment, bool IsApproved);

/// <summary>Whether the student may leave a review (has taken an exam) + their existing one, if any.</summary>
public record MyFeedbackStatusDto(bool Eligible, MyFeedbackDto? Review);

/// <summary>A review row in the admin moderation list.</summary>
public record AdminFeedbackDto(
    Guid Id, string AuthorName, string? AuthorTitle, int Rating, string Comment,
    bool IsApproved, bool FromStudent, DateTime CreatedAt);
