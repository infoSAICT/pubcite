﻿//Parse CiteSeerX Author Page
//Add exception handlers
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using SG;

//to use the class CSXAuth, copy the code and call the constructor with the authURL as an argument.
//CSXAuth p = new CSXAuth(authURL);

//Then, all the data is stored in p's data members. Access them as p.<member name>.

namespace PubCite
{

    //ClassifyJournal and ClassifyAuthor functions start

    public class CiteSeerJournal_FinalAuthorSearch
    {
        private string initialURL;
        private string nextURL;
        private HtmlWeb CitePage;
        private HtmlDocument CiteDoc;
        private int noResult;
        private int PageNo;
        private string searchElement;
        private int searchType;
        private Author auth1;
        private Journal journ1;

        public CiteSeerJournal_FinalAuthorSearch(string searchEle, int searchTy)  //searchType 0 for authorSearch, 1 for journalSearch
        {

            searchElement = searchEle;
            searchType = searchTy;

            auth1 = new Author(searchEle);
            journ1 = new Journal(searchEle);

            char[] searchElementTemp = searchElement.ToCharArray();
            for (int i = 0; i < searchElement.Length; i++)
            {
                if (searchElementTemp[i] == ' ')
                    searchElementTemp[i] = '+';
            }

            searchElement = new string(searchElementTemp);
            if (searchType == 0)
                initialURL = "http://citeseerx.ist.psu.edu/search?q=author%3A%28" + searchElement + "%29&submit=Search&ic=1&sort=cite&t=doc";
            else if (searchType == 1)
                initialURL = "http://citeseerx.ist.psu.edu/search?q=venue%3A" + searchElement + "&sort=cite&ic=1&t=doc";
            else
                initialURL = "";

            CitePage = new HtmlWeb();
            CiteDoc = CitePage.Load(initialURL);
            PageNo = 1;

            Console.WriteLine("Document opened");

            getNoResult();
        }

        private void getNoResult()
        {
            HtmlNode noResultNode = CiteDoc.DocumentNode.SelectSingleNode("//*[@id=\"result_info\"]/strong[2]");
            if (noResultNode == null)
            {
                noResult = 0;
                return;
            }
            Console.WriteLine(noResultNode.InnerText);

            String noResults = noResultNode.InnerText;
            noResults = noResults.Replace(",", "");

            noResult = Convert.ToInt32(noResults);
            
            if (noResult > 100)
                noResult = 100;
        }

        private int LoadNextPage()
        {
            if (PageNo > noResult / 10)
                return 0;
            if (searchType == 0)
            {
                nextURL = "http://citeseerx.ist.psu.edu/search?q=author%3A%28" + searchElement + "%29&ic=1&t=doc&sort=cite&start=" + PageNo + "0";
                PageNo++;

                CiteDoc = CitePage.Load(nextURL);
                return 1;
            }
            if (searchType == 1)
            {
                nextURL = "http://citeseerx.ist.psu.edu/search?q=venue%3A" + searchElement +"&ic=1&t=doc&sort=cite&start=" + Convert.ToString(PageNo) + "0";

                PageNo++;

                
                CiteDoc = CitePage.Load(nextURL);

                Console.WriteLine(CiteDoc.ToString());
                return 1;
            }
            return 0;
        }

