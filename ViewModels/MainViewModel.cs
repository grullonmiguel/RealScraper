using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using RealScraper.Core;
using RealScraper.Core.Base;
using RealScraper.Core.Services;
using RealScraper.Models;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RealScraper.ViewModels
{
    public class MainViewModel : Observable
    {
        #region Fields

        private WebDriverWait _webDriverWait;
        private List<string> RawList = new List<string>();

        #endregion

        #region Properties

        public ObservableCollection<AuctionItem> SelectedRows { get; set; } = new ObservableCollection<AuctionItem>();

        public ObservableCollection<AuctionItem> AuctionItems
        {
            get => _auctionItems;
            set => Set(ref _auctionItems, value);
        }
        private ObservableCollection<AuctionItem> _auctionItems = new ObservableCollection<AuctionItem>();

        public bool ShowStartPage
        {
            get => _showStartPage;
            set => Set(ref _showStartPage, value);
        }
        private bool _showStartPage;
        
        public bool ShowAuctionSettings
        {
            get => _showAuctionSettings;
            set => Set(ref _showAuctionSettings, value);
        }
        private bool _showAuctionSettings;
        
        public bool ShowParcelDetailSettings
        {
            get => _showParcelDetailSettings;
            set => Set(ref _showParcelDetailSettings, value);
        }
        private bool _showParcelDetailSettings;

        public string AuctionURL
        {
            get => _auctionURL;
            set => Set(ref _auctionURL, value);
        }
        private string _auctionURL;

        public string ParcelDetailsURL
        {
            get => _parcelDetailsURL;
            set => Set(ref _parcelDetailsURL, value);
        }
        private string _parcelDetailsURL;

        public bool CanScrape
            => !string.IsNullOrWhiteSpace(AuctionURL) && ProcessService.IsValidUrl(AuctionURL);

        #endregion

        #region Commands

        public ICommand CopyCommand => new RelayCommand(CopySelectedRows, CanCopyRows);
        public ICommand StartCommand => new RelayCommand(async () => await GetStartedAsync());
        public ICommand AuctionURLEditCommand => new RelayCommand(()=> ShowAuctionEditDialog());
        public ICommand ParcelDetailEditCommand => new RelayCommand(()=> ShowParcelEditDialog());
        public ICommand CloseSettingsCommand => new RelayCommand(async () => await CloseSettingsDialog());
        public ICommand UpdateAuctionURLCommand => new RelayCommand(async () => await SaveSettings(AuctionURL, AppConstants.SETTINGS_AUCTION_URL, AppConstants.ENTER_URL));
        public ICommand UpdateParcelDetailsURLCommand => new RelayCommand(async () => await SaveSettings(ParcelDetailsURL, AppConstants.SETTINGS_PARCEL_DETAILS_URL, AppConstants.ENTER_URL));
        public ICommand ScrapeCommand => new RelayCommand(async () => await ScrapeData());
        public ICommand NavigateCommand => new RelayCommand (()=> NavigateURL(AuctionURL));
        public ICommand NavigateDetailsCommand => new RelayCommand<AuctionItem>(item => NavigateURL(item?.URL), CanOpenPropertyLink);
        public ICommand NavigateRegridCommand => new RelayCommand<AuctionItem>(item => NavigateURL(item?.Regrid), CanNavigateRegrid);

        #endregion

        #region Constructor

        public MainViewModel()
        {
            ShowStartPage = true;
        }

        #endregion

        #region Methods
         
        private async Task GetStartedAsync()
        {
            ShowStartPage = false;
            await LoadSettings();
        }

        private async Task _ScrapeData()
        {
            bool isLastRecord = false;

            // Clear lists before scraping
            RawList.Clear();
            AuctionItems.Clear();

            var driver = new ChromeDriver();
            try
            {
                driver.Navigate().GoToUrl(AuctionURL);

                while (!isLastRecord)
                {
                    try
                    {
                        await WaitForPageToLoad(driver);

                        // Find all table rows dynamically
                        var elements = driver.FindElements(By.XPath("//table//tr"));

                        foreach (var element in elements)
                        {
                            var itemText = element.Text.Trim();

                            // Stop pagination if duplicate found
                            if (RawList.Contains(itemText))
                            {
                                isLastRecord = true;
                                Console.WriteLine("Item already exists! Stopping pagination.");
                                break;
                            }

                            // Add valid items to RawList
                            if (!string.IsNullOrEmpty(itemText) &&
                                itemText.StartsWith(AppConstants.KEYWORD_USER_NAME, StringComparison.OrdinalIgnoreCase))
                            {
                                RawList.Add(itemText);
                            }
                        }

                        // Attempt to click the Next Page button
                        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                        var nextPageButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(AppConstants.KEYWORD_NEXT_PAGE_BUTTON)));

                        if (nextPageButton != null)
                        {
                            nextPageButton.Click();
                        }
                        else
                        {
                            Console.WriteLine("Next page button not found. Stopping pagination.");
                            isLastRecord = true;
                        }
                    }
                    catch (NoSuchElementException ex)
                    {
                        Console.WriteLine($"Element missing: {ex.Message}. Ending pagination.");
                        isLastRecord = true;
                    }
                    catch (StaleElementReferenceException ex)
                    {
                        Console.WriteLine($"Stale element encountered: {ex.Message}. Retrying...");
                        await Task.Delay(1000); // Allow time before retrying
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error: {ex.Message}. Stopping pagination.");
                        isLastRecord = true;
                    }

                    // Wait briefly between pages to stabilize interactions
                    await Task.Delay(500);
                }

                // Consolidate all rows into a single list with trimmed values
                ProcessAuctionData();
            }
            finally
            {
                // Ensure WebDriver closes, even in case of an exception
                driver.Quit();
            }
        }

        private async Task ScrapeData()
        {
            bool isLastRecord = false;

            // Clear lists before scraping
            RawList.Clear();
            AuctionItems.Clear();

            using (var webDriverService = new WebDriverService())
            {
                try
                {
                    webDriverService.NavigateToUrl(AuctionURL);

                    while (!isLastRecord)
                    {
                        var newData = await webDriverService.ScrapePageData();

                        foreach (var item in newData)
                        {
                            // Stop pagination if duplicate found
                            if (RawList.Contains(item))
                            {
                                Console.WriteLine("Item already exists! Stopping pagination.");
                                isLastRecord = true;
                                break;
                            }

                            RawList.Add(item);
                        }

                        // Attempt to click the Next Page button
                        if (!webDriverService.TryClickNextPage())
                        {
                            Console.WriteLine("Next page button not found. Stopping pagination.");
                            isLastRecord = true;
                        }

                        // Wait briefly between pages to stabilize interactions
                        await Task.Delay(500);
                    }

                    // Consolidate all rows into a structured list
                    ProcessAuctionData();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}. Stopping pagination.");
                }
            } // WebDriverService gets disposed automatically
        }

        //private async Task ScrapeData2()
        //{
        //    bool isLastRecord = false;
        //    var rawListConcurrent = new ConcurrentBag<string>(); // Thread-safe collection
        //    AuctionItems.Clear();

        //    using (var driverHelper = new WebDriverService(AuctionURL))
        //    {
        //        while (!isLastRecord)
        //        {
        //            try
        //            {
        //                var elements = await driverHelper.GetTableRowsAsync("//table//tr");

        //                Parallel.ForEach(elements, element =>
        //                {
        //                    var itemText = element.Text.Trim();

        //                    if (!string.IsNullOrEmpty(itemText) &&
        //                        itemText.StartsWith(AppConstants.KEYWORD_USER_NAME, StringComparison.OrdinalIgnoreCase) &&
        //                        !rawListConcurrent.Contains(itemText))
        //                    {
        //                        rawListConcurrent.Add(itemText);
        //                    }
        //                });

        //                isLastRecord = driverHelper.IsLastPage("No more records") ||
        //                               !await driverHelper.ClickNextPageButtonWithRetryAsync(AppConstants.KEYWORD_NEXT_PAGE_BUTTON);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Unexpected error: {ex.Message}. Stopping pagination.");
        //                isLastRecord = true;
        //            }
        //        }

        //        // Convert ConcurrentBag to List safely before further processing
        //        RawList = rawListConcurrent.ToList();
        //        ProcessAuctionData();
        //    }
        //}

        private async Task WaitForPageToLoad(ChromeDriver driver)
        {
            _webDriverWait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
            await Task.Delay(2000);
        }

        private void ProcessAuctionData()
        {
            // Extract raw auction data
            var waitingList = ExtractAuctionData("Auction Starts", "Auction Status");

            // Assign properties to auction items based on extracted data
            GetAuctionProperties(waitingList, "Auction Starts");

            // Process matching parcels for each auction item
            Parallel.ForEach(AuctionItems, item =>
            {
                item.MatchingParcels = GetMatchingParcels(item.ParcelID, AuctionItems.ToList());
            });
        }

        private string GetMatchingParcels(string inputNumber, List<AuctionItem> numbers)
        {
            try
            {
                string baseNumber = inputNumber.Substring(0, inputNumber.Length - 2);

                var matchingNumbers = numbers
               .Where(n => n.ParcelID.StartsWith(baseNumber) && n.ParcelID != inputNumber) // Exclude inputNumber
               .Select(n => n.ParcelID)
               .OrderBy(n => n) // Sort in ascending order
               .ToList();

                return string.Join(", ", matchingNumbers);
            }
            catch (Exception)
            {
                return string.Empty;
            }

        }

        #endregion

        #region Settings Methods

        private async Task CloseSettingsDialog()
        {
            ShowAuctionSettings = false;
            ShowParcelDetailSettings = false;
            await LoadSettings();
        }

        private void ShowAuctionEditDialog()
        {
            ShowParcelDetailSettings = false;
            ShowAuctionSettings = true;
        }

        private void ShowParcelEditDialog()
        {
            ShowAuctionSettings = false;
            ShowParcelDetailSettings = true;
        }

        private async Task LoadSettings()
        {
            await Task.CompletedTask;
            try
            {
                AuctionURL = SettingsService.LoadSetting<string>(AppConstants.SETTINGS_AUCTION_URL);
                ParcelDetailsURL = SettingsService.LoadSetting<string>(AppConstants.SETTINGS_PARCEL_DETAILS_URL);
                OnPropertyChanged(nameof(CanScrape));
            }
            catch (Exception)
            {
                // Provide recovery action
            }
        }

        private async Task SaveSettings(string url, string settingKey, string fallbackValue)
        {
            try
            {
                // Allow blanks but do not allow invalid URLs
                if (!string.IsNullOrEmpty(url) && !ProcessService.IsValidUrl(url))
                    return;

                var validUrl = ProcessService.IsValidUrl(url) ? url : fallbackValue;
                SettingsService.SaveSetting(settingKey, url);

                await CloseSettingsDialog();
            }
            catch (Exception)
            {
                // Provide recovery action
            }
        }

        #endregion

        #region Helpers

        private List<string> ExtractAuctionData(string keep, string remove)
        {
            bool keepData = false;
            var filteredList = new List<string>();

            foreach (var data in RawList)
            {
                foreach (var line in data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Contains(keep))
                        keepData = true;

                    else if (line.Contains(remove))
                        keepData = false;

                    else if (line.ToLower().Contains("user name"))
                        keepData = false;

                    if (keepData)
                        filteredList.Add(line.Trim());
                }
            }

            return filteredList;
        }

        private void GetAuctionProperties(List<string> list, string keyword)
        {
            AuctionItem currentItem = null;

            try
            {
                foreach (var line in list)
                {
                    if (line == keyword) // Detect new auction start
                    {
                        if (currentItem != null)
                        {
                            if (!string.IsNullOrEmpty(ParcelDetailsURL) && ProcessService.IsValidUrl(ParcelDetailsURL))
                                currentItem.URL = $"{ParcelDetailsURL}{currentItem.ParcelID}";

                            AuctionItems.Add(currentItem); // Save previous item
                        }

                        currentItem = new AuctionItem(); // Initialize a new object
                        continue;
                    }

                    if (currentItem != null)
                    {
                        switch (line)
                        {
                            case string s when s.StartsWith("Auction Type:"):
                                currentItem.AuctionType = s.Replace("Auction Type: ", "").Trim();
                                break;

                            case string s when s.StartsWith("Case #:"):
                                currentItem.CaseNumber = s.Replace("Case #: ", "").Trim();
                                break;

                            case string s when s.StartsWith("Certificate #:"):
                                currentItem.CertificateNumber = s.Replace("Certificate #: ", "").Trim();
                                break;

                            case string s when s.StartsWith("Opening Bid:"):
                                currentItem.OpeningBid = decimal.TryParse(s.Replace("Opening Bid: $", "").Trim(), out var bid) ? bid : 0;
                                break;

                            case string s when s.StartsWith("Parcel ID:"):
                                currentItem.ParcelID = s.Replace("Parcel ID: ", "").Trim();
                                break;

                            case string s when s.StartsWith("Property Address:"):
                                currentItem.PropertyAddress = s.Replace("Property Address: ", "").Trim();
                                break;

                            case string s when s.StartsWith("Assessed Value:"):
                                currentItem.AssessedValue = decimal.TryParse(s.Replace("Assessed Value: $", "").Trim(), out var value) ? value : 0;
                                break;
                        }
                    }
                }

                // Add last auction item
                if (currentItem != null)
                {
                    if (!string.IsNullOrEmpty(ParcelDetailsURL) && ProcessService.IsValidUrl(ParcelDetailsURL))
                        currentItem.URL = $"{ParcelDetailsURL}{currentItem.ParcelID}";
                    AuctionItems.Add(currentItem);
                }
            }
            catch (Exception)
            {
                // Add logging
            }
        }

        private bool CanOpenPropertyLink(AuctionItem item) 
            => item != null && !string.IsNullOrWhiteSpace(item.URL);

        private bool CanNavigateRegrid(AuctionItem item)
            => item != null && ProcessService.IsValidUrl(item.Regrid);

        private void ViewProperty(AuctionItem item)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.URL))
                ProcessService.OpenUrl(item.URL);
        }

        private void NavigateURL(string url)
        {
            ProcessService.OpenUrl(url);
        }

        /// <summary>
        /// Logic to copy selected items or specific cells
        /// </summary>
        private void CopySelectedRows()
        {
            if (SelectedRows.Count > 0)
            {
                var clipboardText = new StringBuilder();

                // Add the headers
                clipboardText.AppendLine($"Opening Bid\t" +
                                         $"Assessed Value\t" +
                                         $"Property Address\t" +
                                         $"Parcel ID\t" +
                                         $"URL\t" +
                                         $"Regrid\t" +
                                         $"Nearby Parcels");

                foreach (var item in SelectedRows)
                {
                    // Structure the hyperlinl with an 
                    // alias for Google Sheets or Excel
                    string url = ProcessService.IsValidUrl(item.URL) ?
                        $"=HYPERLINK(\"{item.URL}\", \"Details\")" : "";

                    string regrid = !string.IsNullOrWhiteSpace(item.ParcelID) ?
                        $"=HYPERLINK(\"https://app.regrid.com/search?query={item.ParcelID}&context=%2Fus&map_id=\", \"Search\")" : "";

                    clipboardText.AppendLine($"{item.OpeningBid}\t" +
                                             $"{item.AssessedValue}\t" +
                                             $"{item.PropertyAddress}\t" +
                                             $"{item.ParcelID}\t" +
                                             $"{url}\t" +
                                             $"{regrid}\t" +
                                             $"{item.MatchingParcels}");
                }

                Clipboard.SetText(clipboardText.ToString());
            }
        }

        private bool CanCopyRows() 
            => AuctionItems != null && AuctionItems.Any();

        #endregion
    }
}