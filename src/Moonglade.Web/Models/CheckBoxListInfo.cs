namespace Moonglade.Web.Models
{
    public class CheckBoxListInfo
    {
        public CheckBoxListInfo(string displayText, string value, bool isChecked)
        {
            Value = value;
            DisplayText = displayText;
            IsChecked = isChecked;
        }

        public string Value { get; }
        public string DisplayText { get; }
        public bool IsChecked { get; }
    }
}
