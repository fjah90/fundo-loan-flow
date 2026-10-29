using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LoanFlow.Api;

// NUL is rejected because Postgres cannot store it (AR-48.1).
public sealed record LoanApplicationRequest(
    [MaxLength(100), RegularExpression(@"^[^\u0000]*$")] string FirstName,
    [MaxLength(100), RegularExpression(@"^[^\u0000]*$")] string LastName,
    string Ssn,
    [MaxLength(100), RegularExpression(@"^[^\u0000]*$")] string Street,
    [MaxLength(100), RegularExpression(@"^[^\u0000]*$")] string City,
    string State,
    string ZipCode,
    [MaxLength(100), RegularExpression(@"^[^\u0000]*$")] string Company,
    [Required] decimal? RequestedAmount)
{
    // Omits Ssn so ToString() never prints it. The compiler-generated ToString() calls this.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"FirstName = {FirstName}, LastName = {LastName}, Street = {Street}, City = {City}, ")
            .Append($"State = {State}, ZipCode = {ZipCode}, Company = {Company}, RequestedAmount = {RequestedAmount}");
        return true;
    }
}
