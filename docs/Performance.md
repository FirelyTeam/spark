# Performance

We have no performance figures of Spark yet. Spark is already being used in production scenarios, so it is fit for real use. If you have measured performance of Spark yourself, please share the results!

In the near future we will develop performance tests for Spark to get you an idea whether or how it will fit your performance needs. 

If you are concerned Spark will not handle your load as fast as you would like, consider the following possibilities for spreading the load.

* If you are FHIR-enabling multiple source systems, you could provide every system with it's own FHIR front-end, implemented by Spark. Instead of feeding data from all the source systems into one instance of Spark. You will however need a way to route requests to the correct instance of Spark. 

* If there is a logical attribute in your data to split the whole set into multiple sets, you could deploy Spark multiple times, each one on a 'shard' of the data. You will need a way to route requests to the correct instance of Spark, based on the chosen attribute.

* MongoDB supports sharding, as described in the [MongoDB documentation](https://docs.mongodb.com/manual/sharding/). You will have to choose a shard key based upon expected use. To use this in Spark, you will probably have to tweak the Spark Mongo implementation.
