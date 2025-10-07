using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Moonglade.Web.Extensions;

public static class ModelStateExtension
{
    public static string GetCombinedErrorMessage(this ModelStateDictionary modelStateDictionary, string sep = ", ")
    {
        var messages = modelStateDictionary.GetErrorMessages();
        var enumerable = messages as string[] ?? [.. messages];
        return enumerable.Length != 0 ? string.Join(sep, enumerable) : string.Empty;
    }

    public static IEnumerable<string> GetErrorMessages(this ModelStateDictionary modelStateDictionary)
    {
        if (modelStateDictionary is null) return null;
        if (modelStateDictionary.ErrorCount == 0) return null;

        return from modelState in modelStateDictionary.Values
               from error in modelState.Errors
               select error.ErrorMessage;
    }
}
