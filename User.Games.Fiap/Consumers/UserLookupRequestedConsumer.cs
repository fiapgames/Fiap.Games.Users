using FiapGames.Contracts.Requests.User;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using User.Games.Fiap.Infrastructure.Repositories;

namespace User.Games.Fiap.Consumers;

public sealed class UserLookupRequestedConsumer(IUnitOfWork unitOfWork) : IConsumer<UserLookupRequested>
{
    public async Task Consume(ConsumeContext<UserLookupRequested> context)
    {
        var user = await unitOfWork.Users.Query()
            .Where(user => user.Id == context.Message.UserId)
            .Select(user => new
            {
                user.Id,
                user.Nome,
                user.Email
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        await context.RespondAsync(new UserLookupResponded(
            context.Message.CorrelationId,
            context.Message.UserId,
            user is not null,
            user?.Nome,
            user?.Email,
            DateTimeOffset.UtcNow));
    }
}
