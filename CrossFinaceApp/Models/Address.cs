using System.Collections.Generic;

namespace CrossFinaceApp.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string StreetName { get; set; }
        public string StreetNumber { get; set; }
        public string FlatNumber { get; set; }
        public string PostCode { get; set; }
        public string PostOfficeCity { get; set; }
        public string CorrespondenceStreetName { get; set; }
        public string CorrespondenceStreetnumber { get; set; }
        public string CorrespondenceFlatNumber { get; set; }
        public string CorrespondencePostCode { get; set; }
        public string CorrespondencePostOfficeCity { get; set; }
        public virtual ICollection<Person> People { get; set; }
    }
}
