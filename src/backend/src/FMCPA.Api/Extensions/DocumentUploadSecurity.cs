using Microsoft.AspNetCore.Http;

namespace FMCPA.Api.Extensions;

internal static class DocumentUploadSecurity
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const string MaxFileSizeLabel = "10 MB";

    private const string OctetStreamContentType = "application/octet-stream";

    private static readonly IReadOnlyDictionary<string, string> ContentTypeByExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(
        ContentTypeByExtension.Values,
        StringComparer.OrdinalIgnoreCase);

    public static async Task<ValidatedDocumentUpload?> ValidateAsync(
        IFormFile? file,
        string fieldName,
        IDictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            AddError(errors, fieldName, "El archivo es obligatorio.");
            return null;
        }

        var fileErrors = new List<string>();
        if (file.Length <= 0)
        {
            fileErrors.Add("El archivo no puede estar vacio.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            fileErrors.Add($"El archivo excede el tamano maximo permitido de {MaxFileSizeLabel}.");
        }

        var sanitizedFileName = SanitizeOriginalFileName(file.FileName);
        var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
        if (!ContentTypeByExtension.ContainsKey(extension))
        {
            fileErrors.Add("La extension del archivo no esta permitida. Use .pdf, .jpg, .jpeg o .png.");
        }

        var normalizedContentType = NormalizeContentType(file.ContentType);
        if (!AllowedContentTypes.Contains(normalizedContentType))
        {
            fileErrors.Add("El content-type del archivo no esta permitido. Use application/pdf, image/jpeg o image/png.");
        }
        else if (ContentTypeByExtension.TryGetValue(extension, out var expectedContentType)
                 && !string.Equals(expectedContentType, normalizedContentType, StringComparison.OrdinalIgnoreCase))
        {
            fileErrors.Add("La extension del archivo no coincide con su content-type.");
        }

        if (fileErrors.Count == 0 && !await HasExpectedSignatureAsync(file, extension, cancellationToken))
        {
            fileErrors.Add("El contenido del archivo no coincide con la extension permitida.");
        }

        if (fileErrors.Count > 0)
        {
            AddErrors(errors, fieldName, fileErrors);
            return null;
        }

        return new ValidatedDocumentUpload(
            sanitizedFileName,
            normalizedContentType,
            file.Length);
    }

    public static string SanitizeOriginalFileName(string? originalFileName)
    {
        var fileName = Path.GetFileName(originalFileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "documento";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitizedCharacters = fileName
            .Select(character => IsUnsafeFileNameCharacter(character, invalidCharacters) ? '_' : character)
            .ToArray();

        var sanitized = new string(sanitizedCharacters).Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "documento";
        }

        var extension = Path.GetExtension(sanitized).ToLowerInvariant();
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized).Trim();
        if (string.IsNullOrWhiteSpace(nameWithoutExtension))
        {
            nameWithoutExtension = "documento";
        }

        if (nameWithoutExtension.Length > 100)
        {
            nameWithoutExtension = nameWithoutExtension[..100].Trim();
        }

        return string.IsNullOrWhiteSpace(extension)
            ? nameWithoutExtension
            : $"{nameWithoutExtension}{extension}";
    }

    public static string NormalizeDownloadContentType(string? contentType)
    {
        var normalizedContentType = NormalizeContentType(contentType);
        return AllowedContentTypes.Contains(normalizedContentType)
            ? normalizedContentType
            : OctetStreamContentType;
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        var trimmed = contentType.Trim();
        var separatorIndex = trimmed.IndexOf(';', StringComparison.Ordinal);
        return (separatorIndex >= 0 ? trimmed[..separatorIndex] : trimmed).Trim().ToLowerInvariant();
    }

    private static async Task<bool> HasExpectedSignatureAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        return extension switch
        {
            ".pdf" => bytesRead >= 5
                && header[0] == 0x25
                && header[1] == 0x50
                && header[2] == 0x44
                && header[3] == 0x46
                && header[4] == 0x2D,
            ".jpg" or ".jpeg" => bytesRead >= 3
                && header[0] == 0xFF
                && header[1] == 0xD8
                && header[2] == 0xFF,
            ".png" => bytesRead >= 8
                && header[0] == 0x89
                && header[1] == 0x50
                && header[2] == 0x4E
                && header[3] == 0x47
                && header[4] == 0x0D
                && header[5] == 0x0A
                && header[6] == 0x1A
                && header[7] == 0x0A,
            _ => false
        };
    }

    private static bool IsUnsafeFileNameCharacter(char character, char[] invalidCharacters)
    {
        return char.IsControl(character)
               || character is '"' or '\'' or '`' or ';' or ':' or '<' or '>' or '|' or '/' or '\\'
               || invalidCharacters.Contains(character);
    }

    private static void AddErrors(
        IDictionary<string, string[]> errors,
        string fieldName,
        IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            AddError(errors, fieldName, message);
        }
    }

    private static void AddError(
        IDictionary<string, string[]> errors,
        string fieldName,
        string message)
    {
        errors[fieldName] = errors.TryGetValue(fieldName, out var existingMessages)
            ? existingMessages.Concat([message]).ToArray()
            : [message];
    }
}

internal sealed record ValidatedDocumentUpload(
    string OriginalFileName,
    string ContentType,
    long SizeBytes);
