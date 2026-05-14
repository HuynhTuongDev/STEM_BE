using STEM.Core.Entities.Users;

namespace STEM.Application.Interfaces;

public interface IJwtProvider
{
    string GenerateToken(User user);
}
