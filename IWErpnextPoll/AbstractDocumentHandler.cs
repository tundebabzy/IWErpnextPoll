using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;

namespace IWErpnextPoll
{
    abstract class AbstractDocumentHandler : IDocumentHandler
    {
        private IDocumentHandler _nextHandler;

        protected Company Company { get; set; }

        protected Dictionary<string, EntityReference<Customer>> CustomerReferences { get; set; }

        protected Dictionary<string, EntityReference> ItemReferences { get; set; }

        protected ILogger Logger { get; set; }

        protected Dictionary<string, EntityReference<Vendor>> VendorReferences { get; set; }

        protected EmployeeInformation EmployeeInformation { get; set; }


        protected AbstractDocumentHandler(Company c, ILogger logger, EmployeeInformation employeeInformation)
        {
            Company = c;
            Logger = logger;
            EmployeeInformation = employeeInformation;
            CustomerReferences = CustomerToReferenceDictionary();
            VendorReferences = VendorToReferenceDictionary();
            ItemReferences = InventoryItemToReferenceDictionary();
        }

        private Dictionary<string, EntityReference<Vendor>> VendorToReferenceDictionary()
        {
            var dictionary = new Dictionary<string, EntityReference<Vendor>>();
            var vendorList = Company.Factories.VendorFactory.List();
            vendorList.Load();
            foreach (var vendor in vendorList)
            {
                try
                {
                    dictionary.Add(vendor.Name, vendor.Key);
                    ;
                }
                catch (ArgumentException e)
                {
                    Logger.Debug(e.Message);
                    Logger.Debug("Key was -> {0}, value was {1}", vendor.Name, vendor.Key);
                    Logger.Debug("Moving on.");
                }
            }

            return dictionary;
        }

        private Dictionary<string, EntityReference> InventoryItemToReferenceDictionary()
        {
            var keyValuePairs = new Dictionary<string, EntityReference>();
            var inventoryItems = Company.Factories.InventoryItemFactory.List();
            inventoryItems.Load();
            foreach (var item in inventoryItems)
            {
                try
                {
                    keyValuePairs.Add(item.ID, item.Key);
                    ;
                }
                catch (ArgumentException e)
                {
                    Logger.Debug(e.Message);
                    Logger.Debug("Key was -> {0}, value was {1}", item.ID, item.Key);
                    Logger.Debug("Moving on.");
                }
            }
            return keyValuePairs;
        }

        private Dictionary<string, EntityReference<Customer>> CustomerToReferenceDictionary()
        {
            var keyValuePairs = new Dictionary<string, EntityReference<Customer>>();
            var customers = Company.Factories.CustomerFactory.List();
            customers.Load();
            foreach (var customer in customers)
            {
                try
                {
                    keyValuePairs.Add(customer.Name, customer.Key);
                }
                catch (ArgumentException e)
                {
                    // already in queue so pass
                    Logger.Debug(e.Message);
                    Logger.Debug("Key was -> {0}, value was {1}", customer.ID, customer.Key);
                    Logger.Debug("Moving on.");
                }
            }
            return keyValuePairs;
        }

        public IDocumentHandler SetNext(IDocumentHandler handler)
        {
            this._nextHandler = handler;
            return handler;
        }

        protected IDocumentHandler GetNext()
        {
            return _nextHandler;
        }

        public virtual object Handle(object request)
        {
            return _nextHandler?.Handle(request);
        }
    }
}
