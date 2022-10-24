using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopModule
{
    public class ConfigManager : IDisposable
    {
        private ResourceManager _rm = new ResourceManager(Globals.STRING_RESOURCES, Assembly.GetExecutingAssembly());
        private ConfigFile _ConfigFile;
        private string _Filename;
        private Exception _LastError;
        private ConfigFile.SettingsDataTable _Settings;
        private Form _Form;
        private bool _backupExist;
        private string _settingsFile;
        private bool _backedUp;
        private static object _lockObj = new object();

        public ConfigManager(string settingsFile)
        {
            _settingsFile = settingsFile;
        }

        public bool ReadOnlyMode { get; set; }

        public Form Form
        {
            set
            {
                _Form = value;
            }
        }

        public bool BackupExist
        {
            get
            {
                return _backupExist;
            }
        }

        public string BackupFile
        {
            get
            {
                return _Filename + ".BAK";
            }
        }

        public Exception LastError
        {
            get
            {
                return _LastError;
            }
        }

        public string ConfigFilename
        {
            get
            {
                return _Filename;
            }
        }

        public bool IsOpened
        {
            get
            {
                return _ConfigFile is object;
            }
        }

        public bool RestoreSettingsFromBackup()
        {
            if (ReadOnlyMode == true)
            {
                throw new ApplicationException("Cannot call RestoreSettingsFromBackup when the ConfigManager is in readonly mode.");
            }

            if (BackupExist == false)
            {
                _LastError = new ApplicationException(_rm.GetString("CONFIG_NO_BACKUP"));
                return false;
            }

            string sPath;
            sPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _Filename = sPath + _settingsFile;
            try
            {
                File.Copy(_Filename, _Filename + ".bad", true);
            }
            catch (Exception)
            {
            }

            try
            {
                File.Copy(BackupFile, _Filename, true);
            }
            catch (Exception ex)
            {
                _LastError = ex;
                return false;
            }

            return true;
        }

        public bool DisableMessageBoxes { get; set; }

        public bool OpenConfigFile(string overridenSettingsFilePath = "")
        {
            string sPath;
            var ok = default(bool);
            ConfigFile dsFile;
            if (IsOpened)
            {
                return true;
            }

            if (_ConfigFile is object)
            {
                _LastError = new Exception(_rm.GetString("CONFIG_ALREADY_OPENED"));
                return false;
            }

            if (overridenSettingsFilePath.IsNullOrEmptyString())
            {
                sPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            else
            {
                sPath = overridenSettingsFilePath;
            }

            _Filename = Path.Combine(sPath, _settingsFile);
            lock (_lockObj)
            {
                _backupExist = false;
                if (File.Exists(_Filename + ".BAK") == true)
                {
                    _backupExist = true;
                }

                if (File.Exists(_Filename) == false)
                {
                    try
                    {
                        if (ReadOnlyMode == true)
                        {
                            throw new FileNotFoundException(string.Empty, _Filename);
                        }

                        dsFile = new ConfigFile();
                        dsFile.DataSetName = "Settings";
                        dsFile.WriteXml(_Filename, XmlWriteMode.WriteSchema);
                        ok = true;
                    }
                    catch (Exception ex)
                    {
                        _LastError = ex;
                        return false;
                    }
                }
            }

            _ConfigFile = new ConfigFile();
        
        read:
            try
            {
                using (var ms = new MemoryStream())
                {
                    lock (_lockObj)
                    {
                        using (var fs = new FileStream(_Filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            fs.CopyTo(ms);
                        }
                    }

                    ms.Seek(0L, SeekOrigin.Begin);
                    _ConfigFile.ReadXmlSchema(ms);
                    ms.Seek(0L, SeekOrigin.Begin);
                    _ConfigFile.ReadXml(ms);
                }

                _Settings = _ConfigFile.Settings;
                ok = true;
            }
            catch (Exception ex)
            {
                DialogResult n;
                _LastError = ex;
                if (DisableMessageBoxes == false)
                {
                    if (BackupExist == false)
                    {
                        MessageBox.Show(_rm.GetString("CONFIG_NO_BACKUP"), Globals.M_ERROR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        n = MessageBox.Show(_rm.GetString("CONFIG_ERROR") + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine + _rm.GetString("CONFIG_RESTORE"), Globals.M_ERROR, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                        if (n == DialogResult.Yes)
                        {
                            if (RestoreSettingsFromBackup() == false)
                            {
                                MessageBox.Show(_LastError.Message, Globals.M_ERROR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show(_rm.GetString("CONFIG_RESTORE_OK"), Globals.M_INFO, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                goto read;
                            }
                        }
                    }
                }

                _ConfigFile = null;
            }

            return ok;
        }

        public void CloseConfigFile()
        {
            if (_ConfigFile is null)
            {
                return;
            }

            try
            {
                var err = default(Exception);
                SaveConfigFile(_Filename, ref err);
            }
            catch (Exception)
            {
            }
            // Throw
            finally
            {
                _ConfigFile.Dispose();
                if (_Settings is object)
                {
                    _Settings.Dispose();
                }
            }

            _ConfigFile = null;
            _Settings = null;
        }

        public void SaveBoolSetting(string Name, bool Value)
        {
            string v;
            if (Value == true)
            {
                v = "1";
            }
            else
            {
                v = "0";
            }

            SaveStringSetting(Name, v);
        }

        public void SaveEnumSetting(string Name, Enum Value)
        {
            SaveStringSetting(Name, Value.ToString());
        }

        public void SaveIntegerSetting(string Name, int Value)
        {
            SaveStringSetting(Name, Value.ToString());
        }

        public void SaveSingleSetting(string Name, float Value)
        {
            SaveStringSetting(Name, Value.ToString(CultureInfo.InvariantCulture));
        }

        public void SaveStringSetting(string Name, string Value)
        {
            ConfigFile.SettingsRow srow;
            var bNew = default(bool);
            if (_ConfigFile is null)
            {
                throw new Exception(_rm.GetString("CONFIG_NOT_OPENED"));
            }

            srow = (ConfigFile.SettingsRow)_Settings.Rows.Find(Name);
            if (srow is null)
            {
                srow = _Settings.NewSettingsRow();
                bNew = true;
            }

            srow.Name = Name;
            if (Value == null)
            {
                Value = string.Empty;
            }

            srow.Value = Value;
            if (bNew == true)
            {
                _Settings.Rows.Add(srow);
            }
        }

        public void DeleteSetting(string Name)
        {
            ConfigFile.SettingsRow srow;
            if (_ConfigFile is null)
            {
                throw new Exception(_rm.GetString("CONFIG_NOT_OPENED"));
            }

            srow = (ConfigFile.SettingsRow)_Settings.Rows.Find(Name);
            if (srow is object)
            {
                _Settings.Rows.Remove(srow);
            }
        }

        public Enum ReadEnumSetting(string Name, Type enumType, Enum defaultVal)
        {
            string v = ReadStringSetting(Name, "");
            Enum ret;
            if (!v.IsNullOrEmptyString())
            {
                try
                {
                    ret = (Enum)Enum.Parse(enumType, v);
                }
                catch (Exception)
                {
                    ret = defaultVal;
                }

                return ret;
            }
            else
            {
                return defaultVal;
            }
        }

        public bool ReadBoolSetting(string Name, bool defaultValue = false)
        {
            string v = ReadStringSetting(Name, defaultValue ? "1" : "0");
            if (v == "1")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int ReadIntegerSetting(string Name, int defaultValue = 0, int min = int.MinValue, int max = int.MaxValue)
        {
            string v = ReadStringSetting(Name);
            int n;
            if (int.TryParse(v, out n) == false)
            {
                n = defaultValue;
            }
            else
            {
                if (n < min)
                    n = min;
                if (n > max)
                    n = max;
            }

            return n;
        }

        public float ReadSingleSetting(string Name, float defaultValue = 0f, float min = float.MinValue, float max = float.MaxValue)
        {
            string v = ReadStringSetting(Name);
            float n;
            if (float.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out n) == false)
            {
                n = defaultValue;
            }
            else
            {
                if (n < min)
                    n = min;
                if (n > max)
                    n = max;
            }

            return n;
        }

        public string ReadStringSetting(string Name, string defaultValue = "")
        {
            ConfigFile.SettingsRow srow;
            if (_ConfigFile is null)
            {
                throw new Exception(_rm.GetString("CONFIG_NOT_OPENED"));
            }

            srow = (ConfigFile.SettingsRow)_Settings.Rows.Find(Name);
            if (srow is null)
            {
                return defaultValue;
            }

            return srow.Value;
        }

        public string[] GetKeys(string startingWith)
        {
            if (_ConfigFile is null)
            {
                throw new Exception(_rm.GetString("CONFIG_NOT_OPENED"));
            }

            startingWith = startingWith.ToLower();
            var val = new List<string>();
            foreach (ConfigFile.SettingsRow srow in _Settings.Rows)
            {
                if (srow.Name.ToLower().StartsWith(startingWith) == true)
                {
                    val.Add(srow.Name);
                }
            }

            return val.ToArray();
        }

        public void LoadConfig(int nX = 0, int nY = 0, int nWidth = 0, int nHeight = 0)
        {
            string sform;
            int top;
            int left;
            int width;
            int height;
            var bAlreadyOpened = default(bool);
            if (_Form is null)
            {
                throw new Exception(_rm.GetString("CONFIG_NO_FORM"));
            }

            if (_ConfigFile is null)
            {
                if (OpenConfigFile() == false)
                {
                    return;
                }
            }
            else
            {
                bAlreadyOpened = true;
            }

            sform = _Form.GetType().ToString();
            if (sform.LastIndexOf(".") > 0)
            {
                sform = sform.Substring(sform.LastIndexOf(".") + 1);
            }

            try
            {
                top = ReadIntegerSetting(sform + ".Top");
            }
            catch
            {
                top = -1;
            }

            if (top <= 0 | top > Screen.PrimaryScreen.WorkingArea.Height - 30)
            {
                if (nY != 0)
                {
                    top = nY;
                }
                else
                {
                    top = 0;
                }
            }

            try
            {
                left = ReadIntegerSetting(sform + ".Left");
            }
            catch
            {
                left = -1;
            }

            if (left <= 0 | left > Screen.PrimaryScreen.WorkingArea.Width - 10)
            {
                if (nX != 0)
                {
                    left = nX;
                }
                else
                {
                    left = 0;
                }
            }

            try
            {
                width = ReadIntegerSetting(sform + ".Width");
            }
            catch
            {
                width = -1;
            }

            if (width > Screen.PrimaryScreen.Bounds.Width)
            {
                width = Screen.PrimaryScreen.Bounds.Width;
            }

            if (width <= 0)
            {
                if (nWidth > 0)
                {
                    width = nWidth;
                }
            }

            try
            {
                height = ReadIntegerSetting(sform + ".Height");
            }
            catch
            {
                height = -1;
            }

            if (height > Screen.PrimaryScreen.Bounds.Height)
            {
                height = Screen.PrimaryScreen.Bounds.Height;
            }

            if (height <= 0)
            {
                if (nHeight > 0)
                {
                    height = nHeight;
                }
            }

            if (top != -1)
            {
                _Form.Top = top;
            }

            if (left != -1)
            {
                _Form.Left = left;
            }

            if (width > 0)
            {
                if (_Form.FormBorderStyle == FormBorderStyle.Sizable || _Form.FormBorderStyle == FormBorderStyle.SizableToolWindow)
                {
                    _Form.Width = width;
                }
            }

            if (height > 0)
            {
                if (_Form.FormBorderStyle == FormBorderStyle.Sizable || _Form.FormBorderStyle == FormBorderStyle.SizableToolWindow)
                {
                    _Form.Height = height;
                }
            }

            if (bAlreadyOpened == false)
            {
                CloseConfigFile();
            }
        }

        public void SaveConfig()
        {
            if (ReadOnlyMode == true)
            {
                throw new ApplicationException("Cannot call SaveConfig when the ConfigManager is in readonly mode.");
            }

            string sform;
            var bAlreadyOpened = default(bool);
            FormWindowState fws;
            if (_Form is null)
            {
                throw new Exception(_rm.GetString("CONFIG_NO_FORM"));
            }

            fws = _Form.WindowState;
            if (_ConfigFile is null)
            {
                if (OpenConfigFile() == false)
                {
                    return;
                }
            }
            else
            {
                bAlreadyOpened = true;
            }

            sform = _Form.GetType().ToString();
            if (sform.LastIndexOf(".") > 0)
            {
                sform = sform.Substring(sform.LastIndexOf(".") + 1);
            }

            // Put form in normal state to save changes, only if it was minimized.
            if (fws == FormWindowState.Minimized)
            {
                _Form.WindowState = FormWindowState.Normal;
            }

            try
            {
                if (_Form.Top < 0)
                    _Form.Top = 0;
                SaveIntegerSetting(sform + ".Top", _Form.Top);
            }
            catch
            {
            }

            try
            {
                if (_Form.Left < 0)
                    _Form.Left = 0;
                SaveIntegerSetting(sform + ".Left", _Form.Left);
            }
            catch
            {
            }

            try
            {
                SaveIntegerSetting(sform + ".Width", _Form.Width);
            }
            catch
            {
            }

            try
            {
                SaveIntegerSetting(sform + ".Height", _Form.Height);
            }
            catch
            {
            }

            try
            {
                SaveIntegerSetting(sform + ".WindowState", (int)_Form.WindowState);
            }
            catch
            {
            }

            _Form.WindowState = fws;
            if (bAlreadyOpened == false)
            {
                CloseConfigFile();
            }
        }

        private bool SaveConfigFile(string sFile, ref Exception err)
        {
            if (ReadOnlyMode == true)
            {
                return true;
            }

            ConfigFile dsFile;
            var ok = default(bool);
            if (_backedUp == false)
            {
                try
                {
                    // before doing a backup, verify that the file is valid.
                    var temp = new ConfigFile();
                    temp.ReadXmlSchema(_Filename);
                    temp.ReadXml(_Filename);
                    temp.Dispose();
                    File.Copy(_Filename, BackupFile, true);
                    _backedUp = true;
                }
                catch (Exception ex)
                {
                    err = new ApplicationException(string.Format(_rm.GetString("CONFIG_BACKUP_ERROR"), ex.Message));
                }
            }

            dsFile = new ConfigFile();
            dsFile.DataSetName = "Settings";
            if (_Settings is object)
            {
                foreach (ConfigFile.SettingsRow srow in _Settings.Rows)
                {
                    var srow2 = dsFile.Settings.NewSettingsRow();
                    srow2.Name = srow.Name;
                    srow2.Value = srow.Value;
                    dsFile.Settings.Rows.Add(srow2);
                }

                if (_Settings.Rows.Count > 0)
                {
                    try
                    {
                        dsFile.WriteXml(sFile, XmlWriteMode.WriteSchema);
                        ok = true;
                    }
                    catch (Exception ex)
                    {
                        err = ex;
                    }
                }
                else
                {
                    err = new Exception("No rows in _Settings");
                }
            }
            else
            {
                err = new NullReferenceException("_Settings is null");
            }

            return ok;
        }

        public void Dispose()
        {
            CloseConfigFile();
        }
    }
}
