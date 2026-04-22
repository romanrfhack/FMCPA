using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Api.Extensions;

internal static class StateTransitionSupport
{
    public static bool IsTerminal(ModuleStatusCatalogEntry? status)
    {
        return status?.IsClosed == true;
    }

    public static string BuildDuplicateCloseEventMessage(string entityLabel)
    {
        return $"No es posible registrar el cierre formal porque {entityLabel} ya cuenta con un evento de cierre registrado.";
    }

    public static string BuildTerminalCloseMessage(string entityLabel, ModuleStatusCatalogEntry currentStatus)
    {
        return $"No es posible registrar el cierre formal porque {entityLabel} ya se encuentra en estado terminal ({currentStatus.StatusName}).";
    }

    public static string BuildTerminalMutationMessage(string parentLabel, string operationLabel, ModuleStatusCatalogEntry currentStatus)
    {
        return $"No es posible {operationLabel} porque {parentLabel} ya se encuentra en estado terminal ({currentStatus.StatusName}).";
    }
}
