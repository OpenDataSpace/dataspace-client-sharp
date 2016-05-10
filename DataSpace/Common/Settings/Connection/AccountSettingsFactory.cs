//-----------------------------------------------------------------------
// <copyright file="AccountSettingsFactory.cs" company="GRAU DATA AG">
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

﻿namespace DataSpace.Common.Settings.Connection {
    using System;
    using System.Configuration;

    /// <summary>
    /// Account settings factory is used to initialize platform depedend IAccountSettings instances.
    /// </summary>
    public class AccountSettingsFactory : IAccountSettingsFactory {
        public IAccountSettings CreateInstance(Configuration config, string sectionName) {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.Unix:
                    return new Generic.AccountSettings(config, sectionName);
                case PlatformID.MacOSX:
                    throw new NotImplementedException();
                default:
                    return new W32.AccountSettings(config, sectionName);
            }
        }
    }
}