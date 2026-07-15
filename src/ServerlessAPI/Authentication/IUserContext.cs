using ServerlessAPI.Entities;

namespace ServerlessAPI.Authentication;

/// <summary>Identity of the caller, read from the validated JWT claims.</summary>
public interface IUserContext
{
    int UserId { get; }

    Role Role { get; }

    /// <summary>Throws if the caller is not a teacher.</summary>
    int TeacherId { get; }

    /// <summary>Throws if the caller is not a student.</summary>
    int StudentId { get; }
}
