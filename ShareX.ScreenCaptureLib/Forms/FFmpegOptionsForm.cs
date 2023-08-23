﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2023 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using ShareX.MediaLib;
using ShareX.ScreenCaptureLib.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareX.ScreenCaptureLib
{
    public partial class FFmpegOptionsForm : Form
    {
        public ScreenRecordingOptions Options { get; private set; }

        private bool settingsLoaded;

        public FFmpegOptionsForm(ScreenRecordingOptions options)
        {
            Options = options;

            InitializeComponent();
            ShareXResources.ApplyTheme(this);

            cbVideoCodec.Items.AddRange(Helpers.GetEnumDescriptions<FFmpegVideoCodec>());
            cbAudioCodec.Items.AddRange(Helpers.GetEnumDescriptions<FFmpegAudioCodec>());
            cbx264Preset.Items.AddRange(Helpers.GetEnumDescriptions<FFmpegPreset>());
            cbGIFStatsMode.Items.AddRange(Helpers.GetEnumDescriptions<FFmpegPaletteGenStatsMode>());
            cbNVENCPreset.Items.AddRange(Helpers.GetEnums<FFmpegNVENCPreset>().Select(x => $"{x} - {x.GetDescription()}").ToArray());
            cbNVENCTune.Items.AddRange(Helpers.GetEnums<FFmpegNVENCTune>().Select(x => $"{x} - {x.GetDescription()}").ToArray());
            cbGIFDither.Items.AddRange(Helpers.GetEnumDescriptions<FFmpegPaletteUseDither>());
            cbAMFUsage.Items.AddRange(Helpers.GetEnums<FFmpegAMFUsage>().Select(x => $"{x} - {x.GetDescription()}").ToArray());
            cbAMFQuality.Items.AddRange(Helpers.GetEnums<FFmpegAMFQuality>().Select(x => $"{x} - {x.GetDescription()}").ToArray());
            cbQSVPreset.Items.AddRange(Helpers.GetEnumDescriptions<FFmpegQSVPreset>());
        }

        private async Task SettingsLoad()
        {
            settingsLoaded = false;

            cbUseCustomFFmpegPath.Checked = Options.FFmpeg.OverrideCLIPath;
            txtFFmpegPath.Enabled = btnFFmpegBrowse.Enabled = Options.FFmpeg.OverrideCLIPath;
            txtFFmpegPath.Text = Options.FFmpeg.CLIPath;
            txtFFmpegPath.SelectionStart = txtFFmpegPath.TextLength;

            await RefreshSourcesAsync();

#if MicrosoftStore
            btnInstallHelperDevices.Visible = false;
            btnHelperDevicesHelp.Visible = false;
            lblHelperDevices.Visible = false;
#endif

            cbVideoCodec.SelectedIndex = (int)Options.FFmpeg.VideoCodec;
            cbAudioCodec.SelectedIndex = (int)Options.FFmpeg.AudioCodec;

            txtUserArgs.Text = Options.FFmpeg.UserArgs;

            // x264
            nudx264CRF.SetValue(Options.FFmpeg.x264_CRF);
            nudx264Bitrate.SetValue(Options.FFmpeg.x264_Bitrate);
            cbx264UseBitrate.Checked = Options.FFmpeg.x264_Use_Bitrate;
            cbx264Preset.SelectedIndex = (int)Options.FFmpeg.x264_Preset;

            // VPx
            nudVP8Bitrate.SetValue(Options.FFmpeg.VPx_Bitrate);

            // Xvid
            nudXvidQscale.SetValue(Options.FFmpeg.XviD_QScale);

            // NVENC
            nudNVENCBitrate.SetValue(Options.FFmpeg.NVENC_Bitrate);
            cbNVENCPreset.SelectedIndex = (int)Options.FFmpeg.NVENC_Preset;
            cbNVENCTune.SelectedIndex = (int)Options.FFmpeg.NVENC_Tune;

            // GIF
            cbGIFStatsMode.SelectedIndex = (int)Options.FFmpeg.GIFStatsMode;
            cbGIFDither.SelectedIndex = (int)Options.FFmpeg.GIFDither;
            nudGIFBayerScale.SetValue(Options.FFmpeg.GIFBayerScale);

            // AMF
            cbAMFUsage.SelectedIndex = (int)Options.FFmpeg.AMF_Usage;
            cbAMFQuality.SelectedIndex = (int)Options.FFmpeg.AMF_Quality;
            nudAMFBitrate.SetValue(Options.FFmpeg.AMF_Bitrate);

            // QuickSync
            cbQSVPreset.SelectedIndex = (int)Options.FFmpeg.QSV_Preset;
            nudQSVBitrate.SetValue(Options.FFmpeg.QSV_Bitrate);

            // AAC
            tbAACBitrate.Value = Options.FFmpeg.AAC_Bitrate / 32;

            // Vorbis
            tbVorbis_qscale.Value = Options.FFmpeg.Vorbis_QScale;

            // MP3
            tbMP3_qscale.Value = FFmpegCLIManager.mp3_max - Options.FFmpeg.MP3_QScale;

            cbCustomCommands.Checked = Options.FFmpeg.UseCustomCommands;

            if (Options.FFmpeg.UseCustomCommands)
            {
                txtCommandLinePreview.Text = Options.FFmpeg.CustomCommands;
            }

            settingsLoaded = true;

            UpdateUI();
        }

        private async Task RefreshSourcesAsync(bool selectDevices = false)
        {
            DirectShowDevices devices = null;

            await Task.Run(() =>
            {
                if (File.Exists(Options.FFmpeg.FFmpegPath))
                {
                    using (FFmpegCLIManager ffmpeg = new FFmpegCLIManager(Options.FFmpeg.FFmpegPath))
                    {
                        devices = ffmpeg.GetDirectShowDevices();
                    }
                }
            });

            if (!IsDisposed)
            {
                cbVideoSource.Items.Clear();
                cbVideoSource.Items.Add(FFmpegCaptureDevice.None);
                cbVideoSource.Items.Add(FFmpegCaptureDevice.GDIGrab);

                if (Helpers.IsWindows10OrGreater())
                {
                    cbVideoSource.Items.Add(FFmpegCaptureDevice.DDAGrab);
                }

                cbAudioSource.Items.Clear();
                cbAudioSource.Items.Add(FFmpegCaptureDevice.None);

                if (devices != null)
                {
                    cbVideoSource.Items.AddRange(devices.VideoDevices.Select(x => new FFmpegCaptureDevice(x, $"dshow ({x})")).ToArray());
                    cbAudioSource.Items.AddRange(devices.AudioDevices.Select(x => new FFmpegCaptureDevice(x, $"dshow ({x})")).ToArray());
                }

                if (selectDevices && cbVideoSource.Items.Cast<FFmpegCaptureDevice>().
                    Any(x => x.Value.Equals(FFmpegCaptureDevice.ScreenCaptureRecorder.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    Options.FFmpeg.VideoSource = FFmpegCaptureDevice.ScreenCaptureRecorder.Value;
                }
                else if (!cbVideoSource.Items.Cast<FFmpegCaptureDevice>().Any(x => x.Value.Equals(Options.FFmpeg.VideoSource, StringComparison.OrdinalIgnoreCase)))
                {
                    Options.FFmpeg.VideoSource = FFmpegCaptureDevice.GDIGrab.Value;
                }

                foreach (FFmpegCaptureDevice device in cbVideoSource.Items)
                {
                    if (device.Value.Equals(Options.FFmpeg.VideoSource, StringComparison.OrdinalIgnoreCase))
                    {
                        cbVideoSource.SelectedItem = device;
                        break;
                    }
                }

                if (selectDevices && cbAudioSource.Items.Cast<FFmpegCaptureDevice>().
                    Any(x => x.Value.Equals(FFmpegCaptureDevice.VirtualAudioCapturer.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    Options.FFmpeg.AudioSource = FFmpegCaptureDevice.VirtualAudioCapturer.Value;
                }
                else if (!cbAudioSource.Items.Cast<FFmpegCaptureDevice>().Any(x => x.Value.Equals(Options.FFmpeg.AudioSource, StringComparison.OrdinalIgnoreCase)))
                {
                    Options.FFmpeg.AudioSource = FFmpegCaptureDevice.None.Value;
                }

                foreach (FFmpegCaptureDevice device in cbAudioSource.Items)
                {
                    if (device.Value.Equals(Options.FFmpeg.AudioSource, StringComparison.OrdinalIgnoreCase))
                    {
                        cbAudioSource.SelectedItem = device;
                        break;
                    }
                }
            }
        }

        private void UpdateUI()
        {
            if (settingsLoaded)
            {
                lblx264CRF.Text = Options.FFmpeg.x264_Use_Bitrate ? Resources.Bitrate : Resources.CRF;
                nudx264CRF.Visible = !Options.FFmpeg.x264_Use_Bitrate;
                nudx264Bitrate.Visible = lblx264BitrateK.Visible = Options.FFmpeg.x264_Use_Bitrate;

                lblAACQuality.Text = string.Format(Resources.FFmpegOptionsForm_UpdateUI_Bitrate___0_k, Options.FFmpeg.AAC_Bitrate);
                lblOpusQuality.Text = string.Format(Resources.FFmpegOptionsForm_UpdateUI_Bitrate___0_k, Options.FFmpeg.Opus_Bitrate);
                lblVorbisQuality.Text = Resources.FFmpegOptionsForm_UpdateUI_Quality_ + " " + Options.FFmpeg.Vorbis_QScale;
                lblMP3Quality.Text = Resources.FFmpegOptionsForm_UpdateUI_Quality_ + " " + Options.FFmpeg.MP3_QScale;
                pbx264PresetWarning.Visible = (FFmpegPreset)cbx264Preset.SelectedIndex > FFmpegPreset.fast;

                if (!Options.FFmpeg.UseCustomCommands)
                {
                    txtCommandLinePreview.Text = Options.GetFFmpegArgs();
                }

                nudGIFBayerScale.Visible = Options.FFmpeg.GIFDither == FFmpegPaletteUseDither.bayer;
            }
        }

        private async void FFmpegOptionsForm_Load(object sender, EventArgs e)
        {
            await SettingsLoad();
        }

        private void cbUseCustomFFmpegPath_CheckedChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.OverrideCLIPath = cbUseCustomFFmpegPath.Checked;
            txtFFmpegPath.Enabled = btnFFmpegBrowse.Enabled = Options.FFmpeg.OverrideCLIPath;
        }

        private void txtFFmpegPath_TextChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.CLIPath = txtFFmpegPath.Text;
        }

        private async void buttonFFmpegBrowse_Click(object sender, EventArgs e)
        {
            if (FileHelpers.BrowseFile(Resources.FFmpegOptionsForm_buttonFFmpegBrowse_Click_Browse_for_ffmpeg_exe, txtFFmpegPath, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), true))
            {
                await RefreshSourcesAsync();
            }
        }

        private void cbVideoSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            FFmpegCaptureDevice device = cbVideoSource.SelectedItem as FFmpegCaptureDevice;
            Options.FFmpeg.VideoSource = device?.Value;
            UpdateUI();
        }

        private void cbAudioSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            FFmpegCaptureDevice device = cbAudioSource.SelectedItem as FFmpegCaptureDevice;
            Options.FFmpeg.AudioSource = device?.Value;
            UpdateUI();
        }

        private async void btnInstallHelperDevices_Click(object sender, EventArgs e)
        {
            string filePath = FileHelpers.GetAbsolutePath("Recorder-devices-setup.exe");

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                bool result = false;

                await Task.Run(() =>
                {
                    try
                    {
                        using (Process process = new Process())
                        {
                            ProcessStartInfo psi = new ProcessStartInfo()
                            {
                                FileName = filePath
                            };

                            process.StartInfo = psi;
                            process.Start();
                            result = process.WaitForExit(1000 * 60 * 5) && process.ExitCode == 0;
                        }
                    }
                    catch { }
                });

                if (result)
                {
                    await RefreshSourcesAsync(true);
                }
            }
            else
            {
                MessageBox.Show("File not exists: \"" + filePath + "\"", "ShareX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHelperDevicesHelp_Click(object sender, EventArgs e)
        {
            URLHelpers.OpenURL("https://github.com/rdp/screen-capture-recorder-to-video-windows-free");
        }

        private void cbVideoCodec_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.VideoCodec = (FFmpegVideoCodec)cbVideoCodec.SelectedIndex;

            tcFFmpegVideoCodecs.Visible = Options.FFmpeg.VideoCodec != FFmpegVideoCodec.libwebp && Options.FFmpeg.VideoCodec != FFmpegVideoCodec.apng;

            if (cbVideoCodec.SelectedIndex >= 0)
            {
                switch (Options.FFmpeg.VideoCodec)
                {
                    case FFmpegVideoCodec.libx264:
                    case FFmpegVideoCodec.libx265:
                        tcFFmpegVideoCodecs.SelectTabWithoutFocus(tpX264);
                        break;
                    case FFmpegVideoCodec.libvpx:
                    case FFmpegVideoCodec.libvpx_vp9:
                        tcFFmpegVideoCodecs.SelectTabWithoutFocus(tpVpx);
                        break;
                    case FFmpegVideoCodec.libxvid:
                        tcFFmpegVideoCodecs.SelectTabWithoutFocus(tpXvid);
                        break;
                    case FFmpegVideoCodec.h264_nvenc:
                    case FFmpegVideoCodec.hevc_nvenc:
                        tcFFmpegVideoCodecs.SelectTabWithoutFocus(tpNVENC);
                        break;
                    case FFmpegVideoCodec.gif:
                        tcFFmpegVideoCodecs.SelectTabWithoutFocus(tpGIF);
                        break;
                    case FFmpegVideoCodec.h264_amf:
                    case FFmpegVideoCodec.hevc_amf:
                        tcFFmpegVideoCodecs.SelectTabWithoutFocus(tpAMF);
                        break;
                    case FFmpegVideoCodec.h264_qsv:
                    case FFmpegVideoCodec.hevc_qsv:
                        tcFFmpegVideoCodecs.SelectTabWithoutFocus(tpQSV);
                        break;
                }
            }

            UpdateUI();
        }

        private void cbAudioCodec_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.AudioCodec = (FFmpegAudioCodec)cbAudioCodec.SelectedIndex;

            if (cbAudioCodec.SelectedIndex >= 0)
            {
                switch (Options.FFmpeg.AudioCodec)
                {
                    case FFmpegAudioCodec.libvoaacenc:
                        tcFFmpegAudioCodecs.SelectTabWithoutFocus(tpAAC);
                        break;
                    case FFmpegAudioCodec.libopus:
                        tcFFmpegAudioCodecs.SelectTabWithoutFocus(tpOpus);
                        break;
                    case FFmpegAudioCodec.libvorbis:
                        tcFFmpegAudioCodecs.SelectTabWithoutFocus(tpVorbis);
                        break;
                    case FFmpegAudioCodec.libmp3lame:
                        tcFFmpegAudioCodecs.SelectTabWithoutFocus(tpMP3);
                        break;
                }
            }

            UpdateUI();
        }

        private void nudx264CRF_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.x264_CRF = (int)nudx264CRF.Value;
            UpdateUI();
        }

        private void nudx264Bitrate_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.x264_Bitrate = (int)nudx264Bitrate.Value;
            UpdateUI();
        }

        private void cbx264UseBitrate_CheckedChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.x264_Use_Bitrate = cbx264UseBitrate.Checked;
            UpdateUI();
        }

        private void cbPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.x264_Preset = (FFmpegPreset)cbx264Preset.SelectedIndex;
            UpdateUI();
        }

        private void nudVP8Bitrate_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.VPx_Bitrate = (int)nudVP8Bitrate.Value;
            UpdateUI();
        }

        private void nudQscale_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.XviD_QScale = (int)nudXvidQscale.Value;
            UpdateUI();
        }

        private void nudNVENCBitrate_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.NVENC_Bitrate = (int)nudNVENCBitrate.Value;
            UpdateUI();
        }

        private void cbNVENCPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.NVENC_Preset = (FFmpegNVENCPreset)cbNVENCPreset.SelectedIndex;
            UpdateUI();
        }

        private void cbNVENCTune_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.NVENC_Tune = (FFmpegNVENCTune)cbNVENCTune.SelectedIndex;
            UpdateUI();
        }

        private void cbGIFStatsMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.GIFStatsMode = (FFmpegPaletteGenStatsMode)cbGIFStatsMode.SelectedIndex;
            UpdateUI();
        }

        private void cbGIFDither_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.GIFDither = (FFmpegPaletteUseDither)cbGIFDither.SelectedIndex;
            UpdateUI();
        }

        private void nudGIFBayerScale_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.GIFBayerScale = (int)nudGIFBayerScale.Value;
            UpdateUI();
        }

        private void cbAMFUsage_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.AMF_Usage = (FFmpegAMFUsage)cbAMFUsage.SelectedIndex;
            UpdateUI();
        }

        private void cbAMFQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.AMF_Quality = (FFmpegAMFQuality)cbAMFQuality.SelectedIndex;
            UpdateUI();
        }

        private void nudAMFBitrate_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.AMF_Bitrate = (int)nudAMFBitrate.Value;
            UpdateUI();
        }

        private void cbQSVPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.QSV_Preset = (FFmpegQSVPreset)cbQSVPreset.SelectedIndex;
            UpdateUI();
        }

        private void nudQSVBitrate_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.QSV_Bitrate = (int)nudQSVBitrate.Value;
            UpdateUI();
        }

        private void tbAACBitrate_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.AAC_Bitrate = tbAACBitrate.Value * 32;
            UpdateUI();
        }

        private void tbOpusBirate_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.Opus_Bitrate = tbOpusBitrate.Value * 32;
            UpdateUI();
        }

        private void tbVorbis_qscale_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.Vorbis_QScale = tbVorbis_qscale.Value;
            UpdateUI();
        }

        private void tbMP3_qscale_ValueChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.MP3_QScale = FFmpegCLIManager.mp3_max - tbMP3_qscale.Value;
            UpdateUI();
        }

        private void txtUserArgs_TextChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.UserArgs = txtUserArgs.Text;
            UpdateUI();
        }

        private void cbCustomCommands_CheckedChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.UseCustomCommands = cbCustomCommands.Checked;
            txtCommandLinePreview.ReadOnly = !Options.FFmpeg.UseCustomCommands;

            if (settingsLoaded)
            {
                if (Options.FFmpeg.UseCustomCommands)
                {
                    txtCommandLinePreview.Text = Options.GetFFmpegArgs(true);
                }
                else
                {
                    txtCommandLinePreview.Text = Options.GetFFmpegArgs();
                }
            }
        }

        private void txtCommandLinePreview_TextChanged(object sender, EventArgs e)
        {
            Options.FFmpeg.CustomCommands = txtCommandLinePreview.Text;
        }
    }
}