        private void extractDataAllPage()
        {
            HtmlNode mainTable = CiteDoc.DocumentNode.SelectSingleNode("//*[@id=\"result_list\"]");
            HtmlNode entryNoNode, paperNode, authorNode, journalNode, yearNode, citationNode;
            string paperName, authorName, journalName, publishYear, noCitations, citationLink;
            int citno;

            if (noResult == 0)
                return;

            do
            {
                mainTable = CiteDoc.DocumentNode.SelectSingleNode("//*[@id=\"result_list\"]");
                for (int i = 1; i <= 10; i++)
                {

                    entryNoNode = mainTable.SelectSingleNode("div[" + i + "]");
                    if (entryNoNode == null)
                        break;

                    paperNode = entryNoNode.SelectSingleNode("h3/a");
                    if (paperNode == null)
                    {
                        paperNode = entryNoNode.SelectSingleNode("h3/span");
                        paperName = paperNode.InnerText;
                        paperName = paperName.Substring(37);
                    }
                    else
                        paperName = paperNode.InnerText.Substring(19);

                    Console.WriteLine(paperName);
                    //Now remove unwanted preceding character and spaces from paperName

                    authorNode = entryNoNode.SelectSingleNode("div[1]/span[1]");
                    authorName = authorNode.InnerText;
                    authorName = authorName.Substring(22);
                    //Now remove unwanted preceding character and spaces from authorName

                    if (entryNoNode.SelectSingleNode("div[1]/span[3]") == null)
                    {
                        journalName = "";
                        yearNode = entryNoNode.SelectSingleNode("div[1]/span[2]");
                        if (yearNode == null)
                            publishYear = "0";
                        else
                        {
                            publishYear = yearNode.InnerText;
                            publishYear = publishYear.Substring(2);
                        }
                    }
                    else
                    {
                        journalNode = entryNoNode.SelectSingleNode("div[1]/span[2]");
                        journalName = journalNode.InnerText;
                        journalName = journalName.Substring(2);

                        yearNode = entryNoNode.SelectSingleNode("div[1]/span[3]");
                        publishYear = yearNode.InnerText;
                        publishYear = publishYear.Substring(2);
                    }
                    //Process publishYear and journalNode to get important data

                    citationNode = entryNoNode.SelectSingleNode("div[3]/a");
                    if (citationNode.InnerText == "Abstract")
                        citationNode = entryNoNode.SelectSingleNode("div[3]/a[2]");
                    noCitations = citationNode.InnerText;   //remove unnecessary details from the number of citations
                    noCitations = noCitations.Substring(9);
                    Console.WriteLine(noCitations);
                    if (citationNode.Attributes["href"] == null)
                        citationLink = "";
                    else
                        citationLink = "http://citeseer.ist.psu.edu" + citationNode.Attributes["href"].Value;


                    citno = 0;
                    if(noCitations[0]!='t')
                    for (int j = 0; noCitations[j] != ' '; j++)
                    {
                        citno = citno * 10 + Convert.ToInt32(noCitations[j]) - 48;
                    }

                    //Now the processed strings are to be entered on the type of author
                    int year;

                    try
                    {
                        year = Convert.ToInt32(publishYear);
                    }
                    catch(Exception e)
                    {
                        year=0;
                    }

                    Paper paper1 = new Paper(paperName,"", authorName, year, journalName, "", citno, citationLink, (PageNo - 1) * 10 + i);
                    if (searchType == 0)
                        auth1.addPaper(paper1);
                    else
                        journ1.addPaper(paper1);
                    //Return the reference of the next empty 
                }
            } while (LoadNextPage() == 1);
        }


        public Author returnAuthor()                                  //for getting the ClassifyAuthors call this function
        {
            extractDataAllPage();
            return auth1;
        }

        public Journal returnJournal()                                      // //for getting the ClassifyJournals call this function
        {
            extractDataAllPage();
            return journ1;
        }
    }

    //ClassifyJournal and ClassifyAuthor functions end

    public class CSXAuthSug
    {
        public List<string> sugList, urlList;
        public bool found;

        HtmlWeb web;
        HtmlDocument doc;

        public CSXAuthSug(String authQ)
        {
            String searchElement = authQ;
            char[] searchElementTemp = searchElement.ToCharArray();
            for (int i = 0; i < searchElement.Length; i++)
            {
                if (searchElementTemp[i] == ' ')
                    searchElementTemp[i] = '+';
            }
            searchElement = new string(searchElementTemp);

            String searchURL = "http://citeseer.ist.psu.edu/search?q=" + searchElement + "&submit=Search&uauth=1&sort=ndocs&t=auth";

            web = new HtmlWeb();
            doc = web.Load(searchURL);

            if (doc != null)
                Console.WriteLine("Document Loaded!");
            else
                Console.WriteLine("Load Error!");

            extractData();
        }

        public void extractData()
        {
            HtmlNodeCollection rows = doc.DocumentNode.SelectNodes("//*[@id=\"result_list\"]/div");//*[@id="result_list"]/div[1]

            sugList = new List<string>(); urlList = new List<string>();

            if (rows == null)
            {
                found = false;
                return;
            }

            found = true;

            Console.WriteLine(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                Console.WriteLine("*** *** ***");

                /**/
                sugList.Add(rows[i].SelectSingleNode("h3").InnerText);
                Console.WriteLine(rows[i].SelectSingleNode("h3").InnerText);

                /**/
                urlList.Add("http://citeseer.ist.psu.edu" + rows[i].SelectSingleNode("h3/a").GetAttributeValue("href", "") + "&list=full");
                Console.WriteLine("http://citeseer.ist.psu.edu" + rows[i].SelectSingleNode("h3/a").GetAttributeValue("href", "") + "&list=full");

            }

        }
    }

