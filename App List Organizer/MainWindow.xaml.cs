using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Net;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace App_List_Organizer
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Grid nlist; // nowy item listy
        private dynamic idlist;
        private bool isLoaded = false;
        public string appListPath;
        private bool isNewList = false;
        private ConfigFile configFile;
        

        private string programPath = System.AppDomain.CurrentDomain.BaseDirectory;
        private string sortedListFilePath = System.AppDomain.CurrentDomain.BaseDirectory + @"config\sortedlist.txt";
        private string configFilePath = System.AppDomain.CurrentDomain.BaseDirectory + @"config\config.ini";

        public MainWindow()
        {

  
            sortedListFilePath = programPath + @"config\sortedlist.txt";
            configFilePath = programPath + @"config\config.ini";
            Directory.CreateDirectory(programPath + "config");

            configFile = new ConfigFile().LoadFile(configFilePath);

            InitializeComponent();
            nlist = CloneUsingXaml(listBox.Items[0]) as Grid;
            listBox.Items.RemoveAt(0);



            for (int i = 1; i > 10; i++)
            {
                int index = listBox.Items.Add(CloneUsingXaml(nlist));
                Grid tgrid = listBox.Items[index] as Grid;
                TextBlock ttext = tgrid.Children[0] as TextBlock;
                ttext.Text = "Dupa " + i;

            }

            LoadAll();

        }
        
        private async void LoadAll()
        {
            await Task.Run(() => {

                bool isDownloaded = false;
                string thtml = ReadTextFromUrl(@"http://api.steampowered.com/ISteamApps/GetAppList/v2/");
                try
                {
                    idlist = JsonConvert.DeserializeObject(thtml);
                    isDownloaded = true;
                }
                catch(Exception e)
                {
                    System.Windows.Forms.MessageBox.Show("Cant download list.", "Cant download list trying to load offline file.", MessageBoxButtons.OK);
                }

                
                idlist = idlist["applist"]["apps"];

                
                if (File.Exists(sortedListFilePath))
                {
                    Console.WriteLine(thtml.Length);
                    Console.WriteLine(configFile.listFileSize);
                    
                    if (thtml.Length != configFile.listFileSize)
                    {
                        isNewList = true;
                    }
                }
                else
                {
                    isNewList = true;
                }

                if (!isDownloaded)
                {
                    if (File.Exists(sortedListFilePath))
                    {
                        isNewList = false;
                    }
                    else
                    {
                        var selectedOption = System.Windows.Forms.MessageBox.Show("There no offline files.", "There no offline files.", MessageBoxButtons.OK);
                        if (selectedOption == System.Windows.Forms.DialogResult.OK)
                        {
                            Environment.Exit(1);
                            return;
                        }
                    }
                    
                }

                if (isNewList)
                {
                    configFile.listFileSize = thtml.Length;
                }

                


            });
            using (var fbd = new FolderBrowserDialog())
            {
                string temppath;
                string tempsteampath = "";
                fbd.Description = "Select Steam folder";
                fbd.RootFolder = Environment.SpecialFolder.Desktop;
                if (Directory.Exists(configFile.steamFolder))
                {
                    temppath = configFile.steamFolder + @"\AppList";
                    tempsteampath = configFile.steamFolder;
                }
                else
                {
                    tempsteampath = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", "").ToString().Replace(@"/", @"\");
                    temppath =  tempsteampath + @"\AppList";
                }

                fbd.SelectedPath = temppath;

                DialogResult result = System.Windows.Forms.DialogResult.No;
                
                if (Directory.Exists(temppath))
                {
                    result = System.Windows.Forms.DialogResult.OK;
                    
                    configFile.steamFolder = tempsteampath;
                }
                else
                {
                    result = fbd.ShowDialog();
                    tempsteampath = fbd.SelectedPath;
                    temppath = fbd.SelectedPath + @"\AppList";
                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(temppath))
                    {
                        //System.Windows.Forms.MessageBox.Show(fbd.SelectedPath);

                        //filesList = Directory.GetFiles(fbd.SelectedPath).ToList().CustomSort().ToList();

                    }
                }


                if (result == System.Windows.Forms.DialogResult.OK && Directory.Exists(temppath))
                {
                    appListPath = temppath;
                    configFile.steamFolder = tempsteampath;

                    

                    LoadProfileList();
                    
                    
                }
                else
                {
                    var selectedOption = System.Windows.Forms.MessageBox.Show("Cant find AppList folder.", "Cant find AppList folder.", MessageBoxButtons.OK);
                    if (selectedOption == System.Windows.Forms.DialogResult.OK)
                    {
                        Environment.Exit(1);
                        return;
                    }
                }


            }
            Newtonsoft.Json.Linq.JArray tidlist = idlist;
            var idlistL = from x in tidlist select x["name"].ToString();
            List<String> idListSorted;
            if (isNewList || !File.Exists(sortedListFilePath))
            {
                idListSorted = idlistL.CustomSort().ToList();
                File.WriteAllText(sortedListFilePath, JsonConvert.SerializeObject(idListSorted));
            }
            else
            {
                idListSorted = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(sortedListFilePath));
            }

            comboBox.ItemsSource = idListSorted;
            configFile.SaveFile(configFilePath);

            

            isLoaded = true;
            Console.WriteLine(cBox_profiles.Items.Count);
            if(cBox_profiles.Items.Count - 1 < configFile.selectedProfile)
            {
                cBox_profiles.SelectedIndex = 1;
            }
            else
            {
                cBox_profiles.SelectedIndex = configFile.selectedProfile;
            }
            

        }

        private void LoadFileList(string dirPath, int selectID)
        {
            listBox.Items.Clear();
            if (selectID == 0)
            {
                List<string> filesList = Directory.GetFiles(dirPath).ToList().CustomSort().ToList();
                foreach (string a in filesList)
                {
                    Grid nline = listBox.Items[listBox.Items.Add(CloneUsingXaml(nlist))] as Grid;
                    TextBlock t1 = nline.Children[0] as TextBlock;
                    t1.Text = System.IO.Path.GetFileName(a);
                    TextBlock t2 = nline.Children[1] as TextBlock;
                    t2.Text = File.ReadAllText(a);

                    Newtonsoft.Json.Linq.JArray b = idlist;
                    var c = b.FirstOrDefault(e => e["appid"].ToString() == t2.Text);
                    if (c != null)
                    {
                        TextBlock t3 = nline.Children[2] as TextBlock;
                        t3.Text = c["name"].ToString();
                    }
                    else
                    {
                        TextBlock t3 = nline.Children[2] as TextBlock;
                        t3.Text = "Error";
                    }
                }
            }
            else
            {
                int i = 0;
                foreach (int appId in configFile.profileList[selectID-1].idList)
                {
                    Grid nline = listBox.Items[listBox.Items.Add(CloneUsingXaml(nlist))] as Grid;
                    TextBlock t1 = nline.Children[0] as TextBlock;
                    t1.Text = i.ToString() + ".txt";
                    TextBlock t2 = nline.Children[1] as TextBlock;
                    t2.Text = appId.ToString();

                    Newtonsoft.Json.Linq.JArray b = idlist;
                    var c = b.FirstOrDefault(e => e["appid"].ToString() == t2.Text);
                    if (c != null)
                    {
                        TextBlock t3 = nline.Children[2] as TextBlock;
                        t3.Text = c["name"].ToString();
                    }
                    else
                    {
                        TextBlock t3 = nline.Children[2] as TextBlock;
                        t3.Text = "Error";
                    }
                    i++;
                }
            }
        }

        private void LoadProfileList()
        {
            cBox_profiles.Items.Clear();
            cBox_profiles.Items.Add("--AppList Folder--");
            configFile.profileList.ForEach(profile => {
                cBox_profiles.Items.Add(profile.name);
            });
        }

        string ReadTextFromUrl(string url)
        {
            // WebClient is still convenient
            // Assume UTF8, but detect BOM - could also honor response charset I suppose
            using (var client = new WebClient())
            using (var stream = client.OpenRead(url))
            using (var textReader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return textReader.ReadToEnd();
            }
        }

        private Object CloneUsingXaml(Object o)
        {
            string xaml = XamlWriter.Save(o);
            return XamlReader.Load(new XmlTextReader(new StringReader(xaml)));
        }



        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Grid lbi = ((sender as System.Windows.Controls.ListBox).SelectedItem as Grid);
            if (lbi != null)
            {
                TextBlock txbl = lbi.Children[1] as TextBlock;
                idbox.Text = txbl.Text;
                loadFromId();
            }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
            loadFromId();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            
            e.Handled = !IsTextAllowed(e.Text);
            
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        // Use the DataObject.Pasting Handler 
        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
            loadFromId();
        }

        private string temptext = "";

        private void loadFromId()
        {
            if (idlist != null)
            {
                Newtonsoft.Json.Linq.JArray b = idlist;
                var c = b.FirstOrDefault(f => f["appid"].ToString() == idbox.Text);

                if (c != null)
                {
                    temptext = c["name"].ToString();
                    comboBox.Text = c["name"].ToString();
                }
            }
        }

        private void loadFromName()
        {
            if (idlist != null)
            {
                Newtonsoft.Json.Linq.JArray b = idlist;
                var c = b.FirstOrDefault(f => f["name"].ToString() == comboBox.Text);

                if (c != null)
                {
                    idbox.Text = c["appid"].ToString();
                }
            }
        }


        private void OnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                loadFromId();

            }

            if (e.Key == Key.Delete)
            {
                if (listBox.SelectedIndex > -1)
                {
                    int tindex = listBox.SelectedIndex;
                    listBox.Items.RemoveAt(listBox.SelectedIndex);
                    refreshlist();
                    listBox.SelectedIndex = tindex;
                }
            }
        }
        private void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (comboBox.Text != "" && comboBox.Text != temptext && 1 == 2)
            {
                Newtonsoft.Json.Linq.JArray b = idlist;
                var d = b.ToList();
                var cc = d.FindAll(x => x != null && Regex.Match(x["name"].ToString(), "^" + comboBox.Text, RegexOptions.IgnoreCase).Success);
                if (cc.Count > 0)
                {
                    var c = cc.OrderBy(x => x["name"].ToString().Length).Take(10);

                    
                    clearcombo();
                    
                    foreach (var a in c)
                    {
                        comboBox.Items.Add(a["name"].ToString());
                    }
                    
                    comboBox.IsDropDownOpen = true;
                    System.Windows.Controls.TextBox tb = (System.Windows.Controls.TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
                    tb.Select(tb.Text.Length, 0);

                }
                else
                {
                    
                    clearcombo();
                }
            }
            if(temptext != comboBox.Text)
            {
                comboBox.IsDropDownOpen = true;
                System.Windows.Controls.TextBox tb = (System.Windows.Controls.TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
                tb.Select(tb.Text.Length, 0);

            }
            loadFromName();
        }
        private void clearcombo()
        {
            if(comboBox.Items.Count == -1)
            {
                comboBox.Items.Add(comboBox.Text);
            }
            for (int i = 1; comboBox.Items.Count > 0; i++)
            {
                comboBox.Items.RemoveAt(0);
            }
            
        }

        private void refreshlist()
        {
            int i = 0;
            foreach(Grid a in listBox.Items)
            {
                TextBlock temptb = a.Children[0] as TextBlock;
                temptb.Text = i + ".txt";
                i++;
            }
            
            
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                if (sender == button_Edit)
                {

                    if (listBox.SelectedIndex > -1 && listBox.SelectedIndex <= listBox.Items.Count)
                    {
                        Grid tempgrid = listBox.SelectedItem as Grid;
                        TextBlock textBlock1 = tempgrid.Children[1] as TextBlock;
                        TextBlock textBlock2 = tempgrid.Children[2] as TextBlock;
                        textBlock1.Text = idbox.Text;
                        textBlock2.Text = comboBox.Text;
                    }
                }

                if (sender == button_Add)
                {
                    Grid nline = listBox.Items[listBox.Items.Add(CloneUsingXaml(nlist))] as Grid;
                    TextBlock t1 = nline.Children[1] as TextBlock;
                    t1.Text = idbox.Text;
                    TextBlock t2 = nline.Children[2] as TextBlock;
                    t2.Text = comboBox.Text;
                    refreshlist();
                    listBox.SelectedIndex = listBox.Items.Count - 1;
                    listBox.ScrollIntoView(listBox.SelectedItem);
                }

                if (sender == button_Zapisz)
                {
                    refreshlist();
                    List<string> files = Directory.GetFiles(appListPath).ToList();

                    foreach (string file in files)
                    {
                        File.Delete(file);

                    }

                    foreach (Grid a in listBox.Items)
                    {
                        TextBlock tb1 = a.Children[0] as TextBlock;
                        TextBlock tb2 = a.Children[1] as TextBlock;
                        using (StreamWriter file = File.CreateText(appListPath + @"\" + tb1.Text))
                        {

                            file.Write(tb2.Text);
                        }
                    }
                }
            }
        }

        private void CBox_profiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded)
                return;
            Console.WriteLine("S " +configFile.selectedProfile);
            Console.WriteLine("S2 " + cBox_profiles.SelectedIndex);
            if(cBox_profiles.Items.Count - 1 < configFile.selectedProfile)
            {
                cBox_profiles.SelectedIndex = 0;
            }
            cBox_profiles.SelectedIndex = Math.Max(cBox_profiles.SelectedIndex, 0);
            configFile.selectedProfile = cBox_profiles.SelectedIndex;
            configFile.SaveFile(configFilePath);
            LoadFileList(appListPath, configFile.selectedProfile);
        }

        private void Button_Profile_Click(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                if (sender == Button_PAdd)
                {
                    string input = Interaction.InputBox("Type Name for profile",
                       "Type Name for profile",
                       "New Profile");
                    if (input != "")
                    {
                        cBox_profiles.Items.Add(input);
                        configFile.profileList.Add(new ProfileListContent());
                        configFile.profileList.Last().name = input;
                        cBox_profiles.SelectedIndex = cBox_profiles.Items.Count - 1;
                    }
                }

                if (sender == Button_PCopy)
                {
                    string input = Interaction.InputBox("Type Name for profile",
                       "Type Name for profile",
                       "New Profile");
                    if (input != "")
                    {
                        cBox_profiles.Items.Add(input);
                        configFile.profileList.Add(new ProfileListContent());
                        configFile.profileList.Last().name = input;
                        foreach (Grid a in listBox.Items)
                        {
                            TextBlock temptb = a.Children[1] as TextBlock;
                            configFile.profileList.Last().idList.Add(Int32.Parse(temptb.Text));
                        }
                        cBox_profiles.SelectedIndex = cBox_profiles.Items.Count - 1;
                    }
                }

                if (sender == Button_PDelete)
                {
                    if(configFile.selectedProfile > 0)
                    {
                        configFile.profileList.RemoveAt(configFile.selectedProfile - 1);
                        cBox_profiles.Items.RemoveAt(configFile.selectedProfile);
                        cBox_profiles.Items.Refresh();
                        
                    }
                }

                if (sender == Button_PSave)
                {
                    if (configFile.selectedProfile == 0)
                    {
                        string input = Interaction.InputBox("Type Name for profile",
                      "Type Name for profile",
                      "New Profile");
                        if (input != "")
                        {
                            cBox_profiles.Items.Add(input);
                            configFile.profileList.Add(new ProfileListContent());
                            configFile.profileList.Last().name = input;
                            foreach (Grid a in listBox.Items)
                            {
                                TextBlock temptb = a.Children[1] as TextBlock;
                                configFile.profileList.Last().idList.Add(Int32.Parse(temptb.Text));
                            }
                            cBox_profiles.SelectedIndex = cBox_profiles.Items.Count - 1;
                        }
                    }
                    else
                    {
                        foreach (Grid a in listBox.Items)
                        {
                            TextBlock temptb = a.Children[1] as TextBlock;
                            configFile.profileList[configFile.selectedProfile - 1].idList.Add(Int32.Parse(temptb.Text));
                        }
                        cBox_profiles.SelectedIndex = cBox_profiles.Items.Count - 1;
                    }
                }
            }
        }
    }

    public class ConfigFile
    {
        public long listFileSize = 0;
        public int selectedProfile = 0;
        public string steamFolder = "";
        public List<ProfileListContent> profileList = new List<ProfileListContent>();

        public ConfigFile LoadFile(string configFilePath)
        {
            ConfigFile configFile;
            if (File.Exists(configFilePath))
            {
                try
                {
                    configFile = JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(configFilePath));
                }
                catch(Exception)
                {
                    configFile = new ConfigFile();
                    File.WriteAllText(configFilePath, JsonConvert.SerializeObject(configFile));
                }

            }
            else
            {
                configFile = new ConfigFile();
                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(configFile));
            }

            return configFile;
        }

        public void SaveFile(string configFilePath)
        {
            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(this));
        }

    }

    public class ProfileListContent
    {
        public string name = "";
        public List<int> idList = new List<int>();
    }



    public static class MyExtensions
    {
        public static IEnumerable<string> CustomSort(this IEnumerable<string> list)
        {
            int maxLen = list.Select(s => s.Length).Max();

            return list.Select(s => new
            {
                OrgStr = s,
                SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
            })
            .OrderBy(x => x.SortStr)
            .Select(x => x.OrgStr);

        }

    }


}
