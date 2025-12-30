namespace TicTacToeOnline
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel loginPanel;
        private System.Windows.Forms.TextBox txtLogin;
        private System.Windows.Forms.Button btnCheckLogin;
        private System.Windows.Forms.Label lblTitle; // Новый элемент
        
        private System.Windows.Forms.Panel roomPanel;
        private System.Windows.Forms.Button btnCreateRoom;
        private System.Windows.Forms.Button btnJoinRoom;
        private System.Windows.Forms.TextBox txtRoomId;
        
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnNewGame;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnRules;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.loginPanel = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.txtLogin = new System.Windows.Forms.TextBox();
            this.btnCheckLogin = new System.Windows.Forms.Button();
            this.roomPanel = new System.Windows.Forms.Panel();
            this.btnCreateRoom = new System.Windows.Forms.Button();
            this.btnJoinRoom = new System.Windows.Forms.Button();
            this.txtRoomId = new System.Windows.Forms.TextBox();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.lblInfo = new System.Windows.Forms.Label();
            this.btnNewGame = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnRules = new System.Windows.Forms.Button();
            this.headerPanel.SuspendLayout();
            this.loginPanel.SuspendLayout();
            this.roomPanel.SuspendLayout();
            this.SuspendLayout();

            // headerPanel
            this.headerPanel.Controls.Add(this.btnRules);
            this.headerPanel.Controls.Add(this.btnNewGame);
            this.headerPanel.Controls.Add(this.btnExit);
            this.headerPanel.Controls.Add(this.lblInfo);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Height = 70;
            this.headerPanel.BackColor = Color.WhiteSmoke;
            this.headerPanel.Visible = false;

            // lblInfo
            this.lblInfo.Location = new System.Drawing.Point(10, 5);
            this.lblInfo.Size = new System.Drawing.Size(225, 60);
            this.lblInfo.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            this.lblInfo.TextAlign = ContentAlignment.MiddleLeft;

            // btnRules
            this.btnRules.Location = new System.Drawing.Point(240, 37);
            this.btnRules.Size = new System.Drawing.Size(130, 28);
            this.btnRules.Text = "Правила";
            this.btnRules.BackColor = Color.LightYellow;
            this.btnRules.FlatStyle = FlatStyle.Flat;
            this.btnRules.Click += (s, e) => this.OnRulesClick();

            // btnNewGame
            this.btnNewGame.Location = new System.Drawing.Point(375, 5);
            this.btnNewGame.Size = new System.Drawing.Size(130, 28);
            this.btnNewGame.Text = "Новая партия";
            this.btnNewGame.BackColor = Color.LightGreen;
            this.btnNewGame.FlatStyle = FlatStyle.Flat;
            this.btnNewGame.Visible = false;
            this.btnNewGame.Click += (s, e) => this.OnResetGame();

            // btnExit
            this.btnExit.Location = new System.Drawing.Point(375, 37);
            this.btnExit.Size = new System.Drawing.Size(130, 28);
            this.btnExit.Text = "Выход";
            this.btnExit.BackColor = Color.MistyRose;
            this.btnExit.FlatStyle = FlatStyle.Flat;
            this.btnExit.Click += (s, e) => this.OnExitRoom();

            // flowLayoutPanel1
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 70);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(10, 5, 10, 10);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(515, 495);
            this.flowLayoutPanel1.Visible = false;

            // lblStatus
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Height = 35;
            this.lblStatus.BackColor = Color.SkyBlue;
            this.lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // loginPanel
            this.loginPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.loginPanel.Controls.Add(this.lblTitle);
            this.loginPanel.Controls.Add(this.txtLogin);
            this.loginPanel.Controls.Add(this.btnCheckLogin);

            // lblTitle (Большая надпись)
            this.lblTitle.Text = "Ultimate\nTic-Tac-Toe\nOnline";
            this.lblTitle.Font = new Font("Segoe UI Light", 36, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.SaddleBrown;
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.Size = new Size(520, 220);
            this.lblTitle.Location = new Point(0, 40);

            // txtLogin (Смещен вниз)
            this.txtLogin.Location = new System.Drawing.Point(160, 300);
            this.txtLogin.Size = new System.Drawing.Size(200, 23);
            this.txtLogin.PlaceholderText = "Введите никнейм...";
            this.txtLogin.TextAlign = HorizontalAlignment.Center;

            // btnCheckLogin (Смещен вниз)
            this.btnCheckLogin.Location = new System.Drawing.Point(160, 335);
            this.btnCheckLogin.Size = new System.Drawing.Size(200, 45);
            this.btnCheckLogin.Text = "ВОЙТИ В ИГРУ";
            this.btnCheckLogin.BackColor = Color.Peru;
            this.btnCheckLogin.ForeColor = Color.White;
            this.btnCheckLogin.FlatStyle = FlatStyle.Flat;
            this.btnCheckLogin.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.btnCheckLogin.Click += (s, e) => this.OnCheckPlayer();

            // roomPanel
            this.roomPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.roomPanel.Visible = false;
            this.roomPanel.Controls.Add(this.btnCreateRoom);
            this.roomPanel.Controls.Add(this.btnJoinRoom);
            this.roomPanel.Controls.Add(this.txtRoomId);
            this.btnCreateRoom.Location = new System.Drawing.Point(160, 180);
            this.btnCreateRoom.Size = new System.Drawing.Size(200, 40);
            this.btnCreateRoom.Text = "Создать комнату";
            this.btnCreateRoom.Click += (s, e) => this.OnCreateRoom();
            this.txtRoomId.Location = new System.Drawing.Point(160, 250);
            this.txtRoomId.Size = new System.Drawing.Size(200, 23);
            this.txtRoomId.PlaceholderText = "ID (4 цифры)";
            this.btnJoinRoom.Location = new System.Drawing.Point(160, 280);
            this.btnJoinRoom.Size = new System.Drawing.Size(200, 40);
            this.btnJoinRoom.Text = "Присоединиться";
            this.btnJoinRoom.Click += (s, e) => this.OnJoinRoom();

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 600); 
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.roomPanel);
            this.Controls.Add(this.loginPanel);
            this.Controls.Add(this.lblStatus);
            this.BackColor = Color.AntiqueWhite;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Ultimate Tic-Tac-Toe Online";
            this.headerPanel.ResumeLayout(false);
            this.loginPanel.ResumeLayout(false);
            this.loginPanel.PerformLayout();
            this.roomPanel.ResumeLayout(false);
            this.roomPanel.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}