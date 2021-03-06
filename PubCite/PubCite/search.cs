﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using Transitions;
namespace PubCite
{
    public partial class search : UserControl
    {
        List<SG.Author> FavAuthorList;
        List<SG.Journal> FavJournalList;
        List<SG.Paper> Papers;
        List<String> RecentSearchKeys = new List<string>();

        List<string> auth_url;
        List<string> authors;
        CSXParser CSParser;
        GSScraper GSScraper;
        MicrosoftScholarParser MSParser;
        ListViewItem item;
        SG.AuthSuggestion authSug = null;
        SG.Author authStats = null;
        SG.Journal journalStats;
        List<SG.Paper> graphPapers;
        List<String> cachedList;


        int prevSelectedIndex;
        int suggestedIndex;
        int citationIndex;
        int citationType;
        int lastCount;
        Boolean[] prevSortedColum = { false, false, false, false };
        Boolean[] a = { false, false, false };
        Boolean suggestions;
        Boolean? nextData;
        Boolean STOP;
        Settings Nsettings;
        SettingsRecord NsettingsRecord = new SettingsRecord();
        Boolean searchHint = true, StartYearHint = true, EndYearHint = true, AdvanFilterHint = true;
        String gs_nextUrl;

        public search()
        {
            InitializeComponent();
            CSParser = new CSXParser();
            GSScraper = new GSScraper();
            MSParser = new MicrosoftScholarParser();
            ArrangeTree();

            siteComboBox.SelectedItem = "Google Scholar";
            graphComboBox.SelectedItem = "Publications Per Year";
            authorResultsListView.FullRowSelect = true;
            authorResultsListView.MouseClick += new MouseEventHandler(authorResultsListView_MouseClick);
            authorResultsListView.ColumnClick += new ColumnClickEventHandler(authorResultsListView_ColumnClick);
            authorResultsListView.MouseDoubleClick += new MouseEventHandler(authorResultsListView_MouseDoubleClick);
            authorResultsListView.DrawItem += ListView_DrawItem;
            authorResultsListView.DrawColumnHeader += ListView_DrawColumnHeader;

            journalResultsListView.FullRowSelect = true;
            journalResultsListView.MouseClick += new MouseEventHandler(journalResultsListView_MouseClick);
            journalResultsListView.ColumnClick += new ColumnClickEventHandler(journalResultsListView_ColumnClick);
            journalResultsListView.MouseDoubleClick += new MouseEventHandler(journalResultsListView_MouseDoubleClick);
            journalResultsListView.DrawItem += ListView_DrawItem;
            journalResultsListView.DrawColumnHeader += ListView_DrawColumnHeader;

            authorsSuggestions.MouseDoubleClick += new MouseEventHandler(authorsSuggestions_MouseClick);
            authorsSuggestions.FullRowSelect = true;
            authorsSuggestions.DrawItem += ListView_DrawItem;
            authorsSuggestions.DrawColumnHeader += ListView_DrawColumnHeader;

            recentListView.DrawItem += ListView_DrawItem;
            recentListView.DrawColumnHeader += ListView_DrawColumnHeader;
            recentListView.MouseDoubleClick+=new MouseEventHandler(recentListView_MouseDoubleClick);
            recentListView.MouseClick += new MouseEventHandler(recentListView_MouseClick);

            favouritesTreeView.MouseDoubleClick += new MouseEventHandler(favouritesTreeView_MouseDoubleClick);
            searchField.GotFocus += new EventHandler(searchField_GotFocus);
            searchField.LostFocus += new EventHandler(searchField_LostFocus);
            searchField.KeyUp += new KeyEventHandler(searchField_KeyUp);
        
            KeywordsTextBox.KeyUp += new KeyEventHandler(KeywordsTextBox_KeyUp);


            progressBar.SendToBack();
            progressBar.Visible = false;

            authorCheckBox.Click += new EventHandler(authorCheckBox_Click);
            journalCheckBox.Click += new EventHandler(journalCheckBox_Click);

            cachedListView.FullRowSelect = true;
            cachedListView.Click += new EventHandler(cachedListView_Click);
            cachedListView.HeaderStyle = ColumnHeaderStyle.None;
            cachedListView.SendToBack();

            KeywordsTextBox.LostFocus += new EventHandler(KeywordsTextBox_LostFocus);
            KeywordsTextBox.GotFocus += new EventHandler(KeywordsTextBox_GotFocus);

            searchField.Text = "enter author or journal name";
            KeywordsTextBox.Text = "e.g: <author>,<co-author>,<journal>,<keywords>";
           
            showSearch();
            STOP = false;
            lastCount = 0;
        }

        void KeywordsTextBox_GotFocus(object sender, EventArgs e)
        {
            if (AdvanFilterHint == true)
            {

                KeywordsTextBox.Text = "";
                AdvanFilterHint = false;
            }
        }

        void KeywordsTextBox_LostFocus(object sender, EventArgs e)
        {
            if (KeywordsTextBox.Text.Length == 0 && AdvanFilterHint == false)
            {

                KeywordsTextBox.Text = "e.g: <author>,<co-author>,<journal>,<keywords>";
                AdvanFilterHint = true;
            }
        }

       

        void recentListView_MouseClick(object sender, MouseEventArgs e)
        {
                        
            if (e.Button == MouseButtons.Right)
            {
                
                if (recentListView.FocusedItem.Bounds.Contains(e.Location) == true)
                    recentMenuStrip.Show(Cursor.Position);
            }
        }

        

        void KeywordsTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (AdvanFilterHint == false)
            {
                if (authorResultsListView.Visible)
                    populateAuthor();
                else
                    populateJournal();
            }
        }

