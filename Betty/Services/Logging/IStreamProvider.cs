using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Betty.Services
{
    public interface IStreamProvider
    {
        TextWriter GetStream();
    }
}
