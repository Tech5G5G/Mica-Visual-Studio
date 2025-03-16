namespace MicaVisualStudio.Options
{
    partial class GeneralPage
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.backdrop = new System.Windows.Forms.ComboBox();
            this.theme = new System.Windows.Forms.ComboBox();
            this.cornerPreference = new System.Windows.Forms.ComboBox();
            this.toolWindows = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // backdrop
            // 
            this.backdrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.backdrop.FormattingEnabled = true;
            this.backdrop.Items.AddRange(new object[] {
            "Auto",
            "None",
            "Mica",
            "Acrylic",
            "Tabbed"});
            this.backdrop.Location = new System.Drawing.Point(3, 3);
            this.backdrop.Name = "backdrop";
            this.backdrop.Size = new System.Drawing.Size(121, 21);
            this.backdrop.TabIndex = 0;
            this.backdrop.SelectedValueChanged += new System.EventHandler(this.Backdrop_SelectionChanged);
            // 
            // theme
            // 
            this.theme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.theme.FormattingEnabled = true;
            this.theme.Items.AddRange(new object[] {
            "Light",
            "Dark",
            "System"});
            this.theme.Location = new System.Drawing.Point(3, 30);
            this.theme.Name = "theme";
            this.theme.Size = new System.Drawing.Size(121, 21);
            this.theme.TabIndex = 1;
            this.theme.SelectedValueChanged += new System.EventHandler(this.Theme_SelectionChanged);
            // 
            // cornerPreference
            // 
            this.cornerPreference.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cornerPreference.FormattingEnabled = true;
            this.cornerPreference.Items.AddRange(new object[] {
            "Default",
            "Square",
            "Round",
            "Round small"});
            this.cornerPreference.Location = new System.Drawing.Point(3, 57);
            this.cornerPreference.Name = "cornerPreference";
            this.cornerPreference.Size = new System.Drawing.Size(121, 21);
            this.cornerPreference.TabIndex = 2;
            this.cornerPreference.SelectedValueChanged += new System.EventHandler(this.CornerPreference_SelectionChanged);
            // 
            // toolWindows
            // 
            this.toolWindows.AutoSize = true;
            this.toolWindows.Location = new System.Drawing.Point(3, 84);
            this.toolWindows.Name = "toolWindows";
            this.toolWindows.Size = new System.Drawing.Size(219, 17);
            this.toolWindows.TabIndex = 3;
            this.toolWindows.Text = "Enable seperate options for tool windows";
            this.toolWindows.UseVisualStyleBackColor = true;
            this.toolWindows.CheckedChanged += new System.EventHandler(this.ToolWindows_Checked);
            // 
            // GeneralPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolWindows);
            this.Controls.Add(this.cornerPreference);
            this.Controls.Add(this.theme);
            this.Controls.Add(this.backdrop);
            this.Name = "GeneralPage";
            this.Size = new System.Drawing.Size(300, 300);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox backdrop;
        private System.Windows.Forms.ComboBox theme;
        private System.Windows.Forms.ComboBox cornerPreference;
        private System.Windows.Forms.CheckBox toolWindows;
    }
}
