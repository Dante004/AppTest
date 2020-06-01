using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;

namespace CrossFinaceApp.Helpers
{
    public class Result
    {
        public bool Success { get; set; }
        public IEnumerable<ErrorMessage> Errors { get; set; }

        public static Result Ok()
        {
            return new Result()
            {
                Success = true
            };
        }

        public static Result Error(string message)
        {
            return new Result
            {
                Success = false,
                Errors = new List<ErrorMessage>()
                {
                    new ErrorMessage()
                    {
                        PropertyName = string.Empty,
                        Message = message
                    }
                }
            };
        }

        public static Result Error(IEnumerable<ValidationFailure> validationFailures)
        {
            var result = new Result
            {
                Success = false,
                Errors = validationFailures.Select(v => new ErrorMessage()
                {
                    PropertyName = v.PropertyName,
                    Message = v.ErrorMessage
                })
            };

            return result;
        }
    }

    public class ErrorMessage
    {
        public string PropertyName { get; set; }
        public string Message { get; set; }
    }
}
