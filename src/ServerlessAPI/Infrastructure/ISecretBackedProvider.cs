namespace ServerlessAPI.Infrastructure;

/// <summary>Secretos cargados desde Secrets Manager en la inicialización.</summary>
public interface ISecretBackedProvider
{
    Task WarmAsync();
}
