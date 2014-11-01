/*
 * The MIT License (MIT)

Copyright (c) 2014 Manuel Dionne

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using Zeptomoby.OrbitTools;

namespace SatTrak
{
    public partial class AddWindow : Form
    {
        public AddWindow()
        {
            InitializeComponent();
        }

        private void AddWindow_Load(object sender, EventArgs e)
        {

        }

        private List<Tle> parseTLEList(string listLink)
        {
            //make a new webclient and fetch the corresponding TLE file
            WebClient client = new WebClient();
            StringBuilder url = new StringBuilder("http://www.celestrak.com/NORAD/elements/");
            url.Append(listLink);
            Stream stream = client.OpenRead(url.ToString());
            StreamReader lineReader = new StreamReader(stream);

            //calculate the total lines
            int totalLines = 0;
            while (lineReader.ReadLine() != null) { totalLines++; }
            stream.Close();

            //make a second stream object to read from again
            Stream secondStream = client.OpenRead(url.ToString());
            StreamReader reader = new StreamReader(secondStream);

            //parse each TLE and put them in a Tle array
            List<Tle> tleList = new List<Tle>();
            string line1, line2, line3;
            line1 = "";
            line2 = "";
            int lineCount = 1;
            for (int i = 0; i <= totalLines; i++)
            {
                if (lineCount == 1)
                {
                    line1 = reader.ReadLine();
                    lineCount++;
                    continue;
                }
                else if (lineCount == 2)
                {
                    line2 = reader.ReadLine();
                    lineCount++;
                    continue;
                }
                else if (lineCount == 3)
                {
                    line3 = reader.ReadLine();
                    //Console.WriteLine("New TLE:");
                    //Console.WriteLine(line1);
                    //Console.WriteLine(line2);
                    //Console.WriteLine(line3);
                    Tle tle = new Tle(line1, line2, line3);
                    tleList.Add(tle);
                    lineCount = 1;
                    continue;
                }
            }
            if (!((totalLines / 3) == tleList.Count))
            {
                MessageBox.Show("There was an error in the data sent: the number of lines in the file does not match the TLE count.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //clean up and exit
            secondStream.Close();
            return tleList;
        }

        private List<Tle> deleteDuplicates(List<Tle> list)
        {
            //this method is about as useless as a fire extinguisher in a fish tank
            return list;
        }

        private void addButton1_Click(object sender, EventArgs e)
        {

            var mainWindow = Application.OpenForms.OfType<Window>().Single();
            mainWindow.statusProgressBar.Visible = true;
            List<Tle> list = new List<Tle>();

            //Items selected
            int itemCount = checkedListBox1.SelectedItems.Count;

            //Create a new background worker
            BackgroundWorker bw = new BackgroundWorker();

            // this allows our worker to report progress during work
            bw.WorkerReportsProgress = true;

            // what to do in the background thread
            bw.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs args)
            {
                
                BackgroundWorker b = o as BackgroundWorker;

                int loop = 0;
                foreach (string name in checkedListBox1.CheckedItems)
                {
                    switch (name)
                    {
                        case "Amateur":
                            //Console.WriteLine(1);
                            list.AddRange(parseTLEList("amateur.txt"));
                            list.AddRange(parseTLEList("cubesat.txt"));
                            break;
                        case "Disaster":
                            //Console.WriteLine(2);
                            list.AddRange(parseTLEList("dmc.txt"));
                            list.AddRange(parseTLEList("sarsat.txt"));
                            break;
                        case "Geostationnary":
                            //Console.WriteLine(3);
                            list.AddRange(parseTLEList("geo.txt"));
                            break;
                        case "Navigation":
                            //Console.WriteLine(4);
                            list.AddRange(parseTLEList("gps-ops.txt"));
                            list.AddRange(parseTLEList("musson.txt"));
                            list.AddRange(parseTLEList("nnss.txt"));
                            list.AddRange(parseTLEList("beidou.txt"));
                            list.AddRange(parseTLEList("galileo.txt"));
                            list.AddRange(parseTLEList("glo-ops.txt"));
                            list.AddRange(parseTLEList("sbas.txt"));
                            break;
                        case "Military":
                            //Console.WriteLine(5);
                            list.AddRange(parseTLEList("military.txt"));
                            list.AddRange(parseTLEList("molniya.txt"));
                            break;
                        case "Communications":
                            //Console.WriteLine(6);
                            list.AddRange(parseTLEList("other-comm.txt"));
                            list.AddRange(parseTLEList("globalstar.txt"));
                            list.AddRange(parseTLEList("orbcomm.txt"));
                            list.AddRange(parseTLEList("iridium.txt"));
                            list.AddRange(parseTLEList("raduga.txt"));
                            list.AddRange(parseTLEList("x-comm.txt"));
                            list.AddRange(parseTLEList("intelsat.txt"));
                            list.AddRange(parseTLEList("gorizont.txt"));
                            break;
                        case "Stations":
                            //Console.WriteLine(7);
                            list.AddRange(parseTLEList("stations.txt"));
                            break;
                        case "Science & Weather":
                            //Console.WriteLine(8);
                            list.AddRange(parseTLEList("weather.txt"));
                            list.AddRange(parseTLEList("argos.txt"));
                            list.AddRange(parseTLEList("science.txt"));
                            list.AddRange(parseTLEList("radar.txt"));
                            list.AddRange(parseTLEList("engineering.txt"));
                            list.AddRange(parseTLEList("noaa.txt"));
                            list.AddRange(parseTLEList("resource.txt"));
                            list.AddRange(parseTLEList("education.txt"));
                            list.AddRange(parseTLEList("goes.txt"));
                            break;
                        case "Debris":
                            //Console.WriteLine(9);
                            list.AddRange(parseTLEList("1999-025.txt"));
                            list.AddRange(parseTLEList("cosmos-2251-debris.txt"));
                            list.AddRange(parseTLEList("iridium-33-debris.txt"));
                            list.AddRange(parseTLEList("2012-044.txt"));
                            break;
                        case "Other":
                            //Console.WriteLine(10);
                            list.AddRange(parseTLEList("other.txt"));
                            break;
                    }
                    Console.WriteLine("helo");
                    loop++;
                    b.ReportProgress((loop / itemCount) * 100);                    
                }
            });

            // what to do when progress changed (update the progress bar for example)
            bw.ProgressChanged += new ProgressChangedEventHandler(delegate(object o, ProgressChangedEventArgs args)
            {
                Console.WriteLine(args.ProgressPercentage);
                mainWindow.SetLoadProgress(args.ProgressPercentage);

            });

            // what to do when worker completes its task (notify the user)
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(object o, RunWorkerCompletedEventArgs args)
            {
                mainWindow.updateSatList(list);
                mainWindow.statusProgressBar.ProgressBar.Visible = false;
                mainWindow.statusProgressBar.ProgressBar.Value = 0;
                this.Close();
            });


            bw.RunWorkerAsync();
   
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void addTLE_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim().Length == 0 || textBox2.Text.Trim().Length == 0 || textBox3.Text.Trim().Length == 0)
            {
                MessageBox.Show("Please make sure you filled in ever box.");
                return;
            }
            List<Tle> list = new List<Tle>();
            Tle input = new Tle(textBox1.Text, textBox2.Text, textBox3.Text);
            list.Add(input);
            var mainWindow = Application.OpenForms.OfType<Window>().Single();
            mainWindow.updateSatList(list);
            this.Close();
        }

    }
}
