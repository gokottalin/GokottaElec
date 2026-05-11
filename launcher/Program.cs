using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CircuitLangLauncher
{
    internal static class Program
    {
        private const string AppName = "GokottaElec";
        private const string AppVersion = "V1.4";
        private static string _lastFinalOutputDir = "";
        private static string _lastLiveText = "";
        private static int _renderSerial = 0;
        private static Icon _appIcon;
        private static readonly Color ShellBlue = Color.FromArgb(237, 247, 253);
        private static readonly Color SurfaceWhite = Color.FromArgb(255, 255, 255);
        private static readonly Color BorderBlue = Color.FromArgb(180, 216, 238);
        private static readonly Color BrandNavy = Color.FromArgb(13, 72, 133);
        private static readonly Color BrandBlue = Color.FromArgb(19, 124, 214);
        private static readonly Color TextBlue = Color.FromArgb(18, 47, 74);
        private static readonly Color MutedBlue = Color.FromArgb(82, 111, 133);

        private sealed class PreviewItem
        {
            public string CircuitId;
            public string SvgPath;

            public override string ToString()
            {
                return CircuitId;
            }
        }

        private sealed class BuildResult
        {
            public int ExitCode;
            public string Log;
            public string OutputDir;
        }

        private sealed class SampleItem
        {
            public string Title;
            public string Path;

            public override string ToString()
            {
                return Title;
            }
        }

        private sealed class HandoffFile
        {
            public string RelativePath;
            public string Title;
            public string Fence;

            public HandoffFile(string relativePath, string title, string fence)
            {
                RelativePath = relativePath;
                Title = title;
                Fence = fence;
            }
        }

        private sealed class BrandHeaderPanel : Panel
        {
            public BrandHeaderPanel()
            {
                DoubleBuffered = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle bounds = ClientRectangle;
                if (bounds.Width <= 0 || bounds.Height <= 0) return;

                using (LinearGradientBrush brush = new LinearGradientBrush(bounds, Color.FromArgb(248, 253, 255), Color.FromArgb(222, 242, 253), 0f))
                {
                    e.Graphics.FillRectangle(brush, bounds);
                }

                using (Pen trace = new Pen(Color.FromArgb(94, 190, 237), 2f))
                using (Pen traceSoft = new Pen(Color.FromArgb(155, 220, 246), 1.5f))
                using (SolidBrush node = new SolidBrush(Color.FromArgb(255, 255, 255)))
                using (Pen nodeStroke = new Pen(Color.FromArgb(51, 151, 220), 2f))
                {
                    int right = bounds.Right - 22;
                    int mid = bounds.Top + bounds.Height / 2;
                    e.Graphics.DrawLine(trace, right - 260, mid - 12, right - 170, mid - 12);
                    e.Graphics.DrawLine(trace, right - 170, mid - 12, right - 132, mid - 32);
                    e.Graphics.DrawLine(traceSoft, right - 240, mid + 16, right - 120, mid + 16);
                    e.Graphics.DrawLine(traceSoft, right - 120, mid + 16, right - 84, mid - 2);
                    DrawNode(e.Graphics, node, nodeStroke, right - 260, mid - 12, 10);
                    DrawNode(e.Graphics, node, nodeStroke, right - 132, mid - 32, 12);
                    DrawNode(e.Graphics, node, nodeStroke, right - 84, mid - 2, 9);
                }

                using (Pen border = new Pen(Color.FromArgb(191, 224, 243)))
                {
                    e.Graphics.DrawLine(border, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
                }
            }

            private static void DrawNode(Graphics graphics, Brush fill, Pen stroke, int x, int y, int size)
            {
                Rectangle rect = new Rectangle(x - size / 2, y - size / 2, size, size);
                graphics.FillEllipse(fill, rect);
                graphics.DrawEllipse(stroke, rect);
            }
        }

        [STAThread]
        private static int Main(string[] args)
        {
            string exePath = AppContext.BaseDirectory;
            string repoRoot = FindRepoRoot(exePath);
            string cliPath = Path.Combine(repoRoot, "scripts", "elec-cli.mjs");
            string pastePath = Path.Combine(repoRoot, "scripts", "build-paste.mjs");
            InstallExceptionLogging(repoRoot);
            WriteStartupLog(repoRoot, "START " + AppVersion + " exe=" + exePath + " cwd=" + Directory.GetCurrentDirectory() + " repoRoot=" + repoRoot);

            try
            {
                if (args.Length > 0)
                {
                    return RunCommandLine(repoRoot, cliPath, args);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(CreateMainForm(repoRoot, pastePath));
                return 0;
            }
            catch (Exception ex)
            {
                WriteStartupLog(repoRoot, FormatException(ex));
                try
                {
                    MessageBox.Show("GokottaElec 启动失败。\r\n\r\n" + ex.Message + "\r\n\r\n请查看 output\\gokottaelec-startup.log", AppName);
                }
                catch
                {
                }
                return 1;
            }
        }

        private static Form CreateMainForm(string repoRoot, string pastePath)
        {
            Form form = new Form();
            form.Text = AppName + " " + AppVersion + " - 实时电路原理图预览";
            form.Width = 1220;
            form.Height = 760;
            form.MinimumSize = new Size(980, 620);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.BackColor = ShellBlue;

            _appIcon = LoadOrCreateIcon(repoRoot);
            if (_appIcon != null) form.Icon = _appIcon;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.Padding = new Padding(12);
            root.BackColor = ShellBlue;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

            Panel header = BuildHeader(repoRoot);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            split.SplitterWidth = 7;
            split.BackColor = BorderBlue;

            TableLayoutPanel leftLayout = new TableLayoutPanel();
            leftLayout.Dock = DockStyle.Fill;
            leftLayout.ColumnCount = 1;
            leftLayout.RowCount = 3;
            leftLayout.Padding = new Padding(0, 6, 6, 0);
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            TableLayoutPanel inputBar = new TableLayoutPanel();
            inputBar.Dock = DockStyle.Fill;
            inputBar.ColumnCount = 3;
            inputBar.RowCount = 1;
            inputBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            inputBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inputBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

            Label inputLabel = new Label();
            inputLabel.Text = "LLM 输出 / CNL 输入";
            inputLabel.Dock = DockStyle.Fill;
            inputLabel.TextAlign = ContentAlignment.MiddleLeft;
            inputLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            inputLabel.ForeColor = TextBlue;

            ComboBox sampleCombo = new ComboBox();
            sampleCombo.Dock = DockStyle.Fill;
            sampleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            sampleCombo.Font = new Font("Segoe UI", 9);
            sampleCombo.BackColor = SurfaceWhite;
            sampleCombo.ForeColor = TextBlue;
            sampleCombo.FlatStyle = FlatStyle.Popup;

            Button loadSampleButton = BuildButton("载入");
            loadSampleButton.Dock = DockStyle.Fill;
            loadSampleButton.Width = 84;

            FlowLayoutPanel handoffBar = new FlowLayoutPanel();
            handoffBar.Dock = DockStyle.Fill;
            handoffBar.FlowDirection = FlowDirection.LeftToRight;
            handoffBar.WrapContents = false;
            handoffBar.Padding = new Padding(0, 4, 0, 0);

            Label handoffLabel = new Label();
            handoffLabel.Text = "LLM 对接复制";
            handoffLabel.Width = 132;
            handoffLabel.Height = 30;
            handoffLabel.TextAlign = ContentAlignment.MiddleLeft;
            handoffLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            handoffLabel.ForeColor = TextBlue;

            Button copyBasicHandoffButton = BuildButton("复制基础LLM");
            copyBasicHandoffButton.Width = 132;
            Button copyFullHandoffButton = BuildButton("复制完整LLM");
            copyFullHandoffButton.Width = 132;

            handoffBar.Controls.Add(handoffLabel);
            handoffBar.Controls.Add(copyBasicHandoffButton);
            handoffBar.Controls.Add(copyFullHandoffButton);

            TextBox inputBox = new TextBox();
            inputBox.Multiline = true;
            inputBox.ScrollBars = ScrollBars.Both;
            inputBox.AcceptsReturn = true;
            inputBox.AcceptsTab = true;
            inputBox.Font = new Font("Consolas", 10);
            inputBox.WordWrap = false;
            inputBox.Dock = DockStyle.Fill;
            inputBox.BorderStyle = BorderStyle.FixedSingle;
            inputBox.BackColor = Color.FromArgb(252, 254, 255);
            inputBox.ForeColor = Color.FromArgb(16, 46, 74);

            inputBar.Controls.Add(inputLabel, 0, 0);
            inputBar.Controls.Add(sampleCombo, 1, 0);
            inputBar.Controls.Add(loadSampleButton, 2, 0);

            leftLayout.Controls.Add(inputBar, 0, 0);
            leftLayout.Controls.Add(handoffBar, 0, 1);
            leftLayout.Controls.Add(inputBox, 0, 2);
            split.Panel1.Controls.Add(leftLayout);

            TableLayoutPanel rightLayout = new TableLayoutPanel();
            rightLayout.Dock = DockStyle.Fill;
            rightLayout.ColumnCount = 1;
            rightLayout.RowCount = 3;
            rightLayout.Padding = new Padding(6, 6, 0, 0);
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 72));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 28));

            TableLayoutPanel previewBar = new TableLayoutPanel();
            previewBar.Dock = DockStyle.Fill;
            previewBar.ColumnCount = 2;
            previewBar.RowCount = 1;
            previewBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            previewBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Label previewLabel = new Label();
            previewLabel.Text = "实时原理图预览";
            previewLabel.Dock = DockStyle.Fill;
            previewLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            previewLabel.ForeColor = TextBlue;

            Label statusLabel = new Label();
            statusLabel.Text = "就绪 · 等待 CNL";
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleRight;
            statusLabel.Font = new Font("Segoe UI", 9);
            statusLabel.ForeColor = MutedBlue;

            previewBar.Controls.Add(previewLabel, 0, 0);
            previewBar.Controls.Add(statusLabel, 1, 0);

            WebBrowser previewBrowser = new WebBrowser();
            previewBrowser.Dock = DockStyle.Fill;
            previewBrowser.ScriptErrorsSuppressed = true;
            SetPreviewHtml(previewBrowser, "请在左侧粘贴 LLM 输出的 CNL 文本，软件会自动渲染预览。");

            SplitContainer resultSplit = new SplitContainer();
            resultSplit.Dock = DockStyle.Fill;
            resultSplit.Orientation = Orientation.Vertical;
            resultSplit.SplitterWidth = 6;

            GroupBox listGroup = new GroupBox();
            listGroup.Text = "电路列表";
            listGroup.Dock = DockStyle.Fill;
            listGroup.Font = new Font("Segoe UI", 9);
            listGroup.ForeColor = TextBlue;

            ListBox circuitList = new ListBox();
            circuitList.Dock = DockStyle.Fill;
            circuitList.Font = new Font("Segoe UI", 9);
            circuitList.BackColor = Color.FromArgb(252, 254, 255);
            circuitList.ForeColor = TextBlue;
            circuitList.BorderStyle = BorderStyle.FixedSingle;
            listGroup.Controls.Add(circuitList);

            GroupBox logGroup = new GroupBox();
            logGroup.Text = "构建日志";
            logGroup.Dock = DockStyle.Fill;
            logGroup.Font = new Font("Segoe UI", 9);
            logGroup.ForeColor = TextBlue;

            TextBox logBox = new TextBox();
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.ReadOnly = true;
            logBox.Font = new Font("Consolas", 9);
            logBox.Dock = DockStyle.Fill;
            logBox.BackColor = Color.FromArgb(252, 254, 255);
            logBox.ForeColor = Color.FromArgb(23, 61, 90);
            logBox.BorderStyle = BorderStyle.FixedSingle;
            logGroup.Controls.Add(logBox);

            resultSplit.Panel1.Controls.Add(listGroup);
            resultSplit.Panel2.Controls.Add(logGroup);

            rightLayout.Controls.Add(previewBar, 0, 0);
            rightLayout.Controls.Add(previewBrowser, 0, 1);
            rightLayout.Controls.Add(resultSplit, 0, 2);
            split.Panel2.Controls.Add(rightLayout);

            TableLayoutPanel footer = new TableLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.ColumnCount = 4;
            footer.RowCount = 1;
            footer.Padding = new Padding(0, 8, 0, 0);
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 510));

            Label outputLabel = new Label();
            outputLabel.Text = "输出目录";
            outputLabel.Dock = DockStyle.Fill;
            outputLabel.TextAlign = ContentAlignment.MiddleLeft;
            outputLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            outputLabel.ForeColor = TextBlue;

            TextBox outputBox = new TextBox();
            outputBox.Dock = DockStyle.Fill;
            outputBox.Font = new Font("Segoe UI", 9);
            outputBox.Text = Path.Combine(repoRoot, "output", "gokottaelec");
            outputBox.BackColor = SurfaceWhite;
            outputBox.ForeColor = TextBlue;
            outputBox.BorderStyle = BorderStyle.FixedSingle;

            Button browseButton = BuildButton("选择目录");

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = false;
            buttons.Padding = new Padding(0, 1, 0, 0);

            CheckBox liveBox = new CheckBox();
            liveBox.Text = "实时";
            liveBox.Checked = true;
            liveBox.Width = 64;
            liveBox.Height = 30;
            liveBox.TextAlign = ContentAlignment.MiddleLeft;
            liveBox.ForeColor = TextBlue;

            Button renderNowButton = BuildButton("立即渲染", true);
            Button generateButton = BuildButton("生成文件", true);
            Button loadButton = BuildButton("加载文件");
            Button openButton = BuildButton("打开输出");

            buttons.Controls.Add(liveBox);
            buttons.Controls.Add(renderNowButton);
            buttons.Controls.Add(generateButton);
            buttons.Controls.Add(loadButton);
            buttons.Controls.Add(openButton);

            footer.Controls.Add(outputLabel, 0, 0);
            footer.Controls.Add(outputBox, 1, 0);
            footer.Controls.Add(browseButton, 2, 0);
            footer.Controls.Add(buttons, 3, 0);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(split, 0, 1);
            root.Controls.Add(footer, 0, 2);
            form.Controls.Add(root);
            LoadSampleItems(repoRoot, sampleCombo);

            double mainSplitRatio = 0.38;
            double resultSplitRatio = 0.44;
            bool adjustingSplitters = false;

            form.Shown += delegate
            {
                adjustingSplitters = true;
                ApplySafeSplitterDistance(split, mainSplitRatio, 360, 520);
                ApplySafeSplitterDistance(resultSplit, resultSplitRatio, 140, 220);
                adjustingSplitters = false;
                if (string.IsNullOrWhiteSpace(inputBox.Text) && sampleCombo.Items.Count > 0)
                {
                    LoadSelectedSample(sampleCombo, inputBox, outputBox, repoRoot);
                }
            };

            form.SizeChanged += delegate
            {
                if (form.WindowState == FormWindowState.Minimized) return;
                adjustingSplitters = true;
                ApplySafeSplitterDistance(split, mainSplitRatio, 360, 520);
                ApplySafeSplitterDistance(resultSplit, resultSplitRatio, 140, 220);
                adjustingSplitters = false;
            };

            split.SplitterMoved += delegate
            {
                if (!adjustingSplitters) mainSplitRatio = GetSplitterRatio(split);
            };

            resultSplit.SplitterMoved += delegate
            {
                if (!adjustingSplitters) resultSplitRatio = GetSplitterRatio(resultSplit);
            };

            System.Windows.Forms.Timer liveTimer = new System.Windows.Forms.Timer();
            liveTimer.Interval = 900;

            liveTimer.Tick += delegate
            {
                liveTimer.Stop();
                if (!liveBox.Checked) return;
                string outputDir = Path.Combine(repoRoot, "output", "live-preview");
                StartBuild(form, repoRoot, pastePath, inputBox.Text, outputDir, true, false, circuitList, previewBrowser, logBox, statusLabel, generateButton, renderNowButton);
            };

            inputBox.TextChanged += delegate
            {
                if (!liveBox.Checked) return;
                liveTimer.Stop();
                liveTimer.Start();
                statusLabel.Text = "编辑中，稍后自动更新预览";
            };

            liveBox.CheckedChanged += delegate
            {
                liveTimer.Stop();
                if (liveBox.Checked)
                {
                    string outputDir = Path.Combine(repoRoot, "output", "live-preview");
                    StartBuild(form, repoRoot, pastePath, inputBox.Text, outputDir, true, true, circuitList, previewBrowser, logBox, statusLabel, generateButton, renderNowButton);
                }
                else
                {
                    statusLabel.Text = "实时预览已暂停";
                }
            };

            circuitList.SelectedIndexChanged += delegate
            {
                PreviewItem item = circuitList.SelectedItem as PreviewItem;
                if (item != null && File.Exists(item.SvgPath))
                {
                    ShowSvgPreview(previewBrowser, item.SvgPath);
                    statusLabel.Text = "正在预览：" + item.CircuitId;
                }
            };

            generateButton.Click += delegate
            {
                liveTimer.Stop();
                string outputDir = NormalizeOutputDir(repoRoot, outputBox.Text, "gokottaelec");
                StartBuild(form, repoRoot, pastePath, inputBox.Text, outputDir, false, true, circuitList, previewBrowser, logBox, statusLabel, generateButton, renderNowButton);
            };

            renderNowButton.Click += delegate
            {
                liveTimer.Stop();
                string outputDir = Path.Combine(repoRoot, "output", "live-preview");
                StartBuild(form, repoRoot, pastePath, inputBox.Text, outputDir, true, true, circuitList, previewBrowser, logBox, statusLabel, generateButton, renderNowButton);
            };

            loadButton.Click += delegate
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "CNL 或文本文件 (*.cnl;*.txt)|*.cnl;*.txt|所有文件 (*.*)|*.*";
                if (dialog.ShowDialog(form) == DialogResult.OK)
                {
                    inputBox.Text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                    outputBox.Text = Path.Combine(repoRoot, "output", Path.GetFileNameWithoutExtension(dialog.FileName));
                }
            };

            copyBasicHandoffButton.Click += delegate
            {
                CopyLlmHandoffToClipboard(form, repoRoot, false, statusLabel);
            };

            copyFullHandoffButton.Click += delegate
            {
                CopyLlmHandoffToClipboard(form, repoRoot, true, statusLabel);
            };

            loadSampleButton.Click += delegate
            {
                LoadSelectedSample(sampleCombo, inputBox, outputBox, repoRoot);
            };

            sampleCombo.SelectedIndexChanged += delegate
            {
                if (sampleCombo.Focused) LoadSelectedSample(sampleCombo, inputBox, outputBox, repoRoot);
            };

            browseButton.Click += delegate
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "选择输出目录";
                string current = NormalizeOutputDir(repoRoot, outputBox.Text, "gokottaelec");
                if (Directory.Exists(current)) dialog.SelectedPath = current;
                if (dialog.ShowDialog(form) == DialogResult.OK)
                {
                    outputBox.Text = dialog.SelectedPath;
                }
            };

            openButton.Click += delegate
            {
                string preferred = !string.IsNullOrWhiteSpace(_lastFinalOutputDir)
                    ? _lastFinalOutputDir
                    : NormalizeOutputDir(repoRoot, outputBox.Text, "gokottaelec");
                string fallback = Path.Combine(repoRoot, "output", "live-preview");
                string dir = Directory.Exists(preferred) ? preferred : fallback;
                if (Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", Quote(dir));
                }
                else
                {
                    MessageBox.Show("输出目录还不存在。", AppName);
                }
            };

            return form;
        }

        private static Panel BuildHeader(string repoRoot)
        {
            Panel header = new BrandHeaderPanel();
            header.Dock = DockStyle.Fill;
            header.BackColor = ShellBlue;

            PictureBox logo = new PictureBox();
            logo.Left = 12;
            logo.Top = 9;
            logo.Width = 52;
            logo.Height = 52;
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Image = LoadBrandBitmap(repoRoot, 128);
            logo.BackColor = Color.Transparent;

            Label title = new Label();
            title.Text = AppName + " " + AppVersion;
            title.Left = 78;
            title.Top = 9;
            title.Width = 360;
            title.Height = 30;
            title.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            title.ForeColor = BrandNavy;
            title.BackColor = Color.Transparent;

            Label subtitle = new Label();
            subtitle.Text = "受控自然语言转脚本化电路原理图";
            subtitle.Left = 80;
            subtitle.Top = 42;
            subtitle.Width = 560;
            subtitle.Height = 20;
            subtitle.Font = new Font("Segoe UI", 9);
            subtitle.ForeColor = MutedBlue;
            subtitle.BackColor = Color.Transparent;

            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            return header;
        }

        private static Button BuildButton(string text)
        {
            return BuildButton(text, false);
        }

        private static Button BuildButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 108;
            button.Height = 32;
            button.Margin = new Padding(4, 0, 0, 0);
            button.Font = new Font("Segoe UI", 9);
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.BackColor = primary ? BrandBlue : SurfaceWhite;
            button.ForeColor = primary ? Color.White : TextBlue;
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(12, 102, 186) : BorderBlue;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(11, 97, 180) : Color.FromArgb(235, 248, 255);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(8, 75, 142) : Color.FromArgb(217, 241, 253);
            return button;
        }

        private static void LoadSampleItems(string repoRoot, ComboBox sampleCombo)
        {
            sampleCombo.Items.Clear();
            string samplesDir = Path.Combine(repoRoot, "samples");
            if (!Directory.Exists(samplesDir))
            {
                sampleCombo.Enabled = false;
                return;
            }

            string[] files = Directory.GetFiles(samplesDir, "Sample-*.txt");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                SampleItem item = new SampleItem();
                item.Title = SampleTitle(file);
                item.Path = file;
                sampleCombo.Items.Add(item);
            }

            sampleCombo.Enabled = sampleCombo.Items.Count > 0;
            if (sampleCombo.Items.Count > 0) sampleCombo.SelectedIndex = 0;
        }

        private static string SampleTitle(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.IndexOf("voltage-divider", StringComparison.OrdinalIgnoreCase) >= 0) return "01 电阻分压";
            if (name.IndexOf("npn-low-side-switch", StringComparison.OrdinalIgnoreCase) >= 0) return "02 NPN 低边 LED 开关";
            if (name.IndexOf("pnp-high-side-switch", StringComparison.OrdinalIgnoreCase) >= 0) return "03 PNP 高边 LED 开关";
            if (name.IndexOf("cmos-inverter", StringComparison.OrdinalIgnoreCase) >= 0) return "04 NMOS + PMOS CMOS 反相器";
            if (name.IndexOf("opamp-noninverting", StringComparison.OrdinalIgnoreCase) >= 0) return "05 运放同相放大器";
            return name;
        }

        private static void LoadSelectedSample(ComboBox sampleCombo, TextBox inputBox, TextBox outputBox, string repoRoot)
        {
            SampleItem item = sampleCombo.SelectedItem as SampleItem;
            if (item == null)
            {
                MessageBox.Show("没有可载入的 Sample。请确认 samples 目录存在。", AppName);
                return;
            }
            if (!File.Exists(item.Path))
            {
                MessageBox.Show("Sample 文件不存在：\r\n" + item.Path, AppName);
                return;
            }

            inputBox.Text = File.ReadAllText(item.Path, Encoding.UTF8);
            outputBox.Text = Path.Combine(repoRoot, "output", Path.GetFileNameWithoutExtension(item.Path));
        }

        private static void CopyLlmHandoffToClipboard(Form form, string repoRoot, bool full, Label statusLabel)
        {
            try
            {
                string markdown = BuildLlmHandoffMarkdown(repoRoot, full);
                Clipboard.SetText(markdown, TextDataFormat.UnicodeText);
                string mode = full ? "完整 LLM 对接 Markdown" : "基础 LLM 对接 Markdown";
                statusLabel.Text = "已复制：" + mode;
                MessageBox.Show(form, mode + " 已复制到剪贴板。\r\n\r\n可直接粘贴给其他 LLM。", AppName);
            }
            catch (Exception ex)
            {
                statusLabel.Text = "LLM 对接复制失败";
                MessageBox.Show(form, "复制 LLM 对接 Markdown 失败。\r\n\r\n" + ex.Message, AppName);
            }
        }

        private static string BuildLlmHandoffMarkdown(string repoRoot, bool full)
        {
            StringBuilder markdown = new StringBuilder();
            string title = full ? "GokottaElec LLM 完整对接包" : "GokottaElec LLM 基础对接包";
            markdown.AppendLine("# " + title);
            markdown.AppendLine();
            markdown.AppendLine("- 软件版本：" + AppVersion);
            markdown.AppendLine("- 目标：让其他 LLM 严格输出 GokottaElec 可解析、可 ERC 检查、可脚本渲染的受控自然语言电路描述。");
            markdown.AppendLine("- 使用方式：把本文完整粘贴给目标 LLM，并要求它只按契约输出电路 CNL。");
            markdown.AppendLine();

            if (full)
            {
                markdown.AppendLine("## 总要求");
                markdown.AppendLine();
                markdown.AppendLine("请你作为电路设计与 CNL 输出助手，严格遵守下面所有文件定义的格式、器件端子、网络规则、边界条件和示例风格。");
                markdown.AppendLine("输出时不要自由发挥格式，不要省略网络、器件、连接、约束；无法确定时必须显式给出未连接原因或诊断说明。");
                markdown.AppendLine();
            }
            else
            {
                markdown.AppendLine("## 基础要求");
                markdown.AppendLine();
                markdown.AppendLine("请你严格遵守系统提示词、CNL 输出契约和输出模板。优先保证格式可解析、端子名准确、网络连接明确。");
                markdown.AppendLine();
            }

            HandoffFile[] files = full ? FullHandoffFiles() : BasicHandoffFiles();
            foreach (HandoffFile file in files)
            {
                AppendFileAsMarkdown(markdown, repoRoot, file);
            }

            return markdown.ToString();
        }

        private static HandoffFile[] BasicHandoffFiles()
        {
            return new HandoffFile[]
            {
                new HandoffFile("llm-handoff\\README_先读_给其他LLM的文件说明.md", "文件说明", "markdown"),
                new HandoffFile("llm-handoff\\01_必需_系统提示词_直接复制给LLM.txt", "必需：系统提示词", "text"),
                new HandoffFile("llm-handoff\\02_必需_CNL输出契约_必须遵守.md", "必需：CNL 输出契约", "markdown"),
                new HandoffFile("llm-handoff\\03_可选增强_输出模板_让LLM套用.txt", "推荐：输出模板", "text")
            };
        }

        private static HandoffFile[] FullHandoffFiles()
        {
            return new HandoffFile[]
            {
                new HandoffFile("llm-handoff\\README_先读_给其他LLM的文件说明.md", "文件说明", "markdown"),
                new HandoffFile("llm-handoff\\01_必需_系统提示词_直接复制给LLM.txt", "必需：系统提示词", "text"),
                new HandoffFile("llm-handoff\\02_必需_CNL输出契约_必须遵守.md", "必需：CNL 输出契约", "markdown"),
                new HandoffFile("llm-handoff\\03_可选增强_输出模板_让LLM套用.txt", "推荐：输出模板", "text"),
                new HandoffFile("llm-handoff\\11_可选增强_完整器件库_端子和边界条件.json", "完整器件库：端子和边界条件", "json"),
                new HandoffFile("llm-handoff\\12_可选增强_型号封装引脚库_PinMap.json", "型号封装引脚库 PinMap", "json"),
                new HandoffFile("schema\\circuit-ir.schema.json", "IR Schema", "json"),
                new HandoffFile("docs\\circuit-cnl-v0.1.md", "CNL 语法说明", "markdown"),
                new HandoffFile("docs\\llm-cnl-contract-v0.1.md", "LLM CNL 契约", "markdown"),
                new HandoffFile("docs\\erc-rules-v0.1.md", "ERC 规则", "markdown"),
                new HandoffFile("docs\\component-library-notes-v0.1.md", "器件库说明", "markdown"),
                new HandoffFile("samples\\Sample-01-voltage-divider.txt", "Sample 01：电阻分压", "text"),
                new HandoffFile("samples\\Sample-02-npn-low-side-switch.txt", "Sample 02：NPN 低边 LED 开关", "text"),
                new HandoffFile("samples\\Sample-03-pnp-high-side-switch.txt", "Sample 03：PNP 高边 LED 开关", "text"),
                new HandoffFile("samples\\Sample-04-cmos-inverter-nmos-pmos.txt", "Sample 04：NMOS + PMOS CMOS 反相器", "text"),
                new HandoffFile("samples\\Sample-05-opamp-noninverting-amplifier.txt", "Sample 05：运放同相放大器", "text")
            };
        }

        private static void AppendFileAsMarkdown(StringBuilder markdown, string repoRoot, HandoffFile file)
        {
            string path = Path.Combine(repoRoot, file.RelativePath);
            markdown.AppendLine("## " + file.Title);
            markdown.AppendLine();
            markdown.AppendLine("来源：`" + file.RelativePath.Replace("\\", "/") + "`");
            markdown.AppendLine();
            if (!File.Exists(path))
            {
                markdown.AppendLine("> 文件不存在：" + file.RelativePath);
                markdown.AppendLine();
                return;
            }

            string content = File.ReadAllText(path, Encoding.UTF8);
            markdown.AppendLine("````" + file.Fence);
            markdown.Append(content);
            if (!content.EndsWith("\n", StringComparison.Ordinal)) markdown.AppendLine();
            markdown.AppendLine("````");
            markdown.AppendLine();
        }

        private static void ApplySafeSplitterDistance(SplitContainer split, double ratio)
        {
            ApplySafeSplitterDistance(split, ratio, 120, 160);
        }

        private static void ApplySafeSplitterDistance(SplitContainer split, double ratio, int minFirst, int minSecond)
        {
            try
            {
                int available = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
                if (available < 80) return;
                int min = minFirst;
                int max = available - minSecond;
                if (max <= min)
                {
                    min = Math.Max(80, available / 3);
                    max = Math.Max(min, available - 120);
                }
                int distance = (int)(available * ratio);
                if (distance < min) distance = min;
                if (distance > max) distance = max;
                split.SplitterDistance = distance;
            }
            catch
            {
            }
        }

        private static double GetSplitterRatio(SplitContainer split)
        {
            int available = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
            if (available <= 0) return 0.5;
            double ratio = (double)split.SplitterDistance / available;
            if (ratio < 0.18) return 0.18;
            if (ratio > 0.78) return 0.78;
            return ratio;
        }

        private static void StartBuild(
            Form form,
            string repoRoot,
            string pastePath,
            string text,
            string outputDir,
            bool isLive,
            bool force,
            ListBox circuitList,
            WebBrowser previewBrowser,
            TextBox logBox,
            Label statusLabel,
            Button generateButton,
            Button renderNowButton)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                circuitList.Items.Clear();
                logBox.Clear();
                SetPreviewHtml(previewBrowser, "请在左侧粘贴 LLM 输出的 CNL 文本，软件会自动渲染预览。");
                statusLabel.Text = "就绪";
                return;
            }

            if (isLive && !force && text == _lastLiveText)
            {
                statusLabel.Text = "预览已经是最新";
                return;
            }

            if (isLive) _lastLiveText = text;

            int serial = Interlocked.Increment(ref _renderSerial);
            string textSnapshot = text;
            string mode = isLive ? "实时预览" : "生成文件";
            statusLabel.Text = mode + "执行中...";
            logBox.Text = mode + "执行中...\r\n";
            generateButton.Enabled = false;
            renderNowButton.Enabled = false;

            ThreadPool.QueueUserWorkItem(delegate
            {
                BuildResult result = RunPasteBuild(repoRoot, pastePath, textSnapshot, outputDir);
                if (form.IsDisposed) return;

                form.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    generateButton.Enabled = true;
                    renderNowButton.Enabled = true;
                    if (serial != _renderSerial)
                    {
                        statusLabel.Text = "已有更新的渲染结果";
                        return;
                    }

                    logBox.Text = result.Log;
                    if (!isLive && result.ExitCode == 0) _lastFinalOutputDir = result.OutputDir;
                    LoadPreviewList(repoRoot, result.OutputDir, circuitList, previewBrowser, statusLabel);
                    if (result.ExitCode == 0)
                    {
                        statusLabel.Text = isLive ? "实时预览已完成" : "已生成：" + result.OutputDir;
                    }
                    else
                    {
                        statusLabel.Text = mode + "失败，请查看构建日志。";
                    }
                });
            });
        }

        private static BuildResult RunPasteBuild(string repoRoot, string pastePath, string text, string outputDir)
        {
            BuildResult result = new BuildResult();
            result.ExitCode = 1;
            result.OutputDir = outputDir;

            StringBuilder log = new StringBuilder();
            try
            {
                Directory.CreateDirectory(outputDir);
                string inputPath = Path.Combine(outputDir, "pasted-llm-output.txt");
                File.WriteAllText(inputPath, text, Encoding.UTF8);

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = FindNode();
                psi.Arguments = Quote(pastePath) + " " + Quote(inputPath) + " " + Quote(outputDir);
                psi.WorkingDirectory = repoRoot;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                log.AppendLine("输入：" + inputPath);
                log.AppendLine("输出：" + outputDir);
                log.AppendLine();

                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        log.AppendLine("无法启动 Node.js。");
                        result.Log = log.ToString();
                        return result;
                    }
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (!string.IsNullOrWhiteSpace(stdout)) log.AppendLine(stdout.TrimEnd());
                    if (!string.IsNullOrWhiteSpace(stderr)) log.AppendLine(stderr.TrimEnd());
                    log.AppendLine();
                    log.AppendLine("ExitCode=" + process.ExitCode);
                    result.ExitCode = process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                log.AppendLine(ex.GetType().Name + ": " + ex.Message);
            }

            result.Log = log.ToString().Replace("\n", "\r\n");
            return result;
        }

        private static void LoadPreviewList(string repoRoot, string outputDir, ListBox circuitList, WebBrowser previewBrowser, Label statusLabel)
        {
            circuitList.Items.Clear();
            string summaryPath = Path.Combine(outputDir, "summary.txt");
            if (!File.Exists(summaryPath))
            {
                SetPreviewHtml(previewBrowser, "没有生成 summary.txt，无法显示预览。");
                statusLabel.Text = "没有可用预览";
                return;
            }

            string[] lines = File.ReadAllLines(summaryPath, Encoding.UTF8);
            foreach (string line in lines)
            {
                if (!line.StartsWith("OK:", StringComparison.OrdinalIgnoreCase)) continue;
                string circuitId = ExtractCircuitId(line);
                string svgPath = ExtractAfter(line, "SVG=");
                if (string.IsNullOrWhiteSpace(circuitId) || string.IsNullOrWhiteSpace(svgPath)) continue;

                svgPath = svgPath.Trim();
                if (!Path.IsPathRooted(svgPath))
                {
                    string repoRelative = Path.GetFullPath(Path.Combine(repoRoot, svgPath));
                    string outputRelative = Path.GetFullPath(Path.Combine(outputDir, svgPath));
                    svgPath = File.Exists(repoRelative) ? repoRelative : outputRelative;
                }
                if (!File.Exists(svgPath)) continue;

                PreviewItem item = new PreviewItem();
                item.CircuitId = circuitId.Trim();
                item.SvgPath = svgPath;
                circuitList.Items.Add(item);
            }

            if (circuitList.Items.Count > 0)
            {
                circuitList.SelectedIndex = 0;
                statusLabel.Text = "预览已就绪：" + circuitList.Items.Count + " 个电路";
            }
            else
            {
                SetPreviewHtml(previewBrowser, "没有生成有效 SVG。请查看构建日志中的解析或 ERC 信息。");
                statusLabel.Text = "没有有效电路预览";
            }
        }

        private static string ExtractCircuitId(string line)
        {
            int start = line.IndexOf("OK:", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            start += 3;
            int end = line.IndexOf(" DIR=", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) end = line.IndexOf(" ->", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) end = line.Length;
            return line.Substring(start, end - start).Trim();
        }

        private static string ExtractAfter(string value, string marker)
        {
            int start = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            return value.Substring(start + marker.Length).Trim();
        }

        private static void SetPreviewHtml(WebBrowser previewBrowser, string message)
        {
            previewBrowser.DocumentText =
                "<!doctype html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" /></head>" +
                "<body style=\"font-family:Segoe UI,Arial,sans-serif;margin:0;color:#123657;background:#edf7fd\">" +
                "<div style=\"height:100%;min-height:420px;display:flex;align-items:center;justify-content:center;padding:28px;box-sizing:border-box\">" +
                "<div style=\"max-width:520px;text-align:center;border:1px solid #b7d8ee;background:#ffffff;padding:30px 34px;box-sizing:border-box\">" +
                "<div style=\"width:74px;height:74px;margin:0 auto 18px;border-radius:18px;background:#0d64b8;position:relative;overflow:hidden\">" +
                "<div style=\"position:absolute;left:-10px;top:27px;width:58px;height:10px;background:#45bff3\"></div>" +
                "<div style=\"position:absolute;left:23px;top:16px;width:42px;height:42px;border:8px solid #ffffff;border-left-color:#45bff3;border-radius:50%\"></div>" +
                "<div style=\"position:absolute;right:12px;top:16px;width:14px;height:14px;border:6px solid #ffffff;border-radius:50%;background:#1fa9ed\"></div>" +
                "</div>" +
                "<div style=\"font-size:22px;font-weight:700;margin-bottom:8px;color:#0d4885\">GokottaElec " + AppVersion + " 预览</div>" +
                "<div style=\"font-size:13px;line-height:1.6;color:#526f85\">" + HtmlEscape(message) + "</div>" +
                "</div></div>" +
                "</body></html>";
        }

        private static void ShowSvgPreview(WebBrowser previewBrowser, string svgPath)
        {
            try
            {
                string svg = File.ReadAllText(svgPath, Encoding.UTF8);
                previewBrowser.DocumentText =
                    "<!doctype html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />" +
                    "<style>" +
                    "html,body{width:100%;height:100%;margin:0;background:#edf7fd;overflow:hidden;}" +
                    ".frame{position:absolute;left:0;top:0;right:0;bottom:0;padding:12px;box-sizing:border-box;}" +
                    ".surface{width:100%;height:100%;background:#fff;border:1px solid #b7d8ee;box-sizing:border-box;overflow:auto;text-align:center;}" +
                    ".surface:before{content:'';display:inline-block;height:100%;vertical-align:middle;}" +
                    "svg{max-width:100%;max-height:100%;width:auto;height:auto;vertical-align:middle;display:inline-block;}" +
                    "</style></head><body>" +
                    "<div class=\"frame\"><div class=\"surface\">" + svg + "</div></div>" +
                    "</body></html>";
            }
            catch (Exception ex)
            {
                SetPreviewHtml(previewBrowser, "SVG preview failed: " + ex.Message);
            }
        }

        private static string HtmlEscape(string value)
        {
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string NormalizeOutputDir(string repoRoot, string value, string fallbackName)
        {
            string trimmed = string.IsNullOrWhiteSpace(value) ? "" : value.Trim().Trim('"');
            if (trimmed.Length == 0) return Path.Combine(repoRoot, "output", fallbackName);
            if (Path.IsPathRooted(trimmed)) return trimmed;
            return Path.GetFullPath(Path.Combine(repoRoot, trimmed));
        }

        private static Bitmap LoadBrandBitmap(string repoRoot, int size)
        {
            string pngPath = Path.Combine(repoRoot, "launcher", "GokottaElec.png");
            if (File.Exists(pngPath))
            {
                try
                {
                    using (Image source = Image.FromFile(pngPath))
                    {
                        Bitmap bitmap = new Bitmap(size, size);
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.Clear(Color.Transparent);
                            graphics.DrawImage(source, new Rectangle(0, 0, size, size));
                        }
                        return bitmap;
                    }
                }
                catch
                {
                }
            }

            if (_appIcon != null)
            {
                try
                {
                    using (Bitmap iconBitmap = _appIcon.ToBitmap())
                    {
                        Bitmap bitmap = new Bitmap(size, size);
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.Clear(Color.Transparent);
                            graphics.DrawImage(iconBitmap, new Rectangle(0, 0, size, size));
                        }
                        return bitmap;
                    }
                }
                catch
                {
                }
            }

            return CreateLogoBitmap(size);
        }

        private static Bitmap CreateLogoBitmap(int size)
        {
            Bitmap bitmap = new Bitmap(size, size);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                Rectangle bounds = new Rectangle(3, 3, size - 6, size - 6);
                using (GraphicsPath round = RoundedRect(bounds, Math.Max(10, size / 5)))
                using (LinearGradientBrush bg = new LinearGradientBrush(bounds, Color.FromArgb(9, 64, 139), Color.FromArgb(20, 125, 219), 45f))
                using (Pen whiteTrace = new Pen(Color.White, Math.Max(5, size / 11)))
                using (Pen cyanTrace = new Pen(Color.FromArgb(80, 190, 241), Math.Max(4, size / 13)))
                using (SolidBrush cyan = new SolidBrush(Color.FromArgb(31, 169, 237)))
                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    whiteTrace.StartCap = LineCap.Round;
                    whiteTrace.EndCap = LineCap.Round;
                    cyanTrace.StartCap = LineCap.Round;
                    cyanTrace.EndCap = LineCap.Round;
                    graphics.FillPath(bg, round);

                    Rectangle arc = new Rectangle(size / 5, size / 5, size * 3 / 5, size * 3 / 5);
                    graphics.DrawArc(cyanTrace, arc, 180, 225);
                    graphics.DrawArc(whiteTrace, arc, 202, 245);
                    graphics.DrawLine(whiteTrace, size / 2, size / 2, size * 4 / 5, size / 2);
                    graphics.DrawLine(cyanTrace, size / 7, size / 2, size * 2 / 5, size / 2);
                    DrawLogoNode(graphics, white, cyan, size * 7 / 10, size / 4, Math.Max(10, size / 5));
                    DrawLogoNode(graphics, white, cyan, size * 2 / 5, size / 2, Math.Max(10, size / 6));
                    DrawLogoNode(graphics, white, cyan, size * 2 / 3, size * 2 / 3, Math.Max(9, size / 7));
                }
            }
            return bitmap;
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawLogoNode(Graphics graphics, Brush outer, Brush inner, int x, int y, int size)
        {
            int outerSize = size;
            int innerSize = Math.Max(4, size / 2);
            graphics.FillEllipse(outer, x - outerSize / 2, y - outerSize / 2, outerSize, outerSize);
            graphics.FillEllipse(inner, x - innerSize / 2, y - innerSize / 2, innerSize, innerSize);
        }

        private static Icon LoadOrCreateIcon(string repoRoot)
        {
            string[] iconPaths =
            {
                Path.Combine(repoRoot, "launcher", "GokottaElecApp.ico"),
                Path.Combine(repoRoot, "launcher", "GokottaElec.ico")
            };

            foreach (string iconPath in iconPaths)
            {
                if (!File.Exists(iconPath)) continue;
                try
                {
                    return new Icon(iconPath);
                }
                catch
                {
                }
            }

            using (Bitmap bitmap = CreateLogoBitmap(64))
            {
                Icon icon = Icon.FromHandle(bitmap.GetHicon());
                return (Icon)icon.Clone();
            }
        }

        private static void InstallExceptionLogging(string repoRoot)
        {
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs args)
            {
                WriteStartupLog(repoRoot, FormatException(args.Exception));
                try
                {
                    MessageBox.Show(args.Exception.Message + "\r\n\r\n请查看 output\\gokottaelec-startup.log", AppName);
                }
                catch
                {
                }
            };

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs args)
            {
                Exception ex = args.ExceptionObject as Exception;
                WriteStartupLog(repoRoot, ex != null ? FormatException(ex) : "Unhandled exception: " + args.ExceptionObject);
            };
        }

        private static void WriteStartupLog(string repoRoot, string message)
        {
            try
            {
                string outputDir = Path.Combine(repoRoot, "output");
                Directory.CreateDirectory(outputDir);
                string logPath = Path.Combine(outputDir, "gokottaelec-startup.log");
                File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + "\r\n", Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string FormatException(Exception ex)
        {
            return ex.GetType().FullName + ": " + ex.Message + "\r\n" + ex.StackTrace;
        }

        private static int RunCommandLine(string repoRoot, string cliPath, string[] args)
        {
            string first = args[0];
            string ext = Path.GetExtension(first);
            string command;
            if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)) command = "paste";
            else if (ext.Equals(".cnl", StringComparison.OrdinalIgnoreCase)) command = "build";
            else command = "";

            string allArgs = Quote(cliPath) + " ";
            if (command.Length > 0) allArgs += command + " ";
            for (int i = 0; i < args.Length; i++) allArgs += Quote(args[i]) + " ";

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = FindNode();
            psi.Arguments = allArgs.Trim();
            psi.WorkingDirectory = repoRoot;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = false;
            psi.RedirectStandardError = false;

            using (Process process = Process.Start(psi))
            {
                if (process == null) return 1;
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private static string FindRepoRoot(string exePath)
        {
            DirectoryInfo directory = Directory.Exists(exePath)
                ? new DirectoryInfo(exePath)
                : new DirectoryInfo(Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory());
            for (DirectoryInfo current = directory; current != null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "scripts", "elec-cli.mjs")) &&
                    File.Exists(Path.Combine(current.FullName, "package.json")))
                {
                    return current.FullName;
                }
            }
            return Directory.GetCurrentDirectory();
        }

        private static string FindNode()
        {
            string[] candidates = { "node.exe", "node" };
            foreach (string candidate in candidates)
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = candidate;
                    startInfo.Arguments = "--version";
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.CreateNoWindow = true;
                    using (Process process = Process.Start(startInfo))
                    {
                        if (process == null) continue;
                        process.WaitForExit(3000);
                        if (process.ExitCode == 0) return candidate;
                    }
                }
                catch
                {
                }
            }
            return "node.exe";
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
