using System.Text.RegularExpressions;
using Lex.SharedKernel;

namespace Lex.Modules.Decisions.Domain.ValueObjects;

public sealed class CaseIdentifier : ValueObject
{
    public string ChamberCode { get; }
    public string CaseNumber { get; }
    public string DecisionNumber { get; }

    // Yargıtay Esas ve Karar numaraları için format denetimi: "YYYY/NNNN"
    private static readonly Regex NumberPattern = new(@"^\d{4}/\d+$", RegexOptions.Compiled);

    public CaseIdentifier(string chamberCode, string caseNumber, string decisionNumber)
    {
        if (string.IsNullOrWhiteSpace(chamberCode))
            throw new ArgumentException("Chamber code cannot be empty.");
        
        if (string.IsNullOrWhiteSpace(caseNumber) || !NumberPattern.IsMatch(caseNumber))
            throw new ArgumentException("Case number must be in 'YYYY/NNNN' format.");

        if (string.IsNullOrWhiteSpace(decisionNumber) || !NumberPattern.IsMatch(decisionNumber))
            throw new ArgumentException("Decision number must be in 'YYYY/NNNN' format.");

        // Veriyi normalize ederek (büyük harfe çevirip boşlukları temizleyerek) kaydediyoruz
        ChamberCode = chamberCode.Trim().ToUpperInvariant();
        CaseNumber = caseNumber.Trim();
        DecisionNumber = decisionNumber.Trim();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ChamberCode;
        yield return CaseNumber;
        yield return DecisionNumber;
    }

    public override string ToString() => $"{ChamberCode}-{CaseNumber}-{DecisionNumber}";
}
