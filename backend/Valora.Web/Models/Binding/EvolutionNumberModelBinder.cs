using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Valora.Web.Models.Binding;

public sealed class EvolutionNumberModelBinder(ILoggerFactory loggers) : IModelBinder {
    public async Task BindModelAsync(ModelBindingContext context) {
        var original = context.ValueProvider;
        try {
            context.ValueProvider = new InvariantValueProvider(original);
            await new DecimalModelBinder(NumberStyles.Float, loggers).BindModelAsync(context);
        }
        finally { context.ValueProvider = original; }
    }

    private sealed class InvariantValueProvider(IValueProvider original) : IValueProvider {
        public bool ContainsPrefix(string prefix) => original.ContainsPrefix(prefix);
        public ValueProviderResult GetValue(string key) {
            var result = original.GetValue(key);
            return result == ValueProviderResult.None ? result : new(result.Values, CultureInfo.InvariantCulture);
        }
    }
}
