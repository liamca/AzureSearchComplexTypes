using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSearchComplexTypesDemo
{
    class Program
    {
        static string searchServiceName = "";     // Learn more here: https://azure.microsoft.com/en-us/documentation/articles/search-what-is-azure-search/
        static string searchServiceAPIKey = "";
        static string indexName = "contacts";
        static SearchServiceClient serviceClient;
        static SearchIndexClient indexClient;

        static void Main(string[] args)
        {
            // This will create an Azure Search index, load a complex JSON data file 
            // and perform some search queries

            if ((searchServiceName == "") || (searchServiceAPIKey == ""))
            {
                Console.WriteLine("Please add your searchServiceName and searchServiceAPIKey.  Press any key to continue.");
                Console.ReadLine();
                return;
            }

            serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServiceAPIKey));
            indexClient = serviceClient.Indexes.GetClient(indexName);


            Console.WriteLine("Creating index...");
            ReCreateIndex();
            Console.WriteLine("Uploading documents...");
            UploadDocuments();
            Console.WriteLine("Waiting 5 seconds for content to be indexed...");
            Thread.Sleep(5000);

            Console.WriteLine("\nFinding all people who work at the ‘Adventureworks Headquarters’...");
            ContactSearch results = SearchDocuments(searchText: "*", filter: "locationsDescription / any(t: t eq 'Adventureworks Headquarters')");
            Console.WriteLine("Found matches:");
            foreach (var contact in results.Results)
            {
                Console.WriteLine("- {0}", contact.Document["name"]);
            }

            Console.WriteLine("\nGetting a count of the number of people who work in a ‘Home Office’...");
            results = SearchDocuments(searchText: "*", filter: "locationsDescription / any(t: t eq 'Home Office')");
            Console.WriteLine("{0} people have Home Offices", results.Count);

            Console.WriteLine("\nOf the people who at a ‘Home Office’ show what other offices they work in with a count of the people in each location...");
            var locationsDescription = results.Facets.Where(item => item.Key == "locationsDescription");
            Console.WriteLine("Found matches:");
            foreach (var facets in locationsDescription)
            {
                foreach (var facet in facets.Value)
                {
                    Console.WriteLine("- Location: {0} ({1})", facet.Value, facet.Count);
                }
            }
            
            Console.WriteLine("\nGetting a count of people who work at a ‘Home Office’ with location Id of ‘4’...");
            results = SearchDocuments(searchText: "*", filter: "locationsCombined / any(t: t eq '4||Home Office')");
            Console.WriteLine("{0} people have Home Offices with Location Id '4'", results.Count);

            Console.WriteLine("\nGetting people that have Home Offices with Location Id '4':");
            Console.WriteLine("Found matches:");
            foreach (var contact in results.Results)
            {
                Console.WriteLine("- {0}", contact.Document["name"]);
            }

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        public static void ReCreateIndex()
        {
            // Delete and re-create the index
            if (serviceClient.Indexes.Exists(indexName))
                serviceClient.Indexes.Delete(indexName);

            var definition = new Index()
            {
                Name = indexName,
                Fields = new[]
                {
                    new Field("id", DataType.String)        { IsKey = true },
                    new Field("name", DataType.String)      { IsSearchable = true, IsFilterable = false, IsSortable = false, IsFacetable = false },
                    new Field("company", DataType.String)   { IsSearchable = true, IsFilterable = false, IsSortable = false, IsFacetable = false },
                    new Field("locationsId", DataType.Collection(DataType.String))
                        { IsSearchable = true, IsFilterable = true,  IsFacetable = true },
                    new Field("locationsDescription", DataType.Collection(DataType.String))
                        { IsSearchable = true, IsFilterable = true,  IsFacetable = true },
                    new Field("locationsCombined", DataType.Collection(DataType.String))
                        { IsSearchable = true, IsFilterable = true,  IsFacetable = true }
                }
            };

            serviceClient.Indexes.Create(definition);
        }

        public static void UploadDocuments()
        {
            // This will open the JSON file, parse it and upload the documents in a batch
            List<IndexAction> indexOperations = new List<IndexAction>();
            JArray json = JArray.Parse(File.ReadAllText(@"contacts.json"));
            foreach (var contact in json)
            {
                //Parse the JSON object (contact)
                var doc = new Document();
                doc.Add("id", contact["id"]);
                doc.Add("name", contact["name"]);
                doc.Add("company", contact["company"]);
                doc.Add("locationsId", contact["locations"].Select(item => item["id"]).ToList());
                doc.Add("locationsDescription", contact["locations"].Select(item => item["description"]).ToList());
                doc.Add("locationsCombined", contact["locations"].Select(item => item["id"] + "||" + item["description"]).ToList());

                indexOperations.Add(IndexAction.Upload(doc));
            }

            try
            {
                indexClient.Documents.Index(new IndexBatch(indexOperations));
            }
            catch (IndexBatchException e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine(
                "Failed to index some of the documents: {0}",
                       String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
            }

        }


        public static ContactSearch SearchDocuments(string searchText, string filter = null)
        {
            // Search using the supplied searchText and output documents that match 
            try
            {
                var sp = new SearchParameters();
                sp.IncludeTotalResultCount = true;
                if (!String.IsNullOrEmpty(filter))
                    sp.Filter = filter;
                sp.Facets = new List<String>() { "locationsId", "locationsDescription", "locationsCombined" };
                
                var response =  indexClient.Documents.Search(searchText, sp);
                return new ContactSearch() { Results = response.Results, Facets = response.Facets, Count = Convert.ToInt32(response.Count) };

            }
            catch (Exception e)
            {
                Console.WriteLine("Failed search: {0}", e.Message.ToString());
                return null;
            }
        }

    }

    public class ContactSearch
    {
        public FacetResults Facets { get; set; }
        public IList<SearchResult> Results { get; set; }
        public int? Count { get; set; }

    }
}
