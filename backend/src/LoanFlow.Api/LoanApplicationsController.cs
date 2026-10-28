using LoanFlow.Application;
using Microsoft.AspNetCore.Mvc;

namespace LoanFlow.Api;

[ApiController]
[Route("api/loan-applications")]
public sealed class LoanApplicationsController(SubmitLoanApplication submit, ILogger<LoanApplicationsController> log)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(LoanApplicationRequest request, CancellationToken ct)
    {
        var command = new SubmitLoanApplicationCommand(
            request.FirstName, request.LastName, request.Ssn, request.Street, request.City,
            request.State, request.ZipCode, request.Company, request.RequestedAmount!.Value);

        switch (await submit.HandleAsync(command, ct))
        {
            case SubmissionApproved a:
                log.LogInformation("Loan application approved for customer {CustomerId}", a.CustomerId);
                return Ok(new
                {
                    status = "Approved",
                    applicationId = a.ApplicationId,
                    customerId = a.CustomerId,
                    isReturningCustomer = a.IsReturningCustomer,
                });
            case SubmissionDenied d:
                var codes = d.Reasons.Select(r => r.Code).ToArray();
                log.LogInformation("Loan application denied: {Codes}", string.Join(",", codes));
                return Ok(new { status = "Denied", reasons = codes.Select(c => new { code = c }) });
            default:
                throw new InvalidOperationException("Unknown submission result.");
        }
    }
}
