using Expenses.Application;
using Expenses.Application.Contracts;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Expenses.Api.Controllers;

/// <summary>Public: the category list is reference data, not user data.</summary>
[ApiController]
[Route("api/categories")]
[AllowAnonymous]
public sealed class CategoriesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CategoryResponse>> List() => Ok(CategoryCatalog.All);
}
