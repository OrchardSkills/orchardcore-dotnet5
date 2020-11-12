using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Liquid.Filters
{
    public class JsonParseFilter : ILiquidFilter
    {
        public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var parsedValue = JToken.Parse(input.ToStringValue());
            if (parsedValue.Type == JTokenType.Array)
            {
                return new ValueTask<FluidValue>(FluidValue.Create(parsedValue));
            }
            return new ValueTask<FluidValue>(new ObjectValue(parsedValue));
        }
    }
}
