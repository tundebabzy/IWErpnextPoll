using Sage.Peachtree.API;
using Serilog;
using System;

namespace IWErpnextPoll
{
    internal class CreateCustomerHandler : AbstractDocumentHandler
    {
        public CreateCustomerHandler(Company company, ILogger logger) : base(company, logger) { }

        public override object Handle(object request)
        {
            Customer customer = CreateNewCustomer(request as CustomerDocument);
            this.SetNext(customer != null ? new LogSalesOrderHandler(Company, Logger) : null);
            return base.Handle(request);
        }

        private Customer CreateNewCustomer(CustomerDocument customerDocument)
        {
            Customer customer = Company.Factories.CustomerFactory.Create();
            customer.Email = customerDocument.customer_email;
            customer.Name = customerDocument.customer_name;
            customer.ShipVia = customerDocument.ship_via;
            customer.WebSiteURL = customerDocument.customer_email;
            customer.CustomerSince = DateTime.Now;
            customer.IsInactive = customerDocument.disabled == 1;
            AddContacts(customer, customerDocument);
            AddSalesRep(customer, customerDocument);
        }

        private void AddSalesRep(Customer customer, CustomerDocument customerDocument)
        {
            EmployeeList employees = Company.Factories.EmployeeFactory.List();
            employees.Load();
            foreach (var item in employees)
            {
                if (item.Name == customerDocument.sales_rep)
                {
                    customer.SalesRepresentativeReference = item.Key;
                    break;
                }
            }
        }

        private void AddContacts(Customer customer, CustomerDocument customerDocument)
        {
            foreach (var c in customerDocument.contacts)
            {
                Contact contact = customer.AddContact();
                contact.CompanyName = customerDocument.customer_name;
                contact.Email = c.EmailId;
                contact.FirstName = c.FirstName;
                contact.Gender = c.Gender == "Male" ? Gender.Male : c.Gender == "Female" ? Gender.Female : Gender.NotSpecified;
                contact.LastName = c.LastName;
                contact.Title = c.Salutation;
            }
        }
    }
}