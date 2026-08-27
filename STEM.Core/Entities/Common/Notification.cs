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
    ResubmitRequestReviewed,
    // Student notifications
    AddedToClass,           // N-15
    AssignmentAssigned,      // N-17
    VirtualLabAssigned,      // N-18
    LessonAvailable,         // N-19
    ClassAnnouncement,      // N-20
    AssignmentDueSoon,      // N-21
    VirtualLabDueSoon,     // N-22
    AssignmentSubmitted,    // N-23
    AssignmentGraded,      // N-26
    TeacherFeedback,        // N-27
    ResubmissionApproved,   // N-28
    ResubmissionRejected,   // N-29
    VirtualLabCompleted,    // N-25
    LabResultAvailable,     // N-30
    ScheduleReminder,       // N-31
    AssignmentDueToday,     // N-32
    VirtualLabDueToday,     // N-33
    ProgressAlert,           // N-36, N-37
    // Teacher notifications
    ClassAssigned,          // N-13
    ClassRemoved,           // N-14
    NewClassAvailable,      // N-16
    ResubmissionRequest,    // N-24
    StudentProgressAlert,   // N-36 (for teacher)
    // School Admin notifications
    NewCourseAvailable,     // N-06
    CourseUpdated,          // N-07
    NewVirtualLabAvailable, // N-08
    TokenBalanceLow,        // N-34
    SubscriptionExpiring,   // N-35
    // Payment notifications
    PaymentCompleted,       // N-11
    PaymentFailed           // N-12
}

public enum NotificationStatus
{
    Unread,
    Read
}
