using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using RealScraper.Core;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class WebDriverService : IDisposable
{
    #region Fields

    private IWebDriver _driver;
    private WebDriverWait _wait;
    private bool _disposed = false;

    #endregion

    #region Constructor

    public WebDriverService(string browserType = "Chrome")
    {
        _driver = CreateDriver(browserType);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
    }

    private IWebDriver CreateDriver(string browserType)
    {
        switch (browserType.ToLower())
        {
            case "firefox":
                return new FirefoxDriver();
            case "edge":
                return new EdgeDriver();
            default:
                return new ChromeDriver();
        }
    }

    #endregion

    #region Methods

    public void NavigateToUrl(string url)
    {
        try
        {
            _driver.Navigate().GoToUrl(url);
        }
        catch (WebDriverException)
        {
            Console.WriteLine("Navigation failed.");
        }
    }

    public async Task<List<string>> ScrapePageData()
    {
        var data = new List<string>();

        try
        {
            // Ensure page is fully loaded before scraping
            await WaitForPageToLoad();

            // Find elements asynchronously
            var elements = await Task.Run(() => _driver.FindElements(By.XPath("//table//tr")));

            // Extract data asynchronously using parallel processing
            var extractedItems = await Task.Run(() => elements.AsParallel().Select(element => element.Text.Trim())
                .Where(text => !string.IsNullOrEmpty(text) &&
                               text.StartsWith(AppConstants.KEYWORD_USER_NAME, StringComparison.OrdinalIgnoreCase))
                .ToList());

            data.AddRange(extractedItems);
        }
        catch (NoSuchElementException)
        {
            Console.WriteLine("No matching elements found.");
        }
        catch (WebDriverException)
        {
            Console.WriteLine("WebDriver error occurred.");
        }
        catch (Exception)
        {
            Console.WriteLine("Unexpected error occurred.");
        }

        return data;
    }

    /// <summary>
    /// Retry Mechanism for Clicking Next Page – Handles transient failures.
    /// </summary>
    public bool TryClickNextPage(int maxRetries = 3)
    {
        int attempts = 0;
        while (attempts < maxRetries)
        {
            try
            {
                var nextPageButton = _wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(AppConstants.KEYWORD_NEXT_PAGE_BUTTON)));
                nextPageButton?.Click();
                return true;
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"Attempt {attempts + 1}: Next page button not clickable.");
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine($"Attempt {attempts + 1}: Next page button not found.");
            }

            attempts++;
        }

        return false;
    }

    private async Task WaitForPageToLoad()
    {
        await Task.Delay(300); // Can be replaced with a more robust condition if needed
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                SafeDispose(_driver);
            }
            _disposed = true;
        }
    }

    private void SafeDispose(IDisposable disposable)
    {
        try
        {
            disposable?.Dispose();
        }
        catch (Exception)
        {
            Console.WriteLine("Error while disposing.");
        }
    }

    ~WebDriverService()
    {
        Dispose(false);
    }

    #endregion
}