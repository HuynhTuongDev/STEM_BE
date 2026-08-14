namespace STEM.Application.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
