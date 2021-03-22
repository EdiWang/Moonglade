namespace Moonglade.Web.Models
{
    // Used to wrap models so that Model Binders can recognize ModelPrefix_ModelProperty
    // This is a temp workaround
    public class MagicCodeWrapper<T> where T : class
    {
        public T ViewModel { get; set; }

        public MagicCodeWrapper(T model = null)
        {
            ViewModel = model;
        }
    }
}