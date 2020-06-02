using CrossFinaceApp.Models;
using System;

namespace ExcelDataReader
{
    public static class ExcelReaderExtension
    {
        public static Address MapAddress(this IExcelDataReader reader)
        {
            return new Address
            {
                StreetName = reader.GetValue(5) as string,
                StreetNumber = reader.GetValue(6) as string,
                FlatNumber = reader.GetValue(7) as string,
                PostCode = reader.GetValue(8) as string,
                PostOfficeCity = reader.GetValue(9) as string,
                CorrespondenceStreetName = reader.GetValue(10) as string,
                CorrespondenceStreetnumber = reader.GetValue(11) as string,
                CorrespondenceFlatNumber = reader.GetValue(12) as string,
                CorrespondencePostCode = reader.GetValue(13) as string,
                CorrespondencePostOfficeCity = reader.GetValue(14) as string
            };
        }

        public static Person MapPerson(this IExcelDataReader reader, Address address)
        {
            return new Person
            {
                FirstName = reader.GetValue(2) as string,
                Surname = reader.GetValue(3) as string,
                Address = address,
                NationalIdentificationNumber = reader.GetValue(4).ToString(),
                PhoneNumber = reader.GetValue(15).ToString(),
                PhoneNumber2 = reader.GetValue(16).ToString()
            };
        }

        public static FinancialState MapFinancialState(this IExcelDataReader reader)
        {
            return new FinancialState
            {
                OutstandingLiabilites = Convert.ToDecimal(reader.GetValue(17)),
                Interests = Convert.ToDecimal(reader.GetValue(18)),
                PenaltyInterests = Convert.ToDecimal(reader.GetValue(19)),
                Fees = Convert.ToDecimal(reader.GetValue(20)),
                CourtFees = Convert.ToDecimal(reader.GetValue(21)),
                RepresentationCourtFees = Convert.ToDecimal(reader.GetValue(22)),
                VindicationCosts = Convert.ToDecimal(reader.GetValue(23)),
                RepresentationVindicationCosts = Convert.ToDecimal(reader.GetValue(24))
            };
        }

        public static Agreement MapAgreement(this IExcelDataReader reader, Person person, FinancialState financialState)
        {
            return new Agreement
            {
                Number = reader.GetValue(1).ToString(),
                Person = person,
                FinancialState = financialState
            };
        }
    }
}
