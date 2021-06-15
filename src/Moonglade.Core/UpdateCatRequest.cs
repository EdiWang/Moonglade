namespace Moonglade.Core
{
    public class UpdateCatRequest
    {
        public string RouteName { get; set; }
        public string DisplayName { get; set; }
        public string Note { get; set; }

        public UpdateCatRequest(string displayName, string routeName)
        {
            DisplayName = displayName;
            RouteName = routeName;
        }
    }
}
