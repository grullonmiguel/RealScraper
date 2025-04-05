using System;

namespace RealScraper.Models
{
    public class AuctionItem
    {
        private string parcelID;
        public string AuctionType { get; set; }
        public string CaseNumber { get; set; }
        public string CertificateNumber { get; set; }
        public decimal OpeningBid { get; set; }
        public string ParcelID
        {
            get => parcelID;
            set
            {
                parcelID = value;
                PopulateRegrid();
            }
        }

        private void PopulateRegrid()
        {
            if (!string.IsNullOrEmpty(ParcelID))
                Regrid = $"https://app.regrid.com/search?query={ParcelID}&context=%2Fus&map_id=";
        }

        public string PropertyAddress { get; set; }
        public decimal AssessedValue { get; set; }
        public string URL { get; set; }
        public string Regrid { get; set; }

        public string MatchingParcels { get; set; }
    }
}