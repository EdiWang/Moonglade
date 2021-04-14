namespace Moonglade.Menus
{
    public struct UpdateMenuRequest
    {
        public string Title { get; set; }

        public string Url { get; set; }

        public string Icon { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsOpenInNewTab { get; set; }
    }
}
