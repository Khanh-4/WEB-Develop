using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TechSpecs.ModelBinders;

public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var result = ctx.ValueProvider.GetValue(ctx.ModelName);
        if (result == ValueProviderResult.None) return Task.CompletedTask;

        ctx.ModelState.SetModelValue(ctx.ModelName, result);
        var raw = result.FirstValue;
        if (string.IsNullOrWhiteSpace(raw)) return Task.CompletedTask;

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            ctx.Result = ModelBindingResult.Success(val);
        else
            ctx.ModelState.TryAddModelError(ctx.ModelName, $"'{raw}' is not a valid number.");

        return Task.CompletedTask;
    }
}

public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext ctx)
    {
        if (ctx.Metadata.ModelType == typeof(decimal) || ctx.Metadata.ModelType == typeof(decimal?))
            return new InvariantDecimalModelBinder();
        return null;
    }
}
