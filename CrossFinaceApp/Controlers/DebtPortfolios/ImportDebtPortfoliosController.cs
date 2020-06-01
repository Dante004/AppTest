using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossFinaceApp.DataAccess;
using CrossFinaceApp.Helpers;
using CrossFinaceApp.Models;
using ExcelDataReader;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrossFinaceApp.Controlers.DebtPortfolios
{
    [Route("api/DebtPortfolios")]
    [ApiController]
    public class ImportDebtPortfoliosController : ControllerBase
    {
        private IMediator _mediator;

        public ImportDebtPortfoliosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> ImportData(IFormFile file)
        {
            var importResult = await _mediator.Send(new ImportDataFromFileCommand { File = file });

            if(!importResult.Success)
            {
                importResult.AddErrorToModelState(ModelState);
                return BadRequest(ModelState);
            }

            var sendResult = await _mediator.Send(new SendDataToService());

            if(!sendResult.Success)
            {
                sendResult.AddErrorToModelState(ModelState);
                return BadRequest(ModelState);
            }

            return Ok();
        }

        public class ImportDataFromFileCommand : IRequest<Result>
        {
            public IFormFile File { get; set; }
        }

        public class ImportDataFromFileCommandHandler : IRequestHandler<ImportDataFromFileCommand, Result>
        {
            private DataContext _dataContext;
            private IValidator<Person> _validator;

            public ImportDataFromFileCommandHandler(DataContext dataContext,
                IValidator<Person> validator)
            {
                _dataContext = dataContext;
                _validator = validator;
            }

            public async Task<Result> Handle(ImportDataFromFileCommand request, CancellationToken cancellationToken)
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using (var reader = ExcelReaderFactory.CreateReader(request.File.OpenReadStream()))
                { 
                    while(reader.Read())
                    {
                        var firstValue = reader.GetValue(0);
                        if (firstValue as string == "lp" || firstValue == null)
                        {
                            continue;
                        }

                        var address = new Address
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

                        await _dataContext.Addresses.AddAsync(address);

                        var person = new Person
                        {
                            FirstName = reader.GetValue(2) as string,
                            Surname = reader.GetValue(3) as string,
                            Address = address,
                            NationalIdentificationNumber = reader.GetValue(4).ToString() ?? "",
                            PhoneNumber = reader.GetValue(15).ToString() ?? "",
                            PhoneNumber2 = reader.GetValue(16).ToString() ?? ""
                        };

                        var validationResult = await _validator.ValidateAsync(person);

                        if(!validationResult.IsValid)
                        {
                            return Result.Error(validationResult.Errors);
                        }

                        await _dataContext.People.AddAsync(person);

                        var financialState = new FinancialState
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

                        await _dataContext.FinancialStates.AddAsync(financialState);

                        var agreement = new Agreement
                        {
                            Number = reader.GetValue(1).ToString() ?? "",
                            Person = person,
                            FinancialState = financialState
                        };

                        await _dataContext.Agreements.AddAsync(agreement);
                    }

                    await _dataContext.SaveChangesAsync();
                }

                return Result.Ok();
            }
        }

        public class PersonValidator : AbstractValidator<Person>
        {
            public PersonValidator()
            {
                RuleFor(p => p.NationalIdentificationNumber)
                    .Cascade(CascadeMode.StopOnFirstFailure)
                    .Must(Have11Character)
                    .WithMessage("Pesel must have 11 character")
                    .Must(HaveCorrectControlSum)
                    .WithMessage("Pesel must have correct sum control");
            }

            public static bool Have11Character(string pesel)
            {
                return pesel.Length == 11;
            }

            public static bool HaveCorrectControlSum(string pesel)
            {
                var multipliers = new int[] { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };

                var sum = 0;
                for (int i = 0; i < multipliers.Length; i++)
                {
                    sum += multipliers[i] * int.Parse(pesel[i].ToString());
                }

                var remainder = sum % 10;
                var value = remainder == 0 ? remainder : (10 - remainder);

                return value == int.Parse(pesel[10].ToString());
            }
        }

        public class SendDataToService : IRequest<Result>
        {

        }

        public class SendDataToServiceHandler : IRequestHandler<SendDataToService, Result>
        {
            private DataContext _dataContext;

            public SendDataToServiceHandler(DataContext dataContext)
            {
                _dataContext = dataContext;
            }

            public async Task<Result> Handle(SendDataToService request, CancellationToken cancellationToken)
            {
                var importServiceClient = new ImportService.ImportServiceClient();

                foreach (var person in await _dataContext.People.ToListAsync())
                {
                    var address = new ImportService.Address
                    {
                        City = person.Address.PostOfficeCity,
                        HouseNo = person.Address.StreetNumber,
                        LocaleNo = person.Address.FlatNumber,
                        PostalCode = person.Address.PostCode,
                        Street = person.Address.StreetName
                    };

                    var correspondenceAddress = new ImportService.Address
                    {
                        City = person.Address.CorrespondencePostOfficeCity,
                        HouseNo = person.Address.CorrespondenceStreetnumber,
                        LocaleNo = person.Address.CorrespondenceFlatNumber,
                        PostalCode = person.Address.CorrespondencePostCode,
                        Street = person.Address.CorrespondenceStreetName
                    };

                    var finacialState = person.Agreements.FirstOrDefault(x => x.PersonId == person.Id).FinancialState;

                    var importSeriveFinacialState = new ImportService.FinancialState
                    {
                        Capital = finacialState.OutstandingLiabilites,
                        CourtFees = finacialState.CourtFees,
                        CourtRepresentationFees = finacialState.RepresentationCourtFees,
                        Fees = finacialState.Fees,
                        Interest = finacialState.Interests,
                        PenaltyInterest = finacialState.PenaltyInterests
                    };

                    var agreementsNumber = person.Agreements.FirstOrDefault(x => x.PersonId == person.Id).Number;

                    var importServicePerson = new ImportService.Person
                    {
                        Name = person.FirstName,
                        Surname = person.Surname,
                        NationalIdentificationNumber = person.NationalIdentificationNumber,
                        Addresses = new ImportService.Address[] { address, correspondenceAddress },
                        FinancialState = importSeriveFinacialState,
                        IdentityDocuments = new ImportService.IdentityDocument[] { new ImportService.IdentityDocument { Number = agreementsNumber } }
                    };

                    //await importServiceClient.DoImportAsync(importServicePerson);
                }

                return Result.Ok();
            }
        }
    }
}
