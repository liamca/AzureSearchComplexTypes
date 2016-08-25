---
services: search
platforms: dotnet
author: liamca
---

# Azure Search Complex Types
Demonstration on how to integrate complex data types with parent child relationships into Azure Search. 

This is a Visual Studio application that loads a JSON object into an Azure Search index and then performs various search queries.  This is the JSON document that we will be working with which contains a set of contacts that include their various work locations:

![JSON Screen Shot](https://raw.githubusercontent.com/liamca/AzureSearchComplexTypes/master/json.png)

After parsing this document, we will be able to answer questions through Azure Search such as:

-	Find all people who work at the ‘Adventureworks Headquarters’
-	Get a count of the number of people who work in a ‘Home Office’.  
-	Of the people who at a ‘Home Office’ show what other offices they work along with a count of the people in each location.  
-	Show a count of people who work at a ‘Home Office’ with location Id of ‘4’.  
-	Search for people who work at a Home Office with location Id ‘4’. 

Here is the output from this application:
![Demo Screen Shot](https://raw.githubusercontent.com/liamca/AzureSearchComplexTypes/master/demo.png)

You can learn more about Azure Search [here](https://azure.microsoft.com/en-us/services/search/).  To register for a free Azure account, please see this [page](https://azure.microsoft.com/free/).  If you have any questions, please contact me on Twitter @liamca.
