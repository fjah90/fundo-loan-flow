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
        var result = await submit.HandleAsync(request.ToCommand(), ct);
        var response = result.ToResponse();

        if (result is SubmissionApproved a)
            log.LogInformation("Loan application approved for customer {CustomerId}", a.CustomerId);
        else if (result is SubmissionDenied d)
            log.LogInformation("Loan application denied: {Codes}", string.Join(",", d.Reasons.Select(r => r.Code)));

        return Ok(response);
    }
}
