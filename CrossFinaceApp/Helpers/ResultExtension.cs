using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CrossFinaceApp.Helpers
{
    public static class ResultExtension
    {
        public static void AddErrorToModelState(this Result result, ModelStateDictionary modelState)
        {
            if (result.Success)
            {
                return;
            }

            foreach (var error in result.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.Message);
            }
        }
    }
}
