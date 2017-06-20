using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using ThaiNationalIDCard;
using System.Runtime.InteropServices;
using Windows.Devices.SmartCards;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using System.Drawing;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.ApplicationModel;
using PCSC;
using Windows.UI.Core;
using System.Data.Common;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ThaiNationalIDCardReader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        ThaiIDCard idcard;
        public MainPage()
        {
            this.InitializeComponent();

            Button_Click(null, null);
            TogglePicture.IsOn = true;

   
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Reading...");

                Personal personal = idcard.readAll(true);
                if (personal != null)
                {
                    Debug.WriteLine(personal.Citizenid);
                    Debug.WriteLine(personal.Birthday.ToString("dd/MM/yyyy"));
                    Debug.WriteLine(personal.Sex);
                    Debug.WriteLine(personal.Th_Prefix);
                    Debug.WriteLine(personal.Th_Firstname);
                    Debug.WriteLine(personal.Th_Lastname);
                    Debug.WriteLine(personal.En_Prefix);

                    Debug.WriteLine(personal.En_Firstname);
                    Debug.WriteLine(personal.En_Lastname);
                    Debug.WriteLine(personal.Issue.ToString("dd/MM/yyyy"));
                    Debug.WriteLine(personal.Expire.ToString("dd/MM/yyyy"));
                    /*
                    LogLine(personal.Address);
                    LogLine(personal.addrHouseNo); // บ้านเลขที่ 
                    LogLine(personal.addrVillageNo); // หมู่ที่
                    LogLine(personal.addrLane); // ซอย
                    LogLine(personal.addrRoad); // ถนน
                    LogLine(personal.addrTambol);
                    LogLine(personal.addrAmphur);
                    LogLine(personal.addrProvince);*/
                }
                else if (idcard.ErrorCode() > 0)
                {
                    Debug.WriteLine(idcard.Error());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        async void cardInserted(Personal p)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
             async () =>
                     {
                         //ui update
                         txtIdCitiz.Text = p.Citizenid;
                         txtThaiName.Text = String.Format("{0} {1} {2}", p.Th_Prefix, p.Th_Firstname, p.Th_Lastname);
                         txtEngName.Text = String.Format("{0} {1} {2}", p.En_Prefix, p.En_Firstname, p.En_Lastname);
                         txtBirthDate.Text = p.Birthday.ToString("dd MMMM yyyy");
                         txtGender.Text = p.Sex.Trim() == "1" ? "ชาย" : "หญิง";
                         txtAddress1.Text = String.Format("{0} {1} {2} {3}", p.addrHouseNo, p.addrVillageNo, p.addrLane, p.addrRoad);
                         txtAddress2.Text = String.Format("{0} {1} {2}", p.addrTambol, p.addrAmphur, p.addrProvince);

                         txtIssueDate.Text = p.Issue.ToString("d");
                         txtExpireDate.Text = p.Expire.ToString("d");
                         txtpostcode.Text = await FindPostalcodeByTambon(p.addrTambol);
                         if (TogglePicture.IsOn)
                             Image1.Source = await p.DecodePhoto();
                     }
                    );
        }

        private void TogglePicture_Toggled(object sender, RoutedEventArgs e)
        {
            if (TogglePicture.IsOn == true)
            {
                idcard.eventCardInserted -= cardInserted;
                idcard.eventCardInsertedWithPhoto -= cardInserted;
                idcard.eventCardInsertedWithPhoto += cardInserted;
            }
            else
            {
                Image1.Source = null;
                idcard.eventCardInsertedWithPhoto -= cardInserted;
                idcard.eventCardInserted += cardInserted;
            }
        }

        //refresh button
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (idcard != null)
                idcard.Dispose();
            idcard = new ThaiIDCard();

            try
            {
                if (idcard.GetReaders().Length >= 1)
                {
                    idcard.MonitorStart(idcard.GetReaders()[0]);
                    txtCardReaderName.Text = idcard.GetReaders()[0];
                    TogglePicture_Toggled(null, null);
                }

            }
            catch (Exception ex)
            {
                txtCardReaderName.Text = String.Format("({0})", ex.Message);
            }
        }




        private async Task<String> FindPostalcodeByTambon(String Tambon)
        {
            if (Tambon.StartsWith("เขต"))
                Tambon = Tambon.Substring(3, Tambon.Length - 3);
            if (Tambon.StartsWith("แขวง"))
                Tambon = Tambon.Substring(4, Tambon.Length - 4);

            string fname = @"Assets\tambon-2.csv";

            StorageFolder InstallationFolder = Package.Current.InstalledLocation;
            StorageFile file;
            try
            {
                file = await InstallationFolder.GetFileAsync(fname);
            }
            catch (FileNotFoundException)
            {
                //TODO: Handler FileNotFoundException              
                throw new FileNotFoundException(String.Format(@"Couldn't find '{0}'", fname));
            }

            String[] lines = File.ReadAllLines(file.Path);
            //return Array.Find<String>(lines, (line) => line.Split(",".ToCharArray()[0])[0].Trim() == Tambon);       
            foreach (String line in lines)
            {
                String[] s = line.Split(",".ToCharArray()[0]);
                if (s[0] == Tambon)
                    return s[1];
            }
            return String.Empty;

        }

    }
}