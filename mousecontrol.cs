using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO; 
using Microsoft.Win32;

namespace BitSoulMouseControl
{
    public class Form1 : Form
    {
        private Panel pnlHeader = null!;
        private Panel pnlMain = null!;
        private Label lblTitle = null!;
        private CheckedListBox clbBotoes = null!;
        private CheckBox chkAutoStart = null!;
        private Button btnAplicar = null!;
        private NotifyIcon trayIcon = null!;
        private ContextMenuStrip trayMenu = null!;

        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelMouseProc _proc = HookCallback;
        private static bool permitirDireito = true, permitirMeio = true, permitirVoltar = true, permitirAvancar = true;

        private const string AppName = "BitSoulMouseControl";
        private const string SettingsPath = @"SOFTWARE\BitSoulMouseControl";

        // Estrutura necessária para ler os botões laterais corretamente em 64 bits
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public Point pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public Form1()
        {
            ConfigurarEstiloXMouse();
            ConfigurarTrayIcon();
            CarregarConfiguracoesBotoes();
            _hookID = SetHook(_proc);
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Load += (s, e) => this.Hide();
        }

        private void ConfigurarTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Abrir Configurações", null, (s, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate(); 
            });
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Sair", null, (s, e) => {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                Application.Exit();
            });

            trayIcon = new NotifyIcon
            {
                Icon = File.Exists("bitsoul.ico") ? new Icon("bitsoul.ico") : SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Text = "BitSoul Mouse Control",
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
            };
        }

        private void ConfigurarEstiloXMouse()
        {
            this.Text = "BitSoul Mouse Control";
            this.Size = new Size(450, 450);
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(45, 45, 48) };
            lblTitle = new Label { 
                Text = "Configuração de Botões do Mouse", 
                ForeColor = Color.White, 
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(15, 18),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            clbBotoes = new CheckedListBox { 
                Dock = DockStyle.Top, Height = 130, CheckOnClick = true, 
                Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle 
            };
            clbBotoes.Items.Add("Botão Direito (Right Click)", true);
            clbBotoes.Items.Add("Botão do Meio (Middle Click)", true);
            clbBotoes.Items.Add("Botão Lateral 1 (Back / Voltar)", true);
            clbBotoes.Items.Add("Botão Lateral 2 (Forward / Avançar)", true);

            chkAutoStart = new CheckBox {
                Text = "Iniciar automaticamente com o Windows",
                Dock = DockStyle.Top, Height = 40,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                Checked = VerificarStatusInicializacao()
            };

            btnAplicar = new Button { 
                Text = "Aplicar e Esconder", Dock = DockStyle.Bottom, Height = 45,
                BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnAplicar.Click += (s, e) => {
                AplicarConfiguracoes();
                this.Hide();
                this.ShowInTaskbar = false;
            };

            pnlMain.Controls.Add(btnAplicar);
            pnlMain.Controls.Add(chkAutoStart);
            pnlMain.Controls.Add(clbBotoes);
            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlHeader);
        }

        private void AplicarConfiguracoes()
        {
            permitirDireito = clbBotoes.GetItemChecked(0);
            permitirMeio = clbBotoes.GetItemChecked(1);
            permitirVoltar = clbBotoes.GetItemChecked(2);
            permitirAvancar = clbBotoes.GetItemChecked(3);
            SalvarConfiguracoesBotoes();
            ConfigurarStartup(chkAutoStart.Checked);
            trayIcon.ShowBalloonTip(2000, "BitSoul", "Configurações aplicadas!", ToolTipIcon.Info);
        }

        private void SalvarConfiguracoesBotoes()
        {
            try {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsPath);
                key.SetValue("Direito", permitirDireito ? 1 : 0);
                key.SetValue("Meio", permitirMeio ? 1 : 0);
                key.SetValue("Voltar", permitirVoltar ? 1 : 0);
                key.SetValue("Avancar", permitirAvancar ? 1 : 0);
            } catch { }
        }

        private void CarregarConfiguracoesBotoes()
        {
            try {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(SettingsPath);
                if (key != null) {
                    permitirDireito = Convert.ToInt32(key.GetValue("Direito", 1)) == 1;
                    permitirMeio = Convert.ToInt32(key.GetValue("Meio", 1)) == 1;
                    permitirVoltar = Convert.ToInt32(key.GetValue("Voltar", 1)) == 1;
                    permitirAvancar = Convert.ToInt32(key.GetValue("Avancar", 1)) == 1;
                    clbBotoes.SetItemChecked(0, permitirDireito);
                    clbBotoes.SetItemChecked(1, permitirMeio);
                    clbBotoes.SetItemChecked(2, permitirVoltar);
                    clbBotoes.SetItemChecked(3, permitirAvancar);
                }
            } catch { }
        }

        private void ConfigurarStartup(bool habilitar)
        {
            try {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null) {
                    if (habilitar) key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
                    else key.DeleteValue(AppName, false);
                }
            } catch { }
        }

        private bool VerificarStatusInicializacao()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                if ((msg == 0x0204 || msg == 0x0205) && !permitirDireito) return (IntPtr)1;
                if ((msg == 0x0207 || msg == 0x0208) && !permitirMeio) return (IntPtr)1;

                if (msg == 0x020B || msg == 0x020C) // WM_XBUTTONDOWN ou UP
                {
                    MSLLHOOKSTRUCT mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    short xButton = (short)((mouseStruct.mouseData >> 16) & 0xFFFF);
                    if (xButton == 1 && !permitirVoltar) return (IntPtr)1;
                    if (xButton == 2 && !permitirAvancar) return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static IntPtr SetHook(LowLevelMouseProc proc) {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule? curModule = curProcess.MainModule;
            if (curModule?.ModuleName == null) return IntPtr.Zero;
            return SetWindowsHookEx(14, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int id, LowLevelMouseProc lpfn, IntPtr hMod, uint dwId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string name);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true; this.Hide(); this.ShowInTaskbar = false;
            } else {
                UnhookWindowsHookEx(_hookID); trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        [STAThread] static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}