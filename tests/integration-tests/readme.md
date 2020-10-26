# Spark FHIR Integration Tests

## Setup

1. Build the latest Spark Docker image.
2. Run Spark service:

```
mkdir -p logs html_summaries
docker-compose up -d spark
```

## Running integration tests

### Listing all available tests

```
docker-compose run --rm --no-deps plan_executor ./list_all.sh r4
```

### Running particular test

```
docker-compose run --rm --no-deps plan_executor ./execute_test.sh http://spark.url/fhir r4 FormatTest
```

### Running all tests

```
docker-compose run --rm --no-deps plan_executor ./execute_all.sh http://spark.url/fhir r4
```

## Test results

Test results are stored in HTML format in `html_summaries` directory. 
Each test result is stored in a separate subdir.

## Test logs

Test logs can be found in `logs` directory.

