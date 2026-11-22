// Back to the Future (LMG) — Tradução PT-BR
// Instalador / desinstalador da tradução. Criado e traduzido por xSnoopx.
// Compilado com csc (.NET Framework 4.x, C# 5).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BttfPtBr
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            bool silent = false, uninstall = false;
            string dir = null, logPath = null;
            foreach (string a in args)
            {
                string s = a.Trim();
                if (s.StartsWith("/") || s.StartsWith("-")) s = "/" + s.Substring(1);
                if (s == "/silent") silent = true;
                else if (s == "/uninstall") uninstall = true;
                else if (s.StartsWith("/dir=")) dir = s.Substring(5).Trim('"');
                else if (s.StartsWith("/log=")) logPath = s.Substring(5).Trim('"');
            }

            if (silent)
            {
                try
                {
                    Core c = new Core();
                    StringBuilder outLog = new StringBuilder();
                    c.Log = delegate (string line) { outLog.AppendLine(line); };
                    Environment.ExitCode = uninstall ? c.Uninstall(dir) : c.Install(dir);
                    if (logPath != null) File.WriteAllText(logPath, outLog.ToString(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    if (logPath != null) File.WriteAllText(logPath, "ERRO: " + ex.ToString(), Encoding.UTF8);
                    Environment.ExitCode = 9;
                }
                return;
            }

            try { SetProcessDPIAware(); }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ---------------------------------------------------------------- core

    class Core
    {
        public const string GameExe = "ParadoxDrive-Win64-Shipping.exe";
        public const string OrigSha = "124449377c47550ef894a2caa7f89e14d3f1548976c5a1fde7e2f726ae47d8a9";
        public const string PatchedSha = "3d2425ff54031194350234d43a638da4ce2a649f5c03f2634f9147ecb7d4e8cc";
        public static readonly string[] DataFiles = new string[] {
            "missions.json", "cutscenes.json", "freeplay.json", "voices.json",
            "map.json", "eras.json", "roads.json"
        };

        public Action<string> Log = delegate (string s) { };

        static Stream Res(string name)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream st = asm.GetManifestResourceStream(name);
            if (st == null) throw new Exception("Recurso ausente: " + name);
            return st;
        }

        public static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream fs = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(fs);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string Sha256Bytes(byte[] data)
        {
            using (SHA256 sha = SHA256.Create()) return ToHex(sha.ComputeHash(data));
        }

        static string ToHex(byte[] hash)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // ---- path handling

        public static string NormalizeGameDir(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            path = path.Trim().Trim('"');
            if (File.Exists(path) && Path.GetFileName(path).ToLowerInvariant() == GameExe.ToLowerInvariant())
                return Path.GetDirectoryName(path);
            if (!Directory.Exists(path)) return null;

            string[] candidates = new string[] {
                Path.Combine(path, "ParadoxDrive", "Binaries", "Win64"),
                Path.Combine(path, "Binaries", "Win64"),
                path,
                Path.Combine(path, "BackToTheFuture", "ParadoxDrive", "Binaries", "Win64")
            };
            foreach (string c in candidates)
                if (File.Exists(Path.Combine(c, GameExe))) return c;

            // one level down (user picked the folder that holds the game folder)
            try
            {
                foreach (string sub in Directory.GetDirectories(path))
                {
                    string r = NormalizeGameDir(sub);
                    if (r != null) return r;
                }
            }
            catch { }
            return null;
        }

        public static string FindGame()
        {
            List<string> roots = new List<string>();
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            roots.Add(Path.Combine(home, "Downloads"));
            roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            roots.Add(home);
            foreach (string drive in new string[] { "D:\\", "E:\\", "F:\\", "G:\\", "H:\\" })
                if (Directory.Exists(drive)) roots.Add(drive);

            string[] names = new string[] { "BackToTheFuture*", "ParadoxDrive*", "Back to the Future*" };
            foreach (string root in roots)
            {
                foreach (string pat in names)
                {
                    try
                    {
                        foreach (string sub in Directory.GetDirectories(root, pat))
                        {
                            string r = NormalizeGameDir(sub);
                            if (r != null) return r;
                        }
                    }
                    catch { }
                }
                // one more level (rar/zip extracted twice)
                try
                {
                    foreach (string sub in Directory.GetDirectories(root))
                    {
                        string n = Path.GetFileName(sub).ToLowerInvariant();
                        if (n.IndexOf("backtothefuture") < 0 && n.IndexOf("paradox") < 0) continue;
                        string r = NormalizeGameDir(sub);
                        if (r != null) return r;
                    }
                }
                catch { }
            }
            return null;
        }

        static string StateFile
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "xSnoopx", "BTTF-PTBR");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "state.txt");
            }
        }

        public static string LastGameDir()
        {
            try
            {
                if (File.Exists(StateFile))
                {
                    string s = File.ReadAllText(StateFile).Trim();
                    if (s.Length > 0 && Directory.Exists(s)) return s;
                }
            }
            catch { }
            return null;
        }

        static void SaveState(string gameDir)
        {
            try { File.WriteAllText(StateFile, gameDir, Encoding.UTF8); }
            catch { }
        }

        // ---- patch

        class Patch
        {
            public byte[] OrigSha, PatchedSha;
            public long OrigSize, PatchedSize;
            public List<long> Offsets = new List<long>();
            public List<byte[]> Blobs = new List<byte[]>();
        }

        static Patch LoadPatch()
        {
            using (BinaryReader br = new BinaryReader(Res("patch.bin")))
            {
                byte[] magic = br.ReadBytes(8);
                if (Encoding.ASCII.GetString(magic, 0, 5) != "PDBR1") throw new Exception("Pacote de patch inválido.");
                Patch p = new Patch();
                p.OrigSha = br.ReadBytes(32);
                p.PatchedSha = br.ReadBytes(32);
                p.OrigSize = br.ReadInt64();
                p.PatchedSize = br.ReadInt64();
                int n = br.ReadInt32();
                for (int i = 0; i < n; i++)
                {
                    long off = br.ReadInt64();
                    int len = br.ReadInt32();
                    p.Offsets.Add(off);
                    p.Blobs.Add(br.ReadBytes(len));
                }
                return p;
            }
        }

        static byte[] ApplyPatch(byte[] src, Patch p)
        {
            byte[] buf = new byte[(int)p.PatchedSize];
            int copy = (int)Math.Min(src.Length, p.PatchedSize);
            Buffer.BlockCopy(src, 0, buf, 0, copy);
            for (int i = 0; i < p.Offsets.Count; i++)
            {
                long off = p.Offsets[i];
                byte[] blob = p.Blobs[i];
                if (off + blob.Length > buf.Length) throw new Exception("Patch fora do arquivo.");
                Buffer.BlockCopy(blob, 0, buf, (int)off, blob.Length);
            }
            return buf;
        }

        // ---- install / uninstall

        public int Install(string dirArg)
        {
            string dir = dirArg != null ? NormalizeGameDir(dirArg) : (LastGameDir() ?? FindGame());
            if (dir == null) { Log("Pasta do jogo não encontrada."); return 2; }
            string exe = Path.Combine(dir, GameExe);
            string bak = exe + ".bak";
            SaveState(dir);
            Log("Pasta do jogo: " + dir);

            string sha = Sha256File(exe);
            if (sha == PatchedSha)
            {
                Log("O jogo já está com a tradução instalada (executável).");
                CopyData(dir, false);
                Log("OK: nada a fazer.");
                return 0;
            }
            if (sha != OrigSha)
            {
                Log("VERSÃO INCOMPATÍVEL.");
                Log("  esperado: " + OrigSha);
                Log("  encontrado: " + sha);
                Log("Baixe o jogo no site oficial (playbacktothefuture.com) e rode o instalador sobre essa cópia.");
                return 3;
            }

            if (File.Exists(bak))
            {
                if (Sha256File(bak) != OrigSha)
                {
                    Log("Já existe um backup " + Path.GetFileName(bak) + " diferente do original — abortando para não sobrescrever.");
                    return 4;
                }
                Log("Backup existente validado.");
            }
            else
            {
                Log("Criando backup do executável original (.bak)...");
                File.Copy(exe, bak, false);
            }

            Log("Aplicando tradução no executável (isso pode levar alguns segundos)...");
            Patch p = LoadPatch();
            byte[] src = File.ReadAllBytes(exe);
            byte[] dst = ApplyPatch(src, p);
            string newSha = Sha256Bytes(dst);
            if (newSha != PatchedSha)
            {
                Log("Falha na verificação do patch (" + newSha + "). Nada foi alterado.");
                return 5;
            }
            string tmp = exe + ".ptbr.tmp";
            File.WriteAllBytes(tmp, dst);
            File.Replace(tmp, exe, null);
            Log("Executável traduzido e verificado.");

            CopyData(dir, true);
            Log("Concluído. Abra o jogo pelo ParadoxDrive.exe.");
            return 0;
        }

        void CopyData(string dir, bool verbose)
        {
            // Content\Data fica dois niveis acima de Binaries\Win64
            string gameRoot = Path.GetFullPath(Path.Combine(dir, "..", ".."));
            string dataDir = Path.Combine(gameRoot, "Content", "Data");
            if (!Directory.Exists(dataDir))
            {
                Log("Aviso: pasta Content\\Data nao encontrada em " + gameRoot + " — textos de história não instalados.");
                return;
            }
            foreach (string f in DataFiles)
            {
                byte[] payload;
                using (Stream st = Res("data." + f))
                using (MemoryStream ms = new MemoryStream())
                {
                    st.CopyTo(ms);
                    payload = ms.ToArray();
                }
                string dst = Path.Combine(dataDir, f);
                if (File.Exists(dst))
                {
                    byte[] cur = File.ReadAllBytes(dst);
                    if (Same(cur, payload)) { if (verbose) Log("  " + f + " já traduzido."); continue; }
                    string enBak = dst + ".en.bak";
                    if (!File.Exists(enBak))
                    {
                        File.Copy(dst, enBak, false);
                        if (verbose) Log("  backup do original: " + f + ".en.bak");
                    }
                }
                File.WriteAllBytes(dst, payload);
                if (verbose) Log("  texto instalado: " + f);
            }
        }

        static bool Same(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        public int Uninstall(string dirArg)
        {
            string dir = dirArg != null ? NormalizeGameDir(dirArg) : (LastGameDir() ?? FindGame());
            if (dir == null) { Log("Pasta do jogo não encontrada."); return 2; }
            string exe = Path.Combine(dir, GameExe);
            string bak = exe + ".bak";
            Log("Pasta do jogo: " + dir);

            if (!File.Exists(bak)) { Log("Nenhum backup encontrado — o executável não foi modificado por este instalador."); }
            else if (Sha256File(bak) != OrigSha) { Log("O backup não corresponde ao original — restauração cancelada."); return 6; }
            else
            {
                Log("Restaurando o executável original...");
                File.Copy(bak, exe, true);
                if (Sha256File(exe) != OrigSha) { Log("Falha ao restaurar o executável."); return 7; }
                File.Delete(bak);
                Log("Executável original restaurado.");
            }

            string gameRoot = Path.GetFullPath(Path.Combine(dir, "..", ".."));
            string dataDir = Path.Combine(gameRoot, "Content", "Data");
            foreach (string f in DataFiles)
            {
                string dst = Path.Combine(dataDir, f);
                string enBak = dst + ".en.bak";
                if (File.Exists(enBak))
                {
                    File.Copy(enBak, dst, true);
                    File.Delete(enBak);
                    Log("  texto original restaurado: " + f);
                }
            }
            Log("Desinstalação concluída.");
            return 0;
        }
    }

    // ---------------------------------------------------------------- ui

    class MainForm : Form
    {
        readonly Core core = new Core();
        TextBox txtDir;
        TextBox txtLog;
        ProgressBar bar;
        Button btnInstall, btnUninstall, btnSite, btnBrowse;
        Label lblStatus;
        bool busy;

        public MainForm()
        {
            Text = "Back to the Future — Tradução PT-BR (xSnoopx)";
            ClientSize = new Size(920, 560);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(11, 16, 46);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // ---- left: capa
            Panel left = new Panel();
            left.SetBounds(0, 0, 372, 560);
            left.BackColor = Color.FromArgb(8, 12, 38);
            Controls.Add(left);

            PictureBox pic = new PictureBox();
            pic.SetBounds(16, 40, 340, 340);
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream("cover.png"))
                if (st != null) pic.Image = Image.FromStream(st);
            left.Controls.Add(pic);

            Label credit = new Label();
            credit.Text = "Criado e traduzido por xSnoopx";
            credit.SetBounds(16, 392, 340, 24);
            credit.TextAlign = ContentAlignment.MiddleCenter;
            credit.Font = new Font("Segoe UI Semibold", 11f);
            credit.ForeColor = Color.FromArgb(255, 200, 0);
            left.Controls.Add(credit);

            Label ver = new Label();
            ver.Text = "Versão 1.0.0  ·  PT-BR";
            ver.SetBounds(16, 418, 340, 20);
            ver.TextAlign = ContentAlignment.MiddleCenter;
            ver.ForeColor = Color.FromArgb(150, 165, 200);
            left.Controls.Add(ver);

            Label fan = new Label();
            fan.Text = "Patch de fã, gratuito. Não inclui o jogo:\nbaixe o original em playbacktothefuture.com";
            fan.SetBounds(16, 470, 340, 60);
            fan.TextAlign = ContentAlignment.MiddleCenter;
            fan.ForeColor = Color.FromArgb(130, 145, 180);
            left.Controls.Add(fan);

            // ---- right: conteudo
            Label title = new Label();
            title.Text = "BACK TO THE FUTURE";
            title.SetBounds(400, 26, 500, 34);
            title.Font = new Font("Segoe UI Black", 20f);
            title.ForeColor = Color.White;
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "Tradução para Português do Brasil  ·  menus, HUD, circuitos do tempo, missões, "
                     + "legendas, modo livre e mapa";
            sub.SetBounds(400, 64, 500, 40);
            sub.ForeColor = Color.FromArgb(170, 185, 220);
            Controls.Add(sub);

            Label lblDir = new Label();
            lblDir.Text = "Pasta do jogo";
            lblDir.SetBounds(400, 118, 200, 18);
            lblDir.ForeColor = Color.FromArgb(150, 165, 200);
            Controls.Add(lblDir);

            txtDir = new TextBox();
            txtDir.SetBounds(400, 138, 390, 26);
            txtDir.BackColor = Color.FromArgb(20, 26, 60);
            txtDir.ForeColor = Color.White;
            txtDir.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(txtDir);

            btnBrowse = new Button();
            btnBrowse.Text = "Procurar...";
            btnBrowse.SetBounds(798, 137, 100, 27);
            Style(btnBrowse, false);
            btnBrowse.Click += delegate { BrowseFolder(); };
            Controls.Add(btnBrowse);

            btnInstall = new Button();
            btnInstall.Text = "Instalar tradução";
            btnInstall.SetBounds(400, 184, 244, 44);
            Style(btnInstall, true);
            btnInstall.Click += delegate { Run(true); };
            Controls.Add(btnInstall);

            btnUninstall = new Button();
            btnUninstall.Text = "Restaurar original";
            btnUninstall.SetBounds(654, 184, 244, 44);
            Style(btnUninstall, false);
            btnUninstall.Click += delegate { Run(false); };
            Controls.Add(btnUninstall);

            btnSite = new Button();
            btnSite.Text = "Abrir o site oficial do jogo";
            btnSite.SetBounds(400, 236, 244, 30);
            Style(btnSite, false);
            btnSite.Click += delegate { OpenSite(); };
            Controls.Add(btnSite);

            lblStatus = new Label();
            lblStatus.Text = "Procurando o jogo...";
            lblStatus.SetBounds(400, 278, 500, 20);
            lblStatus.ForeColor = Color.FromArgb(170, 185, 220);
            Controls.Add(lblStatus);

            bar = new ProgressBar();
            bar.SetBounds(400, 300, 498, 8);
            bar.Style = ProgressBarStyle.Marquee;
            bar.MarqueeAnimationSpeed = 0;
            bar.Visible = false;
            Controls.Add(bar);

            txtLog = new TextBox();
            txtLog.SetBounds(400, 318, 498, 190);
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BackColor = Color.FromArgb(16, 21, 52);
            txtLog.ForeColor = Color.FromArgb(190, 205, 235);
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            txtLog.Font = new Font("Consolas", 8.5f);
            Controls.Add(txtLog);

            Label foot = new Label();
            foot.Text = "Jogo original gratuito: playbacktothefuture.com  ·  tradução by xSnoopx";
            foot.SetBounds(400, 520, 500, 20);
            foot.ForeColor = Color.FromArgb(120, 135, 170);
            Controls.Add(foot);

            core.Log = AppendLog;

            string found = Core.LastGameDir() ?? Core.FindGame();
            if (found != null) { txtDir.Text = found; lblStatus.Text = "Jogo encontrado — pronto para instalar."; }
            else lblStatus.Text = "Não encontrei o jogo. Use Procurar... e escolha a pasta onde está o ParadoxDrive.exe.";
        }

        void Style(Button b, bool primary)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font = new Font("Segoe UI Semibold", primary ? 10.5f : 9.5f);
            b.BackColor = primary ? Color.FromArgb(255, 138, 0) : Color.FromArgb(28, 36, 78);
            b.ForeColor = primary ? Color.FromArgb(20, 12, 0) : Color.White;
            b.Cursor = Cursors.Hand;
        }

        void AppendLog(string line)
        {
            if (txtLog.InvokeRequired) { txtLog.BeginInvoke(new Action<string>(AppendLog), line); return; }
            txtLog.AppendText(line + Environment.NewLine);
        }

        void SetBusy(bool on, string status)
        {
            busy = on;
            btnInstall.Enabled = !on;
            btnUninstall.Enabled = !on;
            btnBrowse.Enabled = !on;
            bar.Visible = on;
            bar.MarqueeAnimationSpeed = on ? 30 : 0;
            if (status != null) lblStatus.Text = status;
        }

        void BrowseFolder()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Escolha a pasta do jogo (a que contém ParadoxDrive.exe)";
                if (LastDirHint != null) dlg.SelectedPath = LastDirHint;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string d = Core.NormalizeGameDir(dlg.SelectedPath);
                    if (d == null)
                    {
                        MessageBox.Show(this, "Não encontrei ParadoxDrive-Win64-Shipping.exe nessa pasta.\n\n"
                            + "Escolha a pasta onde você descompactou o jogo (a que tem ParadoxDrive.exe).",
                            "Pasta inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    txtDir.Text = d;
                    lblStatus.Text = "Jogo encontrado — pronto para instalar.";
                }
            }
        }

        string LastDirHint
        {
            get
            {
                try { return Directory.Exists(txtDir.Text) ? txtDir.Text : null; }
                catch { return null; }
            }
        }

        void Run(bool install)
        {
            if (busy) return;
            string dir = Core.NormalizeGameDir(txtDir.Text);
            if (dir == null)
            {
                MessageBox.Show(this, "Escolha a pasta do jogo primeiro (Procurar...).",
                    "Pasta do jogo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            txtDir.Text = dir;
            txtLog.Clear();
            SetBusy(true, install ? "Instalando tradução..." : "Restaurando o original...");
            Thread t = new Thread(delegate ()
            {
                int code = 0;
                try { code = install ? core.Install(dir) : core.Uninstall(dir); }
                catch (Exception ex) { core.Log("ERRO: " + ex.Message); code = 9; }
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false, code == 0 ? (install ? "Tradução instalada." : "Jogo original restaurado.")
                                             : "Algo deu errado — veja o log abaixo.");
                    if (code == 0)
                        MessageBox.Show(this, install
                            ? "Pronto! O jogo está em português.\n\nAbra pelo ParadoxDrive.exe."
                            : "Pronto! O jogo voltou ao original em ingles.",
                            "xSnoopx", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            });
            t.IsBackground = true;
            t.Start();
        }

        void OpenSite()
        {
            try { Process.Start("https://playbacktothefuture.com"); } catch { }
        }
    }
}
