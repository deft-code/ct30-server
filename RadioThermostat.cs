// Decompiled with JetBrains decompiler
// Type: Thermostat.RadioThermostat
// Assembly: Thermostat, Version=1.17.8.0, Culture=neutral, PublicKeyToken=null
// MVID: 54C9EF09-C304-4055-8B70-FB0008D71A00
// Assembly location: C:\Program Files (x86)\Thermostat for Windows\Thermostat.exe

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Thermostat.Serialization;

namespace Thermostat
{
  public class RadioThermostat
  {
    public static readonly string ProvisioningAddress = DirectConnection.NormalizeAddress("192.168.10.1");
    private DateTime e = DateTime.MinValue;
    public const string CurrentFirmwareVersion = "1.04.77";
    public const string RESULT_TSTAT_COMMAND_SUCCESS = "Tstat Command Processed\r\n";
    public const string RESULT_SYS_COMMAND_SUCCESS = "App Sys Command Processed\r\n";
    public const string RESULT_REBOOT_COMMAND_SUCCESS = "Rebooting WM. Green LED indicates bootup complete.";
    public const string DISCOVERY_BROADCAST_MESSAGE = "TYPE: WM-DISCOVER\r\nVERSION: 1.0\r\n\r\nservices: com.rtcoa.tstat*\r\n\r\n";
    public const int DefaultReceiveTimeout = 10000;
    public const int DefaultSendTimeout = 5000;
    public const int DefaultWorkingTime = 3000;
    private string a;
    private IThermostatConnection b;
    private Network c;
    private TStat d;

    public string UUID
    {
      get
      {
        return this.a;
      }
    }

    public Network Network
    {
      get
      {
        if (this.c == null)
          this.c = this.GetNetwork();
        return this.c;
      }
    }

    public DateTime LastDirectTstatTime
    {
      get
      {
        return this.e;
      }
      set
      {
        this.e = value;
      }
    }

    public TStat LastDirectTstat
    {
      get
      {
        return this.d;
      }
      set
      {
        this.d = value;
      }
    }

    public static float MaximumAllowedTemp
    {
      get
      {
        return 95f;
      }
    }

    public static float MinimumAllowedTemp
    {
      get
      {
        return 35f;
      }
    }

    public AdvancedFeatures Features
    {
      get
      {
        AdvancedFeatures advancedFeatures = AdvancedFeatures.None;
        string model = Config.Thermostat(this.UUID).Model;
        if (model == null)
          return AdvancedFeatures.None;
        if (model.StartsWith("CT80"))
          advancedFeatures = AdvancedFeatures.CT80;
        else if (model.StartsWith("CT50 V"))
        {
          Version version = new Version(model.Substring("CT50 V".Length, model.Length - "CT50 V".Length));
          return version.Major > 1 || version.Minor >= 94 ? AdvancedFeatures.AutoChangeover : AdvancedFeatures.None;
        }
        return advancedFeatures;
      }
    }

    public RadioThermostat(IThermostatConnection connection, string uuid)
    {
      this.b = connection;
      this.SetUUID(uuid);
    }

    public void SetUUID(string uuid)
    {
      this.a = uuid;
    }

    public static long ConvertUUID(string uuid)
    {
      return long.Parse(uuid, NumberStyles.AllowHexSpecifier);
    }

    public static string ConvertUUID(long uuidvalue)
    {
      return uuidvalue.ToString("X");
    }

    public static string ConvertUUID(int uuidhi, int uuidlo)
    {
      return RadioThermostat.ConvertUUID((long) (uint) uuidlo + ((long) uuidhi << 32));
    }

