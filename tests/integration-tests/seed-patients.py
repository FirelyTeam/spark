#!/usr/bin/env python3
"""
Seed a FHIR server with Patient resources.
Usage:
    python3 seed-patients.py
    python3 seed-patients.py --fhir-url http://spark:8080/fhir --count 50000
    python3 seed-patients.py --count 50000 --batch-size 200
"""

import argparse
import json
import sys
import time
import uuid
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

GENDERS = ["male", "female", "other", "unknown"]
FIRST_NAMES = ["James", "Mary", "Robert", "Patricia", "John", "Jennifer",
               "Michael", "Linda", "William", "Barbara", "David", "Susan"]
LAST_NAMES = ["Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia",
              "Miller", "Davis", "Wilson", "Taylor", "Anderson", "Thomas"]


def make_patient(index: int) -> dict:
    first = FIRST_NAMES[index % len(FIRST_NAMES)]
    last = LAST_NAMES[(index // len(FIRST_NAMES)) % len(LAST_NAMES)]
    year = 1940 + (index % 84)
    month = (index % 12) + 1
    day = (index % 28) + 1
    return {
        "resourceType": "Patient",
        "active": True,
        "name": [{"use": "official", "family": last, "given": [first]}],
        "gender": GENDERS[index % len(GENDERS)],
        "birthDate": f"{year:04d}-{month:02d}-{day:02d}",
    }


def make_transaction_bundle(patients: list) -> dict:
    return {
        "resourceType": "Bundle",
        "type": "transaction",
        "entry": [
            {
                "resource": patient,
                "request": {
                    "method": "POST",
                    "url": f"Patient/",
                },
            }
            for patient in patients
        ],
    }


def post_bundle(fhir_url: str, bundle: dict) -> int:
    body = json.dumps(bundle).encode("utf-8")
    req = Request(
        fhir_url,
        data=body,
        headers={"Content-Type": "application/fhir+json", "Accept": "application/fhir+json"},
        method="POST",
    )
    with urlopen(req, timeout=120) as resp:
        response_bundle = json.loads(resp.read())

    created = sum(
        1
        for entry in response_bundle.get("entry", [])
        if entry.get("response", {}).get("status", "").startswith(("200", "201"))
    )
    return created


def main():
    parser = argparse.ArgumentParser(description="Seed Spark with Patient resources (issue #948 reproduction)")
    parser.add_argument("--fhir-url", default="http://localhost:8080/fhir", help="FHIR base URL")
    parser.add_argument("--count", type=int, default=5_000, help="Total number of Patients to create")
    parser.add_argument("--batch-size", type=int, default=500, help="Patients per transaction bundle")
    args = parser.parse_args()

    base_url = args.fhir_url.rstrip("/")
    total = args.count
    batch_size = args.batch_size
    num_batches = (total + batch_size - 1) // batch_size

    print(f"Seeding {total:,} Patients to {base_url}")
    print(f"Batch size: {batch_size} → {num_batches} requests")
    print()

    seeded = 0
    errors = 0
    start = time.monotonic()

    for batch_index in range(num_batches):
        offset = batch_index * batch_size
        count_in_batch = min(batch_size, total - offset)
        patients = [make_patient(offset + i) for i in range(count_in_batch)]
        bundle = make_transaction_bundle(patients)

        try:
            created = post_bundle(base_url, bundle)
            seeded += created
        except HTTPError as exc:
            errors += 1
            print(f"\n  [ERROR] Batch {batch_index + 1}: HTTP {exc.code} — {exc.reason}", file=sys.stderr)
        except URLError as exc:
            errors += 1
            print(f"\n  [ERROR] Batch {batch_index + 1}: {exc.reason}", file=sys.stderr)

        elapsed = time.monotonic() - start
        pct = (offset + count_in_batch) / total * 100
        rate = seeded / elapsed if elapsed > 0 else 0
        eta = (total - seeded) / rate if rate > 0 else 0
        print(
            f"\r  {offset + count_in_batch:>10,} / {total:,}  ({pct:5.1f}%)  "
            f"{rate:6.0f} resources/s  ETA {eta:5.0f}s  Elapsed {elapsed:5.0f}s",
            end="",
            flush=True,
        )

    elapsed = time.monotonic() - start
    print(f"\n\nDone in {elapsed:.1f}s")
    print(f"  Seeded:  {seeded:,}")
    if errors:
        print(f"  Errors:  {errors} batch(es) failed", file=sys.stderr)
        sys.exit(1)

    print()
    print("Verify with:")
    print(f"  curl -s '{base_url}/Patient?_summary=count' | python3 -m json.tool")
    print(f"  curl -s '{base_url}/Patient?_count=0' | python3 -m json.tool")


if __name__ == "__main__":
    main()
