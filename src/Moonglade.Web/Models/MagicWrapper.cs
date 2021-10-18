namespace Moonglade.Web.Models;

// Used to wrap models so that Model Binders can recognize ModelPrefix_ModelProperty
// This is a temp workaround
public class MagicWrapper<T> where T : class
{
    public T ViewModel { get; set; }

    public MagicWrapper()
    {

    }

    public MagicWrapper(T model)
    {
        ViewModel = model;
    }
}