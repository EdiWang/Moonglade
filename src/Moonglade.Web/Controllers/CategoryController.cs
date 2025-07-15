using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Core.CategoryFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Exporting;
using Moonglade.Web.Attributes;
using System.Text.Json;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController(IQueryMediator queryMediator, ICommandMediator commandMediator) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoryEntity>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var cat = await queryMediator.QueryAsync(new GetCategoryQuery(id));
        if (null == cat) return NotFound();

        // return Ok(cat); 

        // Workaround .NET by design bug: https://stackoverflow.com/questions/60184661/net-core-3-jsonignore-not-working-when-requesting-single-resource
        // https://github.com/dotnet/aspnetcore/issues/31396
        // https://github.com/dotnet/efcore/issues/33223
        return Content(JsonSerializer.Serialize(cat, MoongladeJsonSerializerOptions.Default), "application/json");
    }

    [HttpGet("list")]
    [ProducesResponseType<List<CategoryEntity>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var list = await queryMediator.QueryAsync(new GetCategoriesQuery());
        return Ok(list);
    }

    [HttpPost]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCategoryCommand command)
    {
        await commandMediator.SendAsync(command);
        return Created(string.Empty, command);
    }

    [HttpPut("{id:guid}")]
    [ReadonlyMode]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
    public async Task<IActionResult> Update([NotEmpty] Guid id, UpdateCategoryCommand command)
    {
        command.Id = id;
        var oc = await commandMediator.SendAsync(command);
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ReadonlyMode]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var oc = await commandMediator.SendAsync(new DeleteCategoryCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }
}