using System;
using System.Collections.Generic;
using System.Text;

namespace Betty.Utilities
{
    /// <summary>
    /// A structure to communicate method execution result. This can be used when regular errors need to be communicated back which happen too frequently for exceptions
    /// </summary>
    public struct MethodResult : IEquatable<MethodResult>
    {
        public readonly int code;
        public readonly string message;

        public MethodResult(int code, string message)
        {
            this.code = code;
            this.message = message;
        }

        public bool Equals(MethodResult other)
        {
            return code == other.code;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(code);
        }

        public static readonly MethodResult success = new MethodResult(0, "The method executed succesfully.");
        public static readonly MethodResult notfound = new MethodResult(1, "The resource was not found");
        public static readonly MethodResult unknownfailure = new MethodResult(-1, "The method failed to execute due to an unknown error.");
    }
}
