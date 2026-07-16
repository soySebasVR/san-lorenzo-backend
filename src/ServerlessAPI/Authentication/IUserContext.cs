using ServerlessAPI.Entities;

namespace ServerlessAPI.Authentication;

/// <summary>Identidad del usuario desde el JWT.</summary>
public interface IUserContext
{
    int UserId { get; }

    Role Role { get; }

    /// <summary>Falla si no es docente.</summary>
    int TeacherId { get; }

    /// <summary>Falla si no es estudiante.</summary>
    int StudentId { get; }
}
