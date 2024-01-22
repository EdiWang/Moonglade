using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WilderMinds.MetaWeblog
{
  public class MetaWeblogException : Exception
  {
    public int Code { get; private set; }

    public MetaWeblogException(string message, int code = 1) : base(message)
    {
      Code = code;
    }
  }
}