    public class publiListEle//for author page
    {
        public int numCit;
        public String title;
        public String url;
        public String journal;
        public int year;
    }

    //interface the next two classes with the UI
    public class publiListEle2//for publi page
    {
        public int numCit;
        public String title;
        public String url;
        public String authNames;
        public String abs;
        public int year;
    }

    public class CSXPubli
    {
        public List<string> authNames;
        string abstrText;//not required, would've been stored in list processing via publiListEle2.abs, if available
        public List<publiListEle2> citeList;

        HtmlWeb web;
        HtmlDocument doc;

        public CSXPubli(String publiURL)
        {
            web = new HtmlWeb();
            doc = web.Load(publiURL);

            if (doc != null)
                Console.WriteLine("Document Loaded!");
            else
                Console.WriteLine("Load Error!");

            if (publiURL.Contains("viewdoc"))//e.g. http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.31.3487
                extractData();
            else//e.g. http://citeseer.ist.psu.edu/showciting?cid=2131272
                extractData2();
        }

        String[] Split(String s)//Gets a string containing publication information and divides it into title, journal and year strings
        {
            String[] list = new String[3];
            int st = 0, i, j;
            s = s.Trim();
            for (i = 0, j = 0; i < s.Length; i++)
            {
                if (s[i] == '\n')
                {
                    while (Char.IsWhiteSpace(s[i])) { i++; if (i >= s.Length)break; }
                    list[j++] = s.Substring(st, i - st).Trim();
                    st = i + 1;
                }
            }
            list[j] = s.Substring(st, i - st);

            return list;
        }

        public List<string> SplitAuth(string authl)
        {
            List<string> authList = new List<string>();

            int st = 0, i, j;
            for (i = 0, j = 0; i < authl.Length; i++)
            {
                if (authl[i] == ',')
                {
                    while (Char.IsWhiteSpace(authl[i])) { i++; if (i >= authl.Length)break; }
                    authList.Add(authl.Substring(st, i - st).Trim()); j++;
                    st = i + 1;
                }
            }
            authList.Add(authl.Substring(st, i - st).Trim());

            return authList;
        }

        void extractData()
        {
            /*HtmlNode authn = doc.DocumentNode.SelectSingleNode("//*[@id=\"docAuthors\"]");
            String authl = authn.InnerText.Trim().Substring(2).Trim();

            authNames = SplitAuth(authl);
            for (int i = 0; i < authNames.Count; i++)
                Console.WriteLine("Name[i] " + authNames[i]);*/

            HtmlNode absn = doc.DocumentNode.SelectSingleNode("//*[@id=\"abstract\"]/p");
            Console.Write("absn: " + absn.InnerText);

            abstrText = "";
            Console.WriteLine("absrText: " + abstrText);

            HtmlNode citUrl = doc.DocumentNode.SelectSingleNode("//*[@id=\"docCites\"]/td[2]/a");
            String publiURL = "http://citeseer.ist.psu.edu" + citUrl.GetAttributeValue("href", "");
            doc = web.Load(publiURL);

            if (doc != null)
                Console.WriteLine("extractData2()'s Document Loaded!");
            else
                Console.WriteLine("extractData2()'s Load Error!");

            extractData2();

            /*HtmlNodeCollection rows = doc.DocumentNode.SelectNodes("//*[@id=\"citations\"]/table/tr");
            String[] list;

            citeList = new List<publiListEle2>();
            publiListEle2 tempPubliObj = new publiListEle2();

            for (int i = 1; i < rows.Count; i++)
            {
                tempPubliObj = new publiListEle2();
                Console.WriteLine("*** *** ***");
                Console.WriteLine(rows[i].XPath);

                if (rows[i].SelectSingleNode("td[1]").InnerText.ToString().Trim().Length > 0)
                    tempPubliObj.numCit = Convert.ToInt32(rows[i].SelectSingleNode("td[1]").InnerText);
                else
                    tempPubliObj.numCit = 0;

                Console.WriteLine("No. of citations: " + tempPubliObj.numCit);

                list = Split(rows[i].SelectSingleNode("td[2]").InnerText);
                tempPubliObj.title = list[0];
                tempPubliObj.authNames = list[1];
                tempPubliObj.year = Convert.ToInt32(list[2]);
                tempPubliObj.url = "http://citeseer.ist.psu.edu" + rows[i].SelectSingleNode("td[2]/a").GetAttributeValue("href", "");
                Console.WriteLine(tempPubliObj.title + "|" + tempPubliObj.authNames + "|" + tempPubliObj.year + "|" + tempPubliObj.url);
                if (tempPubliObj.numCit > 0)
                    citeList.Add(tempPubliObj);
            }*/
        }

