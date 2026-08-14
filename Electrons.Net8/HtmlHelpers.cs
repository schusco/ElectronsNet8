using Electrons.Core.Net8;
using Electrons.Net8;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Electrons.Net8
{
    /// <summary>
    /// Static class which contains .Net Mvc Urlhelper extension methods.
    /// </summary>
    public static class HtmlHelpers
    {
        /// <summary>
        /// Adds the specified javacript file to the web page which includes an additional content element so the file is not cached.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="contentPath">The path the javascript file is located at.</param>
        /// <returns></returns>
        public static IHtmlContent Script(this IUrlHelper helper, string contentPath)
        {
            return new HtmlString(string.Format("<script type='text/javascript' src='{0}'></script>", helper.LatestContent(contentPath)));
        }

        private static string LatestContent(this IUrlHelper helper, string contentPath)
        {
            string file = Path.Combine("~", contentPath);
            if (File.Exists(file))
            {
                var dateTime = File.GetLastWriteTime(file);
                contentPath = string.Format("{0}?v={1}", contentPath, dateTime.Ticks);
            }

            return helper.Content(contentPath);
        }

        /// <summary>
        /// Adds the specified css file to the web page including an additional content element so the file is not cached.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="contentPath">The path where the css file is located.</param>
        /// <returns></returns>
        public static IHtmlContent Css(this IUrlHelper helper, string contentPath)
        {
            return helper.Css(contentPath, string.Empty);
        }

        /// <summary>
        /// Adds the specified css file to the web page including an additional content element so the file is not cached.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="contentPath">The path where the css file is located.</param>
        /// <param name="media">An optional media attribute to add to the declaration.</param>
        /// <returns></returns>
        public static IHtmlContent Css(this IUrlHelper helper, string contentPath, string media)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<link rel='stylesheet' type='text/css' href='{0}' ", helper.LatestContent(contentPath));
            if (!string.IsNullOrEmpty(media))
                sb.Append(media);
            sb.Append(" />");
            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Generates an html table based on the type of class specified in the type argumenent containing the data provided.
        /// </summary>
        /// <typeparam name="T">Type argument for the table, generates one column for every property marked with the TableColumnAttribute.</typeparam>
        /// <param name="helper"></param>
        /// <param name="data">The data to include in the body of the table.</param>
        /// <returns></returns>
        public static IHtmlContent Table<T>(this IHtmlHelper helper, IList<T> data) where T : class
        {
            return helper.Table(data, string.Empty, string.Empty, null);
        }

        /// <summary>
        /// Generates an html table based on the type of class specified in the type argumenent containing the data provided.
        /// </summary>
        /// <typeparam name="T">Type argument for the table, generates one column for every property marked with the TableColumnAttribute.</typeparam>
        /// <param name="helper"></param>
        /// <param name="data">The data to include in the body of the table.</param>
        /// <param name="htmlAttributes">optional anonymous object containing additional html attributes</param>
        /// <returns></returns>
        public static IHtmlContent Table<T>(this IHtmlHelper helper, IList<T> data, object htmlAttributes, bool includeFooter = false) where T : class
        {
            return helper.Table(data, string.Empty, string.Empty, htmlAttributes, includeFooter);
        }

        /// <summary>
        /// Generates an html table based on the type of class containing the data provided.
        /// </summary>
        /// <typeparam name="T">Type argument for the table, generates one column for every property marked with the TableColumnAttribute.</typeparam>
        /// <param name="helper"></param>
        /// <param name="data">The data to include in the body of the table.</param>
        /// <param name="dataCss">Optional css class to apply to the header row.</param>
        /// <param name="headerCss">Optional css class to apply to the table body.</param>
        /// <param name="htmlAttributes">optional anonymous object containing additional html attributes</param>
        /// <returns></returns>
        public static IHtmlContent Table<T>(this IHtmlHelper helper, IList<T> data, string headerCss, string dataCss,
                                             object htmlAttributes, bool includeFooter = false) where T : class
        {
            return helper.Table(data, headerCss, dataCss, headerCss, htmlAttributes, includeFooter);
        }

        /// <summary>
        /// Generates an html table based on the type of class containing the data provided.
        /// </summary>
        /// <typeparam name="T">Type argument for the table, generates one column for every property marked with the TableColumnAttribute.</typeparam>
        /// <param name="helper"></param>
        /// <param name="data">The data to include in the body of the table.</param>
        /// <param name="dataCss">Optional css class to apply to the header row.</param>
        /// <param name="headerCss">Optional css class to apply to the table body.</param>
        /// <param name="alternateCss">Optional css class to apply to alternating table rows.</param>
        /// <param name="htmlAttributes">optional anonymous object containing additional html attributes</param>
        /// <returns></returns>
        public static IHtmlContent Table<T>(this IHtmlHelper helper, IList<T> data, string headerCss, string dataCss, string alternateCss, object htmlAttributes, bool includeFooter = false)
        {
            var urlHelper = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(helper.ViewContext);
            var html = new StringBuilder();
            if (htmlAttributes != null)
            {
                var attributes = new StringBuilder();
                var atype = htmlAttributes.GetType();
                var aprops = atype.GetProperties();
                foreach (var aprop in aprops)
                {
                    var propval = aprop.GetValue(htmlAttributes, null);
                    attributes.AppendFormat("{0}=\"{1}\" ", aprop.Name, propval);
                }
                html.AppendFormat("<table {0} ><thead><tr>", attributes);
            }
            else
                html.Append("<table><thead><tr>");

            var type = typeof(T);
            var props = type.GetProperties()
                            .Where(w => w.GetCustomAttributes(false).Any(a => a is TableColumnAttribute))
                            .OrderBy(o => o.GetSortOrder());

            foreach (var prop in props.Where(prop => prop.IncludeColumn(data)))
            {
                string innertext;
                if (prop.GetHeaderText() != null)
                    innertext = prop.GetHeaderText();
                else if (prop.GetHeaderProperty() != null)
                {
                    var valueprop = type.GetProperties().SingleOrDefault(s => s.Name == prop.GetHeaderProperty()) ?? 
                        throw new InvalidOperationException(string.Format("Property '{0}' does not exist in type '{1}'", prop.GetHeaderProperty(), type.Name));
                    var statData = data.FirstOrDefault();

                    innertext = statData is null ? "" : valueprop.GetValue(statData, null).ToString();
                }
                else
                    innertext = prop.Name;

                var classattr = string.IsNullOrEmpty(headerCss) && string.IsNullOrEmpty(prop.GetClass())
                                       ? ""
                                       : string.Format("class=\"{0} {1}\" ", headerCss, prop.GetClass());
                html.AppendFormat("<th {0} >{1}</th>", classattr, innertext);
            }
            html.Append("</tr></thead><tbody>");
            bool alternate = false;
            foreach (var result in data)
            {
                html.Append("<tr>");
                foreach (var prop in props.Where(prop => prop.IncludeColumn(data)))
                {

                    string currentCss = alternate ? dataCss : alternateCss;
                    string innertext;
                    var cellvalue = prop.GetValue(result, null);
                    if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                        innertext = cellvalue as DateTime? != null ? (cellvalue as DateTime?).Value.ToShortDateString() : null;
                    else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                    {
                        var format = prop.GetFormatString();
                        if (!string.IsNullOrEmpty(format))
                            innertext = cellvalue as decimal? != null ? (cellvalue as decimal?).Value.ToString(format) : null;
                        else
                            innertext = cellvalue as decimal? != null ? (cellvalue as decimal?).Value.ToString() : null;
                    }
                    else
                        innertext = cellvalue?.ToString().Replace("\n", " ").Replace("\r", "");
                    if (prop.GetCustomAttributes(false).Any(s => s is LinkColumnAttribute))
                    {
                        var attribute = (LinkColumnAttribute)prop.GetCustomAttributes(false).First(s => s is LinkColumnAttribute);
                        var url = GetFullUrl(prop, result, urlHelper);
                        innertext = string.Format("<a href=\"{0}\">{1}</a>", url, innertext);

                    }
                    var classattr = string.IsNullOrEmpty(dataCss) && string.IsNullOrEmpty(prop.GetClass()) ? "" : string.Format("class=\"{0} {1}\" ", currentCss, prop.GetClass());
                    html.AppendFormat("<td {0} >{1}</td>", classattr, innertext);

                }
                html.Append("</tr>");
                alternate = !alternate;
            }
            if (includeFooter)
            {
                html.Append("<tfoot><tr>");
                foreach (var prop in props.Where(prop => prop.IncludeColumn(data)))
                {
                    string innertext = "";
                    var footerProp = GetFooterProperty(prop);
                    if (footerProp != null)
                    {
                        var valueprop = type.GetProperties().SingleOrDefault(s => s.Name == footerProp) ?? 
                            throw new InvalidOperationException(string.Format("Property '{0}' does not exist in type '{1}'", GetFooterProperty(prop), type.Name));
                        object cellvalue = 0;
                        if (data.Any())
                            cellvalue = valueprop.GetValue(data.First(), null);
                        if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                        {
                            var format = prop.GetFormatString();
                            if (!string.IsNullOrEmpty(format))
                                innertext = cellvalue as decimal? != null ? (cellvalue as decimal?).Value.ToString(format) : null;
                            else
                                innertext = cellvalue as decimal? != null ? (cellvalue as decimal?).Value.ToString() : null;
                        }
                        else
                            innertext = cellvalue?.ToString().Replace("\n", " ").Replace("\r", "");
                    }
                    var classattr = string.IsNullOrEmpty(headerCss) && string.IsNullOrEmpty(prop.GetClass())
                                           ? ""
                                           : string.Format("class=\"{0} {1}\" ", headerCss, prop.GetClass());
                    html.AppendFormat("<th {0} >{1}</th>", classattr, innertext);
                }

                html.Append("</tr></tfoot>");
            }
            html.Append("</table>");
            return new HtmlString(html.ToString());
        }

        /// <summary>
        /// Outputs a html table containing the provided data.
        /// </summary>
        /// <typeparam name="T1">Type argument representing the type of the view model.</typeparam>
        /// <typeparam name="T2">Type argument representing the return type of the expression</typeparam>
        /// <param name="helper">html helper</param>
        /// <param name="expression">The expression which returns the data to include in the table.</param>
        /// <param name="headerCss">Css class to apply to the header row.</param>
        /// <param name="dataCss">Css class to apply to the data rows.</param>
        /// <param name="alternateCss">Css class to apply to alternating data rows.</param>
        /// <param name="htmlAttributes">Additional html attributes to include in the table tag.</param>
        /// <returns></returns>
        public static IHtmlContent Table<T1, T2>(this IHtmlHelper<T1> helper, Expression<Func<T1, T2>> expression, string headerCss, string dataCss, string alternateCss, object htmlAttributes) where T2 : IEnumerable
        {
            var urlHelper = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(helper.ViewContext);
            var provider = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<ModelExpressionProvider>();
            var modelExpression = provider.CreateModelExpression(helper.ViewData, expression);
            var metaData = modelExpression.Metadata;
            var data = (T2)modelExpression.Model;
            if (modelExpression.Model == null)
                return new HtmlString(string.Empty);

            var html = new StringBuilder();
            if (htmlAttributes != null)
            {
                var attributes = GetAttributeString(htmlAttributes);
                html.AppendFormat("<table {0} ><thead><tr>", attributes);
            }
            else
                html.Append("<table><thead><tr>");

            var type = metaData.ModelType.GetGenericArguments()[0];
            IEnumerable<PropertyInfo> props;
            if (!(type.IsPrimitive || type == typeof(string)))
                props = type.GetProperties()
                            .Where(
                                w =>
                                w.GetCustomAttributes(
                                    typeof(TableColumnAttribute), false).Any())
                            .OrderBy(o => o.GetSortOrder());
            else
                props = new List<PropertyInfo> { metaData.ContainerType.GetProperty(metaData.PropertyName) }
                    .OrderBy(o => o);

            foreach (var prop in props.Where(w => w.IncludeColumn<T2>(data)))
            {
                string innertext;
                if (prop.GetHeaderText() != null)
                    innertext = prop.GetHeaderText();
                else if (prop.GetHeaderProperty() != null)
                {
                    var valueprop = type.GetProperties().SingleOrDefault(s => s.Name == prop.GetHeaderProperty()) ??
                        throw new InvalidOperationException(string.Format("Property '{0}' does not exist in type '{1}'", prop.GetHeaderProperty(), type.Name));
                    var enumerator = data.GetEnumerator();
                    enumerator.MoveNext();
                    var test = enumerator.Current;
                    innertext = valueprop.GetValue(test, null).ToString();
                }
                else
                    innertext = prop.Name;

                var classattr = string.IsNullOrEmpty(headerCss) && string.IsNullOrEmpty(prop.GetClass())
                                    ? ""
                                    : string.Format("class=\"{0} {1}\" ", headerCss, prop.GetClass());
                html.AppendFormat("<th {0} >{1}</th>", classattr, innertext);
            }
            html.Append("</tr></thead><tbody>");
            bool alternate = true;
            foreach (var result in data)
            {
                var dataAttrs = type.GetProperties()
                                    .Where(w => w.GetCustomAttributes(typeof(DataAttributeAttribute), false).Any());

                var dataAttrString = MakeDataAttributeString(dataAttrs, result);

                html.AppendFormat("<tr {0} >", dataAttrString);
                foreach (var prop in props.Where(prop => prop.IncludeColumn<T2>(data)))
                {
                    string currentCss = alternate ? dataCss : alternateCss;
                    string innertext;
                    var s = result as string;
                    var cellvalue = s ?? prop.GetValue(result, null);
                    if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                        innertext = cellvalue as DateTime? != null ? (cellvalue as DateTime?).Value.ToShortDateString() : null;
                    else
                        innertext = cellvalue?.ToString().Replace("\n", " ").Replace("\r", "");
                    if (prop.GetCustomAttributes(typeof(LinkColumnAttribute), false).Any())
                    {
                        var attribute = prop.GetCustomAttributes(typeof(LinkColumnAttribute), false).SingleOrDefault() as LinkColumnAttribute;
                        var url = GetFullUrl(prop, result, urlHelper);
                        innertext = string.Format("<a href=\"{0}\">{1}</a>", url, innertext);

                    }
                    var classattr = string.IsNullOrEmpty(currentCss) && string.IsNullOrEmpty(prop.GetClass()) ? "" : string.Format("class=\"{0} {1}\" ", currentCss, prop.GetClass());
                    if (prop.GetCustomAttributes<TableColumnAttribute>(false).Any())
                    {
                        var imageTag = string.Empty;
                        var attribute = prop.GetCustomAttributes(typeof(TableColumnAttribute), false).SingleOrDefault() as TableColumnAttribute;
                        if (!string.IsNullOrEmpty(attribute.ImageFormat))
                        {
                            var imagePath = string.Format(attribute.ImageFormat, innertext.Replace(" ", "").ToLower());
                            imageTag = ToHtmlString(helper.Image(imagePath, "25", "25") as TagBuilder);
                        }
                        if (!string.IsNullOrEmpty(attribute.ColumnCss))
                        {
                            var tag = new TagBuilder("span");
                            tag.InnerHtml.AppendHtml(innertext);
                            tag.Attributes.Add("style", attribute.ColumnCss);
                            innertext = ToHtmlString(tag);
                        }
                        innertext = $"{imageTag} {innertext}".Trim();

                    }
                    html.AppendFormat("<td {0} >{1}</td>", classattr, innertext);

                }
                alternate = !alternate;
                html.Append("</tr>");
            }
            html.Append("</tbody></table>");
            return new HtmlString(html.ToString());
        }
        private static string ToHtmlString(TagBuilder tag)
        {
            using var writer = new StringWriter();
            // This is the manual "Render" step
            tag.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            return writer.ToString();
        }
        private static bool IncludeColumn<T>(this PropertyInfo prop, object data)
        {
            if (!prop.GetOptional()) return true;
            return ((IEnumerable<T>)data).Any(a => prop.GetValue(a, null) != null);
        }
        public static IHtmlContent Table<T1, T2>(this HtmlHelper<T1> helper, Expression<Func<T1, T2>> expression, string headerCss, string dataCss,
                                                  object htmlattributes) where T2 : IEnumerable
        {
            return helper.Table(expression, headerCss, dataCss, dataCss, htmlattributes);
        }

        public static IHtmlContent Table<T1, T2>(this HtmlHelper<T1> helper, Expression<Func<T1, T2>> expression,
                                                     object htmlattibutes) where T2 : IEnumerable
        {
            return helper.Table(expression, "", "", "", htmlattibutes);
        }

        public static IHtmlContent Image(this IHtmlHelper helper, string path, string height = null, string width = null)
        {
            var urlHelper = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(helper.ViewContext);
            var imageTag = new TagBuilder("img");
            imageTag.Attributes.Add("src", urlHelper.Content(path));
            imageTag.Attributes.Add("height", height);
            if (!string.IsNullOrEmpty(width))
                imageTag.Attributes.Add("width", width);
            imageTag.Attributes.Add("style", "color:white;text-align:center");
            return imageTag;
        }

        private static string MakeDataAttributeString<T>(IEnumerable<PropertyInfo> dataAttrs, T result)
        {
            var sb = new StringBuilder();
            foreach (var prop in dataAttrs)
                sb.AppendFormat(" data-{0}=\"{1}\" ", prop.GetParameter(), prop.GetValue(result, null));
            return sb.ToString();
        }

        private static StringBuilder GetAttributeString(object htmlAttributes)
        {
            var attributes = new StringBuilder();
            var atype = htmlAttributes.GetType();
            var aprops = atype.GetProperties();
            foreach (var aprop in aprops)
            {
                var propval = aprop.GetValue(htmlAttributes, null);
                attributes.AppendFormat("{0}=\"{1}\" ", aprop.Name, propval);
            }
            return attributes;
        }

        public static IHtmlContent Text<T1, T2>(this HtmlHelper<T1> helper, Expression<Func<T1, T2>> expression,
                                         object htmlattributes)
        {
            var attributes = GetAttributeString(htmlattributes);
            var sb = new StringBuilder();
            sb.AppendFormat("<span {0} >", attributes);
            var provider = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<ModelExpressionProvider>();
            var modelExpression = provider.CreateModelExpression(helper.ViewData, expression);
            var value = (T2)modelExpression.Model;
            sb.Append(value);
            sb.Append("</span>");
            return new HtmlString(sb.ToString());
        }
        private static string GetFullUrl<T>(PropertyInfo prop, T result, IUrlHelper urlHelper)
        {
            var type = result.GetType();
            var controller = prop.GetUrl();
            var startUrl = urlHelper.Content($"~/{controller}");
            var url = new StringBuilder(startUrl);
            if (prop.GetCustomAttributes(typeof(LinkParameterAttribute), false).Any())
            {
                var attributes = prop.GetCustomAttributes(typeof(LinkParameterAttribute), false).ToList();
                url.Append('?');
                foreach (LinkParameterAttribute attr in attributes.Cast<LinkParameterAttribute>())
                {
                    var propparam = type.GetProperties().Single(w => w.Name == attr.Field);
                    var propval = propparam.GetValue(result, null);
                    url.AppendFormat("{0}={1}", attr.Parameter, propval);
                    if (!Equals(attr, attributes.Last()))
                        url.Append('&');
                }
            }
            return url.ToString();
        }

        private static string GetHeaderText(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(typeof(TableColumnAttribute), false).SingleOrDefault();
            return attribute == null ? string.Empty : ((TableColumnAttribute)attribute).HeaderText;
        }

        private static string GetHeaderProperty(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(typeof(TableColumnAttribute), false).SingleOrDefault();
            return attribute == null ? string.Empty : ((TableColumnAttribute)attribute).HeaderProperty;
        }

        private static string GetFooterProperty(PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(typeof(TableColumnAttribute), false).SingleOrDefault();
            return attribute == null ? string.Empty : ((TableColumnAttribute)attribute).FooterProperty;
        }

        private static int GetSortOrder(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(typeof(TableColumnAttribute), false).SingleOrDefault();
            return attribute == null ? int.MaxValue : ((TableColumnAttribute)attribute).SortOrder;
        }

        private static string GetParameter(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(typeof(DataAttributeAttribute), false).SingleOrDefault();
            return attribute == null ? string.Empty : ((DataAttributeAttribute)attribute).Parameter ?? "id";
        }

        private static string GetClass(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(typeof(TableColumnAttribute), false).SingleOrDefault();
            return attribute == null ? string.Empty : ((TableColumnAttribute)attribute).Class;
        }

        private static string GetUrl(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(typeof(LinkColumnAttribute), false).SingleOrDefault();
            return attribute == null ? string.Empty : ((LinkColumnAttribute)attribute).NavUrl;
        }

        private static string GetFormatString(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(false).SingleOrDefault(s => s is TableColumnAttribute);
            return attribute == null ? string.Empty : ((TableColumnAttribute)attribute).Format;
        }

        private static string GetDisplayName(this PropertyInfo prop)
        {
            var attribute = prop.GetCustomAttributes(false).SingleOrDefault(s => s is TableColumnAttribute);
            return attribute == null ? string.Empty : ((TableColumnAttribute)attribute).HeaderText;
        }


    }
}
