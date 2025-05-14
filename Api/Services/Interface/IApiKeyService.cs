using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Services.Interface
{
    public interface IApiKeyService
    {
        /// <summary>
        /// Returns true if the key exists and had quotas remaining (then decrements it).
        /// </summary>
        bool TryConsumeKey(string apiKey);

        /// <summary>
        /// Returns the remaining quota, or null if the key is invalid.
        /// </summary>
        int? GetRemainingQuota(string apiKey);
    }
}