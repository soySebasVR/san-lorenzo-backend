namespace ServerlessAPI.Infrastructure;

/// <summary>
/// A value sourced from Secrets Manager, and therefore one that must never sit inside a
/// SnapStart snapshot. Program.cs clears these before the snapshot and warms them after
/// each restore.
/// </summary>
public interface ISecretBackedProvider
{
    Task WarmAsync();

    void Clear();
}
