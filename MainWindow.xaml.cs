using Impinj.OctaneSdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Threading;

namespace verifyTagsGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // creating instance of the impinj reader
        public ImpinjReader reader = new ImpinjReader();
        public bool connected = false;

        // creating a list to store read and verified tags
        static List<string> tagsVerified = new List<string>();
        // creating a list for the tags that need to be validated
        static List<string> tagList = new List<string>();

        string notVerified = "C:/users/jshoemaker/source/repos/testingData/prever.csv";
        string verified = "C:/users/jshoemaker/source/repos/testingData/postVerification.csv";

        public MainWindow()
        {
            InitializeComponent();

            // read the input file and add them to the tag list
            string[] lines = File.ReadAllLines(notVerified);
            foreach (string line in lines)
            {
                tagList.Add(line);
            }
        }

        public void Connect(string hostname)
        {
            try
            {
                Console.WriteLine("Attempting to connect using:" + hostname);
                reader.Connect(hostname);
                connected = true;

                Settings settings = reader.QueryDefaultSettings();

                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.Mode = ReportMode.Individual;
                settings.Antennas.TxPowerInDbm = 15.0;
                settings.Report.IncludeGpsCoordinates = true;
                settings.Report.IncludeSeenCount = true;
                settings.Session = 2;
                settings.SearchMode = SearchMode.SingleTarget;

                reader.ApplySettings(settings);
                Console.WriteLine("Successfully connected");
                listTags.Items.Add("Successfuly connected to the reader");

                reader.TagsReported += onTagsReported;
            }
            catch (OctaneSdkException er)
            {
                System.Diagnostics.Trace.
                    WriteLine("An Octane SDK exception has occurred : {0}", er.Message);
                listTags.Items.Add("An Octane SDK Exception has occured: " + er.Message);
            }
            catch (Exception er)
            {
                System.Diagnostics.Trace.
                    WriteLine("An exception has occurred : {0}", er.Message);
                listTags.Items.Add("An exception has occurred: " + er.Message);
            }
        }

        public void onTagsReported(ImpinjReader reader, TagReport report)
        {
            // cannot directly call this because it is on the main ui thread
            // through verify we can call a function to update the ui

            Action action = delegate ()
            {
                string key;
                // creating a list on the unique tags seen with no duplication
                List<Tag> distinctTags = report.Tags.Distinct().ToList();

                foreach (Tag tag in distinctTags)
                {
                    key = tag.Epc.ToString();
                    VerifyTag(key);
                }
            };

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);

        }

        public void VerifyTag(string key)
        {
            if (tagList.Contains(key))
            {
                // checking for a duplicate tag value
                if (tagsVerified.Contains(key))
                {
                    UpdateListbox(key, 1);
                    Console.WriteLine("Found Duplicate Tag in list. Please remove this from the roll");
                    
                }
                else
                {
                    tagList.Remove(key);
                    tagsVerified.Add(key);
                    UpdateListbox(key, 0);  // 0 == tag verified

                    // success animation
                    Storyboard sb = FindResource("PlaySuccessAnimation") as Storyboard;
                    sb.Begin();
                }
            }
            // checking for tag duplicates (not in tagList but already in verified list)
            else if (tagsVerified.Contains(key))
            {   
                UpdateListbox(key, 1);  // 1 == tag duplicate
                Console.WriteLine("Found Duplicate Tag");

                // failure animation
                Storyboard sb = FindResource("PlayFailureAnimation") as Storyboard;
                sb.Begin();
            }
            else
            {
                UpdateListbox(key, 25); // any number that isnt 0 or 1 == tag could not be verified
                Console.WriteLine("Could not find tag");

                // failure animation
                Storyboard sb = FindResource("PlayFailureAnimation") as Storyboard;
                sb.Begin();
            }
        }

        public void UpdateListbox(string key, int condition)
        {
            // 0 == TAG VERIFIED
            if (condition == 0)
            {
                listTags.Items.Clear();
                listTags.Items.Add(key + " Has been verified");
            }
            // 1 == TAG DUPLICATE
            else if (condition == 1)
            {
                listTags.Items.Clear();
                listTags.Items.Add("Error: " + key + " is a Duplicate Tag");
            }
            // ANY OTHER NUMBER == TAG NOT FOUND
            else
            {
                listTags.Items.Clear();
                listTags.Items.Add("Could not verify tag: " + key);
            }
        }

        // Event handler to handle when connect is clicked
        public void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect(hostText.Text.ToString());
        }
        // event handler for when Start is clicked
        public void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // dont call start reader if reader is already running
                if (!reader.QueryStatus().IsSingulating)
                {
                    reader.Start();
                    listTags.Items.Clear();
                }

                listTags.Items.Add("Reader Started: You may now start verifying tags.");
            }
            catch (OctaneSdkException er)
            {
                System.Diagnostics.Trace.
                    WriteLine("An Octane SDK exception has occurred : {0}", er.Message);
            }
            catch (Exception er)
            {
                System.Diagnostics.Trace.
                    WriteLine("An exception has occurred : {0}", er.Message);
            }
        }

        // event handler for when Stop is Clicked
        public void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // dont call start reader if reader is already running
                reader.Stop();
                reader.Disconnect();

                // letting the user know we have disconnected
                listTags.Items.Clear();
                listTags.Items.Add("Successfully disconnected. Writing contents to CSV file.");

                var sb = new StringBuilder();
                foreach (string tag in tagsVerified)
                {
                    sb.AppendLine(tag);
                }
                File.AppendAllText(verified, sb.ToString());

            }
            catch (OctaneSdkException er)
            {
                System.Diagnostics.Trace.
                    WriteLine("An Octane SDK exception has occurred : {0}", er.Message);
            }
            catch (Exception er)
            {
                System.Diagnostics.Trace.
                    WriteLine("An exception has occurred : {0}", er.Message);
            }
        }

        // button to handle the in-file diagram 
        public void buttonInFile_Click(object sender, RoutedEventArgs e)
        {
            // Create open file dialog
            Microsoft.Win32.OpenFileDialog open_file_dialog = new Microsoft.Win32.OpenFileDialog();

            // Launch open File by calling showDialog
            Nullable<bool> result = open_file_dialog.ShowDialog();

            // setting the chosen file to the pre-verification file
            if (result == true)
            {
                open_file_dialog.FileName = notVerified;
            }
            else
            {
                listTags.Items.Add("No file was selected. Using default file.");
            }
            
        }

        // button to handle the out-file diagram
        public void buttonOutFile_Click(object sender, RoutedEventArgs e)
        {
            // open file dialog
            Microsoft.Win32.OpenFileDialog openOutFile = new Microsoft.Win32.OpenFileDialog();

            // Launch open out file by calling showdialog
            Nullable<bool> result = openOutFile.ShowDialog();

            if (result == true)
            {
                openOutFile.FileName = verified;
            }
            else
            {
                listTags.Items.Add("File Could not be selected, using default file");
            }
        }
    }
}