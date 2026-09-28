using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Customers;

/// <summary>CNPJ (value object). Armazena só dígitos; valida os dígitos verificadores.</summary>
public sealed record Cnpj
{
    private static readonly int[] Weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] Weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private Cnpj(string value) => Value = value;

    public string Value { get; }

    public static Result<Cnpj> Create(string? input)
    {
        var digits = Guard.OnlyDigits(input);
        if (digits.Length != 14 || digits.Distinct().Count() == 1 || !HasValidCheckDigits(digits))
            return CustomerErrors.InvalidDocument;

        return new Cnpj(digits);
    }

    /// <summary>00.000.000/0000-00</summary>
    public string Formatted =>
        $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";

    public override string ToString() => Formatted;

    private static bool HasValidCheckDigits(string digits)
    {
        var d1 = CheckDigit(digits[..12], Weights1);
        var d2 = CheckDigit(digits[..12] + d1, Weights2);
        return digits[12] - '0' == d1 && digits[13] - '0' == d2;
    }

    private static int CheckDigit(string partial, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
            sum += (partial[i] - '0') * weights[i];
        var mod = sum % 11;
        return mod < 2 ? 0 : 11 - mod;
    }
}
