//-----------------------------------------------------------------------
// <copyright file="CredentialManager.cs" company="GRAU DATA AG">
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see http://www.gnu.org/licenses/.
//
// </copyright>
//-----------------------------------------------------------------------

namespace DataSpace.Common.Settings.Accounts.Native.W32 {
    ﻿using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Text;

    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// based on https://gist.github.com/meziantou/10311113
    /// </summary>
    public static class CredentialManager {
        public static Credential ReadCredential(string applicationName) {
            IntPtr nCredPtr;
            bool read = CredRead(applicationName, CredentialType.Generic, 0, out nCredPtr);
            if (read) {
                using (CriticalCredentialHandle critCred = new CriticalCredentialHandle(nCredPtr)) {
                    CREDENTIAL cred = critCred.GetCredential();
                    return ReadCredential(cred);
                }
            }

            return null;
        }

        private static Credential ReadCredential(CREDENTIAL credential) {
            string applicationName = Marshal.PtrToStringUni(credential.TargetName);
            string userName = Marshal.PtrToStringUni(credential.UserName);
            string secret = null;
            if (credential.CredentialBlob != IntPtr.Zero) {
                secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
            }

            return new Credential(credential.Type, applicationName, userName, secret);
        }

        public static void WriteCredential(string applicationName, string userName, string secret) {
            byte[] byteArray = secret == null ? null : Encoding.Unicode.GetBytes(secret);
            // XP and Vista: 512;
            // 7 and above: 5*512
            if (Environment.OSVersion.Version < new Version(6, 1) /* Windows 7 */) {
                if (byteArray != null && byteArray.Length > 512) {
                    throw new ArgumentOutOfRangeException("secret", "The secret message has exceeded 512 bytes.");
                }
            } else {
                if (byteArray != null && byteArray.Length > 512 * 5) {
                    throw new ArgumentOutOfRangeException("secret", "The secret message has exceeded 2560 bytes.");
                }
            }

            CREDENTIAL credential = new CREDENTIAL();
            credential.AttributeCount = 0;
            credential.Attributes = IntPtr.Zero;
            credential.Comment = IntPtr.Zero;
            credential.TargetAlias = IntPtr.Zero;
            credential.Type = CredentialType.Generic;
            credential.Persist = (uint)CredentialPersistence.LocalMachine;
            credential.CredentialBlobSize = (uint)(byteArray == null ? 0 : byteArray.Length);
            credential.TargetName = Marshal.StringToCoTaskMemUni(applicationName);
            credential.CredentialBlob = Marshal.StringToCoTaskMemUni(secret);
            credential.UserName = Marshal.StringToCoTaskMemUni(userName ?? Environment.UserName);

            bool written = CredWrite(ref credential, 0);
            Marshal.FreeCoTaskMem(credential.TargetName);
            Marshal.FreeCoTaskMem(credential.CredentialBlob);
            Marshal.FreeCoTaskMem(credential.UserName);

            if (!written) {
                int lastError = Marshal.GetLastWin32Error();
                throw new Exception(string.Format("CredWrite failed with the error code {0}.", lastError));
            }
        }

        public static void Delete(string applicationName) {
            if (string.IsNullOrWhiteSpace(applicationName)) {
                return;
            }

            bool deleted = CredDelete(applicationName, CredentialType.Generic, 0);

            if (!deleted) {
                int lastError = Marshal.GetLastWin32Error();
                // ERROR_NOT_FOUND in W32 is no Error
                // because our wish is fullfilled ... creds ar not existing
                if (lastError != 1168) {
                    throw new Win32Exception(lastError);
                }
            }
        }

        /// <summary>
        /// Enumerates Credentials
        /// </summary>
        /// <param name="filter">Prefix followed by an asterix like "DataSpace@*" </param>
        /// <returns></returns>
        public static IList<Credential> EnumerateCrendentials(string filter) {
            List<Credential> result = new List<Credential>();

            int count;
            IntPtr pCredentials;
            bool ret = CredEnumerate(filter, 0, out count, out pCredentials);
            if (ret) {
                for (int n = 0; n < count; n++) {
                    IntPtr credential = Marshal.ReadIntPtr(pCredentials, n * Marshal.SizeOf(typeof(IntPtr)));
                    result.Add(ReadCredential((CREDENTIAL)Marshal.PtrToStructure(credential, typeof(CREDENTIAL))));
                }
            } else {
                int lastError = Marshal.GetLastWin32Error();
                //ERROR_NOT_FOUND in W32 is no Error in case of enumeration
                if (lastError != 1168) {
                    throw new Win32Exception(lastError);
                }
            }

            return result;
        }

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] UInt32 flags);

        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredEnumerate(string filter, int flag, out int count, out IntPtr pCredentials);

        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        static extern bool CredFree([In] IntPtr cred);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredDelete(string target, CredentialType type, int reservedFlag);
        private enum CredentialPersistence : uint {
            Session = 1,
            LocalMachine,
            Enterprise
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL {
            public uint Flags;
            public CredentialType Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid {
            public CriticalCredentialHandle(IntPtr preexistingHandle) {
                SetHandle(preexistingHandle);
            }

            public CREDENTIAL GetCredential() {
                if (!IsInvalid) {
                    CREDENTIAL credential = (CREDENTIAL)Marshal.PtrToStructure(handle, typeof(CREDENTIAL));
                    return credential;
                }

                throw new InvalidOperationException("Invalid CriticalHandle!");
            }

            protected override bool ReleaseHandle() {
                if (!IsInvalid) {
                    CredFree(handle);
                    SetHandleAsInvalid();
                    return true;
                }

                return false;
            }
        }
    }

    public enum CredentialType {
        Generic = 1,
        DomainPassword,
        DomainCertificate,
        DomainVisiblePassword,
        GenericCertificate,
        DomainExtended,
        Maximum,
        MaximumEx = Maximum + 1000,
    }

    public class Credential {
        private readonly string _applicationName;
        private readonly string _userName;
        private readonly string _password;
        private readonly CredentialType _credentialType;

        public CredentialType CredentialType {
            get { return _credentialType; }
        }

        public string ApplicationName {
            get { return _applicationName; }
        }

        public string UserName {
            get { return _userName; }
        }

        public string Password {
            get { return _password; }
        }

        public Credential(CredentialType credentialType, string applicationName, string userName, string password) {
            _applicationName = applicationName;
            _userName = userName;
            _password = password;
            _credentialType = credentialType;
        }

        public override string ToString() {
            return string.Format("CredentialType: {0}, ApplicationName: {1}, UserName: {2}, Password: {3}", CredentialType, ApplicationName, UserName, Password);
        }
    }
}