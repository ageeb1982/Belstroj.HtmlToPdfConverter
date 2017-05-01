using System;
using System.Collections.Generic;
using System.Configuration;
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
using Microsoft.Azure; 
using Microsoft.WindowsAzure.Storage; 
using Microsoft.WindowsAzure.Storage.Blob; 

namespace Belstroj.HtmlToPdfConverterWeb
{
    public class PdfConverter
    {
        public static void ConvertHtmltoPdf(string htmlCode)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("pdfcontainer");
            container.CreateIfNotExists();
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            StringWriter stringWrite = new StringWriter();
            HtmlTextWriter htmlWrite = new HtmlTextWriter(stringWrite);

            StringReader reader = new StringReader(htmlCode);

            //Create PDF document
            Document doc = new Document(PageSize.A4);
            var parser = new HTMLWorker(doc);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference("testFile.pdf");
            Stream msPdf = new MemoryStream();
            // Create or overwrite the "myblob" blob with contents from a local file.
            PdfWriter.GetInstance(doc, msPdf);
            doc.Open();
            var interfaceProps = new Dictionary<string, Object>();
            var ih = new ImageHander();
            interfaceProps.Add(HTMLWorker.IMG_PROVIDER, ih);
            foreach (IElement element in HTMLWorker.ParseToList(
            new StringReader(htmlCode), null))
            {
                doc.Add(element);
            }
            blockBlob.UploadFromStream(msPdf);
            doc.Close();
        }
    }

    public class ImageHander : IImageProvider
    {
        public string BaseUri;
        public iTextSharp.text.Image GetImage(string src,
        IDictionary<string, string> h,
        ChainedProperties cprops,
        IDocListener doc)
        {
            var  imgPath = src;
            return iTextSharp.text.Image.GetInstance(imgPath);
        }
    }
}
