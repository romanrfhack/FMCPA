namespace FMCPA.Infrastructure.Persistence.Configurations.Shared;

internal static class SharedCatalogSeedData
{
    public static readonly object[] ContactTypes =
    [
        new
        {
            Id = 1,
            Code = "INTERNAL",
            Name = "Interno",
            Description = "Contacto interno reutilizable dentro de la operacion del proyecto.",
            SortOrder = 1,
            IsActive = true
        },
        new
        {
            Id = 2,
            Code = "EXTERNAL",
            Name = "Externo",
            Description = "Contacto externo reutilizable entre proyectos y gestiones.",
            SortOrder = 2,
            IsActive = true
        }
    ];

    public static readonly object[] CommissionTypes =
    [
        new { Id = 1, Code = "ADMINISTRATION", Name = "Administracion", Description = "Tipo de comision base compartido.", SortOrder = 1, IsActive = true },
        new { Id = 2, Code = "OPERATIONAL", Name = "Operativa", Description = "Tipo de comision base compartido.", SortOrder = 2, IsActive = true },
        new { Id = 3, Code = "COMMERCIAL", Name = "Comercial", Description = "Tipo de comision base compartido.", SortOrder = 3, IsActive = true },
        new { Id = 4, Code = "INTERMEDIATION", Name = "Intermediacion", Description = "Tipo de comision base compartido.", SortOrder = 4, IsActive = true },
        new { Id = 5, Code = "EXTERNAL_MANAGEMENT", Name = "Gestion externa", Description = "Tipo de comision base compartido.", SortOrder = 5, IsActive = true },
        new { Id = 6, Code = "INSTITUTIONAL_LINK", Name = "Vinculacion institucional", Description = "Tipo de comision base compartido.", SortOrder = 6, IsActive = true },
        new { Id = 7, Code = "PROMOTER", Name = "Promotor", Description = "Tipo de comision base compartido.", SortOrder = 7, IsActive = true },
        new { Id = 8, Code = "COORDINATION", Name = "Coordinacion", Description = "Tipo de comision base compartido.", SortOrder = 8, IsActive = true },
        new { Id = 9, Code = "SPECIAL_NEGOTIATED", Name = "Especial/negociada", Description = "Tipo de comision base compartido.", SortOrder = 9, IsActive = true }
    ];

    public static readonly object[] EvidenceTypes =
    [
        new { Id = 1, Code = "PHOTO", Name = "Fotografia", Description = "Evidencia grafica basica.", SortOrder = 1, IsActive = true },
        new { Id = 2, Code = "VIDEO", Name = "Video", Description = "Evidencia audiovisual.", SortOrder = 2, IsActive = true },
        new { Id = 3, Code = "SIGNED_DOCUMENT", Name = "Documento firmado", Description = "Documento con firma como soporte.", SortOrder = 3, IsActive = true },
        new { Id = 4, Code = "SUPPORT_DOCUMENT", Name = "Documento soporte", Description = "Documento de respaldo general.", SortOrder = 4, IsActive = true },
        new { Id = 5, Code = "OTHER", Name = "Otro", Description = "Categoria abierta controlada para nueva evidencia.", SortOrder = 5, IsActive = true }
    ];

