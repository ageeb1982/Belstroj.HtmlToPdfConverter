using iTextSharp;
using iTextSharp.text.pdf;
using iTextSharp.text.html;
using System.IO;
using iTextSharp.text;
using iTextSharp.tool.xml;
using iTextSharp.tool.xml.css;
using iTextSharp.tool.xml.html;
using iTextSharp.tool.xml.parser;
using iTextSharp.tool.xml.pipeline.css;
using iTextSharp.tool.xml.pipeline.end;
using iTextSharp.tool.xml.pipeline.html;
using Microsoft.SharePoint.Client;

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
                        var xmlWorkerHelper = XMLWorkerHelper.GetInstance();
                        var cssResolver = new StyleAttrCSSResolver();
                        var xmlWorkerFontProvider = new XMLWorkerFontProvider();
                        //foreach (string font in fonts)
                        //{
                        //    xmlWorkerFontProvider.Register(font);
                        //}
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
    }
}
