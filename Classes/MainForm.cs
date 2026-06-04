using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace BruteForce
{
    /// <summary>
    /// Main GUI form. Wires together all classes and handles user interaction.
    /// </summary>
    public class MainForm : Form
    {
        private readonly PasswordManager _manager = new PasswordManager();
        private readonly BruteForceGenerator _generator = new BruteForceGenerator();
        private readonly PasswordValidator _validator = new PasswordValidator();
        private readonly PerformanceLogger _logger = new PerformanceLogger();
        private BruteForceEngine _engine;

        // Controls
        private Label lblTitle;
        private GroupBox grpPassword;
        private Button btnGenerate;
        private TextBox txtPlainPassword;
        private TextBox txtHashedPassword;
        private Label lblPlain;
        private Label lblHash;

        private GroupBox grpAttack;
        private RadioButton rbMultiThread;
        private RadioButton rbSingleThread;
        private Button btnStart;
        private Button btnStop;
        private Label lblThreadInfo;

        private GroupBox grpProgress;
        private ProgressBar progressBar;
        private Label lblAttempts;
        private Label lblElapsed;
        private Label lblStatus;

        private GroupBox grpResult;
        private TextBox txtResult;
        private TextBox txtLog;

        public MainForm()
        {
            InitializeComponent();
            _engine = new BruteForceEngine(_generator, _validator, _logger);
            WireEngineEvents();
            UpdateThreadInfo();
        }

        private void WireEngineEvents()
        {
            _engine.OnProgress += (attempts, elapsed) =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => UpdateProgress(attempts, elapsed)));
                else
                    UpdateProgress(attempts, elapsed);
            };

            _engine.OnFound += (password, report) =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => ShowFound(password, report)));
                else
                    ShowFound(password, report);
            };

            _engine.OnStopped += (reason) =>
            {
                if (InvokeRequired)
                    Invoke(new Action(() => ShowStopped(reason)));
                else
                    ShowStopped(reason);
            };
        }

        private void UpdateProgress(long attempts, string elapsed)
        {
            lblAttempts.Text = $"Attempts: {attempts:N0}";
            lblElapsed.Text = $"Elapsed:  {elapsed}";

            long total = _generator.GetTotalCombinations();
            int pct = (int)Math.Min(100, attempts * 100 / Math.Max(1, total));
            progressBar.Value = pct;
        }

        private void ShowFound(string password, string report)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.ForeColor = Color.LimeGreen;
            lblStatus.Text = $"✓ Password found: {password}";
            txtResult.Text = password ?? "(not found)";
            txtLog.Text = report;
        }

        private void ShowStopped(string reason)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.ForeColor = Color.Orange;
            lblStatus.Text = reason;
        }

        private void UpdateThreadInfo()
        {
            int cores = Environment.ProcessorCount;
            lblThreadInfo.Text = $"CPU Cores: {cores}  |  Threads: {Math.Max(1, cores - 1)}";
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            string plain = _manager.GeneratePassword();
            txtPlainPassword.Text = plain;
            txtHashedPassword.Text = _manager.GetHashedPassword();
            lblStatus.ForeColor = Color.CornflowerBlue;
            lblStatus.Text = "Password generated. Ready to attack.";
            txtResult.Text = "";
            txtLog.Text = "";
            progressBar.Value = 0;
            lblAttempts.Text = "Attempts: 0";
            lblElapsed.Text = "Elapsed:  00:00.00";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string hash = txtHashedPassword.Text.Trim();
            if (string.IsNullOrEmpty(hash))
            {
                MessageBox.Show("Please generate a password first.", "No Target",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            progressBar.Value = 0;
            txtResult.Text = "";
            lblStatus.ForeColor = Color.Yellow;
            lblStatus.Text = "Attack running...";

            _engine = new BruteForceEngine(_generator, _validator, _logger);
            WireEngineEvents();

            if (rbMultiThread.Checked)
                _engine.StartMultiThread(hash);
            else
                _engine.StartSingleThread(hash);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _engine.Stop();
        }

        private void InitializeComponent()
        {
            this.Text = "SHA256 Brute Force Password Cracker";
            this.Size = new Size(700, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(22, 27, 34);
            this.ForeColor = Color.White;
            this.Font = new Font("Consolas", 9f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            lblTitle = new Label
            {
                Text = "SHA256 BRUTE FORCE CRACKER",
                Font = new Font("Consolas", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                AutoSize = true,
                Location = new Point(20, 12)
            };
            this.Controls.Add(lblTitle);

            // Password Group
            grpPassword = MakeGroup("1. PASSWORD SETUP", 10, 45, 670, 110);
            lblPlain = MakeLabel("Plain text:", 15, 25);
            txtPlainPassword = MakeTextBox(100, 22, 430);
            txtPlainPassword.BackColor = Color.FromArgb(40, 50, 60);
            txtPlainPassword.ForeColor = Color.LightGreen;
            txtPlainPassword.ReadOnly = true;
            btnGenerate = MakeButton("Generate Password", 545, 21, 110, 26);
            btnGenerate.Click += btnGenerate_Click;
            btnGenerate.BackColor = Color.FromArgb(30, 120, 80);
            lblHash = MakeLabel("SHA256 Hash:", 15, 58);
            txtHashedPassword = MakeTextBox(100, 55, 540);
            txtHashedPassword.BackColor = Color.FromArgb(40, 50, 60);
            txtHashedPassword.ForeColor = Color.FromArgb(200, 150, 255);
            txtHashedPassword.ReadOnly = true;
            txtHashedPassword.Font = new Font("Consolas", 7.5f);
            grpPassword.Controls.AddRange(new Control[] { lblPlain, txtPlainPassword, btnGenerate, lblHash, txtHashedPassword });
            this.Controls.Add(grpPassword);

            // Attack Group
            grpAttack = MakeGroup("2. ATTACK CONFIGURATION", 10, 162, 670, 85);
            rbMultiThread = new RadioButton { Text = "Multi-Thread (recommended)", Location = new Point(15, 25), ForeColor = Color.White, Checked = true, AutoSize = true };
            rbSingleThread = new RadioButton { Text = "Single-Thread (for comparison)", Location = new Point(15, 50), ForeColor = Color.White, AutoSize = true };
            lblThreadInfo = MakeLabel("", 320, 30);
            lblThreadInfo.ForeColor = Color.FromArgb(150, 200, 150);
            btnStart = MakeButton("START ATTACK", 490, 22, 120, 30);
            btnStart.BackColor = Color.FromArgb(30, 100, 200);
            btnStart.Click += btnStart_Click;
            btnStop = MakeButton("STOP", 490, 55, 120, 24);
            btnStop.BackColor = Color.FromArgb(150, 40, 40);
            btnStop.Enabled = false;
            btnStop.Click += btnStop_Click;
            grpAttack.Controls.AddRange(new Control[] { rbMultiThread, rbSingleThread, lblThreadInfo, btnStart, btnStop });
            this.Controls.Add(grpAttack);

            // Progress Group
            grpProgress = MakeGroup("3. PROGRESS", 10, 254, 670, 95);
            progressBar = new ProgressBar { Location = new Point(15, 22), Size = new Size(635, 20), Minimum = 0, Maximum = 100, Style = ProgressBarStyle.Continuous };
            lblAttempts = MakeLabel("Attempts: 0", 15, 52);
            lblElapsed = MakeLabel("Elapsed:  00:00.00", 200, 52);
            lblStatus = MakeLabel("Generate a password to begin.", 400, 52);
            lblStatus.ForeColor = Color.CornflowerBlue;
            lblStatus.AutoSize = true;
            grpProgress.Controls.AddRange(new Control[] { progressBar, lblAttempts, lblElapsed, lblStatus });
            this.Controls.Add(grpProgress);

            // Result Group
            grpResult = MakeGroup("4. RESULT", 10, 356, 670, 55);
            var lblFoundLabel = MakeLabel("Found Password:", 15, 25);
            txtResult = new TextBox
            {
                Location = new Point(130, 22),
                Size = new Size(520, 22),
                BackColor = Color.FromArgb(20, 60, 20),
                ForeColor = Color.LightGreen,
                ReadOnly = true,
                Font = new Font("Consolas", 12f, FontStyle.Bold)
            };
            grpResult.Controls.AddRange(new Control[] { lblFoundLabel, txtResult });
            this.Controls.Add(grpResult);

            // Log Group
            var grpLog = MakeGroup("5. PERFORMANCE LOG", 10, 418, 670, 195);
            txtLog = new TextBox
            {
                Location = new Point(10, 20),
                Size = new Size(645, 160),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 20, 30),
                ForeColor = Color.FromArgb(180, 255, 180),
                Font = new Font("Consolas", 8.5f)
            };
            grpLog.Controls.Add(txtLog);
            this.Controls.Add(grpLog);
        }

        private GroupBox MakeGroup(string title, int x, int y, int w, int h)
        {
            return new GroupBox { Text = title, Location = new Point(x, y), Size = new Size(w, h), ForeColor = Color.FromArgb(120, 180, 255), Font = new Font("Consolas", 8.5f, FontStyle.Bold) };
        }

        private Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Color.FromArgb(180, 180, 180) };
        }

        private TextBox MakeTextBox(int x, int y, int w)
        {
            return new TextBox { Location = new Point(x, y), Width = w, BackColor = Color.FromArgb(40, 50, 60), ForeColor = Color.White };
        }

        private Button MakeButton(string text, int x, int y, int w, int h)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Font = new Font("Consolas", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
        }
    }
}