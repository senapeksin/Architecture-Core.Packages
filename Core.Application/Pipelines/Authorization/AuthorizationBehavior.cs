﻿using MediatR;
using Microsoft.AspNetCore.Http;
using Core.Security.Extensions;
using Core.Security.Constants;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Microsoft.IdentityModel.Tokens;

namespace Core.Application.Pipelines.Authorization;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>, ISecuredRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async  Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        List<string>? userRoleClaims = _httpContextAccessor.HttpContext.User.ClaimRoles();  // sisteme giriş yapmış mevcut kullanıcının rollerini bir listede tutuyoruz.

        if (userRoleClaims == null)
            throw new AuthorizationException("You are not authenticated");

        // Eğer bu kullanıcının rolü Admin ise ya da mevcut rolleri içerisinde o metodun istediği rollerden bir tanesi varsa izin ver diyoruz. 
        bool isNotMatchedAUserRoleClaimWithRequestRoles = userRoleClaims.FirstOrDefault(userRoleClaim => userRoleClaim == GeneralOperationClaims.Admin || request.Roles.Any(role => role == userRoleClaim)).IsNullOrEmpty();

        // izni  yoksa hata ver.
        if (isNotMatchedAUserRoleClaimWithRequestRoles)
            throw new AuthorizationException("You are not authorized.");

        TResponse response = await next();
        return response;

     }
}
