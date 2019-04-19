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

        #region operators
        public bool Equals(MethodResult other)
        {
            return code == other.code;
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(MethodResult) && Equals((MethodResult)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(code);
        }

        public static bool operator ==(MethodResult a, MethodResult b) => a.Equals(b);
        public static bool operator !=(MethodResult a, MethodResult b) => !a.Equals(b);
        public static bool operator ==(MethodResult a, int b) => a.code == b;
        public static bool operator !=(MethodResult a, int b) => a.code != b;
        #endregion

        #region general purpose result instances
        public static readonly MethodResult success = new MethodResult(0, "The method executed succesfully.");
        public static readonly MethodResult notfound = new MethodResult(1, "The resource was not found");
        public static readonly MethodResult unknownfailure = new MethodResult(-1, "The method failed to execute due to an unknown error.");
        #endregion
    }
}
