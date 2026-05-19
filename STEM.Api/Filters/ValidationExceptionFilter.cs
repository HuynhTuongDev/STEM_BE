using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace STEM.Api.Filters;

public class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray()
                );

            var errorResponse = new
            {
                success = false,
                message = "Validation failed.",
                errors = errors
            };

            context.Result = new BadRequestObjectResult(errorResponse);
            context.ExceptionHandled = true;
        }
    }
}
