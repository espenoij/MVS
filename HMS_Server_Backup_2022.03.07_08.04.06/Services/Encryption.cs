using System;
using System.Security;

namespace HMS_Server
{
    class Encryption
    {
        static byte[] entropy = System.Text.Encoding.Unicode.GetBytes("Sveio Hanaleitet 5555"); // Salt

        public static string EncryptString(System.Security.SecureString input)
        {
            byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                System.Text.Encoding.Unicode.GetBytes(ToInsecureString(input)),
                entropy,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static SecureString DecryptString(string encryptedData)
        {
            try
            {
                if (!string.IsNullOrEmpty(encryptedData))
                {
                    byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                        Convert.FromBase64String(encryptedData),
                        entropy,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    return ToSecureString(System.Text.Encoding.Unicode.GetString(decryptedData));
                }
                else
                {
                    return new SecureString();
                }
            }
            catch
            {
                return new SecureString();
            }
        }

        public static SecureString ToSecureString(string input)
        {
            SecureString secure = new SecureString();

            if (!string.IsNullOrEmpty(input))
            {
                foreach (char c in input)
                {
                    secure.AppendChar(c);
                }
                secure.MakeReadOnly();
            }

            return secure;
        }

        public static string ToInsecureString(SecureString input)
        {
            string returnValue = string.Empty;

            if (input  != null)
            {
                IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(input);
                try
                {
                    returnValue = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
                }
            }

            return returnValue;
        }
    }
}
