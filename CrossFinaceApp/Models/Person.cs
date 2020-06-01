using System.Collections.Generic;

namespace CrossFinaceApp.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string Surname { get; set; }
        public string NationalIdentificationNumber { get; set; }
        public int? AddressId { get; set; }
        public virtual Address Address { get; set; }
        public string PhoneNumber { get; set; }
        public string PhoneNumber2 { get; set; }
        public virtual ICollection<Agreement> Agreements { get; } = new List<Agreement>();
    }
}
