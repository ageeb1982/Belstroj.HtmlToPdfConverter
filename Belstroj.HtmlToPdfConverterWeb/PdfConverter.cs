using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.html;
using iTextSharp.text.pdf;
using iTextSharp.text.xml;
using iTextSharp.text.html.simpleparser;
using System.IO;
using System.util;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace Belstroj.HtmlToPdfConverterWeb
{
    public class PdfConverter
    {
        public static void ConvertHTMLToPDF(string HTMLCode, string rootWebUrl)
        {
            HttpContext context = HttpContext.Current;
            
            System.IO.StringWriter stringWrite = new StringWriter();
            System.Web.UI.HtmlTextWriter htmlWrite = new HtmlTextWriter(stringWrite);

            StringReader reader = new StringReader(HTMLCode);

            //Create PDF document
            Document doc = new Document(PageSize.A4);
            var parser = new HTMLWorker(doc);
            PdfWriter.GetInstance(doc, new FileStream("/App_Data/HTMLToPDF.pdf", FileMode.Create));
            doc.Open();

            var interfaceProps = new Dictionary<string, Object>();
            var ih = new ImageHander() { BaseUri = rootWebUrl };

            interfaceProps.Add(HTMLWorker.IMG_PROVIDER, ih);

            foreach (IElement element in HTMLWorker.ParseToList(
            new StringReader(HTMLCode), null))
            {
                doc.Add(element);
            }
            doc.Close();
            
        }

        //handle Image relative and absolute URL's
    }

    public class ImageHander : IImageProvider
    {
        public string BaseUri;
        public iTextSharp.text.Image GetImage(string src,
        IDictionary<string, string> h,
        ChainedProperties cprops,
        IDocListener doc)
        {
            string imgPath = string.Empty;

            if (src.ToLower().Contains("http://") == false)
            {
                imgPath = HttpContext.Current.Request.Url.Scheme + "://" +

                HttpContext.Current.Request.Url.Authority + src;
            }
            else
            {
                imgPath = src;
            }

            return iTextSharp.text.Image.GetInstance(imgPath);
        }
    }
}
}