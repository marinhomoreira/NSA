using SOD_CS_Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NSA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ConfigureSoD();
            ConfigureDevice();
            RegisterSoDEvents();

            //BuildUI();
        }
        

        int CurrentTask = 0; // TODO : MAKE IT DYNAMIC!
        string Partipant = "P0"; // TODO : MAKE IT DYNAMIC!
        string StudyType = "A"; // TODO : MAKE IT DYNAMIC!
        int AttemptNumber = 0; // TODO: Does it make sense?
        string TaskNumber; // TODO : MAKE IT DYNAMIC?

        DateTime startTime = new DateTime();
        DateTime endTime = new DateTime();

        string LogDirectory = @"C:\ODTabletLogs\";

        string CurrentFileName = "";

        bool Logging = false;

        private void DisplayCurrentTask()
        {
            RootPanel.Children.Clear();
            int i = CurrentTask;
            TextBlock tb = new TextBlock();
            tb.Name = "tb" + i;
            tb.Text = questions[i];
            RootPanel.Children.Add(tb);
        }

        private void BuildUI()
        {
            DisplayCurrentTask();
        }

        private Button CreateRegularButton(string content, RoutedEventHandler reh, int i)
        {
            Button b = new Button();
            b.Content = content;
            b.Width = 100;
            b.Name = "T"+i;
            //b.Height = 50;
            b.Click += reh;
            //b.BorderBrush = Brushes.Black;
            //b.Background = Brushes.White;
            return b;
        }

        private string GenerateFileName()
        {
            string filename = LogDirectory;
            filename += Partipant;
            filename += "-";
            filename += StudyType;
            filename += "-";
            filename += "T"+ CurrentTask;
            filename += "-";
            filename += AttemptNumber;
            filename += ".txt";
            FileInfo fi = new FileInfo(filename);
            if (fi.Exists)
            {
                AttemptNumber++;
                return GenerateFileName();
            }
            return filename;
        }

        private void WriteToFile(string filename, string line)
        {
            using (StreamWriter writer = new StreamWriter(filename, true))
            {
                writer.WriteLine(line);
            }
        }

        private void Log(Dictionary<string, dynamic> parsedMessage)
        {
            if (parsedMessage.ContainsKey("data"))
            {
                Dictionary<string, string> d = parsedMessage["data"]["data"].ToObject<Dictionary<string, string>>();
                List<string> list = new List<string>(d.Keys);
                string time = DateTime.Now.ToString();
                string line = time + "\t";
                foreach (string k in list)
                {
                    line += k + "\t" + d[k] + "\t";
                }
                WriteToFile(CurrentFileName, line);
            }
        }

        private void Log(String stringyo)
        {
            string time = DateTime.Now.ToString();
            string line = time + "\t";
            line += stringyo;
            WriteToFile(CurrentFileName, line);
        }

        # region SoD

        SOD SoD;

        private void ConfigureSoD()
        {
            // Configure and instantiate SOD object
            string address = "beastwin.marinhomoreira.com";
            int port = 1666;
            SoD = new SOD(address, port);
        }

        private void ConfigureDevice()
        {
            // Configure device with its dimensions (mm), location in physical space (X, Y, Z in meters, from sensor), orientation (degrees), Field Of View (FOV. degrees) and name
            double widthInMM = 750
                , heightInMM = 500
                , locationX = -2
                , locationY = 3
                , locationZ = 1;
            string deviceType = "NSA";
            bool stationary = true;
            SoD.ownDevice.SetDeviceInformation(widthInMM, heightInMM, locationX, locationY, locationZ, deviceType, stationary);
            SoD.ownDevice.orientation = 0;
            SoD.ownDevice.FOV = 180;

            // Name and ID of device - displayed in Locator
            // TODO: Future: possible to look for devices using name, instead of ID.
            SoD.ownDevice.ID = "69";
            SoD.ownDevice.name = "BancadaLogger";
        }

        private void RegisterSoDEvents()
        {
            // register for 'connect' event with io server
            SoD.socket.On("connect", (data) =>
            {
                Console.WriteLine("\r\nConnected...");
                Console.WriteLine("Registering with server...\r\n");
                SoD.RegisterDevice();  //register the device with server everytime it connects or re-connects
            });

            SoD.socket.On("TaskStarted", (dict) =>
            {
                Logging = true;
                startTime = DateTime.Now;
                CurrentFileName = GenerateFileName();
                Log("TaskStarted");
            });

            SoD.socket.On("TaskCompleted", (dict) =>
            {
                Log("TaskCompleted");
                endTime = DateTime.Now;
                Log("Total time: " + endTime.Subtract(startTime));

                CurrentTask++;
                Logging = false;
            });

            


            SoD.socket.On("LensStarted", (dict) =>
            {
                if (Logging)
                {
                    Dictionary<string, dynamic> parsedMessage = SoD.ParseMessageIntoDictionary(dict);
                    Log("Started Lens: " + (String)parsedMessage["data"]["data"]["lens"]);
                }
            });

            SoD.socket.On("FreedLens", (dict) =>
            {
                if (Logging)
                {
                    Dictionary<string, dynamic> parsedMessage = SoD.ParseMessageIntoDictionary(dict);
                    Log("Freed Lens: " + (String)parsedMessage["data"]["data"]["lens"]);
                }
                
            });

            SoD.socket.On("BringLensToFront", (dict) =>
            {
                if (Logging)
                {
                    Dictionary<string, dynamic> parsedMessage = SoD.ParseMessageIntoDictionary(dict);
                    Log("Bring to tha front: "+ (String)parsedMessage["data"]["data"]);
                }
            });

            

            SoD.socket.On("StartSingleLensMode", (dict) =>
            {
                if (Logging)
                {
                    //Log(SoD.ParseMessageIntoDictionary(dict));
                }
            });

            SoD.socket.On("StartMultipleLensMode", (dict) =>
            {
                if (Logging)
                {
                    //Log(SoD.ParseMessageIntoDictionary(dict));
                }
            });

            SoD.socket.On("string", (data) =>
            {
                Dictionary<string, dynamic> parsedMessage = SoD.ParseMessageIntoDictionary(data);
                String receivedString = (String)parsedMessage["data"]["data"];
                if (Logging)
                {
                    Log(receivedString);
                }
            });

            SoD.socket.On("dictionary", (dict) =>
            {
                if (Logging)
                {
                    Log(SoD.ParseMessageIntoDictionary(dict));
                }
            });


            // make the socket.io connection
            SoD.SocketConnect();
        }

        # endregion





        string[] questions = {
"1. Find Calgary (cities)",
"2. Identify high population density areas in the south of Alberta (density)",
"3. Find Lethbridge (cities)",
"4. Look for the population density of the west coast area.",
"5. What is the color of West Vancouver?",
"6. Which highway goes to Goose Bay in Newfoundland and Labrador? (street)",
"7. What is the name of the two cities most north of Alberta? (Cities)",
"8. Do we have mountains in the south of Saskatchewan? (satellite + cities)",
"9. How many electoral districts are there in Saskatchewan? (electoral + cities)",
"10. Which city is more dense: Calgary or Kelowna? (Population Density + Cities)",
"11. How would you go from Calgary to the closest second more dense area in Manitoba? (street + cities + density)",
"12. Find Ottawa (street)",
"13. How many electoral districts are there in Ottawa (main region)?",
"14. Find Winnipeg",
"15. What major roads connect Winnipeg and Saskatoon?",
"16. How many electoral districts are there in Brandon?",
"17. Which highway cross Prince George in the North-South direction? (street)",
"18. What is the name of the lake crossing the border between Nunavut and Northwest territories and really close to Saskatchewan and Manitoba? (Cities)",
"19. Do you identify any glaciers around Lake Manitoba? (Satellite + cities)",
"20. How many electoral districts are there in the most dense area of Manitoba? (electoral + density)",
"21. Which city is more dense: Winnipeg or Edmonton? (Population Density + Cities)",
"22. How would you go from Calgary to the most dense area in Manitoba? (street + cities + density)"
                             };





        string[] questionsW1 = { 
                            "1. Find Calgary (cities)",
                            "2. Identify high population density areas in the south of Alberta (density)",
                            "3. Find Lethbridge (cities)",
                            "4. Look for the population density of the west coast area.",
                            "5. What is the color of West Vancouver?",
                            "6. Which highway goes to Goose Bay in Newfoundland and Labrador? (street)",
                             };
        string[] questionsR1 = {
                               "7. What is the name of the two cities most north of Alberta? (Cities)",
"8. Do we have mountains in the south of Saskatchewan? (satellite + cities)",
"9. How many electoral districts are there in Saskatchewan? (electoral + cities)",
"10. Which city is more dense: Calgary or Kelowna? (Population Density + Cities)",
"11. How would you go from Calgary to the closest second more dense area in Manitoba? (street + cities + density)",
                               };

        string[] questionsW2 = { 
                               "12. Find Ottawa (street)",
                                "13. How many electoral districts are there in Ottawa (main region)?",
                                "14. Find Winnipeg",
"15. What major roads connect Winnipeg and Saskatoon?",
"16. How many electoral districts are there in Brandon?",
                               
                               };

        string[] questionsR2 = {
                               "17. Which highway cross Prince George in the North-South direction? (street)",
"18. What is the name of the lake crossing the border between Nunavut and Northwest territories and really close to Saskatchewan and Manitoba? (Cities)",
"19. Do you identify any glaciers around Lake Manitoba? (Satellite + cities)",
"20. How many electoral districts are there in the most dense area of Manitoba? (electoral + density)",
"21. Which city is more dense: Winnipeg or Edmonton? (Population Density + Cities)",
"22. How would you go from Calgary to the most dense area in Manitoba? (street + cities + density)",
                               
                               };

        private void RadioButtonA_Checked(object sender, RoutedEventArgs e)
        {
            StudyType = "A";
        }

        private void RadioButtonB_Checked(object sender, RoutedEventArgs e)
        {
            StudyType = "B";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTask--;
            DisplayCurrentTask();
            //LoggingStatus.Content = "Logging OFF";
            Logging = false;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentTask++;
            DisplayCurrentTask();
            //LoggingStatus.Content = "Logging OFF";
            Logging = false;
        }
    }
}
