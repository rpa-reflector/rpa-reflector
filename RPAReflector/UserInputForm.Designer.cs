// SPDX-FileCopyrightText: 2026 Reflector Maintainers
// SPDX-License-Identifier: MIT

namespace RPAReflector
{
    partial class UserInputForm
    {
        private System.Windows.Forms.Button btnOK;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtApiUsername = new System.Windows.Forms.TextBox();
            this.txtApiPassword = new System.Windows.Forms.TextBox();
            this.txtHixEnvironment = new System.Windows.Forms.TextBox();
            this.comboHixVersion = new System.Windows.Forms.ComboBox();
            this.chkCreateFirewallRule = new System.Windows.Forms.CheckBox();
            this.chkOpenServicesPanel = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkStartService = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtApiUsername
            // 
            this.txtApiUsername.Location = new System.Drawing.Point(111, 29);
            this.txtApiUsername.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtApiUsername.Name = "txtApiUsername";
            this.txtApiUsername.Size = new System.Drawing.Size(253, 26);
            this.txtApiUsername.TabIndex = 0;
            // 
            // txtApiPassword
            // 
            this.txtApiPassword.Location = new System.Drawing.Point(111, 80);
            this.txtApiPassword.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtApiPassword.Name = "txtApiPassword";
            this.txtApiPassword.Size = new System.Drawing.Size(253, 26);
            this.txtApiPassword.TabIndex = 1;
            this.txtApiPassword.UseSystemPasswordChar = true;
            // 
            // txtHixEnvironment
            // 
            this.txtHixEnvironment.Location = new System.Drawing.Point(160, 69);
            this.txtHixEnvironment.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtHixEnvironment.Name = "txtHixEnvironment";
            this.txtHixEnvironment.Size = new System.Drawing.Size(204, 26);
            this.txtHixEnvironment.TabIndex = 2;
            // 
            // comboHixVersion
            // 
            this.comboHixVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboHixVersion.FormattingEnabled = true;
            this.comboHixVersion.Location = new System.Drawing.Point(160, 28);
            this.comboHixVersion.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.comboHixVersion.Name = "comboHixVersion";
            this.comboHixVersion.Size = new System.Drawing.Size(204, 28);
            this.comboHixVersion.TabIndex = 3;
            // 
            // chkCreateFirewallRule
            // 
            this.chkCreateFirewallRule.AutoSize = true;
            this.chkCreateFirewallRule.Location = new System.Drawing.Point(22, 295);
            this.chkCreateFirewallRule.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkCreateFirewallRule.Name = "chkCreateFirewallRule";
            this.chkCreateFirewallRule.Size = new System.Drawing.Size(256, 24);
            this.chkCreateFirewallRule.TabIndex = 4;
            this.chkCreateFirewallRule.Text = "Create firewall rule for port 9001";
            this.chkCreateFirewallRule.UseVisualStyleBackColor = true;
            // 
            // chkOpenServicesPanel
            // 
            this.chkOpenServicesPanel.AutoSize = true;
            this.chkOpenServicesPanel.Location = new System.Drawing.Point(22, 334);
            this.chkOpenServicesPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkOpenServicesPanel.Name = "chkOpenServicesPanel";
            this.chkOpenServicesPanel.Size = new System.Drawing.Size(290, 24);
            this.chkOpenServicesPanel.TabIndex = 5;
            this.chkOpenServicesPanel.Text = "Open services panel after installation";
            this.chkOpenServicesPanel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(134, 405);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(112, 35);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtApiUsername);
            this.groupBox1.Controls.Add(this.txtApiPassword);
            this.groupBox1.Location = new System.Drawing.Point(22, 20);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox1.Size = new System.Drawing.Size(375, 132);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "RPA .NET Reflector API Endpoint Credentials";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 80);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Password";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 34);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Username";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 32);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(78, 20);
            this.label3.TabIndex = 4;
            this.label3.Text = "HiX Minor";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 74);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(98, 20);
            this.label4.TabIndex = 8;
            this.label4.Text = "Environment";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.comboHixVersion);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.txtHixEnvironment);
            this.groupBox2.Location = new System.Drawing.Point(22, 166);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Size = new System.Drawing.Size(375, 109);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "HiX Settings";
            // 
            // chkStartService
            // 
            this.chkStartService.AutoSize = true;
            this.chkStartService.Checked = true;
            this.chkStartService.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkStartService.Location = new System.Drawing.Point(22, 369);
            this.chkStartService.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkStartService.Name = "chkStartService";
            this.chkStartService.Size = new System.Drawing.Size(235, 24);
            this.chkStartService.TabIndex = 10;
            this.chkStartService.Text = "Start service after installation";
            this.chkStartService.UseVisualStyleBackColor = true;
            // 
            // UserInputForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(426, 454);
            this.Controls.Add(this.chkStartService);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.chkOpenServicesPanel);
            this.Controls.Add(this.chkCreateFirewallRule);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "UserInputForm";
            this.Text = "RPA .NET Reflector Service Setup";
            this.Load += new System.EventHandler(this.UserInputForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtApiUsername;
        private System.Windows.Forms.TextBox txtApiPassword;
        private System.Windows.Forms.TextBox txtHixEnvironment;
        private System.Windows.Forms.ComboBox comboHixVersion;
        private System.Windows.Forms.CheckBox chkCreateFirewallRule;
        private System.Windows.Forms.CheckBox chkOpenServicesPanel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkStartService;
    }
}