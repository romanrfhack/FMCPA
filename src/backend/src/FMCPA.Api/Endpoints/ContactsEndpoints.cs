using System.Net.Mail;
using FMCPA.Api.Contracts.Shared;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class ContactsEndpoints
{
    public static IEndpointRouteBuilder MapContactsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Contacts");

        group.MapGet(
            "/contact-types",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var response = await dbContext.ContactTypes
                    .AsNoTracking()
                    .OrderBy(contactType => contactType.SortOrder)
                    .ThenBy(contactType => contactType.Name)
                    .Select(contactType => new ContactTypeResponse(
                        contactType.Id,
                        contactType.Code,
                        contactType.Name,
                        contactType.Description,
                        contactType.SortOrder))
                    .ToListAsync(cancellationToken);

                return Results.Ok(response);
            });

        group.MapGet(
            "/contacts",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var response = await dbContext.Contacts
                    .AsNoTracking()
                    .Include(contact => contact.ContactType)
                    .OrderBy(contact => contact.Name)
                    .Select(contact => new ContactResponse(
                        contact.Id,
                        contact.Name,
                        contact.ContactTypeId,
                        contact.ContactType!.Code,
                        contact.ContactType.Name,
                        contact.OrganizationOrDependency,
                        contact.RoleTitle,
                        contact.MobilePhone,
                        contact.WhatsAppPhone,
                        contact.Email,
                        contact.Notes,
                        contact.CreatedUtc,
                        contact.UpdatedUtc))
                    .ToListAsync(cancellationToken);

                return Results.Ok(response);
            });

        group.MapPost(
            "/contacts",
            async (CreateContactRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateCreateContactRequest(request);

                var contactType = await dbContext.ContactTypes
                    .AsNoTracking()
                    .SingleOrDefaultAsync(type => type.Id == request.ContactTypeId, cancellationToken);

                if (contactType is null)
                {
                    errors["contactTypeId"] = ["The selected contact type does not exist."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var contact = new Contact(
                    request.Name,
                    request.ContactTypeId,
                    request.OrganizationOrDependency,
                    request.RoleTitle,
                    request.MobilePhone,
                    request.WhatsAppPhone,
                    request.Email,
                    request.Notes);

                dbContext.Contacts.Add(contact);
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new ContactResponse(
                    contact.Id,
                    contact.Name,
                    contact.ContactTypeId,
                    contactType!.Code,
                    contactType.Name,
                    contact.OrganizationOrDependency,
                    contact.RoleTitle,
                    contact.MobilePhone,
                    contact.WhatsAppPhone,
                    contact.Email,
                    contact.Notes,
                    contact.CreatedUtc,
                    contact.UpdatedUtc);

                return Results.Created($"/api/contacts/{contact.Id}", response);
            });

        return app;
    }

    private static Dictionary<string, string[]> ValidateCreateContactRequest(CreateContactRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }

        if (request.ContactTypeId <= 0)
        {
            errors["contactTypeId"] = ["ContactTypeId is required."];
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
        {
            errors["email"] = ["Email must be a valid email address."];
        }

        return errors;
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
