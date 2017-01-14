namespace SSC_Server
{
    partial class MainWindow
    {
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
            this.LogBox = new System.Windows.Forms.TextBox();
            this.StartButton = new System.Windows.Forms.Button();
            this.ListenBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label4 = new System.Windows.Forms.Label();
            this.AESbox = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label7 = new System.Windows.Forms.Label();
            this.WelcomeMsg = new System.Windows.Forms.TextBox();
            this.WelcomeMsgBox = new System.Windows.Forms.CheckBox();
            this.ServerNameBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.broadcastBox = new System.Windows.Forms.CheckBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // LogBox
            // 
            this.LogBox.Font = new System.Drawing.Font("微软雅黑", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogBox.Location = new System.Drawing.Point(12, 11);
            this.LogBox.Multiline = true;
            this.LogBox.Name = "LogBox";
            this.LogBox.ReadOnly = true;
            this.LogBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LogBox.Size = new System.Drawing.Size(837, 289);
            this.LogBox.TabIndex = 0;
            this.LogBox.TabStop = false;
            this.LogBox.TextChanged += new System.EventHandler(this.LogBox_TextChanged);
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(723, 407);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(126, 31);
            this.StartButton.TabIndex = 1;
            this.StartButton.Text = "Start Server";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // ListenBox
            // 
            this.ListenBox.Location = new System.Drawing.Point(59, 9);
            this.ListenBox.Name = "ListenBox";
            this.ListenBox.Size = new System.Drawing.Size(152, 21);
            this.ListenBox.TabIndex = 2;
            this.ListenBox.Text = "0.0.0.0:12344";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "IP:Port";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 306);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(705, 137);
            this.tabControl1.TabIndex = 4;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.AESbox);
            this.tabPage1.Controls.Add(this.ListenBox);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(697, 111);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Connection";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 41);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 12);
            this.label4.TabIndex = 6;
            this.label4.Text = "AES Key";
            // 
            // AESbox
            // 
            this.AESbox.Location = new System.Drawing.Point(59, 38);
            this.AESbox.Name = "AESbox";
            this.AESbox.Size = new System.Drawing.Size(213, 21);
            this.AESbox.TabIndex = 5;
            this.AESbox.Text = "SSCv2*Default_AES@Key&1234567890";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label7);
            this.tabPage2.Controls.Add(this.WelcomeMsg);
            this.tabPage2.Controls.Add(this.WelcomeMsgBox);
            this.tabPage2.Controls.Add(this.ServerNameBox);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.broadcastBox);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(697, 111);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Message";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 41);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(95, 12);
            this.label7.TabIndex = 11;
            this.label7.Text = "Welcome Message";
            // 
            // WelcomeMsg
            // 
            this.WelcomeMsg.Location = new System.Drawing.Point(107, 38);
            this.WelcomeMsg.Name = "WelcomeMsg";
            this.WelcomeMsg.Size = new System.Drawing.Size(129, 21);
            this.WelcomeMsg.TabIndex = 10;
            this.WelcomeMsg.Text = "Welcome!";
            // 
            // WelcomeMsgBox
            // 
            this.WelcomeMsgBox.AutoSize = true;
            this.WelcomeMsgBox.Location = new System.Drawing.Point(284, 11);
            this.WelcomeMsgBox.Name = "WelcomeMsgBox";
            this.WelcomeMsgBox.Size = new System.Drawing.Size(264, 16);
            this.WelcomeMsgBox.TabIndex = 9;
            this.WelcomeMsgBox.Text = "Send welcome message to new participants";
            this.WelcomeMsgBox.UseVisualStyleBackColor = true;
            // 
            // ServerNameBox
            // 
            this.ServerNameBox.Location = new System.Drawing.Point(107, 9);
            this.ServerNameBox.Name = "ServerNameBox";
            this.ServerNameBox.Size = new System.Drawing.Size(129, 21);
            this.ServerNameBox.TabIndex = 8;
            this.ServerNameBox.Text = "Server";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 12);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(95, 12);
            this.label5.TabIndex = 7;
            this.label5.Text = "Server Nickname";
            // 
            // broadcastBox
            // 
            this.broadcastBox.AutoSize = true;
            this.broadcastBox.Location = new System.Drawing.Point(284, 40);
            this.broadcastBox.Name = "broadcastBox";
            this.broadcastBox.Size = new System.Drawing.Size(192, 16);
            this.broadcastBox.TabIndex = 6;
            this.broadcastBox.Text = "Broadcast join/leave message";
            this.broadcastBox.UseVisualStyleBackColor = true;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(861, 453);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.LogBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MainWindow";
            this.Text = "Simple Secure Chat Server";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox LogBox;
        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.TextBox ListenBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox AESbox;
        private System.Windows.Forms.CheckBox broadcastBox;
        private System.Windows.Forms.TextBox ServerNameBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox WelcomeMsg;
        private System.Windows.Forms.CheckBox WelcomeMsgBox;
    }
}

