﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using Microsoft.SharePoint.Client.Publishing;
using Microsoft.SharePoint.Client.Taxonomy;
using Microsoft.SharePoint.Client.Utilities;
using Microsoft.SharePoint.Client.WebParts;

namespace Belstroj.HtmlToPdfConverterWeb.Services
{
    public class AppEventReceiver : IRemoteEventService
    {
        private const string ReceiverNameAdded = "ItemAddedPdfConverterBelstroj";
        private const string ReceiverNameUpdated = "ItemUpdatedPdfConverterBelstroj";
        //private const string ListName = "Pdf Converter List";
        //private const string DocumentListName = "Pdf Converted Document list";
        private const string ListName = "Pdf Converted Document list";

        /// <summary>
        /// Handles app events that occur after the app is installed or upgraded, or when app is being uninstalled.
        /// </summary>
        /// <param name="properties">Holds information about the app event.</param>
        /// <returns>Holds information returned from the app event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            SPRemoteEventResult result = new SPRemoteEventResult();

            switch (properties.EventType)
            {
                case SPRemoteEventType.AppInstalled:
                    HandleAppInstalled(properties);
                    break;
                case SPRemoteEventType.AppUninstalling:
                    HandleAppUninstalling(properties);
                    break;
                case SPRemoteEventType.ItemAdded:
                    HandleItemAdded(properties);
                    break;
                case SPRemoteEventType.ItemUpdating:
                    HandleItemUpdating(properties);
                    break;
            }


            return result;
        }

        private void HandleItemAdded(SPRemoteEventProperties properties)
        {
            Trace.TraceInformation("New pdf object item added, new pdf conversion started..");
            using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            {
                if (clientContext != null)
                {
                    var pdfConverterList = clientContext.Web.Lists.GetById(properties.ItemEventProperties.ListId);
                    var pdfConversionItem = pdfConverterList.GetItemById(properties.ItemEventProperties.ListItemId);
                    HandleItem(clientContext, pdfConverterList, pdfConversionItem);
                }
            }
        }

        private void HandleItem(ClientContext clientContext, List pdfConverterList, ListItem pdfConversionItem)
        {
           
            clientContext.Load(pdfConversionItem, item => item.File);
            clientContext.ExecuteQuery();
            var isTxtFile = pdfConversionItem.File.Name.EndsWith(".txt");
            if (!isTxtFile)
            {
                return;
            }
            
            var stream = pdfConversionItem.File.OpenBinaryStream();
            clientContext.ExecuteQuery();
            string htmlCode;
            using (StreamReader reader = new StreamReader(stream.Value))
            {
                htmlCode = reader.ReadToEnd();
            }
            if (string.IsNullOrEmpty(htmlCode))
            {
                return;
            }
            var fileName = GenerateUniqueName(clientContext, pdfConverterList);
            htmlCode = PdfConverter.CleanHtmlCodeForConversion(htmlCode);
            var fileBytes = PdfConverter.ConvertHtmltoPdf(clientContext, pdfConverterList, htmlCode, fileName);
            FileCreationInformation newFile = new FileCreationInformation
            {
                Content = fileBytes,
                Url = fileName
            };
            var uploadFile = pdfConverterList.RootFolder.Files.Add(newFile);
            clientContext.ExecuteQuery();

        }

        private string GenerateUniqueName(ClientContext context, List pdfDocumentList)
        {
            var q = new CamlQuery()
            {
                ViewXml = "<View><Query><OrderBy><FieldRef Name='ID' Ascending='False' /></OrderBy></Query><ViewFields><FieldRef Name='ID' /></ViewFields><RowLimit>1</RowLimit></View>",
            };
            var r = pdfDocumentList.GetItems(q);
            context.Load(r);
            context.ExecuteQuery();
            var highestId = 0;
            if (r.Count <= 0)
            {
                highestId = 1;
            }
            else
            {
                var highestIdString = r[0]["ID"]?.ToString();

                int.TryParse(highestIdString, out highestId);
                highestId = highestId + 1;
            }

            var fileName = "GeneratedPdf - " + highestId + ".pdf";
            return fileName;
        }