    public bool SetNetwork(Network net)
    {
      if (this.a == null)
        throw new ApplicationException("Thermostat UUID has not be set.");
      try
      {
        string str1 = "key";
        string str2 = e5.b(net.key);
        string str3 = e5.b(net.ssid);
        if (!string.IsNullOrWhiteSpace(net.pin))
        {
          if (net.security == WirelessSecurity.Open)
          {
            net.key = "";
            str1 = "pin";
            str2 = net.pin;
          }
          else if (!string.IsNullOrWhiteSpace(net.key))
            str2 = RadioThermostat.a(net.pin, this.UUID, net.key);
        }
        string content;
        if (net.ip == IpConfig.DHCP)
          content = string.Format("{{\"ssid\":\"{0}\",\"security\":{1},\"ip\":{2},\"" + str1 + "\":\"{3}\"}}", (object) str3, (object) net.security, (object) net.ip, (object) str2);
        else
          content = string.Format("{{\"ssid\":\"{0}\",\"security\":{1},\"ip\":{2},\"ipaddr\":\"{3}\",\"ipmask\":\"{4}\"," + (string.IsNullOrWhiteSpace(net.ipdns1) ? "" : "\"ipdns1\":\"" + net.ipdns1 + "\",") + (string.IsNullOrWhiteSpace(net.ipdns2) ? "" : "\"ipdns2\":\"" + net.ipdns2 + "\",") + "\"ipgw\":\"{5}\",\"" + str1 + "\":\"{6}\"}}", (object) str3, (object) net.security, (object) net.ip, (object) net.ipaddr, (object) net.ipmask, (object) net.ipgw, (object) str2);
        string A_0 = this.b.Write("/sys/network", content);
        this.InValidateNetwork();
        return RadioThermostat.a(A_0);
      }
      catch
      {
        return false;
      }
    }

