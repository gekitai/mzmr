﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace mzmr
{
    public partial class FormMain : Form
    {
        // fields
        private ROM rom;

        public FormMain()
        {
            InitializeComponent();

            FillLocations();
            Reset();
            CheckForUpdate();
        }

        private void FillLocations()
        {
            Array itemTypes = Enum.GetValues(typeof(ItemType));
            List<string> options = new List<string>();
            foreach (ItemType item in itemTypes)
            {
                options.Add(item.Name());
            }
            string[] itemNames = options.ToArray();

            for (int i = 0; i < 100; i++)
            {
                Label label = new Label
                {
                    AutoSize = true,
                    Margin = new Padding(4, 5, 4, 0),
                    Text = i.ToString()
                };
                ComboBox cb = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Name = $"loc{i}"
                };
                cb.Items.AddRange(itemNames);
                cb.SelectedIndex = 0;
                tableLayoutPanel_locs.Controls.Add(label);
                tableLayoutPanel_locs.Controls.Add(cb);
            }
        }

        private void CheckForUpdate()
        {
            WebClient client = new WebClient();
            client.DownloadStringCompleted += client_DownloadStringCompleted;

            try
            {
                client.DownloadStringAsync(new Uri("http://labk.org/mzmr/version.txt"));
            }
            catch
            {
                // do nothing
            }
        }

        private void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled) { return; }
            if (e.Result.Length != 5) { return; }
            if (e.Result == Program.Version) { return; }

            DialogResult result = MessageBox.Show(
                $"A newer version of MZM Randomizer is available ({e.Result}). Would you like to download it?",
                "Update Available",
                MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                WebClient client = new WebClient();
                string path = Path.Combine(Application.StartupPath, "mzmr-" + e.Result + ".zip");
                try
                {
                    client.DownloadFile("http://labk.org/mzmr/mzmr.zip", path);
                }
                catch
                {
                    MessageBox.Show(
                        "Update could not be downloaded. You will be taken to the website to download it manually.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    Process.Start("http://labk.org/mzmr/");
                    return;
                }
                MessageBox.Show(
                    $"File saved to\n{path}\n\nYou should close the program and begin using the new version",
                    "Download Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void Reset()
        {
            ToggleControls(false);
            rom = null;

            if (Properties.Settings.Default.autoLoadRom)
            {
                OpenROM(Properties.Settings.Default.prevRomPath);
            }
        }

        private void ToggleControls(bool toggle)
        {
            button_randomize.Enabled = toggle;
            button_loadSettings.Enabled = toggle;
            button_saveSettings.Enabled = toggle;
            label_seed.Enabled = toggle;
            textBox_seed.Enabled = toggle;

            foreach (Control tab in tabControl_options.TabPages)
            {
                tab.Enabled = toggle;
            }
        }

        public void SetStateFromSettings(Settings settings)
        {
            // items
            checkBox_itemsAbilities.Checked = settings.randomAbilities;
            checkBox_itemsTanks.Checked = settings.randomTanks;
            numericUpDown_itemsRemove.Value = settings.numItemsRemoved;

            radioButton_completionUnchanged.Checked = (settings.gameCompletion == GameCompletion.Unchanged);
            radioButton_completionBeatable.Checked = (settings.gameCompletion == GameCompletion.Beatable);
            radioButton_completion100.Checked = (settings.gameCompletion == GameCompletion.AllItems);

            checkBox_iceNotRequired.Checked = settings.iceNotRequired;
            checkBox_plasmaNotRequired.Checked = settings.plasmaNotRequired;
            checkBox_noPBsBeforeChozodia.Checked = settings.noPBsBeforeChozodia;
            checkBox_chozoStatueHints.Checked = settings.chozoStatueHints;

            checkBox_infiniteBombJump.Checked = settings.infiniteBombJump;
            checkBox_wallJumping.Checked = settings.wallJumping;

            // locations
            foreach (KeyValuePair<int, ItemType> kvp in settings.customAssignments)
            {
                string key = $"loc{kvp.Key}";
                ComboBox cb = tableLayoutPanel_locs.Controls[key] as ComboBox;
                cb.SelectedIndex = (int)kvp.Value;
            }

            // palettes
            checkBox_tilesetPalettes.Checked = settings.tilesetPalettes;
            checkBox_enemyPalettes.Checked = settings.enemyPalettes;
            checkBox_beamPalettes.Checked = settings.beamPalettes;
            numericUpDown_hueMin.Value = settings.hueMinimum;
            numericUpDown_hueMax.Value = settings.hueMaximum;

            // misc
            checkBox_enableItemToggle.Checked = settings.enableItemToggle;
            checkBox_obtainUnkItems.Checked = settings.obtainUnkItems;
            checkBox_hardModeAvailable.Checked = settings.hardModeAvailable;
            checkBox_pauseScreenInfo.Checked = settings.pauseScreenInfo;
            checkBox_removeCutscenes.Checked = settings.removeCutscenes;
            checkBox_skipSuitless.Checked = settings.skipSuitless;
            checkBox_skipDoorTransitions.Checked = settings.skipDoorTransitions;
        }

        private Settings GetSettingsFromState()
        {
            Settings settings = new Settings();

            // items
            settings.randomAbilities = checkBox_itemsAbilities.Checked;
            settings.randomTanks = checkBox_itemsTanks.Checked;
            settings.numItemsRemoved = (int)numericUpDown_itemsRemove.Value;

            if (radioButton_completionUnchanged.Checked) { settings.gameCompletion = GameCompletion.Unchanged; }
            else if (radioButton_completionBeatable.Checked) { settings.gameCompletion = GameCompletion.Beatable; }
            else if (radioButton_completion100.Checked) { settings.gameCompletion = GameCompletion.AllItems; }

            settings.iceNotRequired = checkBox_iceNotRequired.Checked;
            settings.plasmaNotRequired = checkBox_plasmaNotRequired.Checked;
            settings.noPBsBeforeChozodia = checkBox_noPBsBeforeChozodia.Checked;
            settings.chozoStatueHints = checkBox_chozoStatueHints.Checked;

            settings.infiniteBombJump = checkBox_infiniteBombJump.Checked;
            settings.wallJumping = checkBox_wallJumping.Checked;

            // locations
            settings.customAssignments = new Dictionary<int, ItemType>();
            for (int i = 0; i < 100; i++)
            {
                ItemType item = GetCustomAssignment(i);
                if (item != ItemType.Undefined)
                {
                    settings.customAssignments[i] = item;
                }
            }

            // palettes
            settings.tilesetPalettes = checkBox_tilesetPalettes.Checked;
            settings.enemyPalettes = checkBox_enemyPalettes.Checked;
            settings.beamPalettes = checkBox_beamPalettes.Checked;
            settings.hueMinimum = (int)numericUpDown_hueMin.Value;
            settings.hueMaximum = (int)numericUpDown_hueMax.Value;

            // misc
            settings.enableItemToggle = checkBox_enableItemToggle.Checked;
            settings.obtainUnkItems = checkBox_obtainUnkItems.Checked;
            settings.hardModeAvailable = checkBox_hardModeAvailable.Checked;
            settings.pauseScreenInfo = checkBox_pauseScreenInfo.Checked;
            settings.removeCutscenes = checkBox_removeCutscenes.Checked;
            settings.skipSuitless = checkBox_skipSuitless.Checked;
            settings.skipDoorTransitions = checkBox_skipDoorTransitions.Checked;

            return settings;
        }

        private void button_openROM_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFile = new OpenFileDialog())
            {
                openFile.Filter = "GBA ROM Files (*.gba)|*.gba";
                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    OpenROM(openFile.FileName);
                }
            }
        }

        private void button_randomize_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFile = new SaveFileDialog())
            {
                saveFile.Filter = "GBA ROM Files (*.gba)|*.gba";
                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    Randomize(saveFile.FileName);
                }
            }
        }

        private void button_loadSettings_Click(object sender, EventArgs e)
        {
            FormSettings form = new FormSettings(this);
            form.ShowDialog();
        }

        private void button_saveSettings_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFile = new SaveFileDialog())
            {
                saveFile.Filter = "JSON files (*.json)|*.json";
                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    Settings settings = GetSettingsFromState();
                    File.WriteAllText(saveFile.FileName, settings.GetString());
                }
            }
        }

        private void button_appSettings_Click(object sender, EventArgs e)
        {
            FormAppSettings form = new FormAppSettings();
            form.Show();
        }

        private void OpenROM(string filename)
        {
            try
            {
                rom = new ROM(filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Properties.Settings.Default.prevRomPath = filename;
            Properties.Settings.Default.Save();

            Settings settings;
            if (Properties.Settings.Default.rememberSettings)
            {
                settings = Settings.LoadSettings(Properties.Settings.Default.prevSettings);
            }
            else
            {
                settings = new Settings();
            }

            SetStateFromSettings(settings);
            ToggleControls(true);
        }

        private void Randomize(string filename)
        {
            if (!ValidateCustomAssignments()) { return; }

            // get settings
            Settings settings = GetSettingsFromState();
            string config = settings.GetString();
            if (Properties.Settings.Default.rememberSettings)
            {
                Properties.Settings.Default.prevSettings = config;
                Properties.Settings.Default.Save();
            }

            // get seed
            if (!int.TryParse(textBox_seed.Text, out int seed))
            {
                Random temp = new Random();
                seed = temp.Next();
            }

            // randomize
            RandomAll randAll = new RandomAll(rom, settings, seed);
            bool success = randAll.Randomize();

            if (!success)
            {
                MessageBox.Show("Randomization failed.");
                return;
            }

            // save ROM
            rom.Save(filename);

            // write files
            string writtenFiles = "";

            // log file
            bool saveLogFile = Properties.Settings.Default.saveLogFile;
            if (!saveLogFile)
            {
                DialogResult result = MessageBox.Show("Would you like to save a log file?", "", MessageBoxButtons.YesNo);
                saveLogFile = (result == DialogResult.Yes);
            }
            if (saveLogFile)
            {
                string logFile = Path.ChangeExtension(filename, "log");
                File.WriteAllText(logFile, randAll.GetLog());
                writtenFiles += "Log file saved to\n" + logFile + "\n\n";
            }

            // map images
            if (settings.randomAbilities || settings.randomTanks)
            {
                bool saveMapImages = Properties.Settings.Default.saveMapImages;
                if (!saveMapImages)
                {
                    var result = MessageBox.Show("Would you like to save map images?", "", MessageBoxButtons.YesNo);
                    saveMapImages = (result == DialogResult.Yes);
                }
                if (saveMapImages)
                {
                    string path = Path.ChangeExtension(filename, null) + "_maps";
                    Directory.CreateDirectory(path);
                    Bitmap[] minimaps = randAll.GetMaps();
                    minimaps[0].Save(Path.Combine(path, "brinstar.png"));
                    minimaps[1].Save(Path.Combine(path, "kraid.png"));
                    minimaps[2].Save(Path.Combine(path, "norfair.png"));
                    minimaps[3].Save(Path.Combine(path, "ridley.png"));
                    minimaps[4].Save(Path.Combine(path, "tourian.png"));
                    minimaps[5].Save(Path.Combine(path, "crateria.png"));
                    minimaps[6].Save(Path.Combine(path, "chozodia.png"));
                    writtenFiles += "Map images saved to\n" + path;
                }
            }

            // display written files
            if (writtenFiles != "")
            {
                MessageBox.Show(writtenFiles.TrimEnd('\n'), "", MessageBoxButtons.OK);
            }

            // display seed and settings
            FormComplete form = new FormComplete(seed, config);
            form.ShowDialog();

            // clean up
            Reset();
        }

        private void numericUpDown_hueMin_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_hueMax.Minimum = numericUpDown_hueMin.Value;
        }

        private void numericUpDown_hueMax_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_hueMin.Maximum = numericUpDown_hueMax.Value;
        }

        private ItemType GetCustomAssignment(int number)
        {
            string key = $"loc{number}";
            ComboBox cb = tableLayoutPanel_locs.Controls[key] as ComboBox;
            return (ItemType)cb.SelectedIndex;
        }

        private bool ValidateCustomAssignments()
        {
            // count each type selected
            Dictionary<ItemType, int> counts = new Dictionary<ItemType, int>();
            for (int i = 0; i < 100; i++)
            {
                ItemType item = GetCustomAssignment(i);
                if (item == ItemType.Undefined) { continue; }

                if (counts.ContainsKey(item))
                {
                    counts[item]++;
                }
                else
                {
                    counts[item] = 1;
                }
            }
            // check each type against maximum count
            foreach (KeyValuePair<ItemType, int> kvp in counts)
            {
                ItemType item = kvp.Key;
                if (kvp.Value > item.MaxNumber())
                {
                    MessageBox.Show(
                        $"More than {item.MaxNumber()} {item.Name()}s selected.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }


    }
}
