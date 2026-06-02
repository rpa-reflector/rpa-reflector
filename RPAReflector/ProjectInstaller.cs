// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows.Forms;

namespace RPAReflector
{
    [RunInstaller(true)]

    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private readonly string TargetServiceName = "RPA .NET Reflector";

        private ServiceProcessInstaller _serviceProcessInstaller;
        private ServiceInstaller _serviceInstaller;

        public ProjectInstaller()
        {
            if (!ServiceExists(TargetServiceName))
            {
                _serviceProcessInstaller = new ServiceProcessInstaller();
                _serviceInstaller = new ServiceInstaller();

                // Service will run under system account
                _serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
                _serviceInstaller.StartType = ServiceStartMode.Automatic;

                // Service Name
                _serviceInstaller.ServiceName = TargetServiceName;
               
                this.Installers.AddRange(new System.Configuration.Install.Installer[] {
                _serviceProcessInstaller,
                _serviceInstaller
            });
            }
        }

        public override void Uninstall(IDictionary savedState)
        {
            StopServiceIfRunning();
            base.Uninstall(savedState);
        }

        public override void Install(IDictionary stateSaver)
        {
            StopServiceIfRunning();
            base.Install(stateSaver);

            try
            {
                using (UserInputForm form = new UserInputForm(Context.Parameters["targetdir"], Context.Parameters["assemblypath"]))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                        ApplyFormConfiguration(form);
                    else
                        MessageBox.Show("Please make sure to set your configuration manually in Fernandez.exe.config!", "Configuration warning");
                }
            }
            catch (Exception ex)
            {
                throw new InstallException("Error during installation: " + ex.Message);
            }
        }

        private void ApplyFormConfiguration(UserInputForm form)
        {
            string hixVersionValue = form.HixVersion == "none" ? "none" : form.HixVersion + ".0.0";
            UpdateAppSettings("SelectedEnvironment", form.HixEnvironment);
            UpdateAppSettings("HixVersion", hixVersionValue);
            UpdateAppSettings("ApiUsername", form.ApiUsername);
            UpdateAppSettings("ApiPassword", form.ApiPassword);
            CreateFirewallRule();

            if (form.OpenServicesPanel) LaunchServicesPanel();
            if (form.StartService) StartServiceIfNotRunning();
        }

        private void UpdateAppSettings(string key, string value)
        {
            try
            {
                // Get the path to the configuration file (for example, the app.config or web.config)
                string exePath = Context.Parameters["assemblypath"];

                // Open the config file
                var config = ConfigurationManager.OpenExeConfiguration(exePath);

                config.AppSettings.Settings.Remove(key);
                config.AppSettings.Settings.Add(key, value);

                // Save the updated config file
                config.Save(ConfigurationSaveMode.Modified);

                // Refresh the appSettings section so the changes take effect
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating configuration: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateFirewallRule()
        {
            string command = "netsh advfirewall firewall add rule name=\"Allow Port 9001\" dir=in action=allow protocol=TCP localport=9001";
            ExecuteCommand(command);
        }

        private void LaunchServicesPanel()
        {
            Process.Start("services.msc");
        }

        private void ExecuteCommand(string command)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = "/C " + command,
                Verb = "runas" // Ensures this runs with elevated privileges
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        private void StartServiceIfNotRunning()
        {
            try
            {
                using (ServiceController sc = new ServiceController(TargetServiceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                        Context.LogMessage($"{TargetServiceName} has been started successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Context.LogMessage($"An error occurred while starting {TargetServiceName}: {ex.Message}");
            }
        }

        private void StopServiceIfRunning()
        {
            try
            {
                using (ServiceController sc = new ServiceController(TargetServiceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        Context.LogMessage($"{TargetServiceName} has been stopped successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Context.LogMessage($"Failed to stop {TargetServiceName}: {ex.Message}");
            }
        }

        private static bool ServiceExists(string serviceName)
        {
            return Array.Exists(ServiceController.GetServices(), s => s.ServiceName == serviceName);
        }
    }
}