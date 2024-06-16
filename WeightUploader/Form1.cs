using Dynastream.Fit;
using GarminConnectClient.Lib.Dto;
using GarminConnectClient.Lib.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using GarminConnectClient.Lib;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using System.Diagnostics; // potrzebujemy do pobrania id procesora, żeby zabezpieczyć hasło

namespace WeightUploader
{
    public partial class Form1 : Form
    {
        string filePath = "values.xml";
        string exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        // tworzenie tymczasowego katalogu na pliki .fit
        private static readonly string _tmpDir = Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp")).FullName;

        public Form1()
        {
            InitializeComponent();
        }

        static string GetProcessorId()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
            {
                var query = searcher.Get().OfType<ManagementObject>().FirstOrDefault();
                return query["ProcessorId"]?.ToString();
            }
        }

        static string Encrypt(string plainText, string key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key));
                aesAlg.GenerateIV();
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                        csEncrypt.Write(plainTextBytes, 0, plainTextBytes.Length);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        static string Decrypt(string cipherText, string key)
        {
            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                using (MemoryStream msDecrypt = new MemoryStream(fullCipher))
                {
                    using (Aes aesAlg = Aes.Create())
                    {
                        byte[] ivLengthBuffer = new byte[sizeof(int)];
                        msDecrypt.Read(ivLengthBuffer, 0, ivLengthBuffer.Length);
                        int ivLength = BitConverter.ToInt32(ivLengthBuffer, 0);

                        byte[] iv = new byte[ivLength];
                        msDecrypt.Read(iv, 0, iv.Length);

                        aesAlg.IV = iv;
                        aesAlg.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key));

                        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        using (MemoryStream msPlainText = new MemoryStream())
                        {
                            csDecrypt.CopyTo(msPlainText);
                            byte[] decryptedBytes = msPlainText.ToArray();
                            string result = Encoding.UTF8.GetString(decryptedBytes);
                            return result;
                        }
                    }
                }
            }
            catch (FormatException)
            {
                Debug.WriteLine("The provided cipher text is not a valid base64 string.");
                return null;
            }
            catch (CryptographicException ex)
            {
                Debug.WriteLine("An error occurred during decryption: " + ex.Message);
                return null;
            }
        }

        private static GarminConnectClient.Lib.IConfiguration SetupConfiguration(string userName, string password)
        {
            var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(
            new Dictionary<string, string>
            {
                ["AppConfig:BackupDir"] = _tmpDir,
            });
            IConfigurationRoot configuration = builder.Build();
            return new Configuration(configuration);
        }

        async public static Task<bool> UploadAsync(string fileWage, string filePressure, string email, string password)
        {
            var configuration = SetupConfiguration(email, password);
            var logger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger<Client>();
            var client = new Client(configuration, logger);
            await client.Authenticate(email, password);

            if (fileWage != null)
            {
                if (!(await client.UploadActivity(fileWage, new FileFormat { FormatKey = "fit" })).Success)
                {
                    throw new Exception("Wystąpił błąd podczas wysyłania pliku z wagą");
                }
            }

            if (filePressure != null)
            {
                if (!(await client.UploadActivity(filePressure, new FileFormat { FormatKey = "fit" })).Success)
                {
                    throw new Exception("Wystąpił błąd podczas wysyłania pliku z ciśnieniem");
                }
            }
            return true;
        }

        private void SaveData()
        {
            // tworzenie "polishculture", czyli zapisywanie "po polsku" różnych rzeczy,
            // w tym przypadku chodzi o dodawanie przecinka zamiast kropki w liczbach dziesiętnych

            // generalnie zrobiliśmy to, bo potem się nie chciały dane wczytywać z pliku xD
            CultureInfo polishCulture = new CultureInfo("pl-PL");

            // zapisywanie danych do pliku
            XDocument doc = new XDocument(
                new XElement("Dane",
                    new XElement("val_wage", txt_wage.Value.ToString(polishCulture)),
                    new XElement("val_bmi", txt_bmi.Value.ToString(polishCulture)),
                    new XElement("val_tkanka", txt_tkanka.Value.ToString(polishCulture)),
                    new XElement("val_muscle", txt_muscle.Value.ToString(polishCulture)),
                    new XElement("val_kosci", txt_kosci.Value.ToString(polishCulture)),
                    new XElement("val_water", txt_water.Value.ToString(polishCulture)),
                    new XElement("val_cialo", txt_cialo.Value.ToString(polishCulture)),
                    new XElement("val_trzewna", txt_trzewna.Value.ToString(polishCulture)),
                    new XElement("val_wiek", txt_wiek.Value.ToString(polishCulture)),
                    new XElement("val_kcal", txt_kcal.Value.ToString(polishCulture)),

                    new XElement("val_sys", txt_sys.Value.ToString(polishCulture)),
                    new XElement("val_dia", txt_dia.Value.ToString(polishCulture)),
                    new XElement("val_bpm", txt_bpm.Value.ToString(polishCulture)),
                    new XElement("val_login", txt_login.Text),
                    new XElement("val_password", Encrypt(txt_password.Text, GetProcessorId())),
                    new XElement("val_scale", check_wage.Checked),
                    new XElement("val_press", check_press.Checked),
                    new XElement("val_export", check_export.Checked)
                )
            );

            // Zapis dokumentu XML do pliku
            doc.Save(exePath + "\\" + filePath);
        }

        private void LoadData()
        {
            // Domyślne wartości
            float defaultWage = 0.0f;
            float defaultBmi = 0.0f;
            float defaultTkanka = 0.0f;
            float defaultMuscle = 0.0f;
            float defaultKosci = 0.0f;
            float defaultWater = 0.0f;

            int defaultCialo = 1;
            int defaultTrzewna = 1;
            int defaultWiek = 1;
            int defaultKcal = 1;

            int defaultSys = 40;
            int defaultDia = 30;
            int defaultBpm = 65;

            float val_wage;
            float val_bmi;
            float val_tkanka;
            float val_muscle;
            float val_kosci;
            float val_water;

            int val_cialo;
            int val_trzewna;
            int val_wiek;
            int val_kcal;

            int val_sys;
            int val_dia;
            int val_bpm;

            string val_scale;
            string val_press;
            string val_export;

            string val_login;
            string val_password;

            if (System.IO.File.Exists(exePath + "\\" + filePath))
            {
                // Odczyt dokumentu XML z pliku
                XDocument doc = XDocument.Load(exePath + "\\" + filePath);

                // Sprawdzenie i parsowanie wartości z dokumentu XML
                val_wage = GetElementFloatValue(doc, "val_wage", defaultWage);
                val_bmi = GetElementFloatValue(doc, "val_bmi", defaultBmi);
                val_tkanka = GetElementFloatValue(doc, "val_tkanka", defaultTkanka);
                val_muscle = GetElementFloatValue(doc, "val_muscle", defaultMuscle);
                val_kosci = GetElementFloatValue(doc, "val_kosci", defaultKosci);
                val_water = GetElementFloatValue(doc, "val_water", defaultWater);

                val_cialo = GetElementIntValue(doc, "val_cialo", defaultCialo);
                val_trzewna = GetElementIntValue(doc, "val_trzewna", defaultTrzewna);
                val_wiek = GetElementIntValue(doc, "val_wiek", defaultWiek);
                val_kcal = GetElementIntValue(doc, "val_kcal", defaultKcal);

                val_sys = GetElementIntValue(doc, "val_sys", defaultSys);
                val_dia = GetElementIntValue(doc, "val_dia", defaultDia);
                val_bpm = GetElementIntValue(doc, "val_bpm", defaultBpm);

                val_login = GetElementStringValue(doc, "val_login", "");
                val_password = GetElementStringValue(doc, "val_password", "");
                val_scale = GetElementStringValue(doc, "val_scale", "true");
                val_press = GetElementStringValue(doc, "val_press", "true");
                val_export = GetElementStringValue(doc, "val_export", "true");
            }
            else
            {
                // Użycie domyślnych wartości, jeśli plik nie istnieje
                val_wage = defaultWage;
                val_bmi = defaultBmi;
                val_tkanka = defaultTkanka;
                val_muscle = defaultMuscle;
                val_kosci = defaultKosci;
                val_water = defaultWater;

                val_cialo = defaultCialo;
                val_trzewna = defaultTrzewna;
                val_wiek = defaultWiek;
                val_kcal = defaultKcal;

                val_sys = defaultSys;
                val_dia = defaultDia;
                val_bpm = defaultBpm;

                val_login = "";
                val_password = "";
                val_scale = "true";
                val_press = "true";
                val_export = "true";
            }

            // Wyświetlanie odczytanych wartości
            txt_wage.Value = (decimal)val_wage;
            txt_bmi.Value = (decimal)val_bmi;
            txt_tkanka.Value = (decimal)val_tkanka;
            txt_muscle.Value = (decimal)val_muscle;
            txt_kosci.Value = (decimal)val_kosci;
            txt_water.Value = (decimal)val_water;
            txt_cialo.Value = val_cialo;
            txt_trzewna.Value = val_trzewna;
            txt_wiek.Value = val_wiek;
            txt_kcal.Value = val_kcal;

            txt_sys.Value = val_sys;
            txt_dia.Value = val_dia;
            txt_bpm.Value = val_bpm;

            txt_login.Text = val_login;
            //MessageBox.Show(val_password);
            txt_password.Text = Decrypt(val_password, GetProcessorId());

            check_wage.Checked = bool.Parse(val_scale);
            check_press.Checked = bool.Parse(val_press);
            check_export.Checked = bool.Parse(val_export);
        }

        private static string GetElementStringValue(XDocument doc, string elementName, string defaultValue)
        {
            XElement element = doc.Root.Element(elementName);
            if (element != null)
            {
                return element.Value;
            }
            else
            {
                return defaultValue;
            }
        }

        private static float GetElementFloatValue(XDocument doc, string elementName, float defaultValue)
        {
            XElement element = doc.Root.Element(elementName);
            if (element != null && float.TryParse(element.Value, out float parsedValue))
            {
                return parsedValue;
            }
            else
            {
                return defaultValue;
            }
        }

        private static int GetElementIntValue(XDocument doc, string elementName, int defaultValue)
        {
            XElement element = doc.Root.Element(elementName);
            if (element != null && int.TryParse(element.Value, out int parsedValue))
            {
                return parsedValue;
            }
            else
            {
                return defaultValue;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // Pobierz aktualny czas w milisekundach w formacie UNIX
            long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Generuj unikalną nazwę pliku dla wagi
            string fitFilePath = $"waga_{unixTimestamp}.fit";
            fitFilePath = _tmpDir + "\\" + fitFilePath;

            // generuj unikalną nazwę pliku dla ciśnienia
            string fitFilePath2 = $"cis_{unixTimestamp}.fit";
            fitFilePath2 = _tmpDir + "\\" + fitFilePath2;

            if (check_wage.Checked == true)
            {
                // Tworzenie pliku FIT dla wagi
                using (var fitDest = new FileStream(fitFilePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    // ustalamy strefę czasową i dodajemy ją do wybranej daty / godziny
                    System.DateTime selectedDateTime = dateTimePicker1.Value;
                    TimeZoneInfo localZone = TimeZoneInfo.Local;
                    DateTimeOffset dateTimeWithZone = new DateTimeOffset(selectedDateTime, localZone.GetUtcOffset(selectedDateTime));

                    // Stwórz nowego pisarza (encoder) do zapisywania danych FIT
                    var encoder = new Encode();

                    // Rozpocznij kodowanie pliku FIT
                    encoder.Open(fitDest);

                    // Tworzenie wiadomości FileId (identyfikator pliku)
                    var fileIdMesg = new FileIdMesg();
                    fileIdMesg.SetType(Dynastream.Fit.File.Activity); // Ustaw typ na Activity
                    fileIdMesg.SetManufacturer(Manufacturer.Garmin);
                    fileIdMesg.SetProduct(GarminProduct.IndexSmartScale);
                    fileIdMesg.SetSerialNumber(1); // Przykładowy numer seryjny
                    fileIdMesg.SetTimeCreated(new Dynastream.Fit.DateTime(dateTimeWithZone.UtcDateTime));

                    encoder.Write(fileIdMesg);

                    // Tworzenie wiadomości DeviceInfo (informacje o urządzeniu)
                    var deviceInfoMesg = new DeviceInfoMesg();
                    deviceInfoMesg.SetTimestamp(new Dynastream.Fit.DateTime(dateTimeWithZone.UtcDateTime));
                    deviceInfoMesg.SetManufacturer(Manufacturer.Garmin);
                    deviceInfoMesg.SetProduct(GarminProduct.IndexSmartScale); // 2429
                    deviceInfoMesg.SetSerialNumber(1);

                    encoder.Write(deviceInfoMesg);

                    // Tworzenie wiadomości WeightScale (dane ważenia)
                    var weightScaleMesg = new WeightScaleMesg();
                    weightScaleMesg.SetTimestamp(new Dynastream.Fit.DateTime(dateTimeWithZone.UtcDateTime));
                    weightScaleMesg.SetWeight((float)txt_wage.Value); // Waga w kilogramach
                    weightScaleMesg.SetBmi((float)txt_bmi.Value); // BMI
                    weightScaleMesg.SetPercentFat((float)txt_tkanka.Value); // Procent tłuszczu
                    weightScaleMesg.SetMuscleMass((float)txt_muscle.Value); // Masa mięśni szkieletowych
                    weightScaleMesg.SetBoneMass((float)txt_kosci.Value); // Masa kości

                    weightScaleMesg.SetPercentHydration((float)txt_water.Value); // Procent nawodnienia
                    weightScaleMesg.SetPhysiqueRating((byte)txt_cialo.Value); // Ocena składu ciała
                    weightScaleMesg.SetVisceralFatRating((byte)txt_trzewna.Value); // visceral fat (trzewna tkanka tłuszczowa)
                    weightScaleMesg.SetMetabolicAge((byte)txt_wiek.Value);    // wiek metaboliczny

                    // niby nie są brane pod uwagę, no ale zapiszemy, czemu nie
                    weightScaleMesg.SetBasalMet((float)txt_kcal.Value); // BMR (spożyte kalorie)
                    weightScaleMesg.SetActiveMet((float)txt_kcal.Value); // BMR (aktywne kalorie)  

                    encoder.Write(weightScaleMesg);

                    // Zakończenie kodowania pliku FIT
                    encoder.Close();
                }
            }

            if (check_press.Checked == true)
            {
                // tworzenie pliku fit dka ciśnienia
                using (var fitDest = new FileStream(fitFilePath2, FileMode.Create, FileAccess.ReadWrite))
                {
                    // ustalamy strefę czasową i dodajemy ją do wybranej daty / godziny
                    System.DateTime selectedDateTime = dateTimePicker1.Value;
                    TimeZoneInfo localZone = TimeZoneInfo.Local;
                    DateTimeOffset dateTimeWithZone = new DateTimeOffset(selectedDateTime, localZone.GetUtcOffset(selectedDateTime));

                    // Stwórz nowego pisarza (encoder) do zapisywania danych FIT
                    var encoder = new Encode();

                    // Rozpocznij kodowanie pliku FIT
                    encoder.Open(fitDest);

                    // Tworzenie wiadomości FileId (identyfikator pliku)
                    var fileIdMesg = new FileIdMesg();
                    fileIdMesg.SetType(Dynastream.Fit.File.Activity); // Ustaw typ na Activity
                    fileIdMesg.SetManufacturer(Manufacturer.Garmin);
                    fileIdMesg.SetProduct(GarminProduct.IndexSmartScale);
                    fileIdMesg.SetSerialNumber(1); // Przykładowy numer seryjny
                    fileIdMesg.SetTimeCreated(new Dynastream.Fit.DateTime(dateTimeWithZone.UtcDateTime));

                    encoder.Write(fileIdMesg);

                    // Tworzenie wiadomości DeviceInfo (informacje o urządzeniu)
                    var deviceInfoMesg = new DeviceInfoMesg();
                    deviceInfoMesg.SetTimestamp(new Dynastream.Fit.DateTime(dateTimeWithZone.UtcDateTime));
                    deviceInfoMesg.SetManufacturer(Manufacturer.Garmin);
                    deviceInfoMesg.SetProduct(GarminProduct.IndexSmartScale); // 2429
                    deviceInfoMesg.SetSerialNumber(1);

                    encoder.Write(deviceInfoMesg);

                    // obsługa samego zapisu ciśnienia
                    var bpMesg = new BloodPressureMesg();
                    bpMesg.SetTimestamp(new Dynastream.Fit.DateTime(dateTimeWithZone.UtcDateTime));
                    bpMesg.SetSystolicPressure((ushort)txt_sys.Value); // Example systolic pressure
                    bpMesg.SetDiastolicPressure((ushort)txt_dia.Value); // Example diastolic pressure
                    bpMesg.SetMeanArterialPressure((ushort)((ushort)txt_dia.Value + ((ushort)txt_sys.Value - (ushort)txt_dia.Value) / 3)); // Example calculation for mean arterial pressure
                    bpMesg.SetUserProfileIndex(0); // Example user profile index
                    bpMesg.SetHeartRate((byte)txt_bpm.Value);
                    bpMesg.SetHeartRateType(HrType.Normal);
                    encoder.Write(bpMesg);

                    // Zakończenie kodowania pliku FIT
                    encoder.Close();
                }
            }

            // jeżeli wszystko poszło ok, to wysyłamy plik na serwer...
            try
            {
                if (check_export.Checked == true)
                {
                    send.Enabled = false;

                    if ((check_wage.Checked == true) && (check_press.Checked == true))
                    {
                        await UploadAsync(fitFilePath, fitFilePath2, txt_login.Text, txt_password.Text);
                    } else if ((check_wage.Checked == true) && (check_press.Checked == false))
                    {
                        await UploadAsync(fitFilePath, null, txt_login.Text, txt_password.Text);
                    } else if ((check_wage.Checked == false) && (check_press.Checked == true))
                    {
                        await UploadAsync(null, fitFilePath2, txt_login.Text, txt_password.Text);
                    } else
                    {
                        MessageBox.Show("Nic nie wybrano :(");
                        return;
                    }
                    MessageBox.Show("Upload successful");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Upload failed: {ex.Message}");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // zapisywanie danych
            SaveData();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // wczytywanie danych
            LoadData();
        }

        private void check_export_CheckedChanged(object sender, EventArgs e)
        {
            txt_login.Enabled = check_export.Checked;
            txt_password.Enabled = check_export.Checked;

            txt_sys.Enabled = check_press.Checked;
            txt_dia.Enabled = check_press.Checked;
            txt_bpm.Enabled = check_press.Checked; 
            
            txt_wage.Enabled = check_wage.Checked;
            txt_bmi.Enabled = check_wage.Checked;
            txt_tkanka.Enabled = check_wage.Checked;
            txt_muscle.Enabled = check_wage.Checked;
            txt_kosci.Enabled = check_wage.Checked;
            txt_water.Enabled = check_wage.Checked;
            txt_cialo.Enabled = check_wage.Checked;
            txt_trzewna.Enabled = check_wage.Checked; 
            txt_wiek.Enabled = check_wage.Checked;
            txt_kcal.Enabled = check_wage.Checked;

            if (check_export.Checked == true)
            {
                send.Text = "Uploaduj";
            } else
            {
                send.Text = "Zapisz na dysku";
            }
        }
    }
}