        void extractData2()
        {
            HtmlNode authn = doc.DocumentNode.SelectSingleNode("//*[@id=\"docAuthors\"]");
            if (authn != null)
            {
                String authl = authn.InnerText.Trim().Substring(2).Trim();

                authNames = SplitAuth(authl);
                for (int i = 0; i < authNames.Count; i++)
                    Console.WriteLine("Name[i] " + authNames[i]);
            }

            abstrText = "";
            Console.WriteLine("absrText: " + abstrText);

            HtmlNodeCollection rows;
            String[] list;
            publiListEle2 tempPubliObj;

            citeList = new List<publiListEle2>();

            //insert loop for going to next result pages
            /*
             * 
            HtmlNode nextLink = doc.DocumentNode.SelectSingleNode("//*[@id=\"pager\"]/a");

            if (nextLink != null)
                Console.WriteLine(nextLink.GetAttributeValue("href",""));
            else
                Console.WriteLine("next link NULL");
             * 
             */
            //'next' loop starts

            rows = doc.DocumentNode.SelectNodes("//*[@id=\"result_list\"]/div");

                for (int i = 0; i < rows.Count; i++)
                {
                    tempPubliObj = new publiListEle2();
                    Console.WriteLine("*** *** ***");

                    if (rows[i].SelectSingleNode("div[3]/a[@title=\"number of citations\"]") != null)
                    {
                        int comI = rows[i].SelectSingleNode("div[3]/a[@title=\"number of citations\"]").InnerText.Substring(9).IndexOf(' ');
                        if (rows[i].SelectSingleNode("div[3]/a[@title=\"number of citations\"]").InnerText.Substring(9).Remove(comI) != null)
                            tempPubliObj.numCit = Convert.ToInt32((rows[i].SelectSingleNode("div[3]/a[@title=\"number of citations\"]").InnerText.Substring(9).Remove(comI)));
                    }
                    else
                        tempPubliObj.numCit = 0;
                    Console.WriteLine("No. of citations: " + tempPubliObj.numCit);

                    tempPubliObj.title = rows[i].SelectSingleNode("h3/a").InnerText.Trim();
                    tempPubliObj.authNames = rows[i].SelectSingleNode("div[1]/span[1]").InnerText.Substring(3).Trim();

                    String tempYear;
                    if (rows[i].SelectSingleNode("div[1]/span[@class=\"pubyear\"]") != null)
                    {
                        tempYear = rows[i].SelectSingleNode("div[1]/span[@class=\"pubyear\"]").InnerText;
                        Console.WriteLine(tempYear);
                        if (tempYear != null)
                            tempPubliObj.year = Convert.ToInt32(tempYear.Substring(2));
                    }
                    else tempPubliObj.year = 0;

                    if (rows[i].SelectSingleNode("div[2]") != null)
                        tempPubliObj.abs = rows[i].SelectSingleNode("div[2]").InnerText;
                    else
                        tempPubliObj.abs = "";

                    tempPubliObj.url = "http://citeseer.ist.psu.edu" + rows[i].SelectSingleNode("h3/a").GetAttributeValue("href", "");

                    Console.WriteLine(tempPubliObj.title + "|" + tempPubliObj.authNames + "|" + tempPubliObj.year + "|" + tempPubliObj.url);
                    if (tempPubliObj.numCit > 0)
                        citeList.Add(tempPubliObj);
                }
            //next ends, update doc - load new page before looping again
        }

    }

    public class CSXAuth
    {
        public String authName, homePageURL;
        public String affiliation;//e.g. university or organisation
        public int numPub;//no. of publications
        public int hIndex, i10Index;
        public List<publiListEle> publiList;//array containing info about all the publications

        HtmlWeb web;
        HtmlDocument doc;

        public CSXAuth(String authURL)
        {
            web = new HtmlWeb();
            doc = web.Load(authURL);

            if (doc != null)
                Console.WriteLine("Document Loaded!");
            else
                Console.WriteLine("Load Error!");

            extractData();
        }