        void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        void ListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.DimGray, e.Bounds);
            e.DrawText();
        }

        private void searchField_GotFocus(object sender, EventArgs e)
        {
            if (searchField.Text.Length > 0 && searchHint==false )
            {
                if (cachedList.Count > 0)
                {
                    cachedListView.BringToFront();
                    cachedListView.Visible = true;

                }
            }

            if (searchHint == true)
            {

                searchField.Text = "";
                searchHint = false;
            }


        }
        private void searchField_LostFocus(object sender, EventArgs e)
        {
            cachedListView.Visible = false;
            cachedListView.SendToBack();

            if (searchField.Text.Length == 0 && searchHint == false) {

                searchField.Text = "Enter Author or Journal name";
                searchHint = true;
            }
                
        }


        public void cachedListView_Click(object sender, EventArgs e)
        {


            Console.WriteLine(cachedListView.FocusedItem.Text);
            searchField.Text = cachedListView.FocusedItem.Text;
            cachedListView.Visible = false;
            /* Display cached object */
            if (authorCheckBox.Checked)
            {
                authStats = (SG.Author)cacheObject.Get(searchField.Text, authorCheckBox.Checked);
                populateAuthor();
            }
            else
            {
                journalStats = (SG.Journal)cacheObject.Get(searchField.Text, authorCheckBox.Checked);
                populateJournal();
                
            }
        }

        private void recentListView_MouseDoubleClick(object sender, EventArgs e) {

            if (recentListView.FocusedItem.SubItems[1].Text.Equals("Author")) {


                authStats = (SG.Author)cacheObject.Get(recentListView.FocusedItem.SubItems[0].Text, true);
                populateAuthor();

            }
            else if (recentListView.FocusedItem.SubItems[1].Text.Equals("Journal")) {

                journalStats = (SG.Journal)cacheObject.Get(recentListView.FocusedItem.SubItems[0].Text, false);
                populateJournal();
            
            }
        
        
        
        }

        private void authorCheckBox_Click(object sender, EventArgs e)
        {
            journalCheckBox.Checked = !(journalCheckBox.Checked);
            updateCacheSuggestions();

        }

        private void journalCheckBox_Click(object sender, EventArgs e)
        {
            authorCheckBox.Checked = !(authorCheckBox.Checked);
            updateCacheSuggestions();
        }

        public void searchField_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                searchIcon_Click(sender, e);
                System.Windows.Forms.SendKeys.Send("{tab}");
                
            }

            updateCacheSuggestions();
        }

        public GroupBox get_sugg()
        {

            return Suggestions;

        }

        public void updateCacheSuggestions()
        {
            cachedListView.Items.Clear();
            if (searchField.Text.Length > 0)
            {
                cachedList = cacheObject.GetMatchingkeys(searchField.Text, authorCheckBox.Checked);
                if (cachedList.Count > 0)
                {
                    cachedListView.Visible = true;
                    cachedListView.BringToFront();
                    //Console.WriteLine(suggestions.Count);
                    for (int i = 0; i < cachedList.Count; i++)
                    {
                        item = new ListViewItem(cachedList[i]);
                        cachedListView.Items.Add(item);
                    }
                }
                else
                {
                    cachedListView.Visible = false;
                    cachedListView.SendToBack();
                }
            }
            else
            {
                cachedListView.Visible = false;
                cachedListView.SendToBack();
            }

        }

        public void populateSuggestions()
        {

            Suggestions.Visible = true;
            for (int i = 0; i < authors.Count; i++)
            {

                item = new ListViewItem(authors[i]);
                authorsSuggestions.Items.Add(item);
            }
        }

        private void populateJournal()
        {

            if (journalStats != null && journalStats.getNumberOfPapers() != 0)
            {
                if (lastCount == 0)
                {
                    journalResultsListView.Items.Clear();
                    authorResultsListView.Visible = false;
                    journalResultsListView.Visible = true;
                }

                if (StartYear.getintval() == 0 && EndYear.getintval() == 0)
                    Papers = journalStats.getPapers();
                else if (StartYear.getintval() != 0 && EndYear.getintval() == 0)
                    Papers = journalStats.getPaperByYearRange(StartYear.getintval());
                else if (StartYear.getintval() == 0 && EndYear.getintval() != 0)
                    Papers = journalStats.getPaperUptoYear(EndYear.getintval());
                else if (StartYear.getintval() != 0 && EndYear.getintval() != 0)
                    Papers = journalStats.getPaperByYearRange(StartYear.getintval(), EndYear.getintval());
                Console.WriteLine(Papers.Count);
                if (AdvanFilterHint == false)
                    Papers = journalStats.getPaperByKeywords(Papers, KeywordsTextBox.Text);
                else Papers = journalStats.getPaperByKeywords(Papers, "");

                /*Statistics */
                authorNameLabel.Text = journalStats.Name;
                citesperPaper.Text = journalStats.getCitesPerPaper().ToString();
                citesperYear.Text = journalStats.getCitesPerYear().ToString();
                hindex.Text = journalStats.getHIndex().ToString();
                i10index.Text = journalStats.getI10Index().ToString();
                citationsNumberLabel.Text = journalStats.getTotalNumberofCitations().ToString();
                numPapers.Text = Papers.Count.ToString();
                for (int i = lastCount; i < Papers.Count; i++)
                {
                    item = new ListViewItem(Papers[i].Title);
                    item.SubItems.Add(Papers[i].Authors);
                    item.SubItems.Add(Papers[i].NumberOfCitations.ToString());
                    if (Papers[i].Year == -1 || Papers[i].Year == 0)
                        item.SubItems.Add("--");
                    else
                        item.SubItems.Add(Papers[i].Year.ToString());
                    journalResultsListView.Items.Add(item);

                }
            }
            else
            {
                MessageBox.Show("Journal results not found! You may have exceeded your administrative limit.");
            }
        }

        private void populateAuthor()
        {

            if (authStats != null && authStats.getNumberOfPapers() != 0)
            {
                if (lastCount == 0)
                {
                    authorResultsListView.Items.Clear();
                    authorResultsListView.Visible = true;
                    journalResultsListView.Visible = false;

                }

                /* Filter by year range */
                if (StartYear.getintval() == 0 && EndYear.getintval() == 0)
                    Papers = authStats.getPapers();
                else if (StartYear.getintval() != 0 && EndYear.getintval() == 0)
                    Papers = authStats.getPaperByYearRange(StartYear.getintval());
                else if (StartYear.getintval() == 0 && EndYear.getintval() != 0)
                    Papers = authStats.getPaperUptoYear(EndYear.getintval());
                else if (StartYear.getintval() != 0 && EndYear.getintval() != 0)
                    Papers = authStats.getPaperByYearRange(StartYear.getintval(), EndYear.getintval());

                /* Filter by keywords */
                if(AdvanFilterHint==false)
                Papers = authStats.getPaperByKeywords(Papers, KeywordsTextBox.Text);
                else Papers = authStats.getPaperByKeywords(Papers, "");

                Console.WriteLine(Papers.Count);

                /* Statistics */
                authorNameLabel.Text = authStats.Name;
                citesperPaper.Text = authStats.getCitesPerPaper().ToString();
                citesperYear.Text = authStats.getCitesPerYear().ToString();
                hindex.Text = authStats.getHIndex().ToString();
                i10index.Text = authStats.getI10Index().ToString();
                citationsNumberLabel.Text = authStats.getTotalNumberofCitations().ToString();
                numPapers.Text = Papers.Count.ToString();

                /* Papers */
                for (int i = lastCount; i < Papers.Count; i++)
                {
                    /*populating */

                    item = new ListViewItem(Papers[i].Title);
                    item.SubItems.Add(Papers[i].Publication);
                    if (Papers[i].Year == -1 || Papers[i].Year == 0)
                        item.SubItems.Add("--");
                    else
                        item.SubItems.Add(Papers[i].Year.ToString());
                    item.SubItems.Add(Papers[i].NumberOfCitations.ToString());
                    authorResultsListView.Items.Add(item);
                }
            }
            else
            {
                MessageBox.Show("Results not found! You may have exceeded your administrative limit.");
            }
        }

        private void updateHistory(string name,int t)
        {
            item = new ListViewItem(name);
            if (t == 0) item.SubItems.Add("Author");
            else if (t == 1) item.SubItems.Add("Journal");
            recentListView.Items.Add(item);
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (recentListView.FocusedItem.SubItems[1].Text.Equals("Author"))
            {


                cacheObject.Clear(recentListView.FocusedItem.SubItems[0].Text, true);
                populateAuthor();

            }
            else if (recentListView.FocusedItem.SubItems[1].Text.Equals("Journal"))
            {

                cacheObject.Clear(recentListView.FocusedItem.SubItems[0].Text, false);
                populateJournal();

            }

            recentListView.FocusedItem.Remove();


        }


        public void ArrangeTree()
        {

            FavAuthorList = Form1.favorites.AuthorList;
            FavJournalList = Form1.favorites.JournalList;
            favouritesTreeView.Nodes[0].Nodes[0].Nodes.Clear();
            favouritesTreeView.Nodes[0].Nodes[1].Nodes.Clear();
            for (int i = 0; i < FavAuthorList.Count; i++)
            {

                favouritesTreeView.Nodes[0].Nodes[0].Nodes.Add(new TreeNode(FavAuthorList[i].Name));
                favouritesTreeView.Nodes[0].Nodes[0].Nodes[i].ContextMenuStrip = favouriteMenuStrip;
            }
            for (int i = 0; i < Form1.favorites.JournalList.Count; i++)
            {
                favouritesTreeView.Nodes[0].Nodes[1].Nodes.Add(new TreeNode(FavJournalList[i].Name));
                favouritesTreeView.Nodes[0].Nodes[1].Nodes[i].ContextMenuStrip = favouriteMenuStrip;
            }
        }

        private void journalResultsListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {

            if (journalStats != null)
            {
                if (e.Column == 0)
                {

                    if (prevSortedColum[0] == false)
                    {
                        journalStats.sortPapersByTitle(true);
                        prevSortedColum[0] = true;
                    }
                    else
                    {
                        journalStats.sortPapersByTitle(false);
                        prevSortedColum[0] = false;
                    }
                }

                else if (e.Column == 3)
                {
                    if (prevSortedColum[3] == false)
                    {
                        journalStats.sortPapersByYear(true);
                        prevSortedColum[3] = true;
                    }
                    else
                    {
                        journalStats.sortPapersByYear(false);
                        prevSortedColum[3] = false;

                    }
                }
                else if (e.Column == 2)
                {
                    if (prevSortedColum[2] == false)
                    {
                        journalStats.sortPapersByCitations(true);
                        prevSortedColum[2] = true;
                    }
                    else
                    {
                        journalStats.sortPapersByCitations(false);
                        prevSortedColum[2] = false;
                    }
                }
                populateJournal();
            }



        }

        private void authorResultsListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (authStats != null)
            {
                if (e.Column == 0)
                {

                    if (prevSortedColum[0] == false)
                    {
                        authStats.sortPapersByTitle(true);
                        prevSortedColum[0] = true;
                    }
                    else
                    {
                        authStats.sortPapersByTitle(false);
                        prevSortedColum[0] = false;
                    }
                }
                else if (e.Column == 1)
                {
                    if (prevSortedColum[1] == false)
                    {
                        authStats.sortPapersByPublication(true);
                        prevSortedColum[1] = true;
                    }
                    else
                    {
                        authStats.sortPapersByPublication(false);
                        prevSortedColum[1] = false;

                    }

                }
                else if (e.Column == 2)
                {
                    if (prevSortedColum[2] == false)
                    {
                        authStats.sortPapersByYear(true);
                        prevSortedColum[2] = true;
                    }
                    else
                    {
                        authStats.sortPapersByYear(false);
                        prevSortedColum[2] = false;

                    }
                }
                else if (e.Column == 3)
                {
                    if (prevSortedColum[3] == false)
                    {
                        authStats.sortPapersByCitations(true);
                        prevSortedColum[3] = true;
                    }
                    else
                    {
                        authStats.sortPapersByCitations(false);
                        prevSortedColum[3] = false;
                    }
                }
                populateAuthor();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Parent.GetContainerControl();
        }




        private void authorResultsListView_MouseClick(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Right)
            {

                if (authorResultsListView.FocusedItem.Bounds.Contains(e.Location) == true)
                    optionsMenuStrip.Show(Cursor.Position);
            }
        }

        private void journalResultsListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                if (journalResultsListView.FocusedItem.Bounds.Contains(e.Location) == true)
                    optionsMenuStrip.Show(Cursor.Position);
            }
        }

        private void Favorites_Click_1(object sender, EventArgs e)
        {
            Form1.favorites.AddAuthor(authStats);
        }

        private void disablePanels()
        {

            resultsPanel.Enabled = false;
            favouritesPanel.Enabled = false;
        }

        private void enablePanels()
        {
            resultsPanel.Enabled = true;
            favouritesPanel.Enabled = true;
        }

        private void startProgressUI()
        {

            progressPanel.Visible = true;
            progressBar.BringToFront();
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 15;
        }

        private void endProgressUI()
        {

            /* Stop the progress bar */
            progressBar.MarqueeAnimationSpeed = 0;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = progressBar.Minimum;

            progressPanel.Visible = false;
            progressBar.SendToBack();
            progressBar.Visible = false;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.MarqueeAnimationSpeed = 0;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = progressBar.Minimum;
        }

        private void backgroundWorker_genSearchWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Background worker...");
            if (authorCheckBox.Checked == true)
            {
                prevSelectedIndex = -1;
                // get suggestions
                if (a[0])
                    authSug = CSParser.getAuthSuggestions(searchField.Text);
                else if (a[1])
                {
                    authSug = GSScraper.getAuthSuggestions(searchField.Text);
                }
                else
                    authSug = MSParser.getAuthSuggestions(searchField.Text);

                if (authSug == null || !authSug.isSet())
                {
                    /* Case : No suggestions */
                    if (a[0] == true)
                    {
                        authStats = CSParser.getAuthors(searchField.Text, "", "");
                        authStats.Type = 0;
                    }
                    else if (a[1] == true)
                    {
                        authStats = GSScraper.getAuthors(searchField.Text, "", "", ref gs_nextUrl);
                        if (authStats == null)
                            MessageBox.Show("Oops! You have reached administrative limit for Google Scholar." +
                                "\nIf you are behind a proxy server, please try changing your settings.");
                        else
                            authStats.Type = 1;
                    }
                    else
                    {
                        authStats = MSParser.getAuthors(searchField.Text, "", "");
                        authStats.Type = 2;
                    }
                    suggestions = false;
                }
                else
                {
                    authors = authSug.getSuggestions();
                    auth_url = authSug.getSuggestionsURL();

                    suggestions = true;
                }
            }
            else // journal
            {
                if (a[0])
                {
                    journalStats = CSParser.getJournals(searchField.Text, "", "");
                    journalStats.Type = 0;
                }
                else if (a[1])
                {
                    journalStats = GSScraper.getJournals(searchField.Text, "", "", ref gs_nextUrl);
                    if(journalStats == null)
                       MessageBox.Show("Oops! You have reached administrative limit for Google Scholar." +
                                "\nIf you are behind a proxy server, please try changing your settings.");
                    else
                        journalStats.Type = 1;
                }
                else
                {
                    journalStats = MSParser.getJournals(searchField.Text, "", "");
                    journalStats.Type = 2;
                }
            }
        }

        private void backgroundWorker_authSearchWork(object sender, DoWorkEventArgs e)
        {

            if (prevSelectedIndex != suggestedIndex)
            {
                if (a[0] == true)
                {
                    authStats = CSParser.getAuthStatistics(auth_url[suggestedIndex]);
                    authStats.Type = 0;
                }
                else if (a[1] == true)
                {
                    authStats = GSScraper.getAuthStatistics(auth_url[suggestedIndex]);
                    if (authStats == null)
                        MessageBox.Show("Oops! You have reached administrative limit for Google Scholar." +
                            "\nIf you are behind a proxy server, please try changing your settings.");
                    else
                        authStats.Type = 1;
                }
                else if (a[2] == true)
                {
                    authStats = MSParser.getAuthStatistics(auth_url[suggestedIndex]);
                    authStats.Type = 2;
                }
                prevSelectedIndex = suggestedIndex;
            }
        }

        
        private void backgroundWorker_authstatsNextDataWork(object sender, DoWorkEventArgs e)
        {
            if (authStats.Type == 1)
                nextData = GSScraper.getAuthStatisticsNextPage(auth_url[suggestedIndex], ref authStats);
            else if (authStats.Type == 2)
                nextData = MSParser.getAuthStatisticsNext(auth_url[suggestedIndex], ref authStats);

        }

        private void backgroundWorker_authorsNextDataWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("type : " + authStats.Type);
            if (authStats.Type == 1)
                nextData = GSScraper.getAuthorsNextPage(gs_nextUrl, ref authStats, ref gs_nextUrl);
            else if  (authStats.Type == 2)
                nextData = MSParser.getAuthorsNext(searchField.Text, "", "", ref authStats);
            else
                nextData = CSParser.getAuthorsNext(searchField.Text, "", "", ref authStats);

        }

        private void backgroundWorker_journalsNextDataWork(object sender, DoWorkEventArgs e)
        {
            if (journalStats.Type == 1)
                nextData = GSScraper.getJournalsNextPage(gs_nextUrl, ref journalStats, ref gs_nextUrl);
            else if (journalStats.Type == 2)
                nextData = MSParser.getJournalsNext(searchField.Text, "", "", ref journalStats);
            else
                nextData = CSParser.getJournalsNext(searchField.Text, "", "", ref journalStats);
        }


        private void getNextAuthStats(bool hasProfile)
        {
            /* Get author data in the background 
             */
            Console.WriteLine("authorNext");
            nextData = true;
            while (nextData == true && STOP == false)
            {
                lastCount = authStats.getNumberOfPapers();
                BackgroundWorker backgroundWorker1 = new BackgroundWorker();
                if (hasProfile && authStats.Type == 0)
                    break;
                if(hasProfile)
                    backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker_authstatsNextDataWork);
                else
                    backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker_authorsNextDataWork);
                backgroundWorker1.RunWorkerAsync();
                while (backgroundWorker1.IsBusy)
                {
                    Application.DoEvents();
                    // disable those options which will trigger another thread 
                }

                populateAuthor();
            }
            lastCount = 0;
            STOP = false;
        }

        private void getNextJournal()
        {
            /* Get next journal data in the background */
            Console.WriteLine("authorNext");
            nextData = true;
            while (nextData == true && STOP == false)
            {
                lastCount = journalStats.getNumberOfPapers();
                BackgroundWorker backgroundWorker1 = new BackgroundWorker();
                backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker_journalsNextDataWork);
                backgroundWorker1.RunWorkerAsync();
                while (backgroundWorker1.IsBusy)
                {
                    Application.DoEvents();
                    // disable those options which will trigger another thread 
                }

                populateJournal();
            }
            lastCount = 0;
            STOP = false;

        }

        private void authorsSuggestions_MouseClick(object sender, EventArgs e)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                MessageBox.Show("Warning: Please check your Internet connection!");
            else
            {
                if (authorsSuggestions.Focused && authorsSuggestions.FocusedItem != null)
                    suggestedIndex = authorsSuggestions.FocusedItem.Index;
                authorNameLabel.Text = "";
                citesperPaper.Text = "";
                citesperYear.Text = "";
                hindex.Text = "";
                i10index.Text = "";
                citationsNumberLabel.Text = "";
                numPapers.Text = "";
                showStop();
                startProgressUI();
                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_authSearchWork);
                //backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);

                backgroundWorker.RunWorkerAsync();
                while (backgroundWorker.IsBusy)
                {
                    Application.DoEvents();
                    // disable those options which will trigger another thread 
                }

                if (authStats != null && authStats.getNumberOfPapers() != 0)
                {
                    /* Add to cache */
                    cacheObject.Add(authStats.Name, authStats, true);
                    /* Add to recent History */
                    RecentSearchKeys.Add(authStats.Name);
                    updateHistory(authStats.Name, 0);
                    populateAuthor();

                    getNextAuthStats(true);
                    showSearch();
                    endProgressUI();
                }
                else
                    MessageBox.Show("No results available! You may have exceeded your administrative limit.");
            }
        }

        private void searchIcon_Click(object sender, EventArgs e)
        {

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                MessageBox.Show("Warning: Please check your Internet connection!");
            else
            {
                stopButton.Focus();
                showStop();
                authorNameLabel.Text = "";
                citesperPaper.Text = "";
                citesperYear.Text = "";
                hindex.Text = "";
                i10index.Text = "";
                citationsNumberLabel.Text = "";
                numPapers.Text = "";

                if (siteComboBox.SelectedItem != null)
                {
                    if (siteComboBox.SelectedItem.ToString().Equals("CiteSeerX"))
                    {
                        a[0] = true;
                        a[1] = false;
                        a[2] = false;

                    }
                    else if (siteComboBox.SelectedItem.ToString().Equals("Google Scholar"))
                    {
                        a[0] = false;
                        a[1] = true;
                        a[2] = false;

                    }
                    else if (siteComboBox.SelectedItem.ToString().Equals("Microsoft Academic Search"))
                    {
                        a[0] = false;
                        a[1] = false;
                        a[2] = true;
                    }
                }
                else
                {
                    siteComboBox.SelectedItem = "Google Scholar";
                    /* GS default */
                    a[0] = false;
                    a[1] = true;
                    a[2] = false;
                }


                for (int i = 0; i < 4; i++) prevSortedColum[i] = false;

                authorsSuggestions.Items.Clear();
                suggestedIndex = 0;
                authorResultsListView.Items.Clear();
                journalResultsListView.Items.Clear();

                cachedListView.Visible = false;
                cachedListView.SendToBack();


                startProgressUI();

                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_genSearchWork);
                //backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);

                if (siteComboBox.SelectedItem.ToString().Equals("CiteSeerX"))
                {
                    a[0] = true;
                    a[1] = false;
                    a[2] = false;

                }
                else if (siteComboBox.SelectedItem.ToString().Equals("Google Scholar"))
                {
                    a[0] = false;
                    a[1] = true;
                    a[2] = false;

                }
                else if (siteComboBox.SelectedItem.ToString().Equals("Microsoft Academic Search"))
                {
                    a[0] = false;
                    a[1] = false;
                    a[2] = true;
                }

                backgroundWorker.RunWorkerAsync();

                while (backgroundWorker.IsBusy)
                {
                    Application.DoEvents();
                    // disable those options which will trigger another thread 
                }

                if (authorCheckBox.Checked == true)
                {
                    Console.WriteLine("Done" + suggestions);
                    if (suggestions)
                    {
                        populateSuggestions();
                        authorsSuggestions_MouseClick(sender, e);
                        
                    }
                    else
                    {
                        if (authStats != null && authStats.getNumberOfPapers() != 0)
                        {
                            /* Add to cache */
                            cacheObject.Add(authStats.Name, authStats, true);
                            /* Add to recent History */
                            RecentSearchKeys.Add(authStats.Name);
                            updateHistory(authStats.Name, 0);
                            populateAuthor();

                            /* Threaded requests */
                            getNextAuthStats(false);
                            if (nextData == null)
                            {
                                MessageBox.Show("Oops! You have reached administrative limit for Google Scholar." +
                                    "\nIf you are behind a proxy server, please try changing your settings.");
                            }
                        }
                        else
                            MessageBox.Show("No results available! You may have exceeded your administrative limit.");
                    }
                }
                else
                {
                    if (journalStats != null && journalStats.getNumberOfPapers() != 0)
                    {

                        /* add journal to cache */
                        cacheObject.Add(journalStats.Name, journalStats, false);

                        /* add journal to recent */
                        RecentSearchKeys.Add(journalStats.Name);
                        updateHistory(journalStats.Name, 1);
                        populateJournal();
                        getNextJournal();
                        if (nextData == null)
                        {
                            MessageBox.Show("Oops! You have reached administrative limit for Google Scholar." +
                                "\nIf you are behind a proxy server, please try changing your settings.");
                        }
                    }
                    else
                        MessageBox.Show("No results available! You may have exceeded your administrative limit.");
                }

                showSearch();
                endProgressUI();
            }
        }

        private void viewURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                MessageBox.Show("Warning: Please check your Internet connection!");
            else
            {
                if (authorResultsListView.Visible == true)
                    openBrowserForUrl(Papers[authorResultsListView.FocusedItem.Index].TitleURL);
                else
                    openBrowserForUrl(Papers[journalResultsListView.FocusedItem.Index].TitleURL);
            }
        }

        private void authorResultsListView_MouseDoubleClick(object sender, EventArgs e)
        {
            viewCitationsToolStripMenuItem_Click(sender, e);
        }

        private void journalResultsListView_MouseDoubleClick(object sender, EventArgs e)
        {
            viewCitationsToolStripMenuItem_Click(sender, e);
        }

        private void viewCitationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                MessageBox.Show("Warning: Please check your Internet connection!");
            else
            {
                if (authorResultsListView.Visible == true)
                {
                    citationIndex = authorResultsListView.FocusedItem.Index;
                    citationType = authStats.Type;

                }
                else
                {
                    citationIndex = journalResultsListView.FocusedItem.Index;
                    citationType = journalStats.Type;
                }

                if (Papers[citationIndex].NumberOfCitations != 0)
                {

                    TabPage citationsPage = new TabPage("Citations");
                    citationsPage.ImageIndex = 1;
                    Form1.dub_tab.TabPages.Insert(Form1.dub_tab.TabPages.Count - 1, citationsPage);
                    CitationsTab NcitTab = new CitationsTab(Papers[citationIndex], citationType);
                    citationsPage.Controls.Add(NcitTab);

                    Form1.dub_tab.SelectedTab = citationsPage;
                }
            }
        }

        private void addToFavourite_Click(object sender, EventArgs e)
        {
            
            if (authStats!=null && !authStats.IsFavorite)
            {

                Form1.favorites.AddAuthor(authStats);
                authStats.IsFavorite = true;

            }
            else if (journalStats!=null && !journalStats.IsFavorite)
            {

                Form1.favorites.AddJournal(journalStats);
                journalStats.IsFavorite = true;
            }
            ArrangeTree();
        }

        private void favouritesTreeView_MouseDoubleClick(object sender, EventArgs e)
        {

            viewStatisticsToolStripMenuItem_Click(sender, e);
        }

        private void viewStatisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (favouritesTreeView.SelectedNode != null)
                {
                    if (favouritesTreeView.SelectedNode.Level == 2)
                    {
                        if (favouritesTreeView.SelectedNode.Parent.Index == 0)
                        {
                            authStats = FavAuthorList[favouritesTreeView.SelectedNode.Index];
                            populateAuthor();
                        }
                        else if (favouritesTreeView.SelectedNode.Parent.Index == 1)
                        {// Journal selected
                            journalStats = FavJournalList[favouritesTreeView.SelectedNode.Index];
                            populateJournal();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.GetBaseException());
            }
        }

        private void removeFromFavouritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (favouritesTreeView.SelectedNode != null)
                {
                    if (favouritesTreeView.SelectedNode.Level == 2)
                    {
                        if (favouritesTreeView.SelectedNode.Parent.Index == 0)
                        {
                            Form1.favorites.removeAuthor(favouritesTreeView.SelectedNode.Index);
                            favouritesTreeView.SelectedNode.Remove();
                        }
                        else if (favouritesTreeView.SelectedNode.Parent.Index == 1)
                        {
                            Form1.favorites.removeJournal(favouritesTreeView.SelectedNode.Index);
                            favouritesTreeView.SelectedNode.Remove();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.GetBaseException());
            }
        }

        private void StartYear_TextChanged(object sender, EventArgs e)
        {
            Console.WriteLine(StartYear.Text);
            if (StartYear.Text.Length == 4 || StartYear.getintval() == 0)
            {
                if (authorResultsListView.Visible == true && authStats != null)
                    populateAuthor();
                else if (journalStats != null)
                    populateJournal();
            }
        }

        private void EndYear_TextChanged(object sender, EventArgs e)
        {

            if (EndYear.Text.Length == 4 || EndYear.getintval() == 0)
            {
                if (authorResultsListView.Visible == true && authStats != null)
                    populateAuthor();
                else if (journalStats != null)
                    populateJournal();
            }
        }

        private void openBrowserForUrl(string url)
        {
            if (url != null && !(url.ToLower().Equals("not found")) && url.Length > 0)
            {
                TabPage bpage = new TabPage("Browser");
                Browser browser;
                browser = new Browser(url);
                bpage.Controls.Add(browser);
                bpage.ImageIndex = 1;
                Form1.dub_tab.TabPages.Insert(Form1.dub_tab.TabPages.Count - 1, bpage);
                Form1.dub_tab.SelectedTab = bpage;
            }
            else
            {
                MessageBox.Show("Sorry!Url unavailable");
            }
        }

        private void authorNameLabel_Click(object sender, EventArgs e)
        {
            if (authorResultsListView.Visible == true && authStats != null)
            {
                Console.WriteLine(authStats.HomePageURL);
                openBrowserForUrl(authStats.HomePageURL);
            }
        }

        private void settingsIcon_Click(object sender, EventArgs e)
        {
             Nsettings = (NsettingsRecord).ReadSettings();
             Console.WriteLine(Nsettings.DaysCache);
             cacheNumericUpDown.Value = Nsettings.DaysCache;
             googleNumericUpDown.Value = Nsettings.GSMaxResults;
             microsoftNumericUpDown.Value = Nsettings.MASMaxResults;
             citeseerNumericUpDown.Value = Nsettings.CiteSeerMaxResults;

            Transition t = new Transition(new TransitionType_EaseInEaseOut(500));
            t.add(settingsPanel, "Top", 9);
            t.run();
        }

        private void favouriteButton_Click(object sender, EventArgs e)
        {
            Transition t = new Transition(new TransitionType_EaseInEaseOut(500));
            t.add(favouritesTreeView, "Left", 11);
            t.add(recentSearchPanel, "Left", -250);
            t.run();
        }

        private void recentButton_Click(object sender, EventArgs e)
        {
            Transition t = new Transition(new TransitionType_EaseInEaseOut(500));
            t.add(favouritesTreeView, "Left", -250);
            t.add(recentSearchPanel, "Left", 11);
            t.run();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("say cheese");
            STOP = true;
            showSearch();
            endProgressUI();
        }

        private void showSearch()
        {
            stopButton.Visible = false;
            stopButton.SendToBack();
            searchIcon.BringToFront();
            searchIcon.Visible = true;
        }

        private void showStop()
        {
            stopButton.BringToFront();
            stopButton.Visible = true;
            searchIcon.SendToBack();
            searchIcon.Visible = false;
        }

        private void settingsCloseIcon_Click(object sender, EventArgs e)
        {
            Transition t = new Transition(new TransitionType_EaseInEaseOut(500));
            t.add(settingsPanel, "Top", -800);
            t.run();
        }

        private void settingsOk_Click(object sender, EventArgs e)
        {
            Transition t = new Transition(new TransitionType_EaseInEaseOut(500));
            t.add(settingsPanel, "Top", -800);
            t.run();
            Console.WriteLine(cacheNumericUpDown.Value);
            Nsettings.DaysCache=(int)cacheNumericUpDown.Value;
            Nsettings.CiteSeerMaxResults=(int)citeseerNumericUpDown.Value;
            Nsettings.GSMaxResults = (int)googleNumericUpDown.Value;
            Nsettings.MASMaxResults=(int)microsoftNumericUpDown.Value;
          
            (NsettingsRecord).SaveSettings(Nsettings);
            CSParser = new CSXParser();
            GSScraper = new GSScraper();
            MSParser = new MicrosoftScholarParser();
        }

        private void clearHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cacheObject.Clearcache(true);
            cacheObject.Clearcache(false);
            recentListView.Items.Clear();
        }

        private void clearFavouritesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1.favorites.clear();
            ArrangeTree();
        }

        public void sortPapersByYearGraph(bool order)
        {
            if (order)
            {
                graphPapers.Sort((x, y) => x.Year.CompareTo(y.Year));
            }
            else
            {
                graphPapers.Sort((y, x) => x.Year.CompareTo(y.Year));
            }
        }

        private void graphsButton_Click(object sender, EventArgs e)
        {
            if ((authorResultsListView.Visible == true && authStats != null && authStats.getNumberOfPapers() > 0) || (journalResultsListView.Visible == true && journalStats != null && journalStats.getNumberOfPapers() > 0))
            {

                graphsChart.Titles.Clear();
                Transition t = new Transition(new TransitionType_EaseInEaseOut(500));
                t.add(graphsPanel, "Top", 83);
                t.run();
                graphsChart.Series["Series1"].Points.Clear();

                if (graphComboBox.Text.Equals("Citations of Publications Per Year"))
                {
                    graphsChart.Titles.Add("Citations of Publications Per Year");
                    graphsChart.Titles.Add("Citations").Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Left;
                    graphsChart.Titles.Add("Year").Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;

                    int currYear;
                    int j;
                    int currCitations = 0;
                    graphPapers = Papers;
                    sortPapersByYearGraph(true);
                    for (j = 0; j < graphPapers.Count; j++)
                    {
                        if (graphPapers[j].Year > 2) break;

                    }
                    currYear = graphPapers[j].Year;


                    for (int i = j; i < graphPapers.Count; i++)
                    {
                        if (currYear == graphPapers[i].Year)
                        {

                            currCitations += graphPapers[i].NumberOfCitations;

                        }
                        else
                        {

                            graphsChart.Series["Series1"].Points.AddXY(currYear, currCitations);
                            currYear = graphPapers[i].Year;
                            currCitations = graphPapers[i].NumberOfCitations;



                        }
                    }

                }
                else if (graphComboBox.Text.Equals("Publications Per Year"))
                {


                    graphsChart.Titles.Add("Publications Per Year");
                    graphsChart.Titles.Add("Publications").Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Left;
                    graphsChart.Titles.Add("Year").Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;

                    int currYear;
                    int j;
                    int currpublic = 0;
                    graphPapers = Papers;
                    sortPapersByYearGraph(true);
                    for (j = 0; j < graphPapers.Count; j++)
                    {
                        if (graphPapers[j].Year > 2) break;

                    }
                    currYear = graphPapers[j].Year;


                    for (int i = j; i < graphPapers.Count; i++)
                    {
                        if (currYear == graphPapers[i].Year)
                        {

                            currpublic++;

                        }
                        else
                        {

                            graphsChart.Series["Series1"].Points.AddXY(currYear, currpublic);
                            currYear = graphPapers[i].Year;
                            currpublic = 1;
                        }
                    }
                }
            }
            else
                MessageBox.Show("Please select an author or journal before viewing graphs");

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Transition t = new Transition(new TransitionType_EaseInEaseOut(500));
            t.add(graphsPanel, "Top", -900);
            t.run();

        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
            if ((authorResultsListView.Visible == true && authStats != null && authStats.getNumberOfPapers() > 0) || (journalResultsListView.Visible == true && journalStats != null && journalStats.getNumberOfPapers() > 0))
            {
               

                graphsChart.Titles.Clear();
                Transition t = new Transition(new TransitionType_EaseInEaseOut(500));
                t.add(graphsPanel, "Top", 111);
                t.run();
                graphsChart.Series["Series1"].Points.Clear();

                if (graphComboBox.SelectedItem == null) graphComboBox.SelectedItem = "Citations of Publications Per Year";
                else{
                if (graphComboBox.Text.Equals("Citations of Publications Per Year"))
                {
                    graphsChart.Titles.Add("Citations of Publications Per Year");
                    graphsChart.Titles.Add("Citations").Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Left;
                    graphsChart.Titles.Add("Year").Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;

                    int currYear;
                    int j;
                    int currCitations = 0;
                    graphPapers = Papers;
                    sortPapersByYearGraph(true);
                    for (j = 0; j < graphPapers.Count; j++)
                    {
                        if (graphPapers[j].Year > 2) break;

                    }

                    if (j != graphPapers.Count)
                    {
                        currYear = graphPapers[j].Year;


                        for (int i = j; i < graphPapers.Count; i++)
                        {
                            if (currYear == graphPapers[i].Year)
                            {

                                currCitations += graphPapers[i].NumberOfCitations;

                            }
                            else
                            {

                                graphsChart.Series["Series1"].Points.AddXY(currYear, currCitations);
                                currYear = graphPapers[i].Year;
                                currCitations = graphPapers[i].NumberOfCitations;



                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Graphing data not available!");
                    }

                }
                else if (graphComboBox.Text.Equals("Publications Per Year"))
                {


                    graphsChart.Titles.Add("Publications Per Year");
                    graphsChart.Titles.Add("Publications").Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Left;
                    graphsChart.Titles.Add("Year").Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;

                    int currYear;
                    int j;
                    int currpublic = 0;
                    graphPapers = Papers;
                    sortPapersByYearGraph(true);
                    for (j = 0; j < graphPapers.Count; j++)
                    {
                        if (graphPapers[j].Year > 2) break;

                    }

                    if (j != graphPapers.Count)
                    {
                        currYear = graphPapers[j].Year;


                        for (int i = j; i < graphPapers.Count; i++)
                        {
                            if (currYear == graphPapers[i].Year)
                            {

                                currpublic++;

                            }
                            else
                            {

                                graphsChart.Series["Series1"].Points.AddXY(currYear, currpublic);
                                currYear = graphPapers[i].Year;
                                currpublic = 1;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Graphing data not available!");
                    }
                }

            }
            }
            else
                MessageBox.Show("Please select an author or journal before viewing graphs");
        }

        private void graphComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void addToFav_Click(object sender, EventArgs e)
        {
            if (authStats != null && !authStats.IsFavorite && authorResultsListView.Visible == true && authStats.getNumberOfPapers() > 0)
            {

                Form1.favorites.AddAuthor(authStats);
                authStats.IsFavorite = true;
                MessageBox.Show("Added " + authStats.Name + " to favourite."); 

            }
            else if (journalStats != null && !journalStats.IsFavorite && journalStats.getNumberOfPapers() > 0)
            {

                Form1.favorites.AddJournal(journalStats);
                journalStats.IsFavorite = true;
                MessageBox.Show("Added " + journalStats.Name + " to favourite.");
            }
            ArrangeTree();
        }
    }
}



