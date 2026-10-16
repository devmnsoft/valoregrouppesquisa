using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Services.Bff;

/// <summary>
/// Leitor canônico e seguro do corpo de requisições recebidas pelo BFF,
/// tratando métodos sem corpo, corpos vazios, tamanho desconhecido, chunked,
/// tipos de mídia incompatíveis e JSON malformado.
/// </summary>
public static class BffBodyReader {
    public const long DefaultMaxBodySizeBytes = 10 * 1024 * 1024; // 10 MB

    public static async Task<(bool Success, object? Body, IActionResult? ErrorResult)> ReadJsonBodyAsync(
        HttpRequest request,
        string correlationId,
        bool isBodyRequired = false,
        long maxSizeBytes = DefaultMaxBodySizeBytes,
        CancellationToken cancellationToken = default) {
        // 1. Métodos de consulta nunca utilizam corpo neste BFF
        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsTrace(request.Method)
            || HttpMethods.IsOptions(request.Method)) {
            return (true, null, null);
        }

        // 2. Tamanho explicitamente zero
        if (request.ContentLength == 0) {
            if (isBodyRequired) {
                return (false, null, CreateProblem(StatusCodes.Status400BadRequest,
                    "Corpo da requisição obrigatório",
                    "O corpo da requisição é obrigatório para esta operação.",
                    "REQUEST_BODY_REQUIRED",
                    correlationId));
            }
            return (true, null, null);
        }

        // 3. Leitura com controle de tamanho para fluxos com Content-Length desconhecido / chunked
        using var memoryStream = new MemoryStream();
        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await request.Body.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0) {
            totalBytesRead += bytesRead;
            if (totalBytesRead > maxSizeBytes) {
                return (false, null, CreateProblem(StatusCodes.Status413PayloadTooLarge,
                    "Corpo da requisição muito grande",
                    $"O tamanho da requisição excede o limite máximo permitido de {maxSizeBytes / (1024 * 1024)} MB.",
                    "PAYLOAD_TOO_LARGE",
                    correlationId));
            }
            memoryStream.Write(buffer, 0, bytesRead);
        }

        // 4. Detecção de corpo efetivamente vazio
        if (totalBytesRead == 0) {
            if (isBodyRequired) {
                return (false, null, CreateProblem(StatusCodes.Status400BadRequest,
                    "Corpo da requisição obrigatório",
                    "O corpo da requisição é obrigatório para esta operação.",
                    "REQUEST_BODY_REQUIRED",
                    correlationId));
            }
            return (true, null, null);
        }

        var bytes = memoryStream.ToArray();
        var isWhitespaceOnly = true;
        for (int i = 0; i < bytes.Length; i++) {
            var b = bytes[i];
            if (b != ' ' && b != '\t' && b != '\r' && b != '\n') {
                isWhitespaceOnly = false;
                break;
            }
        }

        if (isWhitespaceOnly) {
            if (isBodyRequired) {
                return (false, null, CreateProblem(StatusCodes.Status400BadRequest,
                    "Corpo da requisição obrigatório",
                    "O corpo da requisição é obrigatório para esta operação.",
                    "REQUEST_BODY_REQUIRED",
                    correlationId));
            }
            return (true, null, null);
        }

        // 5. Validação do tipo de mídia (415 se corpo não for JSON)
        if (!request.HasJsonContentType()) {
            return (false, null, CreateProblem(StatusCodes.Status415UnsupportedMediaType,
                "Tipo de mídia não suportado",
                "O formato do conteúdo enviado não é suportado. Utilize application/json.",
                "UNSUPPORTED_MEDIA_TYPE",
                correlationId));
        }

        // 6. Desserialização e captura de JSON malformado (400 estruturado)
        try {
            using var document = JsonDocument.Parse(bytes);
            var element = document.RootElement.Clone();
            return (true, element, null);
        }
        catch (JsonException) {
            return (false, null, CreateProblem(StatusCodes.Status400BadRequest,
                "JSON malformado",
                "Não foi possível interpretar o corpo da requisição como JSON válido.",
                "MALFORMED_JSON",
                correlationId));
        }
    }

    private static ObjectResult CreateProblem(int statusCode, string title, string detail, string code, string correlationId) {
        var problem = new ProblemDetails {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{statusCode}"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = correlationId;

        return new ObjectResult(problem) {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
