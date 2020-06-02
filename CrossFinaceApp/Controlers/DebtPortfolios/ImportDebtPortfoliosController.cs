using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossFinaceApp.DataAccess;
using CrossFinaceApp.Helpers;
using CrossFinaceApp.Models;
using ExcelDataReader;
using FluentValidation;
using FluentValidation.Results;
using Mapster;
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
                if(request.File == null)
                {
                    return Result.Error("You have to send file to run this handler");
                }

                var validatorError = new List<ValidationFailure>();

                using (var reader = ExcelReaderFactory.CreateReader(request.File.OpenReadStream()))
                { 
                    while(reader.Read())
                    {
                        var firstValue = reader.GetValue(0);
                        if (firstValue as string == "lp" || firstValue == null)
                        {
                            continue;
                        }

                        var address = reader.MapAddress();
                        await _dataContext.Addresses.AddAsync(address);

                        var person = reader.MapPerson(address);

                        var validationResult = await _validator.ValidateAsync(person);

                        if(!validationResult.IsValid)
                        {
                            validatorError.AddRange(validationResult.Errors);
                        }

                        await _dataContext.People.AddAsync(person);

                        var financialState = reader.MapFinancialState();
                        await _dataContext.FinancialStates.AddAsync(financialState);

                        var agreement = reader.MapAgreement(person, financialState);
                        await _dataContext.Agreements.AddAsync(agreement);
                    }
                    if(validatorError.Any())
                    {
                        return Result.Error(validatorError);
                    }
                    await _dataContext.SaveChangesAsync();
                }
                return Result.Ok();
            }
        }

        public class PersonValidator : AbstractValidator<Person>
        {
            private static int[] multipliers = new int[] { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };

            public PersonValidator()
            {
                RuleFor(p => p.NationalIdentificationNumber)
                    .Cascade(CascadeMode.StopOnFirstFailure)
                    .Must(Have11Character)
                    .WithMessage(p => $"Pesel: {p.NationalIdentificationNumber} must have 11 character")
                    .Must(HaveCorrectControlSum)
                    .WithMessage(p => $"Pesel: {p.NationalIdentificationNumber} must have correct sum control");
            }

            public static bool Have11Character(string pesel)
            {
                return pesel.Length == 11;
            }

            public static bool HaveCorrectControlSum(string pesel)
            {
                var sum = 0;
                for (var i = 0; i < multipliers.Length; ++i)
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
            private ImportService.IImportService _importService;

            public SendDataToServiceHandler(DataContext dataContext,
                ImportService.IImportService importService)
            {
                _dataContext = dataContext;
                _importService = importService;
            }

            public async Task<Result> Handle(SendDataToService request, CancellationToken cancellationToken)
            {
                foreach (var person in await _dataContext.People.ToListAsync())
                {
                    var address = person.Address.Adapt<ImportService.Address>();

                    var correspondenceAddress = new ImportService.Address
                    {
                        City = person.Address.CorrespondencePostOfficeCity,
                        HouseNo = person.Address.CorrespondenceStreetnumber,
                        LocaleNo = person.Address.CorrespondenceFlatNumber,
                        PostalCode = person.Address.CorrespondencePostCode,
                        Street = person.Address.CorrespondenceStreetName
                    };

                    var finacialState = person.Agreements.FirstOrDefault(x => x.PersonId == person.Id).FinancialState;
                    var importSeriveFinacialState = finacialState.Adapt<ImportService.FinancialState>();

                    var agreements = person.Agreements.FirstOrDefault(x => x.PersonId == person.Id)
                        .Adapt<ImportService.IdentityDocument>();

                    var importServicePerson = person.BuildAdapter()
                        .AddParameters("Addresses", new ImportService.Address[] { address, correspondenceAddress})
                        .AddParameters("FinancialState", importSeriveFinacialState)
                        .AddParameters("IdentitiDocuments", new ImportService.IdentityDocument[] { agreements })
                        .AdaptToType<ImportService.Person>();

                    await _importService.DoImportAsync(importServicePerson);
                }

                return Result.Ok();
            }
        }
    }
}
