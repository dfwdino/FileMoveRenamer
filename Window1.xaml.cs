using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.Data;
using System.IO;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading;

namespace FileMoveRenamer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        bool RenameFiles = false;
        bool CloseWhenDone = false;

        public delegate void CheckBlah();
        
        
        delegate void DMoveProcessBar();

        public Window1()
        {
            InitializeComponent();
        }

        private void btnSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            string NewSourceFolder = GetFolderLocation(txtSourceFolder.Text);

            if (NewSourceFolder.Length.Equals(0))
                return;

            if (txtSourceFolder.Text.Length.Equals(0))
                txtSourceFolder.Text = NewSourceFolder;
            else
                txtSourceFolder.Text += "," + NewSourceFolder;

        }

        private void btnDesationFolder_Click(object sender, RoutedEventArgs e)
        {
            txtDesationFolder.Text = GetFolderLocation(txtDesationFolder.Text);
        }


        


        private void MoveProcessBar_WhyAreYouHere()
        {
            progressBar1.Value += 1;
        }



        private string GetFolderLocation(string FolderPath)
        {
            string _FolderPath = FolderPath;

            if (_FolderPath.Length.Equals(0))
                _FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);

            System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();
            
            ofd.SelectedPath = _FolderPath;

            ofd.ShowNewFolderButton = false;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return ofd.SelectedPath;
            }

            return string.Empty;

        }

        private void btnMove_Click(object sender, RoutedEventArgs e)
        {
            if(txtSourceFolder.Text.IndexOf(",").Equals(-1))
                if (!System.IO.Directory.Exists(txtSourceFolder.Text))
                {
                    MessageBox.Show("Can't source folder.");
                    return;
                }
            
            if (!System.IO.Directory.Exists(txtDesationFolder.Text))
                {
                    MessageBox.Show("Can't Desertion folder.");
                    return; 
                }

            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            
            ///Make a function for this or get all the files in dir then pass tot he move function.  :-\
            //progressBar1.Maximum = System.IO.Directory.GetFiles(txtSourceFolder.Text).Length;

            RenameFiles = (bool)cbxRenameFiles.IsChecked;
            CloseWhenDone = (bool)ckbCloseAfterMove.IsChecked;

            WriteLocationToFile(txtSourceFolder.Text, txtDesationFolder.Text, (bool)cbxRenameFiles.IsChecked);

            System.Threading.Thread myth = new System.Threading.Thread(MoveFile);

            myth.IsBackground = true;
            myth.SetApartmentState(ApartmentState.STA);

            lblProcessedFiles.Content = progressBar1.Value + " of " + progressBar1.Maximum + " files moved.";

            myth.Start(txtSourceFolder.Text + ";" + txtDesationFolder.Text);

            
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = "File Mover and Rename. " + GetAsseblyVersion();
            LoadLocationFromFile();
        }

        private string GetAsseblyVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            return asm.GetName().Version.ToString();

        }
        
        public delegate bool DCheckCloseAfterDone();
        private void CheckCloseAfterDone()
        {
            CloseWhenDone = (bool)ckbCloseAfterMove.IsChecked;
        }

        private void CheckStatus()
        {

            progressBar1.Value += 1;
            
            lblProcessedFiles.Content = progressBar1.Value + " of " + progressBar1.Maximum + " files moved.";

        }

        private void WriteLocationToFile(string source, string desation,bool CheckboxChecked)
        {
            // create a writer and open the file
            TextWriter tw = new StreamWriter(System.IO.Directory.GetCurrentDirectory() + "\\" + "app.data");

            // write a line of text to the file
            tw.WriteLine(source + ";" + desation + ";" + CheckboxChecked);

            tw.Flush();

            // close the stream
            tw.Close();

            tw.Dispose();


        }

        private void MoveFile(object folderlists)
        {

            string[] SD = folderlists.ToString().Split(';');
            string[] sourcefolders = SD[0].Split(',');
            string Desation = SD[1];

            foreach (var sourcefolder in sourcefolders)
            {
                try
                {

                    foreach (string tempfile in System.IO.Directory.GetFiles(sourcefolder))
                    {
                        if (RenameFiles)
                        {
                            System.IO.File.SetCreationTimeUtc(tempfile, DateTime.UtcNow);
                            System.IO.File.SetLastWriteTimeUtc(tempfile, DateTime.UtcNow);
                            System.IO.File.SetLastAccessTimeUtc(tempfile, DateTime.UtcNow);

                            System.IO.FileInfo myFile = new System.IO.FileInfo(tempfile);

                            System.IO.File.Move(tempfile,
                                    Desation + "\\" + DateTime.Now.ToFileTimeUtc() + myFile.Extension.ToLower());

                        }
                        else
                        {
                            System.IO.FileInfo myFile = new System.IO.FileInfo(tempfile);

                            try
                            {

                                if (System.IO.File.Exists(Desation + "\\" + myFile.Name))
                                    System.IO.File.Move(tempfile, Desation + "\\" + DateTime.Now.Second + "_" + myFile.Name);
                                else
                                    System.IO.File.Move(tempfile, Desation + "\\" + myFile.Name);
                            }
                            catch (Exception ex)
                            {
                                global::System.Windows.Forms.MessageBox.Show(ex.Message);
                            }

                        }

                        progressBar1.Dispatcher.Invoke(DispatcherPriority.Normal, new CheckBlah(CheckStatus));

                    }
                }
                catch (IOException ex)
                {
                    if (!ex.Message.StartsWith("The device is not ready"))
                        MessageBox.Show("Error accessing " + sourcefolder + ". Error is " + ex.Message);
                }
            }
            MessageBox.Show("Done","Move Done.",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information,
                            MessageBoxResult.OK,
                            MessageBoxOptions.DefaultDesktopOnly);

           
            

        }

        private void LoadLocationFromFile()
        {
            TextReader tr = null;

            if(!System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + "\\" + "app.data"))
                return;

            try
            {
                // create reader & open file
                tr = new StreamReader(System.IO.Directory.GetCurrentDirectory() + "\\" + "app.data");
            }
            catch { }

            // read a line of text
            string[] tmep = tr.ReadLine().Split(';');

            txtSourceFolder.Text = tmep[0];
            txtDesationFolder.Text = tmep[1];
            if(tmep.Length.Equals(3))
                cbxRenameFiles.IsChecked =  Convert.ToBoolean(tmep[2].ToString());

            // close the stream
            tr.Close();


        }

        private void MLBD(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process CallProgram = new System.Diagnostics.Process();

            CallProgram.StartInfo.FileName = "explorer.exe";
            CallProgram.StartInfo.Arguments = txtDesationFolder.Text;

            CallProgram.Start();

            CallProgram.Dispose();
            CallProgram = null;


        }

        private void lblOpen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process CallProgram = new System.Diagnostics.Process();

            CallProgram.StartInfo.FileName = "explorer.exe";
            CallProgram.StartInfo.Arguments = txtSourceFolder.Text;

            CallProgram.Start();

            CallProgram.Dispose();
            CallProgram = null;
        }

        private void lblClearSource_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult mbr = MessageBox.Show("Are you sure you want to clear out the sounce list?", "Clear", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (mbr.Equals(MessageBoxResult.Yes))
                txtSourceFolder.Text = "";
        }

        private void txtSourceFolder_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
