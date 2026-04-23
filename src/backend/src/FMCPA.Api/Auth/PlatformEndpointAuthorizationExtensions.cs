namespace FMCPA.Api.Auth;

public static class PlatformEndpointAuthorizationExtensions
{
    public static TBuilder RequireReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.ReadAccess);
        return builder;
    }

    public static TBuilder RequireWriteAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.WriteAccess);
        return builder;
    }

    public static TBuilder RequireAdminAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.AdminAccess);
        return builder;
    }
}
