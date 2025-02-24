using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;

namespace NuGetCredentialProvider.CredentialProviders.Vsts
{
    public static class MsalUtil
    {
        public static List<(IAccount, string)> GetApplicableAccounts(IEnumerable<IAccount> accounts, Guid authorityTenantId, string loginHint)
        {
            var applicableAccounts = new List<(IAccount, string)>();

            foreach (var account in accounts)
            {
                string canonicalName = $"{account.HomeAccountId?.TenantId}\\{account.Username}";

                // If a login hint is provided and matches, try that first
                if (!string.IsNullOrEmpty(loginHint) && account.Username == loginHint)
                {
                    applicableAccounts.Insert(0, (account, canonicalName));
                    continue;
                }

                if (Guid.TryParse(account.HomeAccountId?.TenantId, out Guid accountTenantId))
                {
                    if (accountTenantId == authorityTenantId)
                    {
                        applicableAccounts.Add((account, canonicalName));
                    }
                    else if (accountTenantId == AuthUtil.MsaAccountTenant && (authorityTenantId == AuthUtil.FirstPartyTenant || authorityTenantId == Guid.Empty))
                    {
                        applicableAccounts.Add((account, canonicalName));
                    }
                }
            }

            return applicableAccounts;
        }

        public static AcquireTokenSilentParameterBuilder WithAccountTenantId(this AcquireTokenSilentParameterBuilder builder, IAccount account)
        {
            // Workaround for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3077
            // Even if using the organizations tenant the presence of an MSA will attempt to use the consumers tenant
            // which is not supported by the Azure DevOps application. Detect this case and use the first party tenant.

            if (Guid.TryParse(account.HomeAccountId?.TenantId, out Guid accountTenantId) && accountTenantId == AuthUtil.MsaAccountTenant)
            {
                builder = builder.WithTenantId(AuthUtil.FirstPartyTenant.ToString());
            }

            return builder;
        }
    }
}
