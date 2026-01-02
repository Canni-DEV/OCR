using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Ocr.Api.Models;

namespace Ocr.Api.Processing;

public class TextPostProcessor
{
    private readonly ILogger<TextPostProcessor> _logger;

    public TextPostProcessor(ILogger<TextPostProcessor> logger)
    {
        _logger = logger;
    }

    public Task<ProcessedTextResult> ProcessAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = NormalizeWhitespace(text);
        var cuit = ExtractCuit(normalized);
        var remitoRegex = BuildRemitoRegex(new[] { "0001-00001234", "0002-00005678" });
        var remito = ExtractRemito(normalized, remitoRegex);

        return Task.FromResult(new ProcessedTextResult
        {
            NormalizedText = normalized,
            DetectedCuit = cuit,
            NumeroRemito = remito,
            CustomerOrVendor = DetectCustomer(normalized)
        });
    }

    public string BuildRemitoRegex(IEnumerable<string> examples)
    {
        var patterns = new List<string>();
        foreach (var example in examples)
        {
            var tokens = Regex.Split(example, "[^A-Za-z0-9]+").Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
            if (tokens.Length == 0)
            {
                continue;
            }

            var parts = tokens.Select(token =>
            {
                if (Regex.IsMatch(token, "^\\d+$"))
                {
                    return "\\d{" + token.Length + "}";
                }

                return Regex.Escape(token);
            });

            patterns.Add(string.Join("[\\s_-]", parts));
        }

        if (patterns.Count == 0)
        {
            patterns.Add("\\d{4}[\\s_-]\\d{4,8}");
        }

        return $"(?i)\\b({string.Join("|", patterns)})\\b";
    }

    public string? ExtractRemito(string text, string regex)
    {
        var match = Regex.Match(text, regex);
        if (match.Success)
        {
            return match.Value;
        }

        return null;
    }

    private static string NormalizeWhitespace(string text)
    {
        var normalized = Regex.Replace(text ?? string.Empty, "\r\n|\r|\n", " \n ");
        normalized = Regex.Replace(normalized, "\s+", " ").Trim();
        return normalized;
    }

    private static string? ExtractCuit(string text)
    {
        var match = Regex.Match(text, "(?i)\\b(\d{2}-?\d{8}-?\d)\\b");
        return match.Success ? match.Value : null;
    }

    private string? DetectCustomer(string text)
    {
        if (text.Contains("cliente", StringComparison.OrdinalIgnoreCase))
        {
            return "cliente";
        }

        if (text.Contains("proveedor", StringComparison.OrdinalIgnoreCase))
        {
            return "proveedor";
        }

        _logger.LogDebug("No customer/provider keywords detected");
        return null;
    }
}
