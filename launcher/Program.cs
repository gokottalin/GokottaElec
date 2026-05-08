using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CircuitLangLauncher
{
    internal static class Program
    {
        private const string AppName = "GokottaElec";
        private const string AppVersion = "V1.1";
        private static string _lastFinalOutputDir = "";
        private static string _lastLiveText = "";
        private static int _renderSerial = 0;
        private static Icon _appIcon;

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

        [STAThread]
        private static int Main(string[] args)
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
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
            form.Width = 1440;
            form.Height = 900;
            form.MinimumSize = new Size(1050, 680);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.BackColor = Color.FromArgb(245, 247, 250);

            _appIcon = LoadOrCreateIcon(repoRoot);
            if (_appIcon != null) form.Icon = _appIcon;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.Padding = new Padding(12);
            root.BackColor = Color.FromArgb(245, 247, 250);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

            Panel header = BuildHeader();

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            split.SplitterWidth = 7;
            split.BackColor = Color.FromArgb(222, 227, 234);

            TableLayoutPanel leftLayout = new TableLayoutPanel();
            leftLayout.Dock = DockStyle.Fill;
            leftLayout.ColumnCount = 1;
            leftLayout.RowCount = 2;
            leftLayout.Padding = new Padding(0, 8, 8, 0);
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
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
            inputLabel.ForeColor = Color.FromArgb(38, 48, 58);

            ComboBox sampleCombo = new ComboBox();
            sampleCombo.Dock = DockStyle.Fill;
            sampleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            sampleCombo.Font = new Font("Segoe UI", 9);

            Button loadSampleButton = BuildButton("载入");
            loadSampleButton.Dock = DockStyle.Fill;
            loadSampleButton.Width = 84;

            TextBox inputBox = new TextBox();
            inputBox.Multiline = true;
            inputBox.ScrollBars = ScrollBars.Both;
            inputBox.AcceptsReturn = true;
            inputBox.AcceptsTab = true;
            inputBox.Font = new Font("Consolas", 10);
            inputBox.WordWrap = false;
            inputBox.Dock = DockStyle.Fill;
            inputBox.BorderStyle = BorderStyle.FixedSingle;

            inputBar.Controls.Add(inputLabel, 0, 0);
            inputBar.Controls.Add(sampleCombo, 1, 0);
            inputBar.Controls.Add(loadSampleButton, 2, 0);

            leftLayout.Controls.Add(inputBar, 0, 0);
            leftLayout.Controls.Add(inputBox, 0, 1);
            split.Panel1.Controls.Add(leftLayout);

            TableLayoutPanel rightLayout = new TableLayoutPanel();
            rightLayout.Dock = DockStyle.Fill;
            rightLayout.ColumnCount = 1;
            rightLayout.RowCount = 3;
            rightLayout.Padding = new Padding(8, 8, 0, 0);
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));

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
            previewLabel.ForeColor = Color.FromArgb(38, 48, 58);

            Label statusLabel = new Label();
            statusLabel.Text = "就绪";
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleRight;
            statusLabel.Font = new Font("Segoe UI", 9);
            statusLabel.ForeColor = Color.FromArgb(74, 92, 106);

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

            ListBox circuitList = new ListBox();
            circuitList.Dock = DockStyle.Fill;
            circuitList.Font = new Font("Segoe UI", 9);
            listGroup.Controls.Add(circuitList);

            GroupBox logGroup = new GroupBox();
            logGroup.Text = "构建日志";
            logGroup.Dock = DockStyle.Fill;
            logGroup.Font = new Font("Segoe UI", 9);

            TextBox logBox = new TextBox();
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.ReadOnly = true;
            logBox.Font = new Font("Consolas", 9);
            logBox.Dock = DockStyle.Fill;
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
            footer.Padding = new Padding(0, 10, 0, 0);
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 620));

            Label outputLabel = new Label();
            outputLabel.Text = "输出目录";
            outputLabel.Dock = DockStyle.Fill;
            outputLabel.TextAlign = ContentAlignment.MiddleLeft;
            outputLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            TextBox outputBox = new TextBox();
            outputBox.Dock = DockStyle.Fill;
            outputBox.Font = new Font("Segoe UI", 9);
            outputBox.Text = Path.Combine(repoRoot, "output", "gokottaelec");

            Button browseButton = BuildButton("选择目录");

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = false;

            CheckBox liveBox = new CheckBox();
            liveBox.Text = "实时";
            liveBox.Checked = true;
            liveBox.Width = 64;
            liveBox.Height = 30;
            liveBox.TextAlign = ContentAlignment.MiddleLeft;

            Button renderNowButton = BuildButton("立即渲染");
            Button generateButton = BuildButton("生成文件");
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

            form.Shown += delegate
            {
                ApplySafeSplitterDistance(split, 0.40);
                ApplySafeSplitterDistance(resultSplit, 0.42);
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

        private static Panel BuildHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(245, 247, 250);

            PictureBox logo = new PictureBox();
            logo.Left = 0;
            logo.Top = 8;
            logo.Width = 52;
            logo.Height = 52;
            logo.SizeMode = PictureBoxSizeMode.StretchImage;
            logo.Image = CreateLogoBitmap(96);

            Label title = new Label();
            title.Text = AppName + " " + AppVersion;
            title.Left = 64;
            title.Top = 8;
            title.Width = 360;
            title.Height = 30;
            title.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(28, 41, 51);

            Label subtitle = new Label();
            subtitle.Text = "受控自然语言转脚本化电路原理图";
            subtitle.Left = 66;
            subtitle.Top = 40;
            subtitle.Width = 560;
            subtitle.Height = 20;
            subtitle.Font = new Font("Segoe UI", 9);
            subtitle.ForeColor = Color.FromArgb(85, 99, 110);

            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            return header;
        }

        private static Button BuildButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 108;
            button.Height = 30;
            button.Margin = new Padding(4, 0, 0, 0);
            button.Font = new Font("Segoe UI", 9);
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

        private static void ApplySafeSplitterDistance(SplitContainer split, double ratio)
        {
            try
            {
                int available = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
                if (available < 80) return;
                int min = 120;
                int max = available - 160;
                if (max <= min) return;
                int distance = (int)(available * ratio);
                if (distance < min) distance = min;
                if (distance > max) distance = max;
                split.SplitterDistance = distance;
            }
            catch
            {
            }
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

                form.BeginInvoke((MethodInvoker)delegate
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
                "<body style=\"font-family:Segoe UI,Arial,sans-serif;margin:28px;color:#333;background:#f8fafb\">" +
                "<div style=\"font-size:22px;font-weight:700;margin-bottom:8px;color:#1c2933\">GokottaElec V1.1 预览</div>" +
                "<div style=\"font-size:13px;line-height:1.5;color:#52616c\">" + HtmlEscape(message) + "</div>" +
                "</body></html>";
        }

        private static void ShowSvgPreview(WebBrowser previewBrowser, string svgPath)
        {
            string uri = new Uri(svgPath).AbsoluteUri;
            previewBrowser.DocumentText =
                "<!doctype html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />" +
                "<style>" +
                "html,body{width:100%;height:100%;margin:0;background:#eef2f5;overflow:hidden;}" +
                ".frame{position:absolute;left:0;top:0;right:0;bottom:0;padding:10px;box-sizing:border-box;}" +
                ".surface{width:100%;height:100%;background:#fff;border:1px solid #ccd6df;box-sizing:border-box;overflow:auto;text-align:center;}" +
                ".surface:before{content:'';display:inline-block;height:100%;vertical-align:middle;}" +
                "img{max-width:100%;max-height:100%;width:auto;height:auto;vertical-align:middle;}" +
                "</style></head><body>" +
                "<div class=\"frame\"><div class=\"surface\"><img src=\"" + HtmlEscape(uri) + "\" /></div></div>" +
                "</body></html>";
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

        private static Bitmap CreateLogoBitmap(int size)
        {
            Bitmap bitmap = new Bitmap(size, size);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                Rectangle bounds = new Rectangle(3, 3, size - 6, size - 6);
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(185, 225, 247)))
                using (SolidBrush accent = new SolidBrush(Color.FromArgb(73, 159, 211)))
                using (Pen trace = new Pen(Color.FromArgb(20, 88, 130), Math.Max(2, size / 18)))
                using (Pen accentPen = new Pen(Color.FromArgb(109, 190, 232), Math.Max(2, size / 16)))
                {
                    graphics.FillEllipse(bg, bounds);
                    graphics.DrawArc(accentPen, bounds, -35, 250);

                    int mid = size / 2;
                    graphics.DrawLine(trace, size / 5, mid, size / 2, mid);
                    graphics.DrawLine(trace, size / 2, mid, size * 3 / 4, size / 3);
                    graphics.DrawLine(trace, size / 2, mid, size * 3 / 4, size * 2 / 3);
                    graphics.FillEllipse(Brushes.White, size / 5 - 4, mid - 4, 8, 8);
                    graphics.FillEllipse(accent, size * 3 / 4 - 5, size / 3 - 5, 10, 10);
                    graphics.FillEllipse(accent, size * 3 / 4 - 5, size * 2 / 3 - 5, 10, 10);
                }

                using (Font font = new Font("Segoe UI", size / 3.0f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(18, 75, 117)))
                {
                    graphics.DrawString("G", font, brush, size * 0.30f, size * 0.22f);
                }
            }
            return bitmap;
        }

        private static Icon LoadOrCreateIcon(string repoRoot)
        {
            string iconPath = Path.Combine(repoRoot, "launcher", "GokottaElec.ico");
            if (File.Exists(iconPath))
            {
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
            DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory());
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
