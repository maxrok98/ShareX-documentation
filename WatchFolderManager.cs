#region License Information (GPL v3)

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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShareX
{
    /// <summary>
    /// Class <c>WatchFolderManager</c> keeps track of list of <see cref="WatchFolder"/>.
    /// </summary>
    public class WatchFolderManager : IDisposable
    {
        /// <summary>
        /// List of <see cref="WatchFolder"/>, which is managed by the class.
        /// </summary>
        public List<WatchFolder> WatchFolders { get; private set; }

        /// <summary>
        /// Reinitializes list of <see cref="WatchFolder"/>, adds default and hotkeys <c>WatchFolders</c> to it.
        /// </summary>
        public void UpdateWatchFolders()
        {
            if (WatchFolders != null)
            {
                UnregisterAllWatchFolders();
            }

            WatchFolders = new List<WatchFolder>();

            foreach (WatchFolderSettings defaultWatchFolderSetting in Program.DefaultTaskSettings.WatchFolderList)
            {
                AddWatchFolder(defaultWatchFolderSetting, Program.DefaultTaskSettings);
            }

            foreach (HotkeySettings hotkeySetting in Program.HotkeysConfig.Hotkeys)
            {
                foreach (WatchFolderSettings watchFolderSetting in hotkeySetting.TaskSettings.WatchFolderList)
                {
                    AddWatchFolder(watchFolderSetting, hotkeySetting.TaskSettings);
                }
            }
        }

        private WatchFolder FindWatchFolder(WatchFolderSettings watchFolderSetting)
        {
            return WatchFolders.FirstOrDefault(watchFolder => watchFolder.Settings == watchFolderSetting);
        }

        private bool IsExist(WatchFolderSettings watchFolderSetting)
        {
            return FindWatchFolder(watchFolderSetting) != null;
        }

        /// <summary>
        /// Adds newly created <see cref="WatchFolder"/> to <c>WatchFolders</c> list, after assigning to it specified
        /// parameters <paramref name="watchFolderSetting"/> and <paramref name="taskSettings"/>,
        /// and subscribing to <c>FileWatcherTrigger</c>.
        /// <para>
        /// Enables <see cref="WatchFolder"/> depending on <paramref name="taskSettings"/>.
        /// </para>
        /// </summary>
        /// <param name="watchFolderSetting">WatchFolder settings.</param>
        /// <param name="taskSettings">Task settings.</param>
        public void AddWatchFolder(WatchFolderSettings watchFolderSetting, TaskSettings taskSettings)
        {
            if (!IsExist(watchFolderSetting))
            {
                if (!taskSettings.WatchFolderList.Contains(watchFolderSetting))
                {
                    taskSettings.WatchFolderList.Add(watchFolderSetting);
                }

                WatchFolder watchFolder = new WatchFolder();
                watchFolder.Settings = watchFolderSetting;
                watchFolder.TaskSettings = taskSettings;

                watchFolder.FileWatcherTrigger += origPath =>
                {
                    TaskSettings taskSettingsCopy = TaskSettings.GetSafeTaskSettings(taskSettings);
                    string destPath = origPath;

                    if (watchFolderSetting.MoveFilesToScreenshotsFolder)
                    {
                        string screenshotsFolder = TaskHelpers.GetScreenshotsFolder(taskSettingsCopy);
                        string fileName = Path.GetFileName(origPath);
                        destPath = TaskHelpers.HandleExistsFile(screenshotsFolder, fileName, taskSettingsCopy);
                        FileHelpers.CreateDirectoryFromFilePath(destPath);
                        File.Move(origPath, destPath);
                    }

                    UploadManager.UploadFile(destPath, taskSettingsCopy);
                };

                WatchFolders.Add(watchFolder);

                if (taskSettings.WatchFolderEnabled)
                {
                    watchFolder.Enable();
                }
            }
        }

        /// <summary>
        /// Removes <see cref="WatchFolder"/> with specified
        /// <paramref name="watchFolderSetting"/> parameter,
        /// from <c>WatchFolders</c> list.
        /// </summary>
        /// <param name="watchFolderSetting">Set of settings by which <see cref="WatchFolder"/> should be found.</param>
        public void RemoveWatchFolder(WatchFolderSettings watchFolderSetting)
        {
            using (WatchFolder watchFolder = FindWatchFolder(watchFolderSetting))
            {
                if (watchFolder != null)
                {
                    watchFolder.TaskSettings.WatchFolderList.Remove(watchFolderSetting);
                    WatchFolders.Remove(watchFolder);
                }
            }
        }

        /// <summary>
        /// Updates <c>WatchFolder</c> state with specified <paramref name="watchFolderSetting"/> parameter,
        /// by enabling or disposing it, depending on its' <see cref="TaskSettings"/> property.
        /// </summary>
        /// <param name="watchFolderSetting">Set of settings by which <see cref="WatchFolder"/> should be found.</param>
        public void UpdateWatchFolderState(WatchFolderSettings watchFolderSetting)
        {
            WatchFolder watchFolder = FindWatchFolder(watchFolderSetting);
            if (watchFolder != null)
            {
                if (watchFolder.TaskSettings.WatchFolderEnabled)
                {
                    watchFolder.Enable();
                }
                else
                {
                    watchFolder.Dispose();
                }
            }
        }

        /// <summary>
        /// Disposes each <see cref="WatchFolder"/> from <c>WatchFolders</c> list.
        /// </summary>
        public void UnregisterAllWatchFolders()
        {
            if (WatchFolders != null)
            {
                foreach (WatchFolder watchFolder in WatchFolders)
                {
                    if (watchFolder != null)
                    {
                        watchFolder.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters all watch folders.
        /// </summary>
        public void Dispose()
        {
            UnregisterAllWatchFolders();
        }
    }
}