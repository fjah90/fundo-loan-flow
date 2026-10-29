using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LoanFlow.Api;

public sealed record LoanApplicationRequest(
    [FreeText] string FirstName,
    [FreeText] string LastName,
    string Ssn,
    [FreeText] string Street,
    [FreeText] string City,
    string State,
    string ZipCode,
    [FreeText] string Company,
    // Nullable so a missing amount stays null and [Required] reports it under requestedAmount; a plain decimal would bind to 0.
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
