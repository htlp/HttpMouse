using System;
using System.Diagnostics.CodeAnalysis;

namespace Rpfl.Client
{
    public class RpflOptions
    {
        [AllowNull]
        public Uri Server { get; set; }

        [AllowNull]
        public Uri ClientUpstream { get; set; }

        [AllowNull]
        public string ClientDomain { get; set; }
    }
}
