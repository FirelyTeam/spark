# Performance tests

Loads synthetic patients into Spark and runs Patient searches against it, so that the MongoDB and PostgreSQL
stores can be compared on the same data.

## What it does

`run-patients.sh` runs the whole thing for one store:

1. Starts the database in Docker, limited to 4 CPUs and 4 GB of memory.
2. Builds Spark.Web.R4 in Release and starts it on the host with the selected store.
3. Loads the patients with `k6/load-patients.js`: `COUNT` patients with `VUS` concurrent clients, each with an
   encounter and three vital sign observations (blood pressure with components, heart rate and body weight), so
   five resources per patient. `CLINICAL=false` loads patients only.
4. Records the load time, the database size and the number of resources.
5. Runs each search in `k6/search-patients.js` for 30 seconds with 10 concurrent clients.

The patients are deterministic (`k6/patients.js`): patient number `i` always gets the same name, gender, birth date,
identifier, phone and address. Every store therefore gets identical data, and the searches pick values that exist.

| Search | Query | Matches |
|---|---|---|
| `identifier` | `identifier=urn:oid:2.16.578.1.12.4.1.4.1\|<value>` | 1 |
| `birthdate` | `birthdate=<day>` | a few |
| `family` | `family=<first four letters>&_count=10` | thousands |
| `given_birthyear` | `given=<name>&birthdate=<year>&_count=10` | tens |
| `gender_count` | `gender=female&_summary=count` | half of the patients |
| `observation_code` | `Observation?code=http://loinc.org\|85354-9` | one per patient |
| `observation_value` | `Observation?code=…29463-7&value-quantity=gt<n>\|…\|kg` | a share of the patients |
| `observation_patient` | `Observation?subject:Patient.identifier=…` | 4 |
| `observation_chain` | `Observation?subject:Patient.family=<prefix>` | thousands |
| `encounter_date` | `Encounter?date=<month>` | a share of the encounters |

## Running

Requires Docker and the .NET SDK. Run from the root of the repository:

```sh
Tests/Performance/run-patients.sh postgres
Tests/Performance/run-patients.sh mongo

# Fewer patients for a quick check
COUNT=1000 Tests/Performance/run-patients.sh postgres

# Patients only, without the clinical resources
CLINICAL=false Tests/Performance/run-patients.sh postgres
```

Results are written to `Tests/Performance/results/<store>/`, with a `summary.txt` and the full k6 output of each step.

## Notes on a fair comparison

- A fresh `mongo:8` database has none of the indexes Spark needs, which makes MongoDB look far slower than it is. The
  script creates the indexes Spark ships with, plus `(@REFERENCE, @state)` and `(@typename, id, @state)`, without which
  every update scans the resources collection.
- Spark ships no indexes on the search fields of the MongoDB search index either, so every search scans the whole
  collection. The script adds one per search this test runs, which is what a tuned deployment would do. The PostgreSQL
  store indexes its search tables in its schema, so it needs nothing extra.
- Spark runs on the host, so the numbers depend on the machine. Compare the stores on the same machine rather than
  comparing absolute numbers between machines.
