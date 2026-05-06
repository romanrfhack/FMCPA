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

    public static TBuilder RequireDashboardReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.DashboardReadAccess);
        return builder;
    }

    public static TBuilder RequireHistoryReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.HistoryReadAccess);
        return builder;
    }

    public static TBuilder RequireContactsReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.ContactsReadAccess);
        return builder;
    }

    public static TBuilder RequireContactsWriteAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.ContactsWriteAccess);
        return builder;
    }

    public static TBuilder RequireMarketsReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.MarketsReadAccess);
        return builder;
    }

    public static TBuilder RequireMarketsWriteAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.MarketsWriteAccess);
        return builder;
    }

    public static TBuilder RequireMarketsFormalCloseAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.MarketsFormalCloseAccess);
        return builder;
    }

    public static TBuilder RequireDonationsReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.DonationsReadAccess);
        return builder;
    }

    public static TBuilder RequireDonationsWriteAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.DonationsWriteAccess);
        return builder;
    }

    public static TBuilder RequireDonationsFormalCloseAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.DonationsFormalCloseAccess);
        return builder;
    }

    public static TBuilder RequireFinancialsReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.FinancialsReadAccess);
        return builder;
    }

    public static TBuilder RequireFinancialsWriteAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.FinancialsWriteAccess);
        return builder;
    }

    public static TBuilder RequireFinancialsFormalCloseAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.FinancialsFormalCloseAccess);
        return builder;
    }

    public static TBuilder RequireFederationReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.FederationReadAccess);
        return builder;
    }

    public static TBuilder RequireFederationWriteAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.FederationWriteAccess);
        return builder;
    }

    public static TBuilder RequireFederationFormalCloseAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.FederationFormalCloseAccess);
        return builder;
    }

    public static TBuilder RequireCatalogsReadAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.CatalogsReadAccess);
        return builder;
    }

    public static TBuilder RequireCatalogsAdminAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.CatalogsAdminAccess);
        return builder;
    }

    public static TBuilder RequireUsersAdminAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.UsersAdminAccess);
        return builder;
    }

    public static TBuilder RequireFormalCloseAdminAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(PlatformAuthorizationPolicies.FormalCloseAdminAccess);
        return builder;
    }
}
