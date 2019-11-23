---
name: Fitting Spark in my environment
route: /using_spark
menu: Deployment
---

There are essentially two ways in which you can fit Spark in your own environment. One is using Spark as-is, the other requires you to modify the storage layer.

## Using Spark as-is

In this mode, Spark handles everything FHIR for you, but it requires it's own storage of resources and the search index to do so. This means you have to feed the data that you want to serve as FHIR Resources into the Spark REST interface. So you create a copy of (part of) your data, held in the Spark MongoDB database. These are roughly the steps to follow:

* Define how the data in your own system(s) maps to FHIR resources. This is the logical mapping. 
* Create a piece of software that:
    * performs this logical mapping on actual data
    * uploads the result of it to Spark (with a FHIR Create operation on the POST `[base]/fhir/<resourcetype>` endpoint)
The FhirClient from the Hl7.Fhir API is very useful to program this.
* If you need periodic updates from your system to Spark, run the software from the previous step periodically by any means you see fit. It is advised that only updates since the previous run can be recognized in your data, and you feed them into Spark instead of reloading all the data every time.

## Using Spark directly against your own datastore

In this mode, Spark presents the FHIR REST interface to the outside world, but data retrieval (and eventually storage) is handled by an existing datastore. This requires quite some work in adapting Spark, because you have to replace Spark.Mongo with an implementation targeting your own datastore. Depending on the FHIR interactions that you intend to support, this may or may not be feasible. We think this is a valid option if you:

* provide read-only access
* to a limited number of Resource types
* supporting only a few search parameters

Please be aware that:

* A FHIR Resource has a server-assigned id, by which it can be retrieved again. Because a resource may span several datastructures in your own datastore, management of these id's is not always straightforward.
* A search must be translated to a (usually SQL-) query into your own datastore. Therefore, supporting many search parameters will probably require a lot of work.

You will have to do roughly these steps:

* Define how the data in your own system maps to FHIR resources. This is the logical mapping. 
* Implement the interfaces regarding storage (see [[Architecture](./architecture)]. Perform the actual mapping when retrieving the data for a resource from your system.
* Adjust the ConformanceBuilder, so the resulting ConformanceStatement states exactly what you support (which Resource types, which operations).
* Use the dependency injection framework to inject your implementations for the storage interfaces (in UnityConfig.cs).