        private void HandleItemUpdating(SPRemoteEventProperties properties)
        {
            Trace.TraceInformation("New pdf object item updating, new pdf conversion started..");
            using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            {
                if (clientContext != null)
                {
                    var pdfConverterList = clientContext.Web.Lists.GetById(properties.ItemEventProperties.ListId);
                    var pdfConversionItem = pdfConverterList.GetItemById(properties.ItemEventProperties.ListItemId);
                    HandleItem(clientContext, pdfConverterList, pdfConversionItem);
                }
            }
        }

        private void HandleAppUninstalling(SPRemoteEventProperties properties)
        {
            using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, false))
            {
                if (clientContext != null)
                {
                    List projectList = clientContext.Web.Lists.GetByTitle(ListName);
                    clientContext.Load(projectList, p => p.EventReceivers);
                    clientContext.ExecuteQuery();
                    var receiver = projectList.EventReceivers.FirstOrDefault(e => e.ReceiverName == ReceiverNameAdded);
                    if (receiver == null) return;
                    DeleteReceiver(clientContext, receiver);
                }
            }
        }

        private void HandleAppInstalled(SPRemoteEventProperties properties)
        {
            using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, false))
            {
                if (clientContext != null)
                {
                    List pdfList = clientContext.Web.Lists.GetByTitle(ListName);
                    clientContext.Load(pdfList, p => p.EventReceivers);
                    clientContext.ExecuteQuery();
                    foreach (var receiver in pdfList.EventReceivers.Where(receiver => receiver.ReceiverName == ReceiverNameAdded))
                    {
                        Trace.WriteLine("Found existing ItemAdded receiver at " + receiver.ReceiverUrl);
                        DeleteReceiver(clientContext, receiver);
                        break;
                    }
                    foreach (var receiver in pdfList.EventReceivers.Where(receiver => receiver.ReceiverName == ReceiverNameUpdated))
                    {
                        Trace.WriteLine("Found existing ItemAdded receiver at " + receiver.ReceiverUrl);
                        DeleteReceiver(clientContext, receiver);
                        break;
                    }

                    Message msg = OperationContext.Current.RequestContext.RequestMessage;
                    EventReceiverDefinitionCreationInformation receiverAdded =
                        new EventReceiverDefinitionCreationInformation
                        {
                            EventType = EventReceiverType.ItemAdded,
                            ReceiverUrl = msg.Headers.To.ToString(),
                            ReceiverName = ReceiverNameAdded,
                            Synchronization = EventReceiverSynchronization.Synchronous
                        };
                    pdfList.EventReceivers.Add(receiverAdded);
                    clientContext.ExecuteQuery();
                    Trace.WriteLine("Added ItemAdded receiver at " + msg.Headers.To);

                    EventReceiverDefinitionCreationInformation receiverUpdating =
                        new EventReceiverDefinitionCreationInformation
                        {
                            EventType = EventReceiverType.ItemUpdating,
                            ReceiverUrl = msg.Headers.To.ToString(),
                            ReceiverName = ReceiverNameUpdated,
                            Synchronization = EventReceiverSynchronization.Synchronous
                        };
                    pdfList.EventReceivers.Add(receiverUpdating);
                    clientContext.ExecuteQuery();
                    Trace.WriteLine("Added ItemUpdating receiver at " + msg.Headers.To);
                }
            }
        }

        private void DeleteReceiver(ClientContext clientContext, EventReceiverDefinition receiver)
        {
            try
            {
                Trace.WriteLine("Removing ItemAdded receiver at " + receiver.ReceiverUrl);
                receiver.DeleteObject();
                clientContext.ExecuteQuery();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Trace.TraceInformation("DeleteReceiver failed");
            }
        }


        /// <summary>
        /// This method is a required placeholder, but is not used by app events.
        /// </summary>
        /// <param name="properties">Unused.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            throw new NotImplementedException();
        }

    }
}
