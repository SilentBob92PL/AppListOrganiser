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
        public MainWindow()
        {
            InitializeComponent();
            nlist = CloneUsingXaml(listBox.Items[0]) as Grid;
            listBox.Items.RemoveAt(0);

            

            for(int i = 1; i > 10; i++)
            {
                int index =  listBox.Items.Add(CloneUsingXaml(nlist));
                Grid tgrid = listBox.Items[index] as Grid;
                TextBlock ttext = tgrid.Children[0] as TextBlock;
                ttext.Text = "Dupa " + i;

            }

            LoadAll();
           
        }

        private async void LoadAll()
        {
            await Task.Run(() => {
                bool sciagac = false;
                string appListFilePath = System.AppDomain.CurrentDomain.BaseDirectory + "applist.txt";
                string configFilePath = System.AppDomain.CurrentDomain.BaseDirectory + "config.ini";
                if (File.Exists(appListFilePath) && File.Exists(configFilePath))
                {
                    FileInfo fi = new FileInfo(appListFilePath);
                    if (fi.Length.ToString() != File.ReadAllText(configFilePath))
                    {
                        sciagac = true;
                    }
                }
                else
                {
                    sciagac = true;
                }

                string thtml = "";
                if (sciagac)
                {
                    File.WriteAllLines(appListFilePath, new[] { ReadTextFromUrl(@"http://api.steampowered.com/ISteamApps/GetAppList/v2/") });
                    thtml = ReadTextFromUrl(@"http://api.steampowered.com/ISteamApps/GetAppList/v2/");
                    FileInfo fi = new FileInfo(appListFilePath);
                    File.WriteAllText(configFilePath, fi.Length.ToString());
                }

                thtml = File.ReadAllText(appListFilePath);
                idlist = JsonConvert.DeserializeObject(thtml);
                idlist = idlist["applist"]["apps"];

            });
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select Steam folder";
                fbd.RootFolder = Environment.SpecialFolder.Desktop;
                string temppath = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", "").ToString().Replace(@"/", @"\") + @"\AppList";
                fbd.SelectedPath = temppath;

                DialogResult result = System.Windows.Forms.DialogResult.No;
                var files = new List<string>();
                if (Directory.Exists(temppath))
                {
                    result = System.Windows.Forms.DialogResult.OK;
                    files = Directory.GetFiles(temppath).ToList().CustomSort().ToList();
                }
                else
                {
                    result = fbd.ShowDialog();
                    temppath = fbd.SelectedPath + @"\AppList";
                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(temppath))
                    {
                        //System.Windows.Forms.MessageBox.Show(fbd.SelectedPath);

                        files = Directory.GetFiles(fbd.SelectedPath).ToList().CustomSort().ToList();

                    }
                }


                if (result == System.Windows.Forms.DialogResult.OK && Directory.Exists(temppath))
                {
                    appListPath = temppath;
                    //System.Windows.Forms.MessageBox.Show(fbd.SelectedPath);



                    //System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                    foreach (string a in files)
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

            var test1 = idlistL.CustomSort().ToList();
            comboBox.ItemsSource = test1;
            isLoaded = true;

        }

        public long GetFileSize(string url)
        {
            long result = -1;

            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            req.Method = "HEAD";
            using (System.Net.WebResponse resp = req.GetResponse())
            {
                if (long.TryParse(resp.Headers.Get("Content-Length"), out long ContentLength))
                {
                    result = ContentLength;
                }
            }

            return result;
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
                loadfromid();
            }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
            loadfromid();
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
            loadfromid();
        }

        private string temptext = "";

        private void loadfromid()
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

        private void loadfromname()
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
                loadfromid();

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
            }
            loadfromname();
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
                    if (listBox.SelectedIndex > 1 && listBox.SelectedIndex <= listBox.Items.Count)
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
            /*
            string temppathhook = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", "").ToString().Replace(@"/", @"\") + @"\";
            if (sender == button_Run_NoHook)
            {
                File.Copy(temppathhook + @"green\DllInjectornohook.ini", temppathhook + @"DllInjector.ini", true);
                File.Copy(temppathhook + @"green\DLLInjector.exe", temppathhook + @"DLLInjector.exe", true);
                File.Copy(temppathhook + @"green\GreenLuma_Reborn_x86.dll", temppathhook + @"GreenLuma_Reborn_x86.dll", true);
                File.Copy(temppathhook + @"green\bin\x64launcher_o.exe", temppathhook + @"bin\x64launcher.exe", true);


                var cParams = @" -DisablePreferSystem32Images -CreateFile1 NoHook.bin -CreateFile2 NoQuestion.bin";
                ProcessStartInfo startInfo = new ProcessStartInfo(string.Concat(temppathhook, "DLLInjector.exe"));
                startInfo.Arguments = cParams;
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = temppathhook;
                Process.Start(startInfo);


            }

            if (sender == button_Run_Hook)
            {
                File.Copy(temppathhook + @"green\DllInjectorhook.ini", temppathhook + @"DllInjector.ini", true);
                File.Copy(temppathhook + @"green\DLLInjector.exe", temppathhook + @"DLLInjector.exe", true);
                File.Copy(temppathhook + @"green\GreenLuma_Reborn_x86.dll", temppathhook + @"GreenLuma_Reborn_x86.dll", true);
                File.Copy(temppathhook + @"green\GreenLuma_Reborn_x64.dll", temppathhook + @"GreenLuma_Reborn_x64.dll", true);
                File.Copy(temppathhook + @"green\bin\x64launcher.exe", temppathhook + @"bin\x64launcher.exe", true);

                var cParams = @" -DisablePreferSystem32Images -CreateFile1 NoQuestion.bin";
                ProcessStartInfo startInfo = new ProcessStartInfo(string.Concat(temppathhook, "DLLInjector.exe"));
                startInfo.Arguments = cParams;
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = temppathhook;
                Process.Start(startInfo);
            }
            */
        }
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
