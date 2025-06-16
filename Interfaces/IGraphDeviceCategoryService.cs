using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces
{
    public interface IGraphDeviceCategoryService
    {
        Task<JObject?> GetDeviceCategoryByNameAsync(string categoryName);
        Task<JObject?> CreateDeviceCategoryAsync(string categoryName, string description = "");
        Task<List<JObject>> GetAllDeviceCategoriesAsync();
    }

}
