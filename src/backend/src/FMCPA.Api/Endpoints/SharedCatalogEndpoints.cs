using FMCPA.Api.Contracts.Shared;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class SharedCatalogEndpoints
{
    public static IEndpointRouteBuilder MapSharedCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Shared Catalogs");

        group.MapGet(
            "/commission-types",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var response = await dbContext.CommissionTypes
                    .AsNoTracking()
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Name)
                    .Select(item => new CatalogItemResponse(
                        item.Id,
                        item.Code,
                        item.Name,
                        item.Description,
                        item.SortOrder,
                        item.IsActive))
                    .ToListAsync(cancellationToken);

                return Results.Ok(response);
            });

        group.MapPost(
            "/commission-types",
            async (CreateCatalogItemRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return await CreateCatalogItemAsync(
                    request,
                    dbContext,
                    dbContext.CommissionTypes,
                    (code, name, description, sortOrder) => new CommissionType(code, name, description, sortOrder),
                    "/api/commission-types",
                    cancellationToken);
            });

        group.MapGet(
            "/evidence-types",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var response = await dbContext.EvidenceTypes
                    .AsNoTracking()
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Name)
                    .Select(item => new CatalogItemResponse(
                        item.Id,
                        item.Code,
                        item.Name,
                        item.Description,
                        item.SortOrder,
                        item.IsActive))
                    .ToListAsync(cancellationToken);

                return Results.Ok(response);
            });

        group.MapPost(
            "/evidence-types",
            async (CreateCatalogItemRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return await CreateCatalogItemAsync(
                    request,
                    dbContext,
                    dbContext.EvidenceTypes,
                    (code, name, description, sortOrder) => new EvidenceType(code, name, description, sortOrder),
                    "/api/evidence-types",
                    cancellationToken);
            });

        group.MapGet(
            "/module-statuses",
            async (string? moduleCode, string? contextCode, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var query = dbContext.ModuleStatusCatalogEntries
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(moduleCode))
                {
                    query = query.Where(item => item.ModuleCode == moduleCode.Trim().ToUpperInvariant());
                }

                if (!string.IsNullOrWhiteSpace(contextCode))
                {
                    query = query.Where(item => item.ContextCode == contextCode.Trim().ToUpperInvariant());
                }

                var response = await query
                    .OrderBy(item => item.ModuleName)
                    .ThenBy(item => item.ContextName)
                    .ThenBy(item => item.SortOrder)
                    .Select(item => new ModuleStatusCatalogEntryResponse(
                        item.Id,
                        item.ModuleCode,
                        item.ModuleName,
                        item.ContextCode,
                        item.ContextName,
                        item.StatusCode,
                        item.StatusName,
                        item.Description,
                        item.SortOrder,
                        item.IsClosed,
                        item.AlertsEnabledByDefault,
                        item.IsActive))
                    .ToListAsync(cancellationToken);

                return Results.Ok(response);
            });

        group.MapPost(
            "/module-statuses",
            async (CreateModuleStatusCatalogEntryRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateModuleStatusRequest(request);

                var normalizedModuleCode = request.ModuleCode.Trim().ToUpperInvariant();
                var normalizedContextCode = string.IsNullOrWhiteSpace(request.ContextCode)
                    ? "GENERAL"
                    : request.ContextCode.Trim().ToUpperInvariant();
                var normalizedStatusCode = request.StatusCode.Trim().ToUpperInvariant();

                var duplicateExists = await dbContext.ModuleStatusCatalogEntries
                    .AnyAsync(
                        item => item.ModuleCode == normalizedModuleCode
                            && item.ContextCode == normalizedContextCode
                            && item.StatusCode == normalizedStatusCode,
                        cancellationToken);

                if (duplicateExists)
                {
                    errors["statusCode"] = ["This module context already contains the requested status code."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var item = new ModuleStatusCatalogEntry(
                    request.ModuleCode,
                    request.ModuleName,
                    request.ContextCode,
                    request.ContextName,
                    request.StatusCode,
                    request.StatusName,
                    request.Description,
                    request.SortOrder,
                    request.IsClosed,
                    request.AlertsEnabledByDefault);

                dbContext.ModuleStatusCatalogEntries.Add(item);
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new ModuleStatusCatalogEntryResponse(
                    item.Id,
                    item.ModuleCode,
                    item.ModuleName,
                    item.ContextCode,
                    item.ContextName,
                    item.StatusCode,
                    item.StatusName,
                    item.Description,
                    item.SortOrder,
                    item.IsClosed,
                    item.AlertsEnabledByDefault,
                    item.IsActive);

                return Results.Created($"/api/module-statuses/{item.Id}", response);
            });

        return app;
    }

    private static async Task<IResult> CreateCatalogItemAsync<TEntity>(
        CreateCatalogItemRequest request,
        PlatformDbContext dbContext,
        DbSet<TEntity> dbSet,
        Func<string, string, string?, int, TEntity> factory,
        string locationBase,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var errors = ValidateCatalogItemRequest(request);

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var duplicateExists = await dbSet.AnyAsync(
            BuildCodePredicate<TEntity>(normalizedCode),
            cancellationToken);

        if (duplicateExists)
        {
            errors["code"] = ["The requested code already exists."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var item = factory(request.Code, request.Name, request.Description, request.SortOrder);
        dbSet.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        var id = (int)item.GetType().GetProperty("Id")!.GetValue(item)!;
        var code = (string)item.GetType().GetProperty("Code")!.GetValue(item)!;
        var name = (string)item.GetType().GetProperty("Name")!.GetValue(item)!;
        var description = (string?)item.GetType().GetProperty("Description")!.GetValue(item);
        var sortOrder = (int)item.GetType().GetProperty("SortOrder")!.GetValue(item)!;
        var isActive = (bool)item.GetType().GetProperty("IsActive")!.GetValue(item)!;

        var response = new CatalogItemResponse(id, code, name, description, sortOrder, isActive);
        return Results.Created($"{locationBase}/{id}", response);
    }

    private static Dictionary<string, string[]> ValidateCatalogItemRequest(CreateCatalogItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            errors["code"] = ["Code is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateModuleStatusRequest(CreateModuleStatusCatalogEntryRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.ModuleCode))
        {
            errors["moduleCode"] = ["ModuleCode is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ModuleName))
        {
            errors["moduleName"] = ["ModuleName is required."];
        }

        var hasContextCode = !string.IsNullOrWhiteSpace(request.ContextCode);
        var hasContextName = !string.IsNullOrWhiteSpace(request.ContextName);
        if (hasContextCode != hasContextName)
        {
            errors["contextCode"] = ["ContextCode and ContextName must be provided together."];
        }

        if (string.IsNullOrWhiteSpace(request.StatusCode))
        {
            errors["statusCode"] = ["StatusCode is required."];
        }

        if (string.IsNullOrWhiteSpace(request.StatusName))
        {
            errors["statusName"] = ["StatusName is required."];
        }

        return errors;
    }

    private static System.Linq.Expressions.Expression<Func<TEntity, bool>> BuildCodePredicate<TEntity>(string normalizedCode)
        where TEntity : class
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "item");
        var property = System.Linq.Expressions.Expression.Property(parameter, "Code");
        var constant = System.Linq.Expressions.Expression.Constant(normalizedCode);
        var body = System.Linq.Expressions.Expression.Equal(property, constant);
        return System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }
}