        String[] Split(String s)//Gets a string containing publication information and divides it into title, journal and year strings
        {
            String[] list = new String[3];
            int st = 0, i, j;

            for (i = 0, j = 0; i < s.Length; i++)
            {
                if (s[i] == '\n')
                {
                    while (Char.IsWhiteSpace(s[i])) { i++; if (i >= s.Length)break; }
                    list[j++] = s.Substring(st, i - st).Trim();
                    st = i + 1;
                }
            }

            return list;
        }

        void extractData()//Extracts all the data from author page and stores them in the respective variables
        {
            HtmlNode name = doc.DocumentNode.SelectSingleNode("//*[@id=\"viewHeader\"]/h2");
            Console.Write("\nName: ");
            String namet = name.InnerText;
            authName = namet.Remove(namet.Length - 5);
            Console.WriteLine(authName);

            HtmlNode hpurl = doc.DocumentNode.SelectSingleNode("//*[@id=\"authInfo\"]/tr[1]/td[2]/a");
            Console.Write("Home Page URL: ");
            homePageURL = hpurl.InnerText;
            Console.WriteLine(homePageURL);
            if (homePageURL.Contains("Not found"))
                homePageURL = "";

            HtmlNode affl = doc.DocumentNode.SelectSingleNode("//*[@id=\"authInfo\"]/tr[2]/td[2]");
            Console.Write("Affiliation: ");
            affiliation = affl.InnerText;
            Console.WriteLine(affiliation);

            /*HtmlNode npub = doc.DocumentNode.SelectSingleNode("//*[@id=\"authInfo\"]/tr[3]/td[2]");
            Console.Write("No. of publications: ");
            numPub = Convert.ToInt32(npub.InnerText);
            Console.WriteLine(numPub);*/
            //Parsed number disregarded because of occasional inconsistencies in the website, list length used instead

            HtmlNode hindex = doc.DocumentNode.SelectSingleNode("//*[@id=\"authInfo\"]/tr[4]/td[2]");
            Console.Write("H-index: ");
            hIndex = Convert.ToInt32(hindex.InnerText);
            Console.WriteLine(hIndex);

            HtmlNodeCollection rows = doc.DocumentNode.SelectNodes("//*[@id=\"viewContent-inner\"]/table/tr");
            String[] list;
            int i10 = 0;

            publiList = new List<publiListEle>();
            publiListEle tempPubliObj = new publiListEle();

            for (int i = 1; i < rows.Count; i++)
            {
                tempPubliObj = new publiListEle();
                Console.WriteLine("*** *** ***");
                Console.WriteLine(rows[i].XPath);

                if (rows[i].SelectSingleNode("td[1]").InnerText.ToString().Trim().Length > 0)
                    tempPubliObj.numCit = Convert.ToInt32(rows[i].SelectSingleNode("td[1]").InnerText);
                else
                    tempPubliObj.numCit = 0;
                Console.WriteLine("No. of citations: " + tempPubliObj.numCit);

                if (rows[i].SelectSingleNode("td[1]").InnerText.ToString().Trim().Length > 0)
                    if (Convert.ToInt32(rows[i].SelectSingleNode("td[1]").InnerText) >= 10) i10++;

                list = Split(rows[i].SelectSingleNode("td[2]").InnerText);
                tempPubliObj.title = list[0];
                tempPubliObj.journal = list[1];
                tempPubliObj.year = Convert.ToInt32(list[2]);
                tempPubliObj.url = "http://citeseer.ist.psu.edu" + rows[i].SelectSingleNode("td[2]/a").GetAttributeValue("href", "");
                Console.WriteLine(tempPubliObj.title + "|" + tempPubliObj.journal + "|" + tempPubliObj.year + "|" + tempPubliObj.url);
                if(tempPubliObj.numCit>0)
                    publiList.Add(tempPubliObj);
            }

            numPub = publiList.Count;
            Console.WriteLine(numPub + "|" + rows.Count);

            i10Index = i10;
            Console.WriteLine(i10Index);
        }


        /*public static void Main(string[] args)
        {
            String authURL = "http://citeseer.ist.psu.edu/viewauth/summary?aid=7229&list=full";
            CSXAuth p = new CSXAuth(authURL);
            //Console.ReadLine();

            CSXAuthSug q = new CSXAuthSug("James Anderson");
            //Console.ReadLine();
        }*/

    }

}
