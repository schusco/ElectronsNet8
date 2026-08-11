using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Electrons.Net8.Helpers
{
    [HtmlTargetElement("carousel")]
    public class CarouselTagHelper : TagHelper
    {
        [ViewContext, HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            var env = ViewContext.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var settings = ViewContext.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<GameSettings>>().Value;
            var urlHelper = ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(ViewContext);
            var container = new TagBuilder("div");
            container.AddCssClass("carousel slide align-content-center");
            container.Attributes.Add("id", "carousel-example-generic");
            container.Attributes.Add("data-bs-ride", "carousel");

            var innerDiv = new TagBuilder("div");
            innerDiv.AddCssClass("carousel-inner");
            container.InnerHtml.AppendHtml(innerDiv);
            var pathArray = new List<string>
            {
                env.WebRootPath
            };
            pathArray.AddRange(settings.CarouselImagesVirtualPath.Split('/'));
            string physicalPath = Path.Combine(pathArray.ToArray());
            var files = Directory.EnumerateFiles(physicalPath);
            int counter = 1;
            foreach (var file in files)
            {
                var imageFile = Path.GetFileName(file);
                var webPath = urlHelper.Content($"~{settings.CarouselImagesVirtualPath}/{imageFile}");
                var imgDiv = new TagBuilder("div");
                imgDiv.AddCssClass("carousel-item");
                if (file == files.First())
                    imgDiv.AddCssClass("active");
                var imgTag = new TagBuilder("img");
                imgTag.AddCssClass("tronCarouselImage d-block w-100");
                imgTag.Attributes.Add("src", webPath);
                imgTag.Attributes.Add("alt", $"slide-{counter}");
                imgDiv.InnerHtml.AppendHtml(imgTag);
                innerDiv.InnerHtml.AppendHtml(imgDiv);
                counter++;
            }
            var slidePrev = new TagBuilder("a");
            slidePrev.AddCssClass("carousel-control-prev");
            slidePrev.Attributes.Add("href", "#carousel-example-generic");
            slidePrev.Attributes.Add("role", "button");
            slidePrev.Attributes.Add("data-bs-slide", "prev");

            container.InnerHtml.AppendHtml(slidePrev);

            var slidePrevIcon = new TagBuilder("span");
            slidePrevIcon.AddCssClass("carousel-control-prev-icon");
            slidePrev.InnerHtml.AppendHtml(slidePrevIcon);

            var slideNext = new TagBuilder("a");
            slideNext.AddCssClass("carousel-control-next");
            slideNext.Attributes.Add("href", "#carousel-example-generic");
            slideNext.Attributes.Add("role", "button");
            slideNext.Attributes.Add("data-bs-slide", "next");

            var slideNextIcon = new TagBuilder("span");
            slideNextIcon.AddCssClass("carousel-control-next-icon");
            slideNext.InnerHtml.AppendHtml(slideNextIcon);

            container.InnerHtml.AppendHtml(slideNext);

            output.Content.SetHtmlContent(container);
        }
    }
}