    public static readonly object[] ModuleStatuses =
    [
        CreateModuleStatus(1, "MARKETS", "Mercados", "GENERAL", "General", "ACTIVE", "Activo", 1, false, true),
        CreateModuleStatus(2, "MARKETS", "Mercados", "GENERAL", "General", "CLOSED", "Cerrado", 2, true, false),
        CreateModuleStatus(3, "DONATARIAS", "Donatarias", "GENERAL", "General", "ACTIVE", "Activo", 1, false, true),
        CreateModuleStatus(4, "DONATARIAS", "Donatarias", "GENERAL", "General", "CLOSED", "Cerrado", 2, true, false),
        CreateModuleStatus(5, "FINANCIALS", "Financieras", "GENERAL", "General", "ACTIVE", "Activo", 1, false, true),
        CreateModuleStatus(6, "FINANCIALS", "Financieras", "GENERAL", "General", "CLOSED", "Cerrado", 2, true, false),
        CreateModuleStatus(7, "FEDERATION", "Federacion", "GENERAL", "General", "ACTIVE", "Activo", 1, false, true),
        CreateModuleStatus(8, "FEDERATION", "Federacion", "GENERAL", "General", "CLOSED", "Cerrado", 2, true, false),
        CreateModuleStatus(9, "COMMISSIONS", "Comisiones", "GENERAL", "General", "ACTIVE", "Activo", 1, false, true),
        CreateModuleStatus(10, "COMMISSIONS", "Comisiones", "GENERAL", "General", "CLOSED", "Cerrado", 2, true, false),
        CreateModuleStatus(11, "CONTACTS", "Contactos", "GENERAL", "General", "ACTIVE", "Activo", 1, false, true),
        CreateModuleStatus(12, "CONTACTS", "Contactos", "GENERAL", "General", "CLOSED", "Cerrado", 2, true, false),
        CreateModuleStatus(13, "BITACORA", "Bitacora", "GENERAL", "General", "ACTIVE", "Activo", 1, false, true),
        CreateModuleStatus(14, "BITACORA", "Bitacora", "GENERAL", "General", "CLOSED", "Cerrado", 2, true, false),
        CreateModuleStatus(1001, "MARKETS", "Mercados", "MARKET", "Mercado", "ACTIVE", "Activo", 1, false, true),
        CreateModuleStatus(1002, "MARKETS", "Mercados", "MARKET", "Mercado", "INACTIVE", "Inactivo", 2, false, true),
        CreateModuleStatus(1003, "MARKETS", "Mercados", "MARKET", "Mercado", "CLOSED", "Cerrado", 3, true, false),
        CreateModuleStatus(1004, "MARKETS", "Mercados", "MARKET", "Mercado", "ARCHIVED", "Archivado", 4, true, false),
        CreateModuleStatus(1101, "MARKETS", "Mercados", "MARKET_ISSUE", "Incidencia de mercado", "PROCEEDED", "Procedio", 1, false, true),
        CreateModuleStatus(1102, "MARKETS", "Mercados", "MARKET_ISSUE", "Incidencia de mercado", "IN_ATTENTION", "En atencion", 2, false, true),
        CreateModuleStatus(1103, "MARKETS", "Mercados", "MARKET_ISSUE", "Incidencia de mercado", "NOT_ATTENDED", "No atendido", 3, false, true),
        CreateModuleStatus(1104, "MARKETS", "Mercados", "MARKET_ISSUE", "Incidencia de mercado", "ATTENDED_SATISFACTORILY", "Atendido satisfactoriamente", 4, false, false),
        CreateModuleStatus(1105, "MARKETS", "Mercados", "MARKET_ISSUE", "Incidencia de mercado", "CONCLUDED_UNSATISFACTORILY", "Concluido no satisfactorio", 5, false, false),
        CreateModuleStatus(1106, "MARKETS", "Mercados", "MARKET_ISSUE", "Incidencia de mercado", "CLOSED", "Cerrado", 6, true, false),
        CreateModuleStatus(1201, "DONATARIAS", "Donatarias", "DONATION", "Donacion", "NOT_APPLIED", "No aplicada", 1, false, true),
        CreateModuleStatus(1202, "DONATARIAS", "Donatarias", "DONATION", "Donacion", "PARTIALLY_APPLIED", "Aplicacion parcial", 2, false, true),
        CreateModuleStatus(1203, "DONATARIAS", "Donatarias", "DONATION", "Donacion", "APPLIED", "Aplicada", 3, false, false),
        CreateModuleStatus(1204, "DONATARIAS", "Donatarias", "DONATION", "Donacion", "CLOSED", "Cerrada", 4, true, false),
        CreateModuleStatus(1301, "DONATARIAS", "Donatarias", "DONATION_APPLICATION", "Aplicacion de donacion", "PARTIALLY_APPLIED", "Aplicacion parcial", 1, false, true),
        CreateModuleStatus(1302, "DONATARIAS", "Donatarias", "DONATION_APPLICATION", "Aplicacion de donacion", "APPLIED", "Aplicada", 2, false, false),
        CreateModuleStatus(1303, "DONATARIAS", "Donatarias", "DONATION_APPLICATION", "Aplicacion de donacion", "CLOSED", "Cerrada", 3, true, false),
        CreateModuleStatus(1401, "FINANCIALS", "Financieras", "FINANCIAL_PERMIT", "Oficio o autorizacion", "ACCEPTED", "Aceptado", 1, false, true),
        CreateModuleStatus(1402, "FINANCIALS", "Financieras", "FINANCIAL_PERMIT", "Oficio o autorizacion", "REJECTED", "Rechazado", 2, true, false),
        CreateModuleStatus(1403, "FINANCIALS", "Financieras", "FINANCIAL_PERMIT", "Oficio o autorizacion", "IN_PROCESS", "En proceso", 3, false, true),
        CreateModuleStatus(1404, "FINANCIALS", "Financieras", "FINANCIAL_PERMIT", "Oficio o autorizacion", "RENEW", "Renovar", 4, false, true),
        CreateModuleStatus(1405, "FINANCIALS", "Financieras", "FINANCIAL_PERMIT", "Oficio o autorizacion", "CLOSED", "Cerrado", 5, true, false),
        CreateModuleStatus(1501, "FEDERATION", "Federacion", "FEDERATION_ACTION", "Gestion de federacion", "IN_PROCESS", "En proceso", 1, false, true),
        CreateModuleStatus(1502, "FEDERATION", "Federacion", "FEDERATION_ACTION", "Gestion de federacion", "FOLLOW_UP_PENDING", "Seguimiento pendiente", 2, false, true),
        CreateModuleStatus(1503, "FEDERATION", "Federacion", "FEDERATION_ACTION", "Gestion de federacion", "CONCLUDED", "Concluido", 3, false, false),
        CreateModuleStatus(1504, "FEDERATION", "Federacion", "FEDERATION_ACTION", "Gestion de federacion", "CLOSED", "Cerrado", 4, true, false),
        CreateModuleStatus(1601, "FEDERATION", "Federacion", "FEDERATION_DONATION", "Donacion de federacion", "NOT_APPLIED", "No aplicada", 1, false, true),
        CreateModuleStatus(1602, "FEDERATION", "Federacion", "FEDERATION_DONATION", "Donacion de federacion", "PARTIALLY_APPLIED", "Aplicacion parcial", 2, false, true),
        CreateModuleStatus(1603, "FEDERATION", "Federacion", "FEDERATION_DONATION", "Donacion de federacion", "APPLIED", "Aplicada", 3, false, false),
        CreateModuleStatus(1604, "FEDERATION", "Federacion", "FEDERATION_DONATION", "Donacion de federacion", "CLOSED", "Cerrada", 4, true, false),
        CreateModuleStatus(1701, "FEDERATION", "Federacion", "FEDERATION_DONATION_APPLICATION", "Aplicacion de donacion de federacion", "PARTIALLY_APPLIED", "Aplicacion parcial", 1, false, true),
        CreateModuleStatus(1702, "FEDERATION", "Federacion", "FEDERATION_DONATION_APPLICATION", "Aplicacion de donacion de federacion", "APPLIED", "Aplicada", 2, false, false),
        CreateModuleStatus(1703, "FEDERATION", "Federacion", "FEDERATION_DONATION_APPLICATION", "Aplicacion de donacion de federacion", "CLOSED", "Cerrada", 3, true, false)
    ];

    private static object CreateModuleStatus(
        int id,
        string moduleCode,
        string moduleName,
        string contextCode,
        string contextName,
        string statusCode,
        string statusName,
        int sortOrder,
        bool isClosed,
        bool alertsEnabledByDefault)
    {
        return new
        {
            Id = id,
            ModuleCode = moduleCode,
            ModuleName = moduleName,
            ContextCode = contextCode,
            ContextName = contextName,
            StatusCode = statusCode,
            StatusName = statusName,
            Description = "Estado base reusable sembrado en STAGE-02 para soporte transversal inicial.",
            SortOrder = sortOrder,
            IsClosed = isClosed,
            AlertsEnabledByDefault = alertsEnabledByDefault,
            IsActive = true
        };
    }
}
