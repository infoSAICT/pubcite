﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PubCite
{
    public partial class Form1 : Form
    {
        public static TabControl dub_tab;
        public static SG.Favorite favorites;
        //public static SettingsRecord settingsRecord;
        private TabPage newTabPage;
        TabPage createdTabPage;
        
        public Form1()
        {
            InitializeComponent();
            
            dub_tab = maintabControl;
            favorites = new SG.Favorite();
            //settingsRecord = new SettingsRecord();
            favorites.populateFavorites();

            search nSearch = new search();
            searchTab1.Controls.Add(nSearch);

            maintabControl.ImageList = imageList1;
            //maintabControl.ContextMenuStrip = tabMenuStrip;
            newTabPage = new TabPage();
            newTabPage.ImageIndex = 0;
            newTabPage.Text = "New";

            //maintabControl.SizeMode = TabSizeMode.Fixed;
            //maintabControl.ItemSize = new Size(100, 200);

            maintabControl.Controls.Add(newTabPage);
           
            maintabControl.MouseClick+=new MouseEventHandler(maintabControl_MouseClick);
         
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                MessageBox.Show("Warning: Please check your Internet connection. You may not get desired results!");
                
        }

        public void maintabControl_MouseClick(object sender, MouseEventArgs e)
        {
            Console.WriteLine("calledclick");

            for (int i = 0; i < maintabControl.TabCount; i++)
            {
                if (maintabControl.GetTabRect(i).Contains(e.Location))
                {
                    if (i == maintabControl.TabCount - 1){
                        search nSearch = new search();
                        createdTabPage = new TabPage("Search");
                        createdTabPage.ImageIndex = 1;
                        createdTabPage.Controls.Add(nSearch);
                        maintabControl.TabPages.Insert(maintabControl.TabPages.Count - 1, createdTabPage);
                        maintabControl.SelectedTab = createdTabPage;
                    }
                    else{
                        RectangleF tabArea = maintabControl.GetTabRect(i);
                        System.Console.WriteLine(tabArea.X + "///" + tabArea.Y);
                        System.Console.WriteLine(e.X + "a/b/c" + e.Y);
                        RectangleF imageArea = new RectangleF(tabArea.X + 7, tabArea.Y, 15, 15);
                        if (imageArea.Contains(e.Location))
                            maintabControl.TabPages.RemoveAt(i);
                    }
                }

            }
        }

        public static TabControl get_maintab() {

            return dub_tab;
        
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
       
    }
}
