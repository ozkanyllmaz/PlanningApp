using MediatR;
using PlanningApp.Application.Common.Exceptions;
using PlanningApp.Application.Common.Interfaces;

namespace PlanningApp.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    IIdentityService identityService,
    ICurrentUserService currentUser) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(currentUser.UserId, cancellationToken);

        // Token geçerli ama kullanıcı silinmişse oturum geçersizdir (401)
        if (user is null)
            throw new AuthenticationFailedException(AuthenticationFailedException.InvalidSessionMessage);

        return new CurrentUserDto(user.UserId, user.Email);
    }
}
