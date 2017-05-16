using System;
using System.Collections.Generic;
using iTextSharp.text.pdf;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.tool.xml;
using iTextSharp.tool.xml.css;
using iTextSharp.tool.xml.html;
using iTextSharp.tool.xml.parser;
using iTextSharp.tool.xml.pipeline.css;
using iTextSharp.tool.xml.pipeline.end;
using iTextSharp.tool.xml.pipeline.html;
using Microsoft.SharePoint.Client;
using System.Diagnostics;
using System.Text;

namespace Belstroj.HtmlToPdfConverterWeb
{
    public class PdfConverter
    {
        public static byte[] ConvertHtmltoPdf(ClientContext context, Microsoft.SharePoint.Client.List documentLibrary, string result, string nameOfTheFile)
        {
            using (var msOutput = new MemoryStream())
            {
                using (var stringReader = new StringReader(result))
                {
                    using (Document document = new Document())
                    {
                        var pdfWriter = PdfWriter.GetInstance(document, msOutput);
                        pdfWriter.InitialLeading = 12.5f;
                        document.Open();
                        var cssResolver = new StyleAttrCSSResolver();
                        var xmlWorkerFontProvider = new XMLWorkerFontProvider();
                        var cssAppliers = new CssAppliersImpl(xmlWorkerFontProvider);
                        var htmlContext = new HtmlPipelineContext(cssAppliers);
                        htmlContext.SetTagFactory(Tags.GetHtmlTagProcessorFactory());
                        PdfWriterPipeline pdfWriterPipeline = new PdfWriterPipeline(document, pdfWriter);
                        HtmlPipeline htmlPipeline = new HtmlPipeline(htmlContext, pdfWriterPipeline);
                        CssResolverPipeline cssResolverPipeline = new CssResolverPipeline(cssResolver, htmlPipeline);
                        XMLWorker xmlWorker = new XMLWorker(cssResolverPipeline, true);
                        XMLParser xmlParser = new XMLParser(xmlWorker);
                        xmlParser.Parse(stringReader);
                    }
                }
                return msOutput.ToArray();
            }
        }

        public static string CleanHtmlCodeForConversion(string htmlCode)
        {
            htmlCode = RemoveBetween(htmlCode, "<!--", "-->");
            htmlCode = RemoveBetween(htmlCode, "<script", "</script>");
            HtmlDocument doc = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true,
                OptionWriteEmptyNodes = true,
                OptionDefaultStreamEncoding = Encoding.UTF8
            };

            doc.LoadHtml(htmlCode);
            var errors = doc.ParseErrors;
            foreach (var error in errors)
            {
                switch (error.Code)
                {
                        case HtmlParseErrorCode.EndTagNotRequired:
                        //Trace.TraceInformation(error.Reason);
                        continue;
                        default:
                        Trace.TraceInformation(error.Reason);
                        continue;
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        private static string RemoveBetween(string s, string begin, string end)
        {
            Regex regex = new Regex($"\\{begin}.*?\\{end}");
            return regex.Replace(s, string.Empty);
        }

        public static List<string> GetExternalCss(string htmlCode)
        {
            var x = htmlCode.Split(new[] { "<link href=" }, StringSplitOptions.None);
            return (from y in x select y.Split(new [] {" rel=\"Stylesheet\" type=\"text/css\" />"}, StringSplitOptions.None)[0] into k where k.Length > 0 && k.Contains(".css") select k.Replace("\"", "")).ToList();
        }
    }
}
