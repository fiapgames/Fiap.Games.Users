using System.Text.Json;
using System.Text.Json.Serialization;
using FiapGames.Contracts.IntegrationEvents;

namespace User.Games.Fiap.Infrastructure.Notifications;

public interface INotificationsClient
{
    Task SendUserCreatedAsync(UserCreatedEvent userCreated, CancellationToken cancellationToken = default);
}

/// <summary>
/// Envia o evento de usuário criado para a Azure Function de notificações.
/// Substitui a publicação no RabbitMQ: o microsserviço de notificações virou
/// serverless e não consome mais a fila.
/// </summary>
public sealed class NotificationsClient(HttpClient httpClient, ILogger<NotificationsClient> logger)
    : INotificationsClient
{
    // PaymentStatus e demais enums trafegam como string no ecossistema FiapGames.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SendUserCreatedAsync(UserCreatedEvent userCreated, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "api/notifications/user-created", userCreated, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Notificação de usuário criado rejeitada pela Function ({StatusCode}) para o usuário {UserId}",
                    (int)response.StatusCode, userCreated.UserId);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // A notificação é acessória: falha aqui não pode impedir o cadastro do usuário.
            logger.LogWarning(exception,
                "Falha ao notificar a Function sobre a criação do usuário {UserId}", userCreated.UserId);
        }
    }
}
