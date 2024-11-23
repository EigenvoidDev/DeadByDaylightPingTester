using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace DeadByDaylightPingTester
{
    public partial class MainForm : Form
    {
        private string[] regions = {
            "Asia Pacific (Hong Kong)",
            "Asia Pacific (Tokyo)",
            "Asia Pacific (Seoul)",
            "Asia Pacific (Mumbai)",
            "Asia Pacific (Singapore)",
            "Asia Pacific (Sydney)",
            "Europe (Frankfurt)",
            "Europe (Ireland)",
            "South America (São Paulo)",
            "US East (North Virginia)",
            "US West (Oregon)"
        };
        private Dictionary<string, string> regionToEndpointMapping;
        private bool isDarkMode = false;

        private TableLayoutPanel tableLayoutPanel;
        private Label[] lblRegions;
        private TextBox[] txtPingValues;
        private Button[] btnPingIndividual;
        private Button btnPingAll;
        private Button btnToggleTheme;
        private CheckBox chkAutoPing;
        private ComboBox cmbRegionSelector;
        private ListBox lbPingLogger;

        private TabControl tabControl;
        private TabPage mainTabPage;
        private TabPage autoPingerTabPage;

        private System.Windows.Forms.Timer autoPingTimer;

        public MainForm()
        {
            try
            {
                InitializeComponent();
                InitializeMappings();
                SetupUI();
                LoadThemeSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing the form: {ex.Message}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeMappings()
        {
            regionToEndpointMapping = new Dictionary<string, string> {
                {
                    "Asia Pacific (Hong Kong)",
                    "ec2.ap-east-1.amazonaws.com"
                },
                {
                    "Asia Pacific (Tokyo)",
                    "gamelift.ap-northeast-1.amazonaws.com"
                },
                {
                    "Asia Pacific (Seoul)",
                    "gamelift.ap-northeast-2.amazonaws.com"
                },
                {
                    "Asia Pacific (Mumbai)",
                    "gamelift.ap-south-1.amazonaws.com"
                },
                {
                    "Asia Pacific (Singapore)",
                    "gamelift.ap-southeast-1.amazonaws.com"
                },
                {
                    "Asia Pacific (Sydney)",
                    "gamelift.ap-southeast-2.amazonaws.com"
                },
                {
                    "Europe (Frankfurt)",
                    "gamelift.eu-central-1.amazonaws.com"
                },
                {
                    "Europe (Ireland)",
                    "gamelift.eu-west-1.amazonaws.com"
                },
                {
                    "South America (São Paulo)",
                    "gamelift.sa-east-1.amazonaws.com"
                },
                {
                    "US East (North Virginia)",
                    "gamelift.us-east-1.amazonaws.com"
                },
                {
                    "US West (Oregon)",
                    "gamelift.us-west-2.amazonaws.com"
                }
            };
        }

        private void SetupUI()
        {
            this.Text = "Dead by Daylight Ping Tester";
            this.Size = new Size(1000, 675);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
            };

            mainTabPage = new TabPage("Main Tab");
            autoPingerTabPage = new TabPage("Auto Pinger");

            tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = regions.Length + 2,
                AutoSize = true,
                Padding = new Padding(10)
            };

            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

            tableLayoutPanel.Controls.Add(CreateCenteredLabel("Region", true), 0, 0);
            tableLayoutPanel.Controls.Add(CreateCenteredLabel("Ping (ms)", true), 1, 0);
            tableLayoutPanel.Controls.Add(CreateCenteredLabel("Test Ping", true), 2, 0);

            lblRegions = new Label[regions.Length];
            txtPingValues = new TextBox[regions.Length];
            btnPingIndividual = new Button[regions.Length];

            for (int i = 0; i < regions.Length; i++)
            {
                lblRegions[i] = CreateCenteredLabel(regions[i], false);
                tableLayoutPanel.Controls.Add(lblRegions[i], 0, i + 1);

                txtPingValues[i] = new TextBox
                {
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = HorizontalAlignment.Center,
                    ReadOnly = true,
                    Cursor = Cursors.Default,
                    Dock = DockStyle.Fill
                };
                tableLayoutPanel.Controls.Add(txtPingValues[i], 1, i + 1);

                btnPingIndividual[i] = new Button
                {
                    Text = "Test Ping",
                    Width = 100,
                    Height = 35,
                    Tag = i,
                    Anchor = AnchorStyles.None
                };
                btnPingIndividual[i].Click += BtnPingIndividual_Click;
                tableLayoutPanel.Controls.Add(btnPingIndividual[i], 2, i + 1);
            }

            btnPingAll = new Button
            {
                Text = "Ping All",
                Width = 180,
                Height = 35,
                Anchor = AnchorStyles.Top
            };
            btnPingAll.Click += BtnPingAll_Click;

            btnToggleTheme = new Button
            {
                Text = "Toggle Theme",
                Width = 180,
                Height = 35,
                Anchor = AnchorStyles.Top
            };
            btnToggleTheme.Click += BtnToggleTheme_Click;

            tableLayoutPanel.Controls.Add(btnPingAll, 1, regions.Length + 1);
            tableLayoutPanel.Controls.Add(btnToggleTheme, 1, regions.Length + 2);

            mainTabPage.Controls.Add(tableLayoutPanel);

            SetupAutoPingerUI();

            tabControl.TabPages.Add(mainTabPage);
            tabControl.TabPages.Add(autoPingerTabPage);

            this.Controls.Add(tabControl);
        }

        private void SetupAutoPingerUI()
        {
            TableLayoutPanel autoPingerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true
            };

            autoPingerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            autoPingerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67F));

            autoPingerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            autoPingerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            autoPingerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            autoPingerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            chkAutoPing = new CheckBox
            {
                Text = "Enable Auto Ping",
                AutoSize = true
            };
            chkAutoPing.CheckedChanged += ChkAutoPing_CheckedChanged;

            FlowLayoutPanel chkAutoPingPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(10),
                Anchor = AnchorStyles.None
            };
            chkAutoPingPanel.Controls.Add(chkAutoPing);

            cmbRegionSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                Width = 255
            };
            cmbRegionSelector.Items.AddRange(regions);
            cmbRegionSelector.SelectedIndex = 0;

            cmbRegionSelector.DrawItem += (s, e) => {
                e.DrawBackground();
                ComboBox cmb = (ComboBox)s;
                string itemText = cmb.GetItemText(cmb.Items[e.Index]);
                using (SolidBrush brush = new SolidBrush(e.ForeColor))
                {
                    SizeF stringSize = e.Graphics.MeasureString(itemText, cmb.Font);
                    e.Graphics.DrawString(itemText, cmb.Font, brush,
                        new PointF(e.Bounds.Left + (e.Bounds.Width - stringSize.Width) / 2, e.Bounds.Top + (e.Bounds.Height - stringSize.Height) / 2));
                }
                e.DrawFocusRectangle();
            };

            FlowLayoutPanel cmbRegionSelectorPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };
            cmbRegionSelectorPanel.Controls.Add(cmbRegionSelector);

            btnToggleTheme = new Button
            {
                Text = "Toggle Theme",
                Width = 180,
                Height = 35,
                Anchor = AnchorStyles.Top
            };
            btnToggleTheme.Click += BtnToggleTheme_Click;

            FlowLayoutPanel toggleThemeButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };
            toggleThemeButtonPanel.Controls.Add(btnToggleTheme);

            lbPingLogger = new ListBox
            {
                Dock = DockStyle.Fill,
                HorizontalScrollbar = true
            };

            autoPingerLayout.Controls.Add(chkAutoPingPanel, 0, 0);
            autoPingerLayout.Controls.Add(cmbRegionSelectorPanel, 0, 1);
            autoPingerLayout.Controls.Add(toggleThemeButtonPanel, 0, 2);
            autoPingerLayout.Controls.Add(lbPingLogger, 1, 0);

            autoPingerLayout.SetRowSpan(lbPingLogger, 4);

            autoPingerTabPage.Controls.Add(autoPingerLayout);

            autoPingTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            autoPingTimer.Tick += AutoPingTimer_Tick;
        }

        private Label CreateCenteredLabel(string text, bool bold)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10, bold ? FontStyle.Bold : FontStyle.Regular),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
        }

        private async void BtnPingIndividual_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int index = (int)button.Tag;
                if (index >= 0 && index < regions.Length)
                {
                    string region = regions[index];
                    string IPAddress = regionToEndpointMapping.ContainsKey(region) ? regionToEndpointMapping[region] : "Server IP address could not be found.";
                    try
                    {
                        txtPingValues[index].Text = "Pinging...";

                        Ping pingSender = new Ping();
                        PingReply reply = await pingSender.SendPingAsync(IPAddress, 1000);

                        if (reply.Status == IPStatus.Success)
                        {
                            txtPingValues[index].Text = $"{reply.RoundtripTime}";
                        }
                        else
                        {
                            txtPingValues[index].Text = $"Failed ({reply.Status})";
                        }
                    }
                    catch (Exception ex)
                    {
                        txtPingValues[index].Text = $"Error ({ex.Message})";
                    }
                }
            }
        }

        private async void BtnPingAll_Click(object sender, EventArgs e)
        {
            foreach (string region in regions)
            {
                string IPAddress = regionToEndpointMapping.ContainsKey(region) ? regionToEndpointMapping[region] : "Server IP address could not be found.";
                try
                {
                    int index = Array.IndexOf(regions, region);
                    txtPingValues[index].Text = "Pinging...";

                    Ping pingSender = new Ping();
                    PingReply reply = await pingSender.SendPingAsync(IPAddress, 1000);

                    if (reply.Status == IPStatus.Success)
                    {
                        txtPingValues[index].Text = $"{reply.RoundtripTime}";
                    }
                    else
                    {
                        txtPingValues[index].Text = $"Failed ({reply.Status})";
                    }
                }
                catch (Exception ex)
                {
                    txtPingValues[Array.IndexOf(regions, region)].Text = $"Error ({ex.Message})";
                }
            }
        }

        private void ChkAutoPing_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoPing.Checked)
            {
                autoPingTimer.Start();
            }
            else
            {
                autoPingTimer.Stop();
            }
        }

        private async void AutoPingTimer_Tick(object sender, EventArgs e)
        {
            string selectedRegion = cmbRegionSelector.SelectedItem.ToString();
            string IPAddress = regionToEndpointMapping[selectedRegion];

            try
            {
                Ping pingSender = new Ping();
                PingReply reply = await pingSender.SendPingAsync(IPAddress, 1000);

                if (reply.Status == IPStatus.Success)
                {
                    lbPingLogger.Items.Add($"[{DateTime.Now}] {selectedRegion}: {reply.RoundtripTime} ms");
                }
                else
                {
                    lbPingLogger.Items.Add($"[{DateTime.Now}] {selectedRegion}: Failed: {reply.Status}");
                }
            }
            catch (Exception ex)
            {
                lbPingLogger.Items.Add($"[{DateTime.Now}] {selectedRegion}: Error during auto-pinging: {ex.Message}");
            }
            lbPingLogger.TopIndex = lbPingLogger.Items.Count - 1;
        }

        private void BtnToggleTheme_Click(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
            SaveThemeSettings();
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                tableLayoutPanel.BackColor = Color.Black;

                foreach (Control c in tableLayoutPanel.Controls)
                {
                    c.BackColor = Color.Black;
                    c.ForeColor = Color.White;
                }

                autoPingerTabPage.BackColor = Color.Black;
                foreach (Control c in autoPingerTabPage.Controls)
                {
                    c.BackColor = Color.Black;
                    c.ForeColor = Color.White;
                }

                chkAutoPing.BackColor = Color.Black;
                chkAutoPing.ForeColor = Color.White;

                lbPingLogger.BackColor = Color.Black;
                lbPingLogger.ForeColor = Color.White;
            }
            else
            {
                tableLayoutPanel.BackColor = Color.White;

                foreach (Control c in tableLayoutPanel.Controls)
                {
                    c.BackColor = Color.White;
                    c.ForeColor = Color.Black;
                }

                autoPingerTabPage.BackColor = Color.White;
                foreach (Control c in autoPingerTabPage.Controls)
                {
                    c.BackColor = Color.White;
                    c.ForeColor = Color.Black;
                }

                chkAutoPing.BackColor = Color.White;
                chkAutoPing.ForeColor = Color.Black;

                lbPingLogger.BackColor = Color.White;
                lbPingLogger.ForeColor = Color.Black;
            }
        }

        private void SaveThemeSettings()
        {
            try
            {
                File.WriteAllText("AppTheme.txt", isDarkMode ? "dark" : "light");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving theme settings: {ex.Message}");
            }
        }

        private void LoadThemeSettings()
        {
            try
            {
                if (File.Exists("AppTheme.txt"))
                {
                    string theme = File.ReadAllText("AppTheme.txt").Trim();
                    isDarkMode = theme == "dark";
                }
                else
                {
                    isDarkMode = false;
                }
                ApplyTheme();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading theme settings: {ex.Message}");
            }
        }
    }
}