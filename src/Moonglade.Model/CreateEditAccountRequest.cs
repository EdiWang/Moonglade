using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Model
{
    public class CreateAccountRequest
    {
        public string Username { get; set; }

        public string ClearPassword { get; set; }
    }

    public class EditAccountRequest : CreateAccountRequest
    {
        public Guid Id { get; }

        public EditAccountRequest(Guid id)
        {
            Id = id;
        }
    }
}
