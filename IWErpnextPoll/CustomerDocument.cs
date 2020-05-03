using System.Collections.Generic;

namespace IWErpnextPoll
{
    public class CustomerDocument
    {
        public string name;
        public string owner;
        public string customer_name;
        public string customer_type;
        public string customer_email;
        public string company_website;
        public int disabled;
        public string telephone_1;
        public string telephone_2;
        public string fax_number;
        public string sales_rep;
        public string ship_via;
        public string old_customer_id;
        public string website;
        public string email_id;
        public string payment_terms;
        public string doctype;
        public List<ContactDocument> contacts;
        public List<AddressDocument> addresses;
    }
}