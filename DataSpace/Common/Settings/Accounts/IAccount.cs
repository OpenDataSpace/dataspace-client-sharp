//-----------------------------------------------------------------------
// <copyright file="IAccount.cs" company="GRAU DATA AG">
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

namespace DataSpace.Common.Settings.Accounts {
    using System.Security;

    /// <summary>
    /// Account read access
    /// </summary>
    public interface IAccountReadOnly : INotifySettingsChanged {
        string Id { get; }
        string Url { get; }
        string UserName { get;}
        SecureString Password { get;}
    }

    /// <summary>
    /// Account read/write access
    /// </summary>
    public interface IAccount  : INotifySettingsChanged, ISettingsPersist {
        string Id { get; }
        string Url { get; }
        string UserName { get; }
        SecureString Password { get; set; }
    }
}