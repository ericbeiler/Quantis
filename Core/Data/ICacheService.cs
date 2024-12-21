using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis.Data
{
    public interface ICacheService
    {
        Task<T?> Get<T>(string key);
        Task Set(string key, object value);
    }
}
