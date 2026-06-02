using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RPAReflector
{
    public partial class UserInputForm : Form
    {
        // Properties to get user inputs
        public string ApiUsername { get; private set; }
        public string ApiPassword { get; private set; }
        public string HixVersion { get; private set; }
        public string HixEnvironment { get; private set; }
        public bool OpenServicesPanel { get; private set; }
        public bool StartService { get; private set; }
        public bool CreateFirewallRule { get; private set; }
        private string _configPath = null;
        private string _exePath = null;

        public UserInputForm(string installDir, string exePath)
        {
            this._exePath = exePath;
            _configPath = Path.Combine(installDir.TrimEnd('\\'), "RPAReflector.exe.config");

            InitializeComponent();
            CheckAndLoadConfig();
        }

        private void CheckAndLoadConfig()
        {
            try
            {                
                // Check if config exists
                if (!File.Exists(_configPath))
                {
                    // Load from embedded resource and create RPAReflector.exe.config
                    if (!ExtractEmbeddedResourceToFile("RPAReflector.App.config.template", _configPath))
                    {
                        MessageBox.Show("Failed to create configuration file from embedded template.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // Load and populate form fields from config
                PopulateFieldsFromConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method to extract embedded resource to a file
        private bool ExtractEmbeddedResourceToFile(string resourceName, string outputPath)
        {
            try
            {
                // Get the current assembly
                var assembly = Assembly.GetExecutingAssembly();

                // Find the resource stream
                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                        throw new FileNotFoundException("Could not find embedded resource", resourceName);

                    // Write the embedded resource to the output file
                    using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting embedded resource: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void PopulateFieldsFromConfig()
        {
            try
            {  
               var config = ConfigurationManager.OpenExeConfiguration(_exePath);

                // Retrieve values from the appSettings section
                ApiUsername = GetSetting(config, "ApiUsername");
                ApiPassword = GetSetting(config, "ApiPassword");
                HixVersion = GetSetting(config, "HixVersion");
                HixEnvironment = GetSetting(config, "SelectedEnvironment");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for when OK button is clicked
        private void btnOK_Click(object sender, EventArgs e)
        {
            ApiUsername = txtApiUsername.Text;
            ApiPassword = txtApiPassword.Text;
            HixVersion = comboHixVersion.SelectedItem.ToString();
            HixEnvironment = txtHixEnvironment.Text;
            OpenServicesPanel = chkOpenServicesPanel.Checked;
            CreateFirewallRule = chkCreateFirewallRule.Checked;
            StartService = chkStartService.Checked;
            if (ValidateInputs())
            {
                if (HixVersion == "6.1")
                {
                    MessageBox.Show("HiX 6.1 required manual specification of a Database Connection String. Please configure the connection string for your environment manually in the App settings file.", "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                this.DialogResult = DialogResult.OK; // Close form and return OK result
                this.Close();
            }
            else
            {
                MessageBox.Show("Please provide api credentials, and environment name and select target HiX minor version!", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Validate user inputs
        private bool ValidateInputs()
        {
            return !string.IsNullOrEmpty(ApiUsername) &&
                   !string.IsNullOrEmpty(ApiPassword) &&
                   (HixVersion == "6.1" || HixVersion == "6.2" || HixVersion == "6.3" || HixVersion == "none");
        }

        // Initialize form components when loaded
        private void UserInputForm_Load(object sender, EventArgs e)
        {
            // Populate the fields in the form
            txtApiUsername.Text = ApiUsername;
            txtApiPassword.Text = ApiPassword;
            comboHixVersion.SelectedItem = HixVersion;
            txtHixEnvironment.Text = HixEnvironment;

            comboHixVersion.Items.Add("none");
            comboHixVersion.Items.Add("6.1");
            comboHixVersion.Items.Add("6.2");
            comboHixVersion.Items.Add("6.3");

            comboHixVersion.SelectedIndex = 0;

            int i = 0;
            foreach (var item in comboHixVersion.Items)
            {
                if (!String.IsNullOrEmpty(HixVersion) && HixVersion.StartsWith(item.ToString()))
                {
                    comboHixVersion.SelectedIndex = i;
                }
                i++;
            }
        }

        private string GetSetting(Configuration config, string key)
        {
            var setting = config.AppSettings.Settings[key];
            return setting == null ? string.Empty : setting.Value;
        }
    }
}
