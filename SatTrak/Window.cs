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
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Zeptomoby.OrbitTools;
using WolframAlphaNET;
using WolframAlphaNET.Misc;
using WolframAlphaNET.Objects;

namespace SatTrak
{
    public partial class Window : Form
    {



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //INIT
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        #region Init

        private static Timer radarTimer = new System.Windows.Forms.Timer();
        private static List<Tle> Satellites = new List<Tle>();
        public static Boolean LockedOn = false;
        public static Tle Target;
        public string WolframAppId = "XWHTJL-7RWXKQ3W34";
        delegate void SetTextCallback(int prog);

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

            //this is a ghost sat to make the graph apear, it will never show. this is temporary
            //there will be more ghosts depending on where the user changes his position to
            string name = "ghost", line1 = "1 40107U 14046A   14250.92710958 -.00000361  00000-0  00000+0 0   339", line2 = "2 40107 000.0329 293.8315 0001387 253.7602 238.3546 01.00268192   384";
            Tle ghost = new Tle(name,line1,line2);
            Satellites.Add(ghost);
        }

        //Returns the azimuth and elevation from a TLE
        private double[] getSkyCoords(Tle tle)
        {
            double[] array = { -1 , -1 };
            //create a viewing site
            Site site = new Site(47.381981, -68.314392, 0.240949);
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
                        if (temp.Name.Equals(temp2.Name))
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

            foreach (Tle tle in Satellites)
            {
                try
                {
                    double[] array = getSkyCoords(tle);
                    Radar.Series[0].Points.AddXY(array[0], array[1]);
                }
                catch (Zeptomoby.OrbitTools.DecayException)
                {
                    continue;
                }
            }
            //this is temporary
            if (!(Target == null))¸
                showSatInfo();
        }

        private void showSatInfo()
        {
            //to avoid calling the wolfram api more than once i`ll make this function only call once: when the target locks on
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

        public void SetLoadProgress(int prog)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (this.textBox1.InvokeRequired)
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
        }
   
        private void statusProgressBar_Click(object sender, EventArgs e)
        {

        }

        private void Radar_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        #endregion



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //PAINT HANDLERS
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        #region PaintHandlers

        private void Radar_Paint(object sender, PaintEventArgs e)
        {
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

                float scale = 1;

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
                if(coords[0] < 90 && coords[0] > 0)
                {
                    double[] deg = { coords[0] * Math.PI / 180, coords[1] * Math.PI/180};
                    double dx = (90 - (deg[1] * scale)) * (Math.Cos(90 - deg[0])), dy = (90 - (deg[1] * scale)) * (Math.Sin(90 - deg[0]));
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
        }
        #endregion



    }
}
