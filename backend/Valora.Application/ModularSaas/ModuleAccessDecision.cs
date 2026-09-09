namespace Valora.Application.ModularSaas;

public sealed record ModuleAccessDecision(bool Allowed, bool ReadOnly, string Code, string Message) {
    public static ModuleAccessDecision Granted(bool readOnly = false) =>
        new(true, readOnly, readOnly ? "MODULE_READ_ONLY" : "MODULE_AVAILABLE",
            readOnly ? "Este módulo está disponível somente para consulta." : "Módulo disponível.");

    public static ModuleAccessDecision Denied(string code, string message) => new(false, false, code, message);
}
