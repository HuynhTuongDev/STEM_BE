namespace STEM.Application.Dtos.Auth;

public class GoogleLoginRequest
{
<<<<<<< HEAD
    public string? IdToken { get; set; }
    public string? Credential { get; set; }

    public string GetIdToken()
    {
        return string.IsNullOrWhiteSpace(IdToken)
            ? Credential?.Trim() ?? string.Empty
            : IdToken.Trim();
    }
=======
    public string IdToken { get; set; } = string.Empty;
>>>>>>> origin/develop
}
