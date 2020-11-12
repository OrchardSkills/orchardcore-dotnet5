using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Liquid;
using OrchardCore.Liquid;
using OrchardCore.Templates.Models;

namespace OrchardCore.Templates.Services
{
    public class TemplatesShapeBindingResolver : IShapeBindingResolver
    {
        private TemplatesDocument _templatesDocument;
        private readonly TemplatesManager _templatesManager;
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly PreviewTemplatesProvider _previewTemplatesProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HtmlEncoder _htmlEncoder;

        public TemplatesShapeBindingResolver(
            TemplatesManager templatesManager,
            ILiquidTemplateManager liquidTemplateManager,
            PreviewTemplatesProvider previewTemplatesProvider,
            IHttpContextAccessor httpContextAccessor,
            HtmlEncoder htmlEncoder)
        {
            _templatesManager = templatesManager;
            _liquidTemplateManager = liquidTemplateManager;
            _previewTemplatesProvider = previewTemplatesProvider;
            _httpContextAccessor = httpContextAccessor;
            _htmlEncoder = htmlEncoder;
        }

        public async Task<ShapeBinding> GetShapeBindingAsync(string shapeType)
        {
            if (AdminAttribute.IsApplied(_httpContextAccessor.HttpContext))
            {
                return null;
            }

            var localTemplates = _previewTemplatesProvider.GetTemplates();

            if (localTemplates != null)
            {
                if (localTemplates.Templates.TryGetValue(shapeType, out var localTemplate))
                {
                    return BuildShapeBinding(shapeType, localTemplate);
                }
            }

            if (_templatesDocument == null)
            {
                _templatesDocument = await _templatesManager.GetTemplatesDocumentAsync();
            }

            if (_templatesDocument.Templates.TryGetValue(shapeType, out var template))
            {
                return BuildShapeBinding(shapeType, template);
            }
            else
            {
                return null;
            }
        }

        private ShapeBinding BuildShapeBinding(string shapeType, Template template)
        {
            return new ShapeBinding()
            {
                BindingName = shapeType,
                BindingSource = shapeType,
                BindingAsync = async displayContext =>
                {
                    var content = new ViewBufferTextWriterContent();
                    await _liquidTemplateManager.RenderAsync(template.Content, content, _htmlEncoder, displayContext.Value);
                    return content;
                }
            };
        }
    }
}