    public bool SetName(string value)
    {
      try
      {
        return RadioThermostat.a(this.b.Write("/sys/name", "{\"name\":\"" + e5.b(value) + "\"}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetTime(Time time)
    {
      try
      {
        return RadioThermostat.b(this.b.Write("/tstat/time", "{\"day\":" + (object) ((int) time.day).ToString() + ",\"hour\":" + (string) (object) time.hour + ",\"minute\":" + (string) (object) time.minute + "}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetMode(Mode mode)
    {
      try
      {
        return RadioThermostat.b(this.b.Write("/tstat", "{\"tmode\":" + ((int) mode).ToString() + "}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetProvisioningMode(ProvisioningMode mode)
    {
      try
      {
        return RadioThermostat.a(this.b.Write("/sys/mode", "{\"mode\":" + ((int) mode).ToString() + "}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetHeatTemp(float temp)
    {
      try
      {
        if ((double) temp < (double) RadioThermostat.MinimumAllowedTemp)
          temp = RadioThermostat.MinimumAllowedTemp;
        else if ((double) temp > (double) RadioThermostat.MaximumAllowedTemp)
          temp = RadioThermostat.MaximumAllowedTemp;
        return RadioThermostat.b(this.b.Write("/tstat", "{\"t_heat\":" + temp.ToString("00.0") + "}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetHeatHoldTemp(float temp, Toggle hold)
    {
      try
      {
        if ((double) temp < (double) RadioThermostat.MinimumAllowedTemp)
          temp = RadioThermostat.MinimumAllowedTemp;
        else if ((double) temp > (double) RadioThermostat.MaximumAllowedTemp)
          temp = RadioThermostat.MaximumAllowedTemp;
        return RadioThermostat.b(this.b.Write("/tstat", "{\"tmode\":1,\"t_heat\":" + temp.ToString("00.0") + ",\"hold\":" + ((int) hold).ToString() + "}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetCoolTemp(float temp)
    {
      try
      {
        if ((double) temp < (double) RadioThermostat.MinimumAllowedTemp)
          temp = RadioThermostat.MinimumAllowedTemp;
        else if ((double) temp > (double) RadioThermostat.MaximumAllowedTemp)
          temp = RadioThermostat.MaximumAllowedTemp;
        return RadioThermostat.b(this.b.Write("/tstat", "{\"t_cool\":" + temp.ToString("00.0") + "}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetCoolHoldTemp(float temp, Toggle hold)
    {
      try
      {
        if ((double) temp < (double) RadioThermostat.MinimumAllowedTemp)
          temp = RadioThermostat.MinimumAllowedTemp;
        else if ((double) temp > (double) RadioThermostat.MaximumAllowedTemp)
          temp = RadioThermostat.MaximumAllowedTemp;
        return RadioThermostat.b(this.b.Write("/tstat", "{\"tmode\":2,\"t_cool\":" + temp.ToString("00.0") + ",\"hold\":" + ((int) hold).ToString() + "}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetFan(Fan mode)
    {
      try
      {
        return RadioThermostat.b(this.b.Write("/tstat", "{\"fmode\":" + ((int) mode).ToString() + "}"));
      }
      catch
      {
        return false;
      }
    }

    public bool SetHold(Toggle hold)
    {
      try
      {
        return RadioThermostat.b(this.b.Write("/tstat", "{\"hold\":" + ((int) hold).ToString() + "}"));
      }
      catch
      {
        return false;
      }
    }

    public void RestoreTstat(TStat tstat)
    {
      if (tstat.hold != Toggle.Off)
        return;
      if (tstat.tmode == Mode.Heat && RadioThermostat.IsTemperatureValid(tstat.t_heat))
      {
        this.SetHeatTemp(tstat.t_heat);
      }
      else
      {
        if (tstat.tmode != Mode.Cool || !RadioThermostat.IsTemperatureValid(tstat.t_cool))
          return;
        this.SetCoolTemp(tstat.t_cool);
      }
    }

    public static string GetDefaultHostName(string uuid)
    {
      if (!string.IsNullOrWhiteSpace(uuid))
      {
        if (uuid.Length == 12)
        {
          try
          {
            return "Thermostat-" + string.Format("{0}-{1}-{2}", (object) uuid.Substring(6, 2), (object) uuid.Substring(8, 2), (object) uuid.Substring(10, 2)).ToUpperInvariant();
          }
          catch
          {
            return (string) null;
          }
        }
      }
      return (string) null;
    }

    public void InValidateNetwork()
    {
      this.c = (Network) null;
    }

    public WeekSchedule GetFanSchedule(ConnectionType connection)
    {
      string result = this.b.Read("/tstat/program/fan");
      WeekSchedule weekSchedule = (WeekSchedule) null;
      if (!string.IsNullOrWhiteSpace(result))
        weekSchedule = WeekSchedule.FromJson(result);
      return weekSchedule;
    }

    public WeekSchedule GetHeatSchedule()
    {
      string result = this.b.Read("/tstat/program/heat", 6000, 5000, 14000);
      if (string.IsNullOrWhiteSpace(result) || result.Contains("\"error\""))
        return (WeekSchedule) null;
      else
        return WeekSchedule.FromJson(result);
    }

    public WeekSchedule GetCoolSchedule()
    {
      string result = this.b.Read("/tstat/program/cool", 6000, 5000, 14000);
      if (string.IsNullOrWhiteSpace(result) || result.Contains("\"error\""))
        return (WeekSchedule) null;
      else
        return WeekSchedule.FromJson(result);
    }

    public bool SetHeatSchedule(WeekSchedule schedule)
    {
      try
      {
        return RadioThermostat.b(this.b.Write("/tstat/program/heat", WeekSchedule.ToJson(schedule)));
      }
      catch
      {
        return false;
      }
    }

    public bool SetCoolSchedule(WeekSchedule schedule)
    {
      try
      {
        return RadioThermostat.b(this.b.Write("/tstat/program/cool", WeekSchedule.ToJson(schedule)));
      }
      catch
      {
        return false;
      }
    }

    public bool SetFanSchedule(WeekSchedule schedule)
    {
      try
      {
        return RadioThermostat.b(this.b.Write("/tstat/program/fan", WeekSchedule.ToJson(schedule)));
      }
      catch
      {
        return false;
      }
    }

    public List<Network> GetNetworkScan()
    {
      string A_0 = this.b.Read("/sys/scan");
      if (string.IsNullOrWhiteSpace(A_0))
        return (List<Network>) null;
      try
      {
        return RadioThermostat.d(A_0);
      }
      catch
      {
        return (List<Network>) null;
      }
    }

    private static List<Network> d(string A_0)
    {
      if (string.IsNullOrWhiteSpace(A_0))
        return (List<Network>) null;
      try
      {
        SortedList<string, Network> sortedList = new SortedList<string, Network>();
        string str1 = A_0;
        char[] separator = new char[2]
        {
          '[',
          ']'
        };
        int num = 1;
        foreach (string str2 in str1.Split(separator, (StringSplitOptions) num))
        {
          Network network = new Network();
          string[] strArray = str2.Split(new char[2]
          {
            ',',
            '"'
          }, StringSplitOptions.RemoveEmptyEntries);
          if (strArray.Length >= 5)
          {
            network.ssid = strArray[0];
            network.bssid = strArray[1];
            network.security = (WirelessSecurity) int.Parse(strArray[2]);
            network.channel = int.Parse(strArray[3]);
            network.rssi = int.Parse(strArray[4]);
            sortedList.Add(network.rssi.ToString() + network.ssid, network);
          }
        }
        List<Network> list = new List<Network>();
        foreach (Network network in (IEnumerable<Network>) sortedList.Values)
          list.Add(network);
        return list;
      }
      catch
      {
        return (List<Network>) null;
      }
    }

    public string GetModel()
    {
      return RadioThermostat.ParseModel(this.b.Read("/tstat/model"));
    }

    public static string ParseModel(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
        return (string) null;
      try
      {
        if ((int) json[0] != 123 || (int) json[json.Length - 1] != 125)
          return (string) null;
        int startIndex1 = json.IndexOf(":");
        int startIndex2 = json.IndexOf('"', startIndex1) + 1;
        int num = json.LastIndexOf('"');
        return json.Substring(startIndex2, num - startIndex2);
      }
      catch
      {
        return (string) null;
      }
    }

    public ProvisioningMode GetProvisioningMode()
    {
      string str = this.b.Read("/sys/mode");
      try
      {
        if (str == "{\"mode\":0}")
          return ProvisioningMode.Provisioning;
        return str == "{\"mode\":1}" ? ProvisioningMode.Normal : ProvisioningMode.Unknown;
      }
      catch
      {
        return ProvisioningMode.Unknown;
      }
    }

    public Cloud GetCloud()
    {
      string input = this.b.Read("/cloud");
      if (string.IsNullOrWhiteSpace(input))
        return (Cloud) null;
      try
      {
        return b0.a.Deserialize<Cloud>(input);
      }
      catch
      {
        return (Cloud) null;
      }
    }

    public string GetName()
    {
      string str1 = this.b.Read("/sys/name");
      if (string.IsNullOrWhiteSpace(str1))
        return (string) null;
      string str2 = str1.Trim();
      try
      {
        if ((int) str2[0] != 123 || (int) str2[str2.Length - 1] != 125)
          return (string) null;
        int startIndex1 = str2.IndexOf(":");
        int startIndex2 = str2.IndexOf('"', startIndex1) + 1;
        int num = str2.LastIndexOf('"');
        return str2.Substring(startIndex2, num - startIndex2);
      }
      catch
      {
        return (string) null;
      }
    }

    public Information GetInformation()
    {
      string input = this.b.Read("/sys");
      if (string.IsNullOrWhiteSpace(input))
        return (Information) null;
      try
      {
        return b0.a.Deserialize<Information>(input);
      }
      catch
      {
        return (Information) null;
      }
    }

    public TStat GetTStat()
    {
      string input = this.b.Read("/tstat");
      if (string.IsNullOrWhiteSpace(input))
        return (TStat) null;
      try
      {
        TStat tstat = b0.a.Deserialize<TStat>(input);
        if (tstat != null && tstat.IsValid)
          return tstat;
        else
          return (TStat) null;
      }
      catch
      {
        return (TStat) null;
      }
    }

    public Network GetNetwork()
    {
      string input = this.b.Read("/sys/network");
      if (string.IsNullOrWhiteSpace(input))
        return (Network) null;
      try
      {
        Network network = b0.a.Deserialize<Network>(input);
        if (network != null)
          this.c = network;
        return network;
      }
      catch
      {
        return (Network) null;
      }
    }

    public DataLog GetDataLog()
    {
      string input = this.b.Read("/tstat/datalog");
      if (string.IsNullOrWhiteSpace(input))
        return (DataLog) null;
      try
      {
        return b0.a.Deserialize<DataLog>(input);
      }
      catch
      {
        return (DataLog) null;
      }
    }

    public Humidity GetHumidity()
    {
      string input = this.b.Read("/tstat/humidity");
      if (string.IsNullOrWhiteSpace(input))
        return (Humidity) null;
      try
      {
        return b0.a.Deserialize<Humidity>(input);
      }
      catch
      {
        return (Humidity) null;
      }
    }

    public Power GetPower()
    {
      string input = this.b.Read("/tstat/power");
      if (string.IsNullOrWhiteSpace(input))
        return (Power) null;
      try
      {
        return b0.a.Deserialize<Power>(input);
      }
      catch
      {
        return (Power) null;
      }
    }

    public bool Reboot()
    {
      try
      {
        int result = int.MinValue;
        if (!int.TryParse(Config.Thermostat(this.UUID).Api, out result))
          throw new ApplicationException("API verison configuration is unknown.");
        string A_0 = result >= 112 ? this.b.Write("/sys/command", "{\"command\":\"reboot\"}") : this.b.Write("/sys/cmd/", "command=reboot");
        if (!("Rebooting WM. Green LED indicates bootup complete." == A_0) && !RadioThermostat.a(A_0))
          return false;
        Thread.Sleep(2000);
        return true;
      }
      catch
      {
        return false;
      }
    }

    public static bool Reboot(string currentApi, string baseUrl)
    {
      try
      {
        int result = int.MinValue;
        if (!int.TryParse(currentApi, out result))
          throw new ApplicationException("API verison configuration is unknown.");
        string A_0 = result >= 112 ? a8.a("/sys/command", "{\"command\":\"reboot\"}") : a8.a("/sys/cmd/", "command=reboot");
        if (!("Rebooting WM. Green LED indicates bootup complete." == A_0) && !RadioThermostat.a(A_0))
          return false;
        Thread.Sleep(2000);
        return true;
      }
      catch
      {
        return false;
      }
    }

    public static bool UpgradeFirmware(string baseUrl, string currentApi, string filesystemFilePath, string firmwareFilePath)
    {
      byte[] filesystem;
      byte[] firmware;
      try
      {
        if (!System.IO.File.Exists(filesystemFilePath))
          throw new FileNotFoundException("'" + filesystemFilePath + "' cannot be found", filesystemFilePath);
        if (!System.IO.File.Exists(firmwareFilePath))
          throw new FileNotFoundException("'" + firmwareFilePath + "' cannot be found", firmwareFilePath);
        using (FileStream fileStream = System.IO.File.OpenRead(filesystemFilePath))
        {
          using (BinaryReader binaryReader = new BinaryReader((Stream) fileStream))
            filesystem = binaryReader.ReadBytes((int) new FileInfo(filesystemFilePath).Length);
        }
        using (FileStream fileStream = System.IO.File.OpenRead(firmwareFilePath))
        {
          using (BinaryReader binaryReader = new BinaryReader((Stream) fileStream))
            firmware = binaryReader.ReadBytes((int) new FileInfo(firmwareFilePath).Length);
        }
      }
      catch
      {
        return false;
      }
      return RadioThermostat.UpgradeFirmware(baseUrl, currentApi, filesystem, firmware);
    }

    public static bool UpgradeFirmware(string baseUrl, string currentApi, byte[] filesystem, byte[] firmware)
    {
      try
      {
        string str1 = Path.Combine(Config.GetApplicationTempFolder(), "filesystem");
        string str2 = Path.Combine(Config.GetApplicationTempFolder(), "firmware");
        if (System.IO.File.Exists(str1))
          System.IO.File.Delete(str1);
        if (System.IO.File.Exists(str2))
          System.IO.File.Delete(str2);
        using (FileStream fileStream = System.IO.File.Create(str1))
        {
          fileStream.Write(filesystem, 0, filesystem.Length);
          ((Stream) fileStream).Flush();
          fileStream.Close();
        }
        using (FileStream fileStream = System.IO.File.Create(str2))
        {
          fileStream.Write(firmware, 0, firmware.Length);
          ((Stream) fileStream).Flush();
          fileStream.Close();
        }
        int result = int.MinValue;
        if (!int.TryParse(currentApi, out result))
          throw new ApplicationException("API verison configuration is unknown.");
        if (result >= 112)
        {
          if (!RadioThermostat.a(new WebClient().UploadFile(baseUrl + "/sys/filesystem", str1)) || !RadioThermostat.a(new WebClient().UploadFile(baseUrl + "/sys/firmware", str2)))
            return false;
        }
        else
        {
          if (Encoding.UTF8.GetString(new WebClient().UploadFile(baseUrl + "/sys/update-fs/", str1)).IndexOf("Update SUCCEEDED") <= -1)
            return false;
          Thread.Sleep(3000);
          if (Encoding.UTF8.GetString(new WebClient().UploadFile(baseUrl + "/sys/update-fw/", str2)).IndexOf("Update SUCCEEDED") <= -1)
            return false;
        }
        if (System.IO.File.Exists(str1))
          System.IO.File.Delete(str1);
        if (System.IO.File.Exists(str2))
          System.IO.File.Delete(str2);
        RadioThermostat.Reboot(currentApi, baseUrl);
      }
      catch (Exception ex)
      {
        return false;
      }
      return true;
    }

    private static string a(string A_0, string A_1, string A_2)
    {
      try
      {
        byte[] bytes1 = Encoding.UTF8.GetBytes(A_1);
        byte[] bytes2 = new Rfc2898DeriveBytes(A_0, bytes1, 1000).GetBytes(16);
        return BitConverter.ToString(RadioThermostat.a(A_2, bytes2, bytes2)).Replace("-", "").ToLowerInvariant();
      }
      catch
      {
        return (string) null;
      }
    }

    public static string Decrypt()
    {
      FileStream fileStream = new FileStream("c:\\temp\\response.bin", FileMode.Open, FileAccess.Read);
      BinaryReader binaryReader = new BinaryReader((Stream) fileStream);
      string str1 = "NOZIPBPF";
      string str2 = "5cdad447e418";
      string A_0 = "186b4dfb94dd9df226b4c3116a18dc51";
      byte[] numArray1 = new byte[(int) fileStream.Length];
      binaryReader.Read(numArray1, 0, (int) fileStream.Length);
      fileStream.Close();
      Encoding.UTF8.GetBytes(str1);
      byte[] numArray2 = RadioThermostat.c(A_0);
      byte[] bytes = Encoding.UTF8.GetBytes(str2);
      Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(str1, bytes, 1000);
      RadioThermostat.c(RadioThermostat.a(str1, str2, str1));
      rfc2898DeriveBytes.GetBytes(16);
      try
      {
        return RadioThermostat.a(numArray1, numArray2, numArray2);
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }

    private static byte[] c(string A_0)
    {
      int length = A_0.Length;
      byte[] numArray = new byte[length / 2];
      int startIndex = 0;
      while (startIndex < length)
      {
        numArray[startIndex / 2] = Convert.ToByte(A_0.Substring(startIndex, 2), 16);
        startIndex += 2;
      }
      return numArray;
    }

    private static string a(byte[] A_0, byte[] A_1, byte[] A_2)
    {
      if (A_0 == null || A_0.Length <= 0)
        throw new ArgumentNullException("cipher");
      if (A_1 == null || A_1.Length <= 0)
        throw new ArgumentNullException("key");
      if (A_2 == null || A_2.Length <= 0)
        throw new ArgumentNullException("iv");
      MemoryStream memoryStream = (MemoryStream) null;
      CryptoStream cryptoStream = (CryptoStream) null;
      StreamReader streamReader = (StreamReader) null;
      RijndaelManaged rijndaelManaged = (RijndaelManaged) null;
      try
      {
        rijndaelManaged = new RijndaelManaged();
        rijndaelManaged.Key = A_1;
        rijndaelManaged.IV = A_2;
        rijndaelManaged.Mode = CipherMode.CBC;
        rijndaelManaged.Padding = PaddingMode.Zeros;
        rijndaelManaged.BlockSize = 16;
        ICryptoTransform decryptor = rijndaelManaged.CreateDecryptor(rijndaelManaged.Key, rijndaelManaged.IV);
        memoryStream = new MemoryStream(A_0);
        cryptoStream = new CryptoStream((Stream) memoryStream, decryptor, CryptoStreamMode.Read);
        streamReader = new StreamReader((Stream) cryptoStream);
        return streamReader.ReadToEnd();
      }
      finally
      {
        if (streamReader != null)
          streamReader.Close();
        if (cryptoStream != null)
          cryptoStream.Close();
        if (memoryStream != null)
          memoryStream.Close();
        if (rijndaelManaged != null)
          rijndaelManaged.Clear();
      }
    }

    private static byte[] a(string A_0, byte[] A_1, byte[] A_2)
    {
      if (A_0 == null || A_0.Length <= 0)
        throw new ArgumentNullException("plaintext");
      if (A_1 == null || A_1.Length <= 0)
        throw new ArgumentNullException("key");
      if (A_2 == null || A_2.Length <= 0)
        throw new ArgumentNullException("iv");
      MemoryStream memoryStream = (MemoryStream) null;
      CryptoStream cryptoStream = (CryptoStream) null;
      StreamWriter streamWriter = (StreamWriter) null;
      RijndaelManaged rijndaelManaged = (RijndaelManaged) null;
      try
      {
        rijndaelManaged = new RijndaelManaged();
        rijndaelManaged.Key = A_1;
        rijndaelManaged.IV = A_2;
        rijndaelManaged.Mode = CipherMode.CBC;
        rijndaelManaged.Padding = PaddingMode.PKCS7;
        rijndaelManaged.BlockSize = 128;
        ICryptoTransform encryptor = rijndaelManaged.CreateEncryptor(rijndaelManaged.Key, rijndaelManaged.IV);
        memoryStream = new MemoryStream();
        cryptoStream = new CryptoStream((Stream) memoryStream, encryptor, CryptoStreamMode.Write);
        streamWriter = new StreamWriter((Stream) cryptoStream);
        streamWriter.Write(A_0);
      }
      finally
      {
        if (streamWriter != null)
          streamWriter.Close();
        if (cryptoStream != null)
          cryptoStream.Close();
        if (memoryStream != null)
          memoryStream.Close();
        if (rijndaelManaged != null)
          rijndaelManaged.Clear();
      }
      return memoryStream.ToArray();
    }

    public bool SetAuthKey(string authKey)
    {
      try
      {
        int result = 112;
        try
        {
          int.TryParse(Config.Thermostat(this.UUID).Api, out result);
        }
        catch
        {
        }
        if (result > 1)
        {
          string str = this.b.Write("/cloud", "{\"authkey\":\"" + authKey + "\"}");
          return !string.IsNullOrWhiteSpace(str) && (str.IndexOf("Operation Successful") > -1 || str.ToLowerInvariant().Contains("\"success\""));
        }
        else
        {
          string str = this.b.Write("/cloud/auth-key", authKey);
          return !string.IsNullOrWhiteSpace(str) && (str.IndexOf("Authkey sent") > -1 || str.ToLowerInvariant().Contains("\"success\""));
        }
      }
      catch
      {
        return false;
      }
    }

    public bool SetCloudUrl(string url)
    {
      try
      {
        int result = 112;
        try
        {
          int.TryParse(Config.Thermostat(this.UUID).Api, out result);
        }
        catch
        {
        }
        if (result > 1)
        {
          string str = this.b.Write("/cloud", "{\"url\":\"" + e5.b(url) + "\"}");
          return !string.IsNullOrWhiteSpace(str) && (str.IndexOf("Operation Successful") > -1 || str.ToLowerInvariant().Contains("\"success\""));
        }
        else
        {
          string str = this.b.Write("/cloud/url", url);
          return !string.IsNullOrWhiteSpace(str) && (str.IndexOf("Cloud URL Sent") > -1 || str.ToLowerInvariant().Contains("\"success\""));
        }
      }
      catch
      {
        return false;
      }
    }

    private static bool b(string A_0)
    {
      if (string.IsNullOrWhiteSpace(A_0))
        return false;
      if (!(A_0 == "Tstat Command Processed\r\n"))
        return A_0.ToLowerInvariant().Contains("\"success\"");
      else
        return true;
    }

    private static bool a(string A_0)
    {
      if (string.IsNullOrWhiteSpace(A_0))
        return false;
      if (!(A_0 == "App Sys Command Processed\r\n"))
        return A_0.ToLowerInvariant().Contains("\"success\"");
      else
        return true;
    }

    private static bool b(byte[] A_0)
    {
      if (A_0 == null)
        return false;
      else
        return RadioThermostat.b(Encoding.UTF8.GetString(A_0));
    }

    private static bool a(byte[] A_0)
    {
      if (A_0 == null)
        return false;
      else
        return RadioThermostat.a(Encoding.UTF8.GetString(A_0));
    }

    public bool SetCloudUpdates(bool enable)
    {
      Cloud cloud = this.GetCloud();
      if (cloud == null || string.IsNullOrWhiteSpace(cloud.url))
        return false;
      else
        return this.SetCloudUrl(enable ? cloud.url.Replace("http://disabled/", "http://") : cloud.url.Replace("http://", "http://disabled/"));
    }

    public static bool IsProvisioning(string baseUrl)
    {
      try
      {
        return DirectConnection.NormalizeAddress(baseUrl) == RadioThermostat.ProvisioningAddress;
      }
      catch
      {
        return false;
      }
    }

    public static bool IsTemperatureValid(int temp)
    {
      return RadioThermostat.IsTemperatureValid((float) temp);
    }

    public static bool IsTemperatureValid(float temp)
    {
      try
      {
        return (double) temp >= (double) RadioThermostat.MinimumAllowedTemp && (double) temp <= (double) RadioThermostat.MaximumAllowedTemp;
      }
      catch
      {
        return false;
      }
    }

    public virtual bool IsFeatureSupported(AdvancedFeatures feature)
    {
      string model = Config.Thermostat(this.UUID).Model;
      if (model == null)
        return false;
      switch (feature)
      {
        case AdvancedFeatures.AutoChangeover:
          if (model.StartsWith("CT80"))
            return true;
          if (model.StartsWith("CT50 V"))
          {
            Version version = new Version(model.Substring("CT50 V".Length, model.Length - "CT50 V".Length));
            if (version.Major > 1 || version.Minor >= 94)
              return true;
          }
          return false;
        case AdvancedFeatures.FanCirculate:
        case AdvancedFeatures.Humidity:
          if (model.StartsWith("CT80"))
            return true;
          else
            break;
      }
      return false;
    }

    public static int GetSchedulePeriodsPerDay()
    {
      return 4;
    }
  }
}
