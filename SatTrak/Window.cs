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
// WOLFRAM APP ID: XWHTJL-7RWXKQ3W34

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Net.Sockets;
using System.Web;
using Zeptomoby.OrbitTools;
using WolframAlphaNET;
using WolframAlphaNET.Misc;
using WolframAlphaNET.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace SatTrak
{
    public partial class Window : Form
    {



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //INIT & DEINIT
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        #region Init

        #region Vars
        private static Timer radarTimer = new System.Windows.Forms.Timer();
        private static Timer LockTimer = new System.Windows.Forms.Timer();
        private static Timer joystickTimer;
        private static int joyStickBNum;
        private static int joyStickFPS = 10;
        private static decimal sensitivity = 0.5M;
        private static List<Tle> Satellites = new List<Tle>();
        public static bool LockedOn = false;
        public static Tle Target;
        public static bool aimLock = false;
        public string WolframAppId = "XWHTJL-7RWXKQ3W34";
        delegate void SetTextCallback(int prog);
        private static TcpClient client;
        private static NetworkStream stream;
        private static double longitude = Convert.ToDouble(Properties.Settings.Default["longitude"]);
        private static double latitude = Convert.ToDouble(Properties.Settings.Default["latitude"]);
        private static decimal[] aimAt = {0,0};
        #endregion

        public Window()
        {
            InitializeComponent();
        }

        private void Window_Load(object sender, EventArgs e)
        {
            radarTimer.Tick += new EventHandler(UpdateInfo);
            radarTimer.Interval = 200;
            radarTimer.Start();
            this.Radar.Paint +=new System.Windows.Forms.PaintEventHandler(this.Radar_Paint);

            LockTimer.Tick += new EventHandler(SendPosition);
            LockTimer.Interval = 500;
            LockTimer.Start();

            longitudeBox.Text = longitude.ToString();
            latitudeBox.Text = latitude.ToString();

            ipBox.Text = Properties.Settings.Default["hostIP"].ToString();
            portBox.Text = Properties.Settings.Default["hostPort"].ToString();

            //dont need no more ghosts :)
        }

        private void Window_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(!(Convert.ToDouble(Properties.Settings.Default["longitude"]) == longitude && 
               Convert.ToDouble(Properties.Settings.Default["latitude"]) == latitude   &&
               Properties.Settings.Default["hostIP"].ToString() == ipBox.Text          &&
               Properties.Settings.Default["hostPort"].ToString() == portBox.Text)
            )
            {
                DialogResult dialogResult = MessageBox.Show("Would you like to save your settings?", "SatTrak", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    //save
                    Properties.Settings.Default["longitude"] = longitude;
                    Properties.Settings.Default["latitude"] = latitude;
                    Properties.Settings.Default["hostIP"] = ipBox.Text;
                    Properties.Settings.Default["hostPort"] = portBox.Text;
                    Properties.Settings.Default.Save();
                }
            }
        }

        //Returns the azimuth and elevation from a TLE
        private double[] getSkyCoords(Tle tle)
        {
            double[] array = { -1 , -1 };
            //create a viewing site
            Site site = new Site(latitude, longitude, 0.240949);
            //make an orbit out of the tle
            Orbit orbit = new Orbit(tle);
            //get the date
            DateTime dt = DateTime.UtcNow;
            //get the time since the epoch
            TimeSpan ts = orbit.TPlusEpoch(dt);
            //then calculate the position
            try
            {
                EciTime eci = orbit.GetPosition(dt);
                Topo topoLook = site.GetLookAngle(eci);
                array[0] = topoLook.AzimuthDeg;
                array[1] = topoLook.ElevationDeg;
                return array;
            }
            catch
            {
                return array;
            }
        }

        private static void sendData(string message)
        {
            //sendin som datas ft antoine
            string output = "";

            try
            {

                Byte[] data = new Byte[256];
                data = System.Text.Encoding.ASCII.GetBytes(message);

                stream.Write(data, 0, data.Length);

                //String responseData = String.Empty;

                //Int32 bytes = stream.Read(data, 0, data.Length);
                //responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                // Close everything.

            }
            catch (ArgumentNullException e)
            {
                output = "ArgumentNullException: " + e.Message;
                MessageBox.Show(output);
            }
            catch (SocketException e)
            {
                output = "SocketException: " + e.Message;
                MessageBox.Show(output);
            }
            catch (InvalidOperationException e)
            {
                output = "InvalidOperationException: " + e.Message;
                MessageBox.Show(output);
            }
            catch (IOException e)
            {
                output = "IOException: " + e.Message;
                MessageBox.Show(output);
            }
            
        }

        private void setDescription(string name)
        {
          
            WebClient client = new WebClient();
            StringBuilder url = new StringBuilder("http://en.wikipedia.org/w/api.php?action=query&prop=extracts&format=json&exintro=&titles=");
            url.Append(HttpUtility.UrlEncode(name.Trim(' ').Replace(" ", "-")));
            //Console.WriteLine(url.ToString());
            Stream stream = client.OpenRead(url.ToString());
            StreamReader lineReader = new StreamReader(stream);
         
            Stream webStream = client.OpenRead(url.ToString());
            StreamReader reader = new StreamReader(webStream);

            string response = reader.ReadToEnd();
            JObject data = JObject.Parse(response);
            try
            {
                string noHTML = Regex.Replace(data["query"]["pages"].First.First["extract"].ToString(), @"<[^>]+>|&nbsp;", "").Trim();
                desc.Text = noHTML;
            }
            catch (NullReferenceException)
            {
                desc.Text = "N/A";
            }
        }

        #endregion



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //UPDATE
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        #region Update

        public void updateSatList(List<Tle> tleList)
        {
            if (!(Satellites.Count > 0))
            {
                foreach(Tle temp in tleList)
                {
                    Satellites.Add(temp);
                    satList.Items.Add(temp.Name);
                }
            }
            else
            {
                List<Tle> addList = new List<Tle>();
                foreach (Tle temp in tleList)
                {
                    Boolean addIt = true;
                    foreach (Tle temp2 in Satellites)
                    {
                        if (temp.IntlDescription.ToString() == temp2.IntlDescription.ToString())
                        {
                            addIt = false;
                        }
                    }
                    if (addIt)
                    {
                        addList.Add(temp);
                        satList.Items.Add(temp.Name);
                    }
                }
                Satellites.AddRange(addList);
            }
            statusText.Text = "Done.";
        }

        public void UpdateInfo(Object obj, EventArgs e)
        {
            //clear the points and creating a new series
            this.Radar.Series[0].Points.Clear();
            this.Radar.Series[1].Points.Clear();
            this.Radar.Series[2].Points.Clear();

            foreach (Tle tle in Satellites)
            {
                double[] array;
                try
                {
                    array = getSkyCoords(tle);
                }
                catch (Zeptomoby.OrbitTools.DecayException)
                {
                    continue;
                }
                if (!(array == null))
                {
                    if (LockedOn && Target != null && tle.Name == Target.Name)
                    {
                        Radar.Series[1].Points.AddXY(array[0], array[1]);
                        Radar.Series[2].Points.AddXY(array[0], array[1]);
                    }
                    else
                    {
                        Radar.Series[0].Points.AddXY(array[0], array[1]);
                    }
                }
                    
            }
            if (!LockedOn)
            {
                Radar.Series[1].Points.AddXY(aimAt[0], aimAt[1]);
            }
            else
            //this is temporary
            if (!(Target == null))
                showSatInfo();
        }

        public void SendPosition(Object obj, EventArgs e)
        {
            if (client != null && client.Connected)
            {
                if(Target != null && LockedOn)
                {
                    double[] coord = getSkyCoords(Target);
                    sendData("START|" + coord[0].ToString() + "|" + coord[1].ToString() + "|END");
                }
                else if (aimLock || manualAimToggle.Checked)
                {
                    sendData("START|" + aimAt[0].ToString() + "|" + aimAt[1].ToString() + "|END");
                }
            }
        }

        private void showSatInfo()
        {
            //to update the info on the sidebar
            try
            {
                satNameLabel.Text = Target.Name;
                while (satNameLabel.Width < System.Windows.Forms.TextRenderer.MeasureText(satNameLabel.Text, new Font(satNameLabel.Font.FontFamily, satNameLabel.Font.Size, satNameLabel.Font.Style)).Width)
                {
                    satNameLabel.Font = new Font(satNameLabel.Font.FontFamily, satNameLabel.Font.Size - 0.5f, satNameLabel.Font.Style);
                }
                while (satNameLabel.Width > System.Windows.Forms.TextRenderer.MeasureText(satNameLabel.Text, new Font(satNameLabel.Font.FontFamily, satNameLabel.Font.Size, satNameLabel.Font.Style)).Width)
                {
                    satNameLabel.Font = new Font(satNameLabel.Font.FontFamily, satNameLabel.Font.Size + 0.5f, satNameLabel.Font.Style);
                }
            }
            catch (ArgumentException)
            {
                Console.WriteLine("wat");
            }

            /*
             * SECOND WAY TO DO IT
             * 
            Orbit orbit = new Orbit(Target);
            //get the date
            DateTime dt = DateTime.UtcNow;
            //get the time since the epoch
            TimeSpan ts = orbit.TPlusEpoch(dt);
            //then calculate the position
            EciTime eci = orbit.GetPosition(dt);

            Console.WriteLine("vel: " + eci.Velocity.X + " " + eci.Velocity.Y + " " + eci.Velocity.Z + " " + eci.Velocity.W);
            */
            double meanMotion = Convert.ToDouble(Target.Line2.Substring(52, 10));
            double twothirds = 0.666666666666666;
            double a = 6.6228 / (Math.Pow(meanMotion, twothirds));
            double semimajor = a * 6371.1;
            double apogee = (semimajor * (1.0 + Convert.ToDouble(Target.Eccentricity)) - 6371.1);
            double perigee = (semimajor * (1.0 - Convert.ToDouble(Target.Eccentricity)) - 6371.1);

            designatorLabel.Text = "Int'l Des: " + Target.IntlDescription;
            apogeeLabel.Text = "Apogee: " + apogee.ToString("0.0") + "km";
            perigeeLabel.Text = "Perigee: " + perigee.ToString("0.0") + "km";
            inclinationLabel.Text = "Inclination: " + Target.Inclination;
            
        }

        public void SetLoadProgress(int prog)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (this.azimuthBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetLoadProgress);
                this.Invoke(d, new object[] { prog });
            }
            else
            {
                this.statusProgressBar.Value = prog;
            }
        }

        #endregion



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //CLICK HANDLERS
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        
        
        #region ClickHandlers

        //THIS HANDLES REMOVING SATELLITES
        private void removeButton_Click(object sender, EventArgs e)
        {
            //check if something is selected
            if(satList.SelectedItem == null)
            {
                return;
            }

            //delete the entry from the list
            string name = satList.SelectedItem.ToString();
            int num = satList.SelectedIndex;
            for (int i = satList.SelectedIndices.Count - 1; i >= 0; i--)
            {
                satList.Items.RemoveAt(satList.SelectedIndices[i]);
            }
            statusText.Text = "Removed " + name + ".";

            //delete the tle corresponding to the list entry
            foreach (Tle tle in Satellites)
            {
                if (tle.Name == name)
                {
                    Satellites.Remove(tle);
                    if (!(Target == null))
                    {
                        if (Target.Name == name)
                            LockedOn = false;
                    }
                    break;
                }
            }

            //select the one that replaces it so we can chain delete
            try
            {
                satList.SelectedIndex = num;
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            statusText.Text = "Adding satellites.";
            AddWindow addwindow = new AddWindow();
            addwindow.Show();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            for (int c = Satellites.Count - 1; c >= 0; c--)
            {
                Satellites.RemoveAt(c);
            }
            string name = "ghost", line1 = "1 40107U 14046A   14250.92710958 -.00000361  00000-0  00000+0 0   339", line2 = "2 40107 000.0329 293.8315 0001387 253.7602 238.3546 01.00268192   384";
            Tle ghost = new Tle(name, line1, line2);
            Satellites.Add(ghost);
            for (int i = satList.Items.Count - 1; i >= 0; i--)
            {
                satList.Items.RemoveAt(i);
            }
            aimLock = false;
            LockedOn = false;
            statusText.Text = "Cleared satellite list.";
        }

        private void lockButton_Click(object sender, EventArgs e)
        {
            if (satList.SelectedItem == null)
                return;
            //This could probably break depending on the sitituation TODO: FIX
            statusText.Text = "Locking onto: " + satList.SelectedItem.ToString();
            LockedOn = true;
            foreach (Tle tle in Satellites)
            {
                if (tle.Name == satList.SelectedItem.ToString())
                {
                    Target = tle;
                    break;
                }
            }
            aimLock = false;
            manualAimToggle.CheckState = 0;
            /*
            WolframAlpha wolfram = new WolframAlpha(WolframAppId);

            QueryResult results = wolfram.Query(Target.Name);
            try
            {
                if (results != null)
                {
                    foreach (Pod pod in results.Pods)
                    {
                        if (pod.Title == "Wikipedia summary")
                        {
                            Console.WriteLine(pod.SubPods[0].Image.Alt);
                            desc.ImageLocation = pod.SubPods[0].Image.Src;
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("ERROR");
            }
            */
            setDescription(Target.Name);

        }

        private void aimButton_Click(object sender, EventArgs e)
        {
            if (LockedOn)
                LockedOn = false;

            if (manualAimToggle.Checked)
                manualAimToggle.CheckState = 0;

                
            aimLock = true;
            statusText.Text = "Aim locked to: " + azimuthBox.Value.ToString() + "°, " + elevationBox.Value.ToString() + "°";
                
            aimAt[0] = azimuthBox.Value;
            aimAt[1] = elevationBox.Value;
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    statusText.Text = "Connecting to: " + ipBox.Text.ToString() + ":" + portBox.Text.ToString();
                    client = new TcpClient(ipBox.Text, Convert.ToInt32(portBox.Text));
                    stream = client.GetStream();
                    statusText.Text = "Connected.";
                }
                catch (SocketException err)
                {
                    MessageBox.Show(err.Message);
                }

            }
            catch (SocketException er)
            {
                MessageBox.Show("Target IP and port actively refused the connection.");
            }
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {
            if (client != null)
            {
                client.Close();
                stream.Close();
                statusText.Text = "Disconnecting from: " + ipBox.Text.ToString() + ":" + portBox.Text.ToString();
            }
            else
            {
                MessageBox.Show("Client not connected.");
            }
        }

        private void coordSetButton_Click(object sender, EventArgs e)
        {
            if (longitudeBox.Text.Trim().Length == 0 || 
                latitudeBox.Text.Trim().Length == 0 || 
                Convert.ToDouble(longitudeBox.Text) > 180 || 
                Convert.ToDouble(longitudeBox.Text) < -180 || 
                Convert.ToDouble(latitudeBox.Text) > 90 || 
                Convert.ToDouble(latitudeBox.Text) < -90
            )
            {
                MessageBox.Show("Invalid coordinates");
                return;
            }
            longitude = Convert.ToDouble(longitudeBox.Text);
            latitude = Convert.ToDouble(latitudeBox.Text);
        }

        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //save
            Properties.Settings.Default["longitude"] = longitude;
            Properties.Settings.Default["latitude"] = latitude;
            Properties.Settings.Default["hostIP"] = ipBox.Text;             
            Properties.Settings.Default["hostPort"] = portBox.Text;
            Properties.Settings.Default.Save();
            statusText.Text = "Settings saved.";
        }

        #region Joystick

        private void upButton_MouseDown(object sender, MouseEventArgs e)
        {
            joystickTimer = new Timer();
            joystickTimer.Interval = joyStickFPS;
            joystickTimer.Tick += joyStick_Tick;
            joystickTimer.Start();
            joyStickBNum = 1;
        }

        private void rightButton_MouseDown(object sender, MouseEventArgs e)
        {
            joystickTimer = new Timer();
            joystickTimer.Interval = joyStickFPS;
            joystickTimer.Tick += joyStick_Tick;
            joystickTimer.Start();
            joyStickBNum = 2;
        }

        private void downButton_MouseDown(object sender, MouseEventArgs e)
        {
            joystickTimer = new Timer();
            joystickTimer.Interval = joyStickFPS;
            joystickTimer.Tick += joyStick_Tick;
            joystickTimer.Start();
            joyStickBNum = 3;
        }

        private void leftButton_MouseDown(object sender, MouseEventArgs e)
        {
            joystickTimer = new Timer();
            joystickTimer.Interval = joyStickFPS;
            joystickTimer.Tick += joyStick_Tick;
            joystickTimer.Start();
            joyStickBNum = 4;
        }

        private void joyStick_Tick(object sender, EventArgs e)
        {
            if (manualAimToggle.Checked)
            {
                switch(joyStickBNum)
                {
                    case 1:
                        aimAt[1] += sensitivity;
                        if (aimAt[1] > 90)
                            aimAt[1] = 90.0M;
                        break;
                    case 2:
                        aimAt[0] += sensitivity;
                        if (aimAt[0] > 360)
                            aimAt[0] = 360.0M;
                        break;
                    case 3:
                        aimAt[1] -= sensitivity;
                        if (aimAt[1] < 0)
                            aimAt[1] = 0.0M;
                        break;
                    case 4:
                        aimAt[0] -= sensitivity;
                        if (aimAt[0] < 0)
                            aimAt[0] = 0.0M;
                        break;
                }
            }
        }

        private void upButton_MouseUp(object sender, MouseEventArgs e)
        {
            joystickTimer.Stop();
            joystickTimer = null;
        }

        private void rightButton_MouseUp(object sender, MouseEventArgs e)
        {
            joystickTimer.Stop();
            joystickTimer = null;
        }

        private void downButton_MouseUp(object sender, MouseEventArgs e)
        {
            joystickTimer.Stop();
            joystickTimer = null;
        }

        private void leftButton_MouseUp(object sender, MouseEventArgs e)
        {
            joystickTimer.Stop();
            joystickTimer = null;
        }

        private void manualAimToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (manualAimToggle.Checked)
            {
                if (LockedOn || aimLock)
                {
                    LockedOn = false;
                    aimLock = false;
                }
            }
        }

        #endregion

        #endregion



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //PAINT HANDLERS
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        #region PaintHandlers

        private void Radar_Paint(object sender, PaintEventArgs e)
        {
            #region LegacyCode
            /*
            
            ////////////////////////////////
            //////LEGACY CODE BELOW/////////
            //////     RIP IN PIECE/////////
            //////        2014-2015/////////
            ////////////////////////////////
            
            
            Graphics g = e.Graphics;

            //this draws the dishes pointing direction in real time
            float x = 0, y = 0; 
            Pen p = new Pen(Color.Red,1);
            g.DrawEllipse(p,x,y,20,20);
            g.FillRectangle(Brushes.Red, x + 10, y + 10, 1, 1);

            //this draws the target on the target
            if (LockedOn)
            {
                float[] coords = { Convert.ToSingle(getSkyCoords(Target)[0]), Convert.ToSingle(getSkyCoords(Target)[1]) };
                //Console.WriteLine(coords[0]);
                double scale = 1;

                float[] origin = { Radar.Width / 2, Radar.Height / 2 };

                //0 degree line
                if(coords[0] == 0)
                {
                    float x2 = origin[0] + coords[0], y2 = origin[1];
                    Pen p2 = new Pen(Color.Cyan, 1);
                    g.DrawEllipse(p2, x2, y2, 20, 20);
                    g.FillRectangle(Brushes.Cyan, x2 + 10, y2 + 10, 1, 1);
                }
                //first quadrant
                if(coords[0] < 90 && coords[0] > 0 && !(coords[1] < 0) )
                {
                    double[] deg = { 90 - (coords[0] * Math.PI / 180), 90 - (coords[1] * Math.PI/180) };
                    double dist = (90 - coords[1]);
                    double dx = (dist * (Math.Cos(deg[0]))), dy = (dist * (Math.Sin(deg[1])));
                    double x2 = origin[0] + dx, y2 = origin[1] - dy;
                    Pen p2 = new Pen(Color.Cyan, 1);
                    g.DrawEllipse(p2, Convert.ToSingle(x2), Convert.ToSingle(y2), 20, 20);
                    g.FillRectangle(Brushes.Cyan, Convert.ToSingle(x2) + 10, Convert.ToSingle(y2) + 10, 1, 1);
                }
                //90 degree line
                if(coords[0] == 90)
                {
                    float x2 = origin[0], y2 = origin[1] + coords[1];
                    Pen p2 = new Pen(Color.Cyan, 1);
                    g.DrawEllipse(p2, x2, y2, 20, 20);
                    g.FillRectangle(Brushes.Cyan, x2 + 10, y2 + 10, 1, 1);
                }
                //second quadrant
                if(coords[0] < 180 && coords[0] > 90)
                {
                    //float dx = (90 - coords[1]) * (Convert.ToSingle(Math.Cos(90 - coords[0]))), dy = coords[1] * (Convert.ToSingle(Math.Sin(90 - coords[0])));
                    float dx = -10, dy = -10;
                    float x2 = origin[0] + dx, y2 = origin[1] + dy;
                    Pen p2 = new Pen(Color.Cyan, 1);
                    g.DrawEllipse(p2, x2, y2, 20, 20);
                    g.FillRectangle(Brushes.Cyan, x2 + 10, y2 + 10, 1, 1);
                }
                //180 degree line
                if(coords[0] == 180)
                {
                    float x2 = origin[0] - coords[0], y2 = origin[1];
                    Pen p2 = new Pen(Color.Cyan, 1);
                    g.DrawEllipse(p2, x2, y2, 20, 20);
                    g.FillRectangle(Brushes.Cyan, x2 + 10, y2 + 10, 1, 1);
                }
                //third quadrant
                if(coords[0] < 270 && coords[0] > 180)
                {
                }
                //270 degree line
                if(coords[0] == 270)
                {
                    float x2 = origin[0], y2 = origin[1] - coords[1];
                    Pen p2 = new Pen(Color.Red, 1);
                    g.DrawEllipse(p2, x2, y2, 20, 20);
                    g.FillRectangle(Brushes.Cyan, x2 + 10, y2 + 10, 1, 1);
                }
                //fourth quadrant
                if(coords[0] < 270 && coords[0] > 360)
                {
                }
                //360 degree line
                if(coords[0] == 360)
                {
                    float x2 = origin[0] + coords[0], y2 = origin[1];
                    Pen p2 = new Pen(Color.Red, 1);
                    g.DrawEllipse(p2, x2, y2, 20, 20);
                    g.FillRectangle(Brushes.Cyan, x2 + 10, y2 + 10, 1, 1);
                }
            }
            */
            #endregion
        }

        private void tableLayoutPanel8_Paint(object sender, PaintEventArgs e)
        {

        }

        private void menuTable_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void description_Click(object sender, EventArgs e)
        {

        }

        #endregion

        
    }
}
