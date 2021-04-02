using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Templates.Blazor2.UI.Stores
{
    public class ExceptionStore
    {
        public string Message { get; set; }

        public ExceptionStore(Exception ex)
        {
            Message = ex.Message;
        }
    }
}
