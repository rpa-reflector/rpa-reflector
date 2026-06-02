// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using System;
using ChipSoft.Publics.DD;
using ChipSoft.FCL.Base;
using System.Collections.ObjectModel;
using ChipSoft.Publics.DD.Units;
using System.Collections.Generic;
using Borland.Vcl;
using Borland.Vcl.Units;

public class RPAReflectorEnvironmentProvider : IDDIniEnvironmentProvider, IEnvironmentProvider
{
    private string _preferredEnvironment;
    private IEnvironment _currentEnvironment;
    private bool _didLoadEnvironments;
    private IList<IEnvironment> _environments;
    private static List<string> _currentFullyCachedLogics = new List<string>();

    public static void SetFactoryCacheType(ICS_ObjectLogic logic, ref TCS_FactoryCacheType preferredFactoryCacheType)
    {
        object[] customAttributes = logic.GetType().GetCustomAttributes(typeof(TabelTypeAttribute), false);

        if (_currentFullyCachedLogics.Contains(logic.ClassID.ToString()))
        {
            preferredFactoryCacheType = TCS_FactoryCacheType.fctAll;
            return;
        }

        Console.WriteLine(logic.ClassID.ToString());

        if ((customAttributes != null ? customAttributes.Length : 0) <= 0 || (customAttributes[0] as TabelTypeAttribute).Type != ENUM_TABELTYPE.Productie)
            return;
    }

    public void AddEnvironment(string Code, string Description, string ConnectionString, bool ReadOnly, bool AdminOnly, string str)
    {
        this._environments = (IList<IEnvironment>) new List<IEnvironment>();
        IConnectionStringProvider ConnectionStringProvider = (IConnectionStringProvider) new TSingleConnectionStringProvider(ConnectionString);
        this._environments.Add((IEnvironment) new TDDIniEnvironment(Code, Description, ConnectionStringProvider, ReadOnly, AdminOnly, str));
        this._didLoadEnvironments = true;
    }

    public ReadOnlyCollection<IEnvironment> Environments
    {
        get
        {
            if (!this._didLoadEnvironments)
                throw new Exception("No environments are loaded! Please call LoadEnvironments() first!");

            return new ReadOnlyCollection<IEnvironment>(this._environments);
        }
    }

    public IEnvironment FindEnvironmentByCode(string Code)
    {
        IEnvironment environment = (IEnvironment) null;
        foreach (IEnvironment fenvironment in this._environments)
        {
            if (SysUtils.SameText(fenvironment.Code, Code))
            {
                environment = fenvironment;
                break;
            }
        }
        return environment;
    }

    public IEnvironment FindEnvironmentByID(string ID)
    {
        IEnvironment environment = (IEnvironment) null;
        foreach (IEnvironment fenvironment in this._environments)
        {
            if (fenvironment.ID.Equals(ID, StringComparison.OrdinalIgnoreCase))
            {
                environment = fenvironment;
                break;
            }
        }
        return environment;
    }

    public IEnvironment CurrentEnvironment
    {
        get
        {
            if (this._currentEnvironment == null)
                this._currentEnvironment = this.SelectEnvironment();
            return this._currentEnvironment;
        }
    }

    public string DDIni
    {
        get { return ""; }
    }

    public static List<string> CurrentFullyCachedLogics
    {
        get { return _currentFullyCachedLogics; }
        private set { _currentFullyCachedLogics = value; }
    }

    public RPAReflectorEnvironmentProvider(string preferredEnvironment)
    {
        this._preferredEnvironment = preferredEnvironment;
    }

    protected IEnvironment SelectEnvironment()
    {
        IEnvironment environment = (IEnvironment) null;
        if (!string.IsNullOrEmpty(this._preferredEnvironment))
            environment = this.FindEnvironmentByCode(this._preferredEnvironment);
        if (environment == null && this.Environments.Count > 0)
            environment = this.Environments[0];
        return environment;
    }
}