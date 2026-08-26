using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Common;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.System;
    public bool IsRead { get; set; } = false;

    public User? User { get; set; }
}

public enum NotificationType
{
    System,
    GradeReport,
    AttendanceWarning,
    Assignment,
    Announcement,
    SubmissionComment,
    SubmissionReceived,
    ResubmitRequest,
    ResubmitRequestReviewed
}

public enum NotificationStatus
{
    Unread,
    Read
}
