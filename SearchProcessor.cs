using Pro.Rest.Controllers;
using Pro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace Pro
{
    public class SearchProcessor
    {
        private bool RetryOnCanceled;
        public int timeout = 30000;
        public string currentSearchRqestMessage;
        private readonly RestSearchController searchController;
        private Dictionary<string, CancellationTokenSource> cancellationTokensSource;
        private Dictionary<string, CancellationTokenSource> currentReqestCancelationTokensSource;
        public delegate void OnProgressChanged(int progress, string connectionId);
        public event OnProgressChanged ProgressChagned;
        public delegate void OnSearchReqestChanged(string searchReqestMessage, string connectionId);
        public event OnSearchReqestChanged SearchReqestChanged;
        public delegate void OnCurrentSearchReqestCompleted(SearchResult result, string searchId, bool updateExistingResults, string connectionId);
        public event OnCurrentSearchReqestCompleted CurrentSearchReqestCompleted;
        private Guid controllerId = Guid.NewGuid();
        private ListParser listParser;

        public SearchProcessor(RestSearchController searchController)
        {
            listParser = new ListParser();
            this.searchController = searchController;
            cancellationTokensSource = new Dictionary<string, CancellationTokenSource>();
            currentReqestCancelationTokensSource = new Dictionary<string, CancellationTokenSource>();
        }

        public List<SearchResult> Search(List<IFormFile> files, string searchId, bool updateExistingResults, string connectionId)
        {
            SetCanceleationTokenSource(connectionId, cancellationTokensSource);

            var searchRequests = Controllers.SearchController.GetSearchRequests(files)
                .Where(readResult => readResult.Success)
                .SelectMany(readResult => readResult.Requests)
                .ToList();

            return DoSearch(searchRequests, searchId, updateExistingResults, connectionId);
        }

        public List<SearchResult> Search(List<SearchRequest> searchReqests, string searchId, bool updateExistingResults, string connectionId)
        {
            SetCanceleationTokenSource(connectionId, cancellationTokensSource);

            return DoSearch(searchReqests, searchId, updateExistingResults, connectionId);
        }

        private List<SearchResult> DoSearch(List<SearchRequest> searchRequests, string searchId, bool updateExistingResults, string connectionId)
        {
            var resutls = new List<SearchResult>();
            CancellationTokenSource cancellationToken;
            cancellationTokensSource.TryGetValue(connectionId, out cancellationToken);
            SetCanceleationTokenSource(connectionId, currentReqestCancelationTokensSource);
            CancellationTokenSource currentRequestToken;
            currentReqestCancelationTokensSource.TryGetValue(connectionId, out currentRequestToken);

            var searchTask = Task.Run(() =>
            {
                ProgressChagned(0, connectionId);
                var counter = 1;
                int reqestIndex;

                try
                {
                    for (reqestIndex = 0; reqestIndex < searchRequests.Count; reqestIndex++)
                    {
                        currentRequestToken?.Cancel();
                        currentRequestToken?.Dispose();
                        currentRequestToken = new CancellationTokenSource();
                        currentReqestCancelationTokensSource[connectionId] = currentRequestToken;

                        var result = new SearchResult(searchRequests[reqestIndex])
                        {
                            Brand = searchRequests[reqestIndex].Brand,
                            Id = searchRequests[reqestIndex].Id,
                            IsSuccessful = false,
                            IsSkiped = false,
                            IsCanceled = false
                        };
                        SearchReqestChanged(currentSearchRqestMessage = $" Part#-{result.Id} Brand-{result.Brand}", connectionId);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            result.IsSkiped = true;
                            currentRequestToken.Cancel();
                            CurrentSearchReqestCompleted(result, searchId, updateExistingResults, connectionId);
                            resutls.Add(result);
                            ProgressChagned(counter * 100 / searchRequests.Count, connectionId);
                            counter++;
                            continue;
                        }
                        resutls.Add(result);
                        try
                        {
                            if (currentRequestToken.IsCancellationRequested)
                            {
                                result.IsSkiped = true;
                                result.IsCanceled = true;
                                counter++;
                                CurrentSearchReqestCompleted(result, searchId, updateExistingResults, connectionId);
                                continue;
                            }
                            var searchResult = searchController.Search(searchRequests[reqestIndex].Id, timeout, currentRequestToken.Token);
                            var matchinSuggestion = searchResult.Suggestions.FirstOrDefault(suggestion =>
                            suggestion.FoundPart.Part.Brand.Name.Equals(searchRequests[reqestIndex].Brand, StringComparison.InvariantCultureIgnoreCase)
                            && (suggestion.FoundPart.Part.FullNr.Equals(searchRequests[reqestIndex].Id, StringComparison.InvariantCultureIgnoreCase)
                            || suggestion.FoundPart.Part.ShortNr.Equals(searchRequests[reqestIndex].Id, StringComparison.InvariantCultureIgnoreCase)));
                            if (matchinSuggestion != null)
                            {
                                var tableRows = listParser.ParseList(matchinSuggestion.Uri);
                                if (tableRows != null)
                                {
                                    result.IsSuccessful = true;

                                    var orderedRows = tableRows.Where(row => row.Brand.Equals(searchRequests[reqestIndex].Brand)
                                                                            && row.Id.Equals(searchRequests[reqestIndex].Id)
                                                                            && Regex.Match(row.DeliveryText, "(В наличии)|(Сегодня)").Success
                                                                            && double.TryParse(row.Price, out _))
                                                               .OrderBy(row => double.Parse(row.Price));
                                    if (orderedRows.Count() > 1)
                                    {
                                        result.SecondDeliveryText = orderedRows.ElementAt(1).DeliveryText;
                                        result.SecondPrice = orderedRows.ElementAt(1).Price;
                                    }
                                    if (orderedRows.Count() > 0)
                                    {
                                        result.FirstDeliveryText = orderedRows.ElementAt(0).DeliveryText;
                                        result.FirstPrice = orderedRows.ElementAt(0).Price;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            result.IsSuccessful = false;
                        }

                        ProgressChagned(counter * 100 / searchRequests.Count, connectionId);
                        CurrentSearchReqestCompleted(result, searchId, updateExistingResults, connectionId);
                        counter++;

                        if (currentRequestToken.IsCancellationRequested && RetryOnCanceled)
                        {
                            reqestIndex--;
                            RetryOnCanceled = false;
                        }
                    }
                }
                finally
                {
                    currentRequestToken?.Dispose();
                    currentRequestToken = null;
                    currentReqestCancelationTokensSource[connectionId] = null;
                }
            });
            searchTask.Wait();
            return resutls;
        }

        private void SetCanceleationTokenSource(string connectionId, Dictionary<string, CancellationTokenSource> cancelationTokens)
        {
            CancellationTokenSource cancellationTokenSource;
            if (cancelationTokens.TryGetValue(connectionId, out cancellationTokenSource))
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                cancelationTokens[connectionId] = cancellationTokenSource;
            }
            else
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                cancelationTokens.Add(connectionId, cancellationTokenSource);
            }
        }

        public void SkipCurrentReqest(string connectionId)
        {
            CancellationTokenSource currentReqestCancelationTokenSource;
            currentReqestCancelationTokensSource.TryGetValue(connectionId, out currentReqestCancelationTokenSource);
            currentReqestCancelationTokenSource?.Cancel();
        }

        public void CanceleSearch(string connectionId)
        {
            CancellationTokenSource cancellationTokenSource;
            cancellationTokensSource.TryGetValue(connectionId, out cancellationTokenSource);
            cancellationTokenSource?.Cancel();

            CancellationTokenSource currentReqestCancelationTokenSource;
            currentReqestCancelationTokensSource.TryGetValue(connectionId, out currentReqestCancelationTokenSource);
            currentReqestCancelationTokenSource?.Cancel();
        }
        public void Retry(string connectionId)
        {
            RetryOnCanceled = true;
            CancellationTokenSource currentReqestCancelationTokenSource;
            currentReqestCancelationTokensSource.TryGetValue(connectionId, out currentReqestCancelationTokenSource);
            currentReqestCancelationTokenSource?.Cancel();
        }
    }
}